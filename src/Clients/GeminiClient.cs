using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Enums;
using GenerativeCS.Models;
using GenerativeCS.Utilities;

namespace GenerativeCS.Clients;

public class GeminiClient
{
    private readonly HttpClient _client = new();

    public GeminiClient() { }

    [SetsRequiredMembers]
    public GeminiClient(string apiKey, string model = "gemini-pro")
    {
        ApiKey = apiKey;
        Model = model;
    }

    public required string ApiKey { get; set; }

    public string Model { get; set; } = "gemini-pro";

    public int MaxAttempts { get; set; } = 5;

    public int? MessageLimit { get; set; }

    public int? CharacterLimit { get; set; }

    public bool IsTimeAware { get; set; }

    public Func<DateTime> TimeCallback { get; set; } = () => DateTime.Now;

    public Func<string, JsonElement, CancellationToken, Task<object?>> DefaultFunctionCallback { get; set; } = (_, _, _) => throw new NotImplementedException("Function callback has not been implemented.");

    public List<ChatFunction> Functions { get; set; } = [];

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (Functions.Count >= 1)
        {
            return await CompleteAsync(new ChatConversation(prompt), cancellationToken);
        }

        var request = CreateCompletionRequest(prompt);

        using var response = await _client.RepeatPostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}", request, cancellationToken, MaxAttempts);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        var generatedMessage = responseDocument.RootElement.GetProperty("candidates")[0];
        var messageContent = generatedMessage.GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;

        return messageContent;
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
    {
        var request = CreateChatCompletionRequest(conversation);

        using var response = await _client.RepeatPostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}", request, cancellationToken, MaxAttempts);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        var responseParts = responseDocument.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
        var allFunctions = Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();

        string messageContent = null!;
        foreach (var part in responseParts.EnumerateArray())
        {
            if (part.TryGetProperty("functionCall", out var functionCallElement) && functionCallElement.TryGetProperty("name", out var functionNameElement))
            {
                var functionName = functionNameElement.GetString()!;
                var argumentsElement = functionCallElement.GetProperty("args");

                conversation.FromAssistant(new FunctionCall(functionName, argumentsElement));

                var function = allFunctions.LastOrDefault(f => f.Name.Equals(functionName, StringComparison.InvariantCultureIgnoreCase));
                if (function != null)
                {
                    if (function.RequiresConfirmation && conversation.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionName)) % 2 != 0)
                    {
                        conversation.FromFunction(new FunctionResult(functionName, "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm."));
                    }
                    else
                    {
                        if (function.Callback != null)
                        {
                            var functionResult = await FunctionInvoker.InvokeAsync(function.Callback, argumentsElement, cancellationToken);
                            conversation.FromFunction(new FunctionResult(functionName, functionResult));
                        }
                        else
                        {
                            var functionResult = await DefaultFunctionCallback(functionName, argumentsElement, cancellationToken);
                            conversation.FromFunction(new FunctionResult(functionName, functionResult));
                        }
                    }
                }
                else
                {
                    conversation.FromFunction(new FunctionResult(functionName, $"Function '{functionName}' was not found."));
                }

                return await CompleteAsync(conversation, cancellationToken);
            }
            else if (part.TryGetProperty("text", out var textElement))
            {
                messageContent = textElement.GetString()!;
                conversation.FromAssistant(messageContent);
            }
            else
            {
                conversation.FromFunction(new FunctionResult("Error", "Either call a function or respond with text."));
                return await CompleteAsync(conversation, cancellationToken);
            }
        }

        return messageContent;
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
            ChatRole.System => "user",
            ChatRole.User => "user",
            ChatRole.Assistant => "model",
            ChatRole.Function => "tool",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid role")
        };
    }

    private static JsonObject CreateCompletionRequest(string prompt)
    {
        var partObject = new JsonObject
        {
            { "text", prompt }
        };

        var partsArray = new JsonArray
        {
            partObject
        };

        var contentObject = new JsonObject
        {
            { "parts", partsArray}
        };

        var contentsArray = new JsonArray
        {
            contentObject
        };

        var requestObject = new JsonObject
        {
            { "contents", contentsArray }
        };

        return requestObject;
    }

    private JsonObject CreateChatCompletionRequest(ChatConversation conversation)
    {
        var messages = conversation.Messages.ToList();
        if (IsTimeAware)
        {
            MessageTools.AddTimeInformation(messages, TimeCallback());
        }

        MessageTools.LimitTokens(messages, MessageLimit, CharacterLimit);
        MessageTools.ReplaceSystemRole(messages);
        MessageTools.MergeMessages(messages);

        var contentsArray = new JsonArray();
        foreach (var message in messages)
        {
            var partObject = new JsonObject();
            var functionCall = message.FunctionCalls.FirstOrDefault();

            if (functionCall != null)
            {
                var functionCallObject = new JsonObject
                {
                    { "name", functionCall.Name },
                    { "args", JsonObject.Create(functionCall.Arguments) }
                };

                partObject.Add("functionCall", functionCallObject);
            }
            else if (message.FunctionResult != null)
            {
                var responseObject = new JsonObject
                {
                    { "name", message.FunctionResult.Name },
                    { "content", JsonSerializer.SerializeToNode(message.FunctionResult.Result) }
                };

                var functionResponseObject = new JsonObject
                {
                    { "name", message.FunctionResult.Name },
                    { "response", responseObject }
                };

                partObject.Add("functionResponse", functionResponseObject);
            }
            else
            {
                partObject.Add("text", message.Content);
            }

            var partsArray = new JsonArray
            {
                partObject
            };

            var contentObject = new JsonObject
            {
                { "role", GetRoleName(message.Role) },
                { "parts", partsArray }
            };

            contentsArray.Add(contentObject);
        }

        var requestObject = new JsonObject
        {
            { "contents", contentsArray },
        };

        var allFunctions = Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();
        if (allFunctions.Count > 0)
        {
            var functionsArray = new JsonArray();
            foreach (var function in allFunctions)
            {
                functionsArray.Add(FunctionSerializer.SerializeFunction(function));
            }

            var functionsObject = new JsonObject
            {
                { "function_declarations", functionsArray }
            };

            var toolsArray = new JsonArray
            {
                functionsObject
            };

            requestObject.Add("tools", toolsArray);
        }

        return requestObject;
    }
}