using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Enums;
using GenerativeCS.Interfaces;
using GenerativeCS.Models;
using GenerativeCS.Utilities;

namespace GenerativeCS.CompletionProviders;

public class ChatGPT<TConversation, TMessage, TFunction> : ICompletionProvider<TConversation, TMessage, TFunction>
    where TConversation : IChatConversation<TMessage, TFunction>, new()
    where TMessage : IChatMessage, new()
    where TFunction : IChatFunction, new()
{
    private readonly HttpClient _client = new();

    public ChatGPT() { }

    [SetsRequiredMembers]
    public ChatGPT(string apiKey, string model = "gpt-3.5-turbo")
    {
        ApiKey = apiKey;
        Model = model;
    }

    public required string ApiKey
    {
        get => _client.DefaultRequestHeaders.Authorization?.Parameter!;
        set => _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", value);
    }

    public string Model { get; set; } = "gpt-3.5-turbo";

    public int? MessageLimit { get; set; }

    public int? CharacterLimit { get; set; }

    public ICollection<TFunction> Functions { get; set; } = new List<TFunction>();

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var conversation = new TConversation();
        conversation.FromSystem(prompt);

        return await CompleteAsync(conversation, cancellationToken);
    }

    public async Task<string> CompleteAsync(TConversation conversation, CancellationToken cancellationToken = default)
    {
        var request = CreateChatCompletionRequest(conversation);
        var response = await _client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var message = document.RootElement.GetProperty("choices")[0].GetProperty("message");

        if (message.TryGetProperty("tool_calls", out var toolCallsElement))
        {
            var allFunctions = Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();
            foreach (var toolCallElement in toolCallsElement.EnumerateArray())
            {
                if (toolCallElement.GetProperty("type").GetString() == "function")
                {
                    var toolCallId = toolCallElement.GetProperty("id").GetString()!;
                    var functionElement = toolCallElement.GetProperty("function");
                    var functionName = functionElement.GetProperty("name").GetString()!;
                    var argumentsElement = functionElement.GetProperty("arguments");

                    argumentsElement = JsonDocument.Parse(argumentsElement.GetString()!).RootElement;
                    conversation.FromAssistant(new FunctionCall(toolCallId, functionName, argumentsElement));

                    var function = allFunctions.LastOrDefault(f => f.Name == functionName);
                    if (function != null)
                    {
                        var functionResult = await FunctionInvoker.InvokeAsync(function.Function!, argumentsElement, cancellationToken);
                        conversation.FromFunction(new FunctionResult(toolCallId, functionName, functionResult));
                    }
                    else
                    {
                        conversation.FromFunction(new FunctionResult(toolCallId, functionName, $"Function '{functionName}' was not found."));
                    }
                }
            }

            return await CompleteAsync(conversation, cancellationToken);
        }

        var text = message.GetProperty("content").GetString()!;
        conversation.FromAssistant(text);

        return text!;
    }

    public void AddFunction(TFunction function)
    {
        Functions.Add(function);
    }

    public void AddFunction(Delegate function)
    {
        var chatFunction = new TFunction
        {
            Name = function.Method.Name,
            Function = function
        };

        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, Delegate function)
    {
        var chatFunction = new TFunction
        {
            Name = name,
            Function = function
        };

        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, string? description, Delegate function)
    {
        var chatFunction = new TFunction
        {
            Name = name,
            Description = description,
            Function = function
        };

        Functions.Add(chatFunction);
    }

    public void RemoveFunction(TFunction function)
    {
        Functions.Remove(function);
    }

    public void RemoveFunction(string name)
    {
        var functionToRemove = Functions.LastOrDefault(f => f.Name == name);
        if (functionToRemove != null)
        {
            Functions.Remove(functionToRemove);
        }
    }

    public void RemoveFunction(Delegate function)
    {
        var functionToRemove = Functions.LastOrDefault(f => f.Function == function);
        if (functionToRemove != null)
        {
            Functions.Remove(functionToRemove);
        }
    }

    public void ClearFunctions()
    {
        Functions.Clear();
    }

    private static string GetRoleName(ChatRole role)
    {
        return role switch
        {
            ChatRole.System => "system",
            ChatRole.User => "user",
            ChatRole.Assistant => "assistant",
            ChatRole.Function => "tool",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    private JsonObject CreateChatCompletionRequest(TConversation conversation)
    {
        var messages = new List<TMessage>(conversation.Messages);
        TokenLimiter.LimitTokens(messages, MessageLimit, CharacterLimit);

        var messagesArray = new JsonArray();
        foreach (var message in messages)
        {
            var messageObject = new JsonObject
            {
                { "role", GetRoleName(message.Role) }
            };

            if (message.Author != null)
            {
                messageObject.Add("name", message.Author);
            }

            if (message.Content != null)
            {
                messageObject.Add("content", message.Content);
            }

            var toolCallsArray = new JsonArray();
            foreach (var functionCall in message.FunctionCalls)
            {
                var functionObject = new JsonObject
                {
                    { "name", functionCall.Name },
                    { "arguments", JsonSerializer.Serialize(functionCall.Arguments) }
                };

                var toolCallObject = new JsonObject
                {
                    { "id", functionCall.Id },
                    { "type", "function" },
                    { "function", functionObject }
                };

                toolCallsArray.Add(toolCallObject);
            }

            if (toolCallsArray.Count > 0)
            {
                messageObject.Add("tool_calls", toolCallsArray);
            }

            if (message.FunctionResult != null)
            {
                messageObject.Add("tool_call_id", message.FunctionResult.Id);
                messageObject.Add("content", JsonSerializer.Serialize(message.FunctionResult.Result));
            }

            messagesArray.Add(messageObject);
        }

        var requestObject = new JsonObject
        {
            { "model", Model },
            { "messages", messagesArray }
        };

        var allFunctions = Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();
        if (allFunctions.Count > 0)
        {
            var toolsArray = new JsonArray();
            foreach (var function in allFunctions)
            {
                var functionObject = FunctionSerializer.SerializeFunction(function);
                var toolObject = new JsonObject
                {
                    { "type", "function" },
                    { "function", functionObject }
                };

                toolsArray.Add(toolObject);
            }

            requestObject.Add("tools", toolsArray);
        }

        return requestObject;
    }
}

public class ChatGPT : ChatGPT<ChatConversation, ChatMessage, ChatFunction>
{
    public ChatGPT() { }

    [SetsRequiredMembers]
    public ChatGPT(string apiKey, string model = "gpt-3.5-turbo") : base(apiKey, model) { }
}
