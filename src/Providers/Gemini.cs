using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Enums;
using GenerativeCS.Models;
using GenerativeCS.Utilities;

namespace GenerativeCS.Providers;

public class Gemini
{
    private readonly HttpClient _client = new();

    public Gemini() { }

    [SetsRequiredMembers]
    public Gemini(string apiKey, string model = "gemini-pro")
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

    public Func<DateTime> TimeDelegate { get; set; } = () => DateTime.Now;

    public List<ChatFunction> Functions { get; set; } = [];

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var request = CreateCompletionRequest(prompt);
        var response = await _client.RepeatPostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}", request, cancellationToken, MaxAttempts);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var message = document.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;

        return message;
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, CancellationToken cancellationToken = default)
    {
        var request = CreateChatCompletionRequest(conversation);
        var response = await _client.RepeatPostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={ApiKey}", request, cancellationToken, MaxAttempts);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var parts = document.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
        var allFunctions = Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();

        string text = null!;
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("functionCall", out var functionCall) && functionCall.TryGetProperty("name", out var functionNameProperty))
            {
                var functionName = functionNameProperty.GetString()!;
                var argumentsElement = functionCall.GetProperty("args");

                conversation.FromAssistant(new FunctionCall(functionName, argumentsElement));

                var function = allFunctions.LastOrDefault(f => f.Name!.Equals(functionName, StringComparison.InvariantCultureIgnoreCase));
                if (function != null)
                {
                    if (function.RequiresConfirmation && conversation.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionName)) % 2 != 0)
                    {
                        conversation.FromFunction(new FunctionResult(functionName, "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm."));
                    }
                    else
                    {
                        var functionResult = await FunctionInvoker.InvokeAsync(function.Operation!, argumentsElement, cancellationToken);
                        conversation.FromFunction(new FunctionResult(functionName, functionResult));
                    }
                }
                else
                {
                    conversation.FromFunction(new FunctionResult(functionName, $"Function '{functionName}' not found."));
                }

                return await CompleteAsync(conversation, cancellationToken);
            }
            else if (part.TryGetProperty("text", out var textProperty))
            {
                text = textProperty.GetString()!;
                conversation.FromAssistant(text);
            }
            else
            {
                conversation.FromFunction(new FunctionResult("Error", "Either call a function or respond with text."));
                return await CompleteAsync(conversation, cancellationToken);
            }
        }

        return text;
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
            ChatRole.Assistant => "model",
            ChatRole.Function => "function",
            _ => "user"
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
            MessageTools.AddTimeInformation(messages, TimeDelegate());
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
