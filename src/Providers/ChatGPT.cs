using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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

    public string? User { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public int? MaxOutputTokens { get; set; }

    public int? MessageLimit { get; set; }

    public int? CharacterLimit { get; set; }

    public int? Seed { get; set; }

    public double? Temperature { get; set; }

    public double? TopP { get; set; }

    public double? FrequencyPenalty { get; set; }

    public double? PresencePenalty { get; set; }

    public bool IsJsonMode { get; set; }

    public bool IsTimeAware { get; set; }

    public List<string> StopWords { get; set; } = [];

    public List<ChatFunction> Functions { get; set; } = [];

    public Func<string, JsonElement, CancellationToken, Task<object?>> DefaultFunctionCallback { get; set; } = (_, _, _) => throw new NotImplementedException("Function callback has not been implemented.");

    public Func<DateTime> TimeCallback { get; set; } = () => DateTime.Now;

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await CompleteAsync(new ChatConversation(prompt), cancellationToken);
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
    {
        var request = CreateChatCompletionRequest(conversation);
        var response = await _client.RepeatPostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, cancellationToken, MaxAttempts);

        _ = response.EnsureSuccessStatusCode();

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
                            if (function.Callback != null)
                            {
                                var functionResult = await FunctionInvoker.InvokeAsync(function.Callback, argumentsElement, cancellationToken);
                                conversation.FromFunction(new FunctionResult(toolCallId, functionName, functionResult));
                            }
                            else
                            {
                                var functionResult = await DefaultFunctionCallback(functionName, argumentsElement, cancellationToken);
                                conversation.FromFunction(new FunctionResult(toolCallId, functionName, functionResult));
                            }
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

    public async IAsyncEnumerable<string> StreamCompletionAsync(ChatConversation conversation, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = CreateChatCompletionRequest(conversation);
        request.Add("stream", true);

        using var response = await _client.RepeatPostAsJsonForStreamAsync("https://api.openai.com/v1/chat/completions", request, cancellationToken, MaxAttempts);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseReader = new StreamReader(responseContent);

        var functionCalls = new List<FunctionCall>();
        var currentToolCallId = string.Empty;
        var currentFunctionName = string.Empty;
        var currentFunctionArguments = string.Empty;
        var entireContent = string.Empty;

        while (!responseReader.EndOfStream)
        {
            var chunk = await responseReader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(chunk))
            {
                continue;
            }

            var chunkParts = chunk.Split("data: ");
            if (chunkParts.Length == 1)
            {
                continue;
            }

            var chunkData = chunkParts[1];
            if (chunkData == "[DONE]")
            {
                break;
            }

            using var chunkDocument = JsonDocument.Parse(chunkData);

            var choice = chunkDocument.RootElement.GetProperty("choices")[0];
            if (choice.TryGetProperty("finish_reason", out var finishReasonProperty) && finishReasonProperty.ValueKind != JsonValueKind.Null)
            {
                break;
            }

            var delta = choice.GetProperty("delta");

            if (delta.TryGetProperty("content", out var contentProperty) && contentProperty.ValueKind != JsonValueKind.Null)
            {
                var content = contentProperty.GetString()!;
                entireContent += content;

                yield return content;
            }

            if (delta.TryGetProperty("tool_calls", out var toolCallsProperty))
            {
                var toolCallProperty = toolCallsProperty[0];
                if (toolCallProperty.TryGetProperty("function", out var functionProperty))
                {
                    if (functionProperty.TryGetProperty("name", out var functionNameProperty))
                    {
                        if (!string.IsNullOrWhiteSpace(currentFunctionName))
                        {
                            functionCalls.Add(new FunctionCall(currentToolCallId, currentFunctionName, JsonDocument.Parse(currentFunctionArguments).RootElement));
                        }

                        currentToolCallId = toolCallProperty.GetProperty("id").GetString()!;
                        currentFunctionName = functionNameProperty.GetString()!;
                        currentFunctionArguments = string.Empty;
                    }

                    if (functionProperty.TryGetProperty("arguments", out var functionArgumentsProperty))
                    {
                        currentFunctionArguments += functionArgumentsProperty.GetString()!;
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(currentFunctionName))
        {
            functionCalls.Add(new FunctionCall(currentToolCallId, currentFunctionName, JsonDocument.Parse(currentFunctionArguments).RootElement));
        }

        if (functionCalls.Count > 0)
        {
            conversation.FromAssistant(functionCalls);
        }

        var allFunctions = Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();
        foreach (var functionCall in functionCalls)
        {
            var function = allFunctions.LastOrDefault(f => f.Name.Equals(functionCall.Name, StringComparison.InvariantCultureIgnoreCase));
            if (function != null)
            {
                if (function.RequiresConfirmation && conversation.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionCall.Name)) % 2 != 0)
                {
                    conversation.FromFunction(new FunctionResult(functionCall.Id!, functionCall.Name, "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm."));
                }
                else
                {
                    if (function.Callback != null)
                    {
                        var functionResult = await FunctionInvoker.InvokeAsync(function.Callback, functionCall.Arguments, cancellationToken);
                        conversation.FromFunction(new FunctionResult(functionCall.Id!, functionCall.Name, functionResult));
                    }
                    else
                    {
                        var functionResult = await DefaultFunctionCallback(functionCall.Name, functionCall.Arguments, cancellationToken);
                        conversation.FromFunction(new FunctionResult(functionCall.Id!, functionCall.Name, functionResult));
                    }
                }
            }
            else
            {
                conversation.FromFunction(new FunctionResult(functionCall.Id!, functionCall.Name, $"Function '{functionCall.Name}' was not found."));
            }
        }

        if (!string.IsNullOrWhiteSpace(entireContent))
        {
            conversation.FromAssistant(entireContent);
        }

        if (functionCalls.Count > 0)
        {
            await foreach (var chunk in StreamCompletionAsync(conversation, cancellationToken))
            {
                yield return chunk;
            }
        }
    }

    public async Task<List<float>> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new JsonObject
        {
            { "input", text },
            { "model", "text-embedding-ada-002" }
        };

        var response = await _client.RepeatPostAsJsonAsync("https://api.openai.com/v1/embeddings", request, cancellationToken, MaxAttempts);
        _ = response.EnsureSuccessStatusCode();

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

    public void AddFunction(string name, bool requiresConfirmation = false)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation = false)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation));
    }

    public void AddFunction(Delegate callback)
    {
        Functions.Add(new ChatFunction(callback));
    }

    public void AddFunction(string name, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, callback));
    }

    public void AddFunction(string name, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, string? description, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, callback));
    }

    public void AddFunction(string name, string? description, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, string? description, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, bool requiresConfirmation, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, callback));
    }

    public void AddFunction(string name, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, callback));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, parameters));
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

    public bool RemoveFunction(Delegate callback)
    {
        var function = Functions.LastOrDefault(f => f.Callback == callback);
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
            MessageTools.AddTimeInformation(messages, TimeCallback());
        }

        MessageTools.LimitTokens(messages, MessageLimit, CharacterLimit);

        var messagesArray = new JsonArray();
        foreach (var message in messages)
        {
            var messageObject = new JsonObject
            {
                { "role", GetRoleName(message.Role) }
            };

            if (message.Name != null)
            {
                messageObject.Add("name", message.Name);
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

        if (conversation.User != null)
        {
            requestObject.Add("user", conversation.User);
        }
        else if (User != null)
        {
            requestObject.Add("user", User);
        }

        if (MaxOutputTokens.HasValue)
        {
            requestObject.Add("max_tokens", MaxOutputTokens.Value);
        }

        if (Seed.HasValue)
        {
            requestObject.Add("seed", Seed.Value);
        }

        if (Temperature.HasValue)
        {
            requestObject.Add("temperature", Temperature.Value);
        }

        if (TopP.HasValue)
        {
            requestObject.Add("top_p", TopP.Value);
        }

        if (FrequencyPenalty.HasValue)
        {
            requestObject.Add("frequency_penalty", FrequencyPenalty.Value);
        }

        if (PresencePenalty.HasValue)
        {
            requestObject.Add("presence_penalty", PresencePenalty.Value);
        }

        if (IsJsonMode)
        {
            var responseFormatObject = new JsonObject
            {
               { "type", "json_object" }
            };

            requestObject.Add("response_format", responseFormatObject);
        }

        if (StopWords != null && StopWords.Count > 0)
        {
            var stopArray = new JsonArray();
            foreach (var stop in StopWords)
            {
                stopArray.Add(stop);
            }

            requestObject.Add("stop", stopArray);
        }


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
