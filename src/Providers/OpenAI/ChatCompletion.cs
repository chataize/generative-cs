using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Interfaces;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.OpenAI;

internal static class ChatCompletion
{
    internal static async Task<string> CompleteAsync<T>(IChatConversation<T> conversation, string apiKey, ChatCompletionOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default) where T : IChatMessage, new()
    {
        options ??= new();
        httpClient ??= new();

        var request = CreateChatCompletionRequest(conversation, options);
        if (options.IsDebugMode)
        {
            Debug.WriteLine(request.ToString());
        }

        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, apiKey, options.MaxAttempts, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (options.IsDebugMode)
        {
            Debug.WriteLine(responseDocument.RootElement.ToString());
        }

        var generatedMessage = responseDocument.RootElement.GetProperty("choices")[0].GetProperty("message");
        if (generatedMessage.TryGetProperty("tool_calls", out var toolCallsElement))
        {
            foreach (var toolCallElement in toolCallsElement.EnumerateArray())
            {
                if (toolCallElement.GetProperty("type").GetString() == "function")
                {
                    var toolCallId = toolCallElement.GetProperty("id").GetString()!;
                    var functionElement = toolCallElement.GetProperty("function");
                    var functionName = functionElement.GetProperty("name").GetString()!;
                    var functionArguments = functionElement.GetProperty("arguments").GetString()!;

                    var message1 = await conversation.FromAssistantAsync(new FunctionCall(toolCallId, functionName, functionArguments));
                    await options.AddMessageCallback(message1);

                    var function = options.Functions.LastOrDefault(f => f.Name.Equals(functionName, StringComparison.InvariantCultureIgnoreCase));
                    if (function != null)
                    {
                        if (function.RequiresConfirmation && conversation.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionName)) % 2 != 0)
                        {
                            var message2 = await conversation.FromFunctionAsync(new FunctionResult(toolCallId, functionName, "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm."));
                            await options.AddMessageCallback(message2);
                        }
                        else
                        {
                            if (function.Callback != null)
                            {
                                var functionValue = await FunctionInvoker.InvokeAsync(function.Callback, functionArguments, cancellationToken);
                                var message3 = await conversation.FromFunctionAsync(new FunctionResult(toolCallId, functionName, functionValue));

                                await options.AddMessageCallback(message3);
                            }
                            else
                            {
                                var functionValue = await options.DefaultFunctionCallback(functionName, functionArguments, cancellationToken);
                                var message4 = await conversation.FromFunctionAsync(new FunctionResult(toolCallId, functionName, JsonSerializer.Serialize(functionValue)));

                                await options.AddMessageCallback(message4);
                            }
                        }
                    }
                    else
                    {
                        var message5 = await conversation.FromFunctionAsync(new FunctionResult(toolCallId, functionName, $"Function '{functionName}' was not found."));
                        await options.AddMessageCallback(message5);
                    }
                }
            }

            return await CompleteAsync(conversation, apiKey, options, httpClient, cancellationToken);
        }

        var messageContent = generatedMessage.GetProperty("content").GetString()!;
        var message6 = await conversation.FromAssistantAsync(messageContent);

        await options.AddMessageCallback(message6);
        return messageContent;
    }

    internal static async IAsyncEnumerable<string> StreamCompletionAsync<T>(IChatConversation<T> conversation, string apiKey, ChatCompletionOptions? options = null, HttpClient? httpClient = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : IChatMessage, new()
    {
        options ??= new();
        httpClient ??= new();

        var request = CreateChatCompletionRequest(conversation, options);
        request.Add("stream", true);

        if (options.IsDebugMode)
        {
            Debug.WriteLine(request.ToString());
        }

        using var response = await httpClient.RepeatPostAsJsonForStreamAsync("https://api.openai.com/v1/chat/completions", request, apiKey, options.MaxAttempts, cancellationToken);
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
            if (options.IsDebugMode)
            {
                Debug.WriteLine(chunkDocument.RootElement.ToString());
            }

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
                            functionCalls.Add(new FunctionCall(currentToolCallId, currentFunctionName, currentFunctionArguments));
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
            functionCalls.Add(new FunctionCall(currentToolCallId, currentFunctionName, currentFunctionArguments));
        }

        if (functionCalls.Count > 0)
        {
            var message1 = await conversation.FromAssistantAsync(functionCalls);
            await options.AddMessageCallback(message1);
        }

        foreach (var functionCall in functionCalls)
        {
            var function = options.Functions.LastOrDefault(f => f.Name.Equals(functionCall.Name, StringComparison.InvariantCultureIgnoreCase));
            if (function != null)
            {
                if (function.RequiresConfirmation && conversation.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionCall.Name)) % 2 != 0)
                {
                    var message2 = await conversation.FromFunctionAsync(new FunctionResult(functionCall.Id!, functionCall.Name, "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm."));
                    await options.AddMessageCallback(message2);
                }
                else
                {
                    if (function.Callback != null)
                    {
                        var functionValue = await FunctionInvoker.InvokeAsync(function.Callback, functionCall.Arguments, cancellationToken);
                        var message3 = await conversation.FromFunctionAsync(new FunctionResult(functionCall.Id!, functionCall.Name, functionValue));

                        await options.AddMessageCallback(message3);
                    }
                    else
                    {
                        var functionValue = await options.DefaultFunctionCallback(functionCall.Name, functionCall.Arguments, cancellationToken);
                        var message4 = await conversation.FromFunctionAsync(new FunctionResult(functionCall.Id!, functionCall.Name, JsonSerializer.Serialize(functionValue)));

                        await options.AddMessageCallback(message4);
                    }
                }
            }
            else
            {
                var message5 = await conversation.FromFunctionAsync(new FunctionResult(functionCall.Id!, functionCall.Name, $"Function '{functionCall.Name}' was not found."));
                await options.AddMessageCallback(message5);
            }
        }

        if (!string.IsNullOrWhiteSpace(entireContent))
        {
            var message6 = await conversation.FromAssistantAsync(entireContent);
            await options.AddMessageCallback(message6);
        }

        if (functionCalls.Count > 0)
        {
            await foreach (var chunk in StreamCompletionAsync(conversation, apiKey, options, httpClient, cancellationToken))
            {
                yield return chunk;
            }
        }
    }

    private static JsonObject CreateChatCompletionRequest<T>(IChatConversation<T> conversation, ChatCompletionOptions options) where T : IChatMessage, new()
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

            if (message.FunctionResult != null && !string.IsNullOrEmpty(message.FunctionResult.Name))
            {
                messageObject.Add("tool_call_id", message.FunctionResult.Id);
                messageObject.Add("content", JsonSerializer.Serialize(message.FunctionResult.Value));
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

        if (options.Functions.Count > 0)
        {
            var toolsArray = new JsonArray();
            foreach (var function in options.Functions)
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
