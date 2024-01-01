using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Enums;
using GenerativeCS.Models;
using GenerativeCS.Options.OpenAI;
using GenerativeCS.Utilities;

namespace GenerativeCS.Services.OpenAI;

internal static class ChatCompletions
{
    internal static async Task<string> CompleteAsync(ChatConversation conversation, string apiKey, HttpClient? httpClient = null, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        httpClient ??= new HttpClient();
        options ??= new ChatCompletionOptions();

        var request = CreateChatCompletionRequest(conversation, options);

        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, cancellationToken, options.MaxAttempts);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        var generatedMessage = responseDocument.RootElement.GetProperty("choices")[0].GetProperty("message");
        if (generatedMessage.TryGetProperty("tool_calls", out var toolCallsElement))
        {
            var allFunctions = options.Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();
            foreach (var toolCallElement in toolCallsElement.EnumerateArray())
            {
                if (toolCallElement.GetProperty("type").GetString() == "function")
                {
                    var toolCallId = toolCallElement.GetProperty("id").GetString()!;
                    var functionElement = toolCallElement.GetProperty("function");
                    var functionName = functionElement.GetProperty("name").GetString()!;
                    var rawArgumentsElement = functionElement.GetProperty("arguments");
                    var argumentsDocument = JsonDocument.Parse(rawArgumentsElement.GetString()!);
                    var argumentsElement = argumentsDocument.RootElement;

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
                                var functionResult = await options.DefaultFunctionCallback(functionName, argumentsElement, cancellationToken);
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

            return await CompleteAsync(conversation, apiKey, httpClient, options, cancellationToken);
        }

        var messageContent = generatedMessage.GetProperty("content").GetString()!;
        conversation.FromAssistant(messageContent);

        return messageContent;
    }

    internal static async IAsyncEnumerable<string> CompleteAsStreamAsync(ChatConversation conversation, string apiKey, HttpClient? httpClient, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        httpClient ??= new HttpClient();
        options ??= new ChatCompletionOptions();

        var request = CreateChatCompletionRequest(conversation, options);
        request.Add("stream", true);

        using var response = await httpClient.RepeatPostAsJsonForStreamAsync("https://api.openai.com/v1/chat/completions", request, cancellationToken, options.MaxAttempts);
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
                            var argumentsDocument = JsonDocument.Parse(currentFunctionArguments);
                            functionCalls.Add(new FunctionCall(currentToolCallId, currentFunctionName, argumentsDocument.RootElement));
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
            var argumentsDocument = JsonDocument.Parse(currentFunctionArguments);
            functionCalls.Add(new FunctionCall(currentToolCallId, currentFunctionName, argumentsDocument.RootElement));
        }

        if (functionCalls.Count > 0)
        {
            conversation.FromAssistant(functionCalls);
        }

        var allFunctions = options.Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();
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
                        var functionResult = await options.DefaultFunctionCallback(functionCall.Name, functionCall.Arguments, cancellationToken);
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
            await foreach (var chunk in CompleteAsStreamAsync(conversation, apiKey, httpClient, options, cancellationToken))
            {
                yield return chunk;
            }
        }
    }

    private static JsonObject CreateChatCompletionRequest(ChatConversation conversation, ChatCompletionOptions options)
    {
        var messages = conversation.Messages.ToList();
        if (options.IsTimeAware)
        {
            MessageTools.AddTimeInformation(messages, options.TimeCallback());
        }

        MessageTools.LimitTokens(messages, options.MessageLimit, options.CharacterLimit);

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
            { "model", options.Model },
            { "messages", messagesArray }
        };

        if (conversation.User != null)
        {
            requestObject.Add("user", conversation.User);
        }
        else if (options.User != null)
        {
            requestObject.Add("user", options.User);
        }

        if (options.MaxOutputTokens.HasValue)
        {
            requestObject.Add("max_tokens", options.MaxOutputTokens.Value);
        }

        if (options.Seed.HasValue)
        {
            requestObject.Add("seed", options.Seed.Value);
        }

        if (options.Temperature.HasValue)
        {
            requestObject.Add("temperature", options.Temperature.Value);
        }

        if (options.TopP.HasValue)
        {
            requestObject.Add("top_p", options.TopP.Value);
        }

        if (options.FrequencyPenalty.HasValue)
        {
            requestObject.Add("frequency_penalty", options.FrequencyPenalty.Value);
        }

        if (options.PresencePenalty.HasValue)
        {
            requestObject.Add("presence_penalty", options.PresencePenalty.Value);
        }

        if (options.IsJsonMode)
        {
            var responseFormatObject = new JsonObject
            {
               { "type", "json_object" }
            };

            requestObject.Add("response_format", responseFormatObject);
        }

        if (options.StopWords != null && options.StopWords.Count > 0)
        {
            var stopArray = new JsonArray();
            foreach (var stop in options.StopWords)
            {
                stopArray.Add(stop);
            }

            requestObject.Add("stop", stopArray);
        }


        var allFunctions = options.Functions.Concat(conversation.Functions).GroupBy(f => f.Name).Select(g => g.Last()).ToList();
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
}
