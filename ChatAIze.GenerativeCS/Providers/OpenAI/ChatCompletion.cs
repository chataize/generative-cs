using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Utilities;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.GenerativeCS.Providers.OpenAI;

/// <summary>
/// Handles OpenAI chat completion requests, including streaming and function calling flows.
/// </summary>
internal static class ChatCompletion
{
    /// <summary>
    /// Serializer options used for chat and function payloads.
    /// </summary>
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Executes a chat completion request and returns the full response text.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="chat">Chat transcript to send.</param>
    /// <param name="apiKey">API key used for the request.</param>
    /// <param name="options">Optional completion options.</param>
    /// <param name="usageTracker">Optional tracker for token usage.</param>
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="recursion">Internal recursion counter used to guard against loops.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Model response text.</returns>
    internal static async Task<string> CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat, string? apiKey,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null, HttpClient? httpClient = null,
        int recursion = 0, CancellationToken cancellationToken = default)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        // Function-calling paths re-enter this method after tool execution; guard against a runaway loop.
        if (recursion >= 5)
        {
            throw new InvalidOperationException("Recursion limit reached (infinite loop detected).");
        }

        options ??= new();
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        var request = CreateChatCompletionRequest(chat, options);
        if (options.IsDebugMode)
        {
            Console.WriteLine(request.ToString());
        }

        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/chat/completions", request, apiKey, options.MaxAttempts, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (options.IsDebugMode)
        {
            Console.WriteLine(responseDocument.RootElement.ToString());
        }

        if (usageTracker is not null)
        {
            var usage = responseDocument.RootElement.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
            var cachedTokens = usage.GetProperty("prompt_tokens_details").GetProperty("cached_tokens").GetInt32();
            var completionTokens = usage.GetProperty("completion_tokens").GetInt32();

            usageTracker.AddPromptTokens(promptTokens);
            usageTracker.AddCachedTokens(cachedTokens);
            usageTracker.AddCompletionTokens(completionTokens);
        }

        var generatedMessage = responseDocument.RootElement.GetProperty("choices")[0].GetProperty("message");
        if (generatedMessage.TryGetProperty("tool_calls", out var toolCallsElement))
        {
            var anySuccessfulToolCall = false;

            foreach (var toolCallElement in toolCallsElement.EnumerateArray())
            {
                if (toolCallElement.GetProperty("type").GetString() == "function")
                {
                    var toolCallId = toolCallElement.GetProperty("id").GetString()!;
                    var functionElement = toolCallElement.GetProperty("function");
                    var functionName = functionElement.GetProperty("name").GetString()!;
                    var functionArguments = functionElement.GetProperty("arguments").GetString()!;

                    var message1 = await chat.FromChatbotAsync(new TFunctionCall { ToolCallId = toolCallId, Name = functionName, Arguments = functionArguments });
                    await options.AddMessageCallback(message1);

                    var function = options.Functions.FirstOrDefault(f => f.Name.NormalizedEquals(functionName));
                    if (function is not null)
                    {
                        // Every odd invocation of a double-check function asks the model to confirm before executing it.
                        if (function.RequiresDoubleCheck && chat.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionName)) % 2 != 0)
                        {
                            var message2 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = toolCallId, Name = functionName, Value = "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm." });
                            await options.AddMessageCallback(message2);
                        }
                        else
                        {
                            if (function.Callback is not null)
                            {
                                var functionValue = await function.Callback.InvokeForStringResultAsync(functionArguments, options.FunctionContext, cancellationToken);
                                var message3 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = toolCallId, Name = functionName, Value = functionValue });
                                await options.AddMessageCallback(message3);
                                if (!functionValue.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                                {
                                    anySuccessfulToolCall = true;
                                }
                            }
                            else
                            {
                                var functionValue = await options.DefaultFunctionCallback(functionName, functionArguments, cancellationToken);
                                if (functionValue is string stringValue)
                                {
                                    var message4 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = toolCallId, Name = functionName, Value = stringValue });
                                    await options.AddMessageCallback(message4);
                                    if (!stringValue.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        anySuccessfulToolCall = true;
                                    }
                                }
                                else
                                {
                                    var message4 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = toolCallId, Name = functionName, Value = JsonSerializer.Serialize(functionValue, JsonOptions) });
                                    await options.AddMessageCallback(message4);
                                    anySuccessfulToolCall = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        var message5 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = toolCallId, Name = functionName, Value = $"Function '{functionName}' was not found." });
                        await options.AddMessageCallback(message5);
                    }
                }
            }

            if (!anySuccessfulToolCall)
            {
                var fallback = await chat.FromChatbotAsync("No tool call succeeded; provide required parameters or respond directly.");
                await options.AddMessageCallback(fallback);
                return fallback.Content ?? string.Empty;
            }

            // Ask the model to continue now that function results have been appended to the chat.
            return await CompleteAsync(chat, apiKey, options, usageTracker, httpClient, recursion + 1, cancellationToken);
        }

        var messageContent = generatedMessage.GetProperty("content").GetString()!;
        var message6 = await chat.FromChatbotAsync(messageContent);

        await options.AddMessageCallback(message6);
        return messageContent;
    }

    /// <summary>
    /// Streams a chat completion response, yielding tokens as they arrive.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="chat">Chat transcript to send.</param>
    /// <param name="apiKey">API key used for the request.</param>
    /// <param name="options">Optional completion options.</param>
    /// <param name="usageTracker">Optional tracker for token usage.</param>
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="recursion">Internal recursion counter used to guard against loops.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async sequence of streamed response chunks.</returns>
    internal static async IAsyncEnumerable<string> StreamCompletionAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat, string? apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null, HttpClient? httpClient = null, int recursion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        if (recursion >= 5)
        {
            throw new InvalidOperationException("Recursion limit reached (infinite loop detected).");
        }

        options ??= new();
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        var request = CreateChatCompletionRequest(chat, options);
        request["stream"] = true;

        if (usageTracker is not null)
        {
            var streamOptions = new JsonObject
            {
                ["include_usage"] = true
            };

            request["stream_options"] = streamOptions;
        }

        if (options.IsDebugMode)
        {
            Console.WriteLine(request.ToString());
        }

        using var response = await httpClient.RepeatPostAsJsonForStreamAsync("https://api.openai.com/v1/chat/completions", request, apiKey, options.MaxAttempts, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseReader = new StreamReader(responseContent);

        var functionCalls = new List<TFunctionCall>();
        var currentToolCallId = string.Empty;
        var currentFunctionName = string.Empty;
        var currentFunctionArguments = string.Empty;
        // Aggregate streamed text chunks so we can add a single chatbot message after the stream completes.
        var entireContent = string.Empty;

        string? chunk;
        while ((chunk = await responseReader.ReadLineAsync(cancellationToken)) is not null)
        {
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
                Console.WriteLine(chunkDocument.RootElement.ToString());
            }

            if (usageTracker is not null)
            {
                var usage = chunkDocument.RootElement.GetProperty("usage");
                if (usage.ValueKind != JsonValueKind.Null)
                {
                    var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
                    var cachedTokens = usage.GetProperty("prompt_tokens_details").GetProperty("cached_tokens").GetInt32();
                    var completionTokens = usage.GetProperty("completion_tokens").GetInt32();

                    usageTracker.AddPromptTokens(promptTokens);
                    usageTracker.AddCachedTokens(cachedTokens);
                    usageTracker.AddCompletionTokens(completionTokens);
                }
            }

            var choices = chunkDocument.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0)
            {
                continue;
            }

            var choice = choices[0];
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
                    // The streaming API sends tool call pieces incrementally; start a new call whenever
                    // a name arrives and keep appending arguments until the next one shows up.
                    if (functionProperty.TryGetProperty("name", out var functionNameProperty))
                    {
                        if (!string.IsNullOrWhiteSpace(currentFunctionName))
                        {
                            functionCalls.Add(new TFunctionCall { ToolCallId = currentToolCallId, Name = currentFunctionName, Arguments = currentFunctionArguments });
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
            // Flush the final in-progress function call that would not be pushed by the loop above.
            functionCalls.Add(new TFunctionCall { ToolCallId = currentToolCallId, Name = currentFunctionName, Arguments = currentFunctionArguments });
        }

        if (functionCalls.Count > 0)
        {
            var message1 = await chat.FromChatbotAsync(functionCalls);
            await options.AddMessageCallback(message1);
        }

        foreach (var functionCall in functionCalls)
        {
            var function = options.Functions.FirstOrDefault(f => f.Name.NormalizedEquals(functionCall.Name));
            if (function is not null)
            {
                // Every odd invocation of a double-check function asks the model to confirm before executing it.
                if (function.RequiresDoubleCheck && chat.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionCall.Name)) % 2 != 0)
                {
                    var message2 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = functionCall.ToolCallId, Name = functionCall.Name, Value = "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm." });
                    await options.AddMessageCallback(message2);
                }
                else
                {
                    if (function.Callback is not null)
                    {
                        var functionValue = await function.Callback.InvokeForStringResultAsync(functionCall.Arguments, options.FunctionContext, cancellationToken);
                        var message3 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = functionCall.ToolCallId, Name = functionCall.Name, Value = functionValue });

                        await options.AddMessageCallback(message3);
                    }
                    else
                    {
                        var functionValue = await options.DefaultFunctionCallback(functionCall.Name, functionCall.Arguments, cancellationToken);
                        if (functionValue is string stringValue)
                        {
                            var message4 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = functionCall.ToolCallId!, Name = functionCall.Name, Value = stringValue });
                            await options.AddMessageCallback(message4);
                        }
                        else
                        {
                            var message4 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = functionCall.ToolCallId!, Name = functionCall.Name, Value = JsonSerializer.Serialize(functionValue, JsonOptions) });
                            await options.AddMessageCallback(message4);
                        }
                    }
                }
            }
            else
            {
                var message5 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = functionCall.ToolCallId, Name = functionCall.Name, Value = $"Function '{functionCall.Name}' was not found." });
                await options.AddMessageCallback(message5);
            }
        }

        if (!string.IsNullOrWhiteSpace(entireContent))
        {
            var message6 = await chat.FromChatbotAsync(entireContent);
            await options.AddMessageCallback(message6);
        }

        if (functionCalls.Count > 0)
        {
            // Re-enter streaming once tool results are in the transcript so the model can continue its reply.
            await foreach (var chunk2 in StreamCompletionAsync(chat, apiKey, options, usageTracker, httpClient, recursion + 1, cancellationToken))
            {
                yield return chunk2;
            }
        }
    }

    /// <summary>
    /// Builds the JSON payload for a chat completion request.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="chat">Chat transcript to send.</param>
    /// <param name="options">Completion options.</param>
    /// <returns>JSON request payload.</returns>
    private static JsonObject CreateChatCompletionRequest<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        var messages = chat.Messages.ToList();

        if (options.SystemMessageCallback is not null)
        {
            MessageTools.AddDynamicSystemMessage<TMessage, TFunctionCall, TFunctionResult>(messages, options.SystemMessageCallback());
        }

        if (options.IsTimeAware)
        {
            MessageTools.AddTimeInformation<TMessage, TFunctionCall, TFunctionResult>(messages, options.TimeCallback());
        }

        MessageTools.RemoveDeletedMessages<TMessage, TFunctionCall, TFunctionResult>(messages);

        if (options.IsIgnoringPreviousFunctionCalls)
        {
            MessageTools.RemovePreviousFunctionCalls<TMessage, TFunctionCall, TFunctionResult>(messages);
        }

        MessageTools.LimitTokens<TMessage, TFunctionCall, TFunctionResult>(messages, options.MessageLimit, options.CharacterLimit);

        var messagesArray = new JsonArray();
        foreach (var message in messages)
        {
            var messageObject = new JsonObject
            {
                ["role"] = GetRoleName(message.Role, options.Model)
            };

            if (message.UserName is not null)
            {
                messageObject["name"] = message.UserName;
            }

            var contentArray = new JsonArray();

            if (message.Content is not null)
            {
                var textObject = new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = message.Content
                };

                contentArray.Add(textObject);
            }

            foreach (var imageUrl in message.ImageUrls)
            {
                var imageUrlObject = new JsonObject
                {
                    ["url"] = imageUrl
                };

                var imageObject = new JsonObject
                {
                    ["type"] = "image_url",
                    ["image_url"] = imageUrlObject
                };

                contentArray.Add(imageObject);
            }

            var toolCallsArray = new JsonArray();
            foreach (var functionCall in message.FunctionCalls)
            {
                var functionObject = new JsonObject
                {
                    ["name"] = functionCall.Name,
                    ["arguments"] = functionCall.Arguments
                };

                var toolCallObject = new JsonObject
                {
                    ["id"] = functionCall.ToolCallId,
                    ["type"] = "function",
                    ["function"] = functionObject
                };

                toolCallsArray.Add(toolCallObject);
            }

            if (toolCallsArray.Count > 0)
                messageObject["tool_calls"] = toolCallsArray;

            if (message.FunctionResult is not null && !string.IsNullOrWhiteSpace(message.FunctionResult.Name))
            {
                messageObject["tool_call_id"] = message.FunctionResult.ToolCallId;
                // Tool results are sent as plain content fields, not as the rich content array used for user text/images.
                messageObject["content"] = message.FunctionResult.Value;
            }
            else
            {
                messageObject["content"] = contentArray;
            }

            messagesArray.Add(messageObject);
        }

        var requestObject = new JsonObject
        {
            ["model"] = options.Model,
            ["messages"] = messagesArray
        };

        if (chat.UserTrackingId is not null)
        {
            requestObject["user"] = chat.UserTrackingId;
        }
        else if (options.UserTrackingId is not null)
        {
            requestObject["user"] = options.UserTrackingId;
        }

        if (options.MaxOutputTokens.HasValue)
        {
            requestObject["max_completion_tokens"] = options.MaxOutputTokens.Value;
        }

        if (options.Seed.HasValue)
        {
            requestObject["seed"] = options.Seed.Value;
        }

        if (options.Temperature.HasValue)
        {
            requestObject["temperature"] = options.Temperature.Value;
        }

        if (options.TopP.HasValue)
        {
            requestObject["top_p"] = options.TopP.Value;
        }

        if (options.FrequencyPenalty.HasValue)
        {
            requestObject["frequency_penalty"] = options.FrequencyPenalty.Value;
        }

        if (options.PresencePenalty.HasValue)
        {
            requestObject["presence_penalty"] = options.PresencePenalty.Value;
        }

        if (options.ReasoningEffort != ReasoningEffort.None)
        {
            requestObject["reasoning_effort"] = options.ReasoningEffort.ToString().ToSnakeLower();
        }

        if (options.Verbosity != Verbosity.Medium)
        {
            requestObject["verbosity"] = options.Verbosity.ToString().ToSnakeLower();
        }

        if (options.ResponseType is not null)
        {
            requestObject["response_format"] = SchemaSerializer.SerializeResponseFormat(options.ResponseType, useOpenAIFeatures: true);
        }
        else if (options.IsJsonMode)
        {
            var responseFormatObject = new JsonObject
            {
                ["type"] = "json_object"
            };

            requestObject["response_format"] = responseFormatObject;
        }

        if (options.Functions.Count > 0 && (!options.IsParallelFunctionCallingOn || options.IsStrictFunctionCallingOn))
        {
            // Force serial tool calls when caller disables parallelism or strict mode demands ordered execution.
            requestObject["parallel_tool_calls"] = false;
        }

        if (options.IsStoringOutputs)
        {
            requestObject["store"] = true;
        }

        if (options.StopWords is not null && options.StopWords.Count > 0)
        {
            var stopArray = new JsonArray();
            foreach (var stop in options.StopWords)
            {
                stopArray.Add(stop);
            }

            requestObject["stop"] = stopArray;
        }

        if (options.Functions.Count > 0)
        {
            var toolsArray = new JsonArray();
            foreach (var function in options.Functions)
            {
                var normalizedName = function.Name.ToSnakeLower();
                if (string.IsNullOrWhiteSpace(normalizedName))
                {
                    continue;
                }

                var functionObject = SchemaSerializer.SerializeFunction(function, useOpenAIFeatures: true, options.IsStrictFunctionCallingOn);
                var toolObject = new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = functionObject
                };

                toolsArray.Add(toolObject);
            }

            if (toolsArray.Count > 0)
                requestObject["tools"] = toolsArray;
        }

        return requestObject;
    }

    /// <summary>
    /// Converts a chat role into the provider-specific role name.
    /// </summary>
    /// <param name="role">Role to convert.</param>
    /// <param name="model">Model identifier used to infer developer role names.</param>
    /// <returns>Provider-specific role string.</returns>
    private static string GetRoleName(ChatRole role, string model)
    {
        var usesDeveloperRole = model.StartsWith("o1") || model.StartsWith("o3");
        return role switch
        {
            ChatRole.System => usesDeveloperRole ? "developer" : "system",
            ChatRole.User => "user",
            ChatRole.Chatbot => "assistant",
            ChatRole.Function => "tool",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid role")
        };
    }
}
