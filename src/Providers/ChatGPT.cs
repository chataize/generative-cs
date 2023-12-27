using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Enums;
using GenerativeCS.Models;
using GenerativeCS.Utilities;

namespace GenerativeCS.Providers;

public class ChatGPT
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

    public int MaxAttempts { get; set; } = 5;

    public int? MessageLimit { get; set; }

    public int? CharacterLimit { get; set; }

    public bool IsTimeAware { get; set; }

    public Func<DateTime> TimeDelegate { get; set; } = () => DateTime.Now;

    public List<ChatFunction> Functions { get; set; } = [];

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await CompleteAsync(new ChatConversation(prompt), cancellationToken);
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
    {
        var request = CreateChatCompletionRequest(conversation);
        var response = await _client.RepeatPostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, cancellationToken, MaxAttempts);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);
        var generatedMessage = responseDocument.RootElement.GetProperty("choices")[0].GetProperty("message");

        if (generatedMessage.TryGetProperty("tool_calls", out var toolCallsElement))
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

                    var function = allFunctions.LastOrDefault(f => f.Name.Equals(functionName, StringComparison.InvariantCultureIgnoreCase));
                    if (function != null)
                    {
                        if (function.RequiresConfirmation && conversation.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionName)) % 2 != 0)
                        {
                            conversation.FromFunction(new FunctionResult(toolCallId, functionName, "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm."));
                        }
                        else
                        {
                            var functionResult = await FunctionInvoker.InvokeAsync(function.Operation!, argumentsElement, cancellationToken);
                            conversation.FromFunction(new FunctionResult(toolCallId, functionName, functionResult));
                        }
                    }
                    else
                    {
                        conversation.FromFunction(new FunctionResult(toolCallId, functionName, $"Function '{functionName}' was not found."));
                    }
                }
            }

            return await CompleteAsync(conversation, cancellationToken);
        }

        var messageContent = generatedMessage.GetProperty("content").GetString()!;
        conversation.FromAssistant(messageContent);

        return messageContent;
    }

    public async Task<List<float>> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new JsonObject
        {
            { "input", text },
            { "model", "text-embedding-ada-002" }
        };

        var response = await _client.RepeatPostAsJsonAsync("https://api.openai.com/v1/embeddings", request, cancellationToken, MaxAttempts);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);
        var embedding = new List<float>();

        foreach (var element in responseDocument.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray())
        {
            embedding.Add(element.GetSingle());
        }

        return embedding;
    }

    public void AddFunction(ChatFunction function)
    {
        Functions.Add(function);
    }

    public void AddFunction(Delegate operation)
    {
        Functions.Add(new ChatFunction(operation));
    }

    public void AddFunction(string name, Delegate operation)
    {
        Functions.Add(new ChatFunction(name, operation));
    }

    public void AddFunction(string name, string? description, Delegate operation)
    {
        Functions.Add(new ChatFunction(name, description, operation));
    }

    public void AddFunction(string name, bool requiresConfirmation, Delegate operation)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, operation));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, Delegate operation)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, operation));
    }

    public bool RemoveFunction(ChatFunction function)
    {
        return Functions.Remove(function);
    }

    public bool RemoveFunction(string name)
    {
        var function = Functions.LastOrDefault(f => f.Name == name);
        if (function == null)
        {
            return false;
        }

        return Functions.Remove(function);
    }

    public bool RemoveFunction(Delegate operation)
    {
        var function = Functions.LastOrDefault(f => f.Operation == operation);
        if (function == null)
        {
            return false;
        }

        return Functions.Remove(function);
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
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid role")
        };
    }

    private JsonObject CreateChatCompletionRequest(ChatConversation conversation)
    {
        var messages = conversation.Messages.ToList();
        if (IsTimeAware)
        {
            MessageTools.AddTimeInformation(messages, TimeDelegate());
        }

        MessageTools.LimitTokens(messages, MessageLimit, CharacterLimit);

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
