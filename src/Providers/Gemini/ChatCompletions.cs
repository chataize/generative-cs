using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Enums;
using GenerativeCS.Models;
using GenerativeCS.Options.Gemini;
using GenerativeCS.Utilities;

namespace GenerativeCS.Providers.Gemini;

internal static class ChatCompletions
{
    internal static async Task<string> CompleteAsync(string prompt, string apiKey, HttpClient? httpClient = null, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        httpClient ??= new();
        options ??= new();

        if (options.Functions.Count >= 1)
        {
            return await CompleteAsync(new ChatConversation(prompt), apiKey, httpClient, options, cancellationToken);
        }

        var request = CreateCompletionRequest(prompt);

        using var response = await httpClient.RepeatPostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{options.Model}:generateContent?key={apiKey}", request, cancellationToken, options.MaxAttempts);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        var generatedMessage = responseDocument.RootElement.GetProperty("candidates")[0];
        var messageContent = generatedMessage.GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;

        return messageContent;
    }

    internal static async Task<string> CompleteAsync(ChatConversation conversation, string apiKey, HttpClient? httpClient = null, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        httpClient ??= new();
        options ??= new();

        var request = CreateChatCompletionRequest(conversation, options);

        using var response = await httpClient.RepeatPostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{options.Model}:generateContent?key={apiKey}", request, cancellationToken, options.MaxAttempts);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);

        var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);
        var responseParts = responseDocument.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");
        var allFunctions = options.Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();

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
                            var functionResult = await options.DefaultFunctionCallback(functionName, argumentsElement, cancellationToken);
                            conversation.FromFunction(new FunctionResult(functionName, functionResult));
                        }
                    }
                }
                else
                {
                    conversation.FromFunction(new FunctionResult(functionName, $"Function '{functionName}' was not found."));
                }

                return await CompleteAsync(conversation, apiKey, httpClient, options, cancellationToken);
            }
            else if (part.TryGetProperty("text", out var textElement))
            {
                messageContent = textElement.GetString()!;
                conversation.FromAssistant(messageContent);
            }
            else
            {
                conversation.FromFunction(new FunctionResult("Error", "Either call a function or respond with text."));
                return await CompleteAsync(conversation, apiKey, httpClient, options, cancellationToken);
            }
        }

        return messageContent;
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

    private static JsonObject CreateChatCompletionRequest(ChatConversation conversation, ChatCompletionOptions options)
    {
        var messages = conversation.Messages.ToList();
        if (options.IsTimeAware)
        {
            MessageTools.AddTimeInformation(messages, options.TimeCallback());
        }

        MessageTools.LimitTokens(messages, options.MessageLimit, options.CharacterLimit);
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

        var allFunctions = options.Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();
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
}
