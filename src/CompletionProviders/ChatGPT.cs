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

    public ICollection<TFunction> Functions { get; set; } = new List<TFunction>();

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var conversation = new TConversation();
        conversation.FromSystem(prompt);

        return await CompleteAsync(conversation, cancellationToken);
    }

    public async Task<string> CompleteAsync(TConversation conversation, CancellationToken cancellationToken = default)
    {
        var messagesArray = new JsonArray();
        foreach (var conversationMessage in conversation.Messages)
        {
            var messageObject = new JsonObject
            {
                { "role", GetRoleName(conversationMessage.Role) }
            };

            if (conversationMessage.Author != null)
            {
                messageObject.Add("name", conversationMessage.Author);
            }

            if (conversationMessage.Content != null)
            {
                messageObject.Add("content", conversationMessage.Content);
            }

            var toolCallsArray = new JsonArray();
            foreach (var functionCall in conversationMessage.FunctionCalls)
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

            if (conversationMessage.FunctionResult != null)
            {
                messageObject.Add("tool_call_id", conversationMessage.FunctionResult.Id);
                messageObject.Add("content", JsonSerializer.Serialize(conversationMessage.FunctionResult.Result));
            }

            messagesArray.Add(messageObject);
        }

        var requestObject = new JsonObject
        {
            { "model", Model },
            { "messages", messagesArray }
        };

        var allFunctions = Functions.Concat(conversation.Functions).ToList();
        if (allFunctions.Count > 0)
        {
            var toolsArray = new JsonArray();
            foreach (var function in allFunctions)
            {
                var functionObject = FunctionSerializer.Serialize(function);
                var toolObject = new JsonObject
                {
                    { "type", "function" },
                    { "function", functionObject }
                };

                toolsArray.Add(toolObject);
            }

            requestObject.Add("tools", toolsArray);
        }

        var response = await _client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestObject, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var message = document.RootElement.GetProperty("choices")[0].GetProperty("message");

        string? lastText = null;
        if (message.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String)
        {
            lastText = contentElement.GetString()!;
            conversation.FromAssistant(lastText);
        }

        if (message.TryGetProperty("tool_calls", out var toolCallsElement))
        {
            var anyFunctionCalled = false;
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

                    var function = allFunctions.Last(f => f.Name == functionName);
                    var functionResult = await FunctionInvoker.InvokeAsync(function.Function!, argumentsElement, cancellationToken);

                    conversation.FromFunction(new FunctionResult(toolCallId, functionName, functionResult));
                    anyFunctionCalled = true;
                }
            }

            if (anyFunctionCalled)
            {
                return await CompleteAsync(conversation, cancellationToken);
            }
        }

        return lastText!;
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
}

public class ChatGPT : ChatGPT<ChatConversation, ChatMessage, ChatFunction>
{
    public ChatGPT() { }

    [SetsRequiredMembers]
    public ChatGPT(string apiKey, string model = "gpt-3.5-turbo") : base(apiKey, model) { }
}
