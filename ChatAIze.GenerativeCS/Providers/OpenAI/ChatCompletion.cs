using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
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
    private const string DefaultChatCompletionsEndpoint = "https://api.openai.com/v1/chat/completions";

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
    /// <param name="requestUri">Request URI used for OpenAI-compatible chat completion calls.</param>
    /// <param name="includeProviderExtensions">True to emit OpenAI-only fields such as store, seed, reasoning effort, and verbosity.</param>
    /// <param name="maxTokensPropertyName">Provider-specific request field used for output token limits.</param>
    /// <param name="mergeSystemMessages">True to collapse multiple system messages into a single leading system message.</param>
    /// <returns>Model response text.</returns>
    internal static async Task<string> CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat, string? apiKey,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null, HttpClient? httpClient = null,
        int recursion = 0, CancellationToken cancellationToken = default,
        string requestUri = DefaultChatCompletionsEndpoint, bool includeProviderExtensions = true,
        string maxTokensPropertyName = "max_completion_tokens", bool mergeSystemMessages = false)
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

        var request = CreateChatCompletionRequest(chat, options, includeProviderExtensions, maxTokensPropertyName, mergeSystemMessages);
        if (options.IsDebugMode)
        {
            Console.WriteLine(request.ToString());
        }

        using var response = await httpClient.RepeatPostAsJsonAsync(requestUri, request, apiKey, options.MaxAttempts, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (options.IsDebugMode)
        {
            Console.WriteLine(responseDocument.RootElement.ToString());
        }

        AddUsage(responseDocument.RootElement, usageTracker);

        var choice = GetPrimaryChoice(responseDocument.RootElement);
        var generatedMessage = choice.GetProperty("message");
        var initialText = ExtractAssistantText(generatedMessage);
        if (!string.IsNullOrWhiteSpace(initialText))
        {
            var textMessage = await chat.FromChatbotAsync(initialText);
            await options.AddMessageCallback(textMessage);
        }

        if (generatedMessage.TryGetProperty("tool_calls", out var toolCallsElement))
        {
            var shouldContinueConversation = false;

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
                            shouldContinueConversation = true;
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
                                    shouldContinueConversation = true;
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
                                        shouldContinueConversation = true;
                                    }
                                }
                                else
                                {
                                    var message4 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = toolCallId, Name = functionName, Value = JsonSerializer.Serialize(functionValue, JsonOptions) });
                                    await options.AddMessageCallback(message4);
                                    shouldContinueConversation = true;
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

            if (!shouldContinueConversation)
            {
                var fallback = await chat.FromChatbotAsync("No tool call succeeded; provide required parameters or respond directly.");
                await options.AddMessageCallback(fallback);
                return initialText + (fallback.Content ?? string.Empty);
            }

            // Ask the model to continue now that function results have been appended to the chat.
            return initialText + await CompleteAsync(chat, apiKey, options, usageTracker, httpClient, recursion + 1, cancellationToken, requestUri, includeProviderExtensions, maxTokensPropertyName, mergeSystemMessages);
        }

        if (string.IsNullOrWhiteSpace(initialText))
        {
            ThrowForEmptyChoice(choice);
        }

        return initialText;
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
    /// <param name="requestUri">Request URI used for OpenAI-compatible chat completion calls.</param>
    /// <param name="includeProviderExtensions">True to emit OpenAI-only fields such as store, seed, reasoning effort, and verbosity.</param>
    /// <param name="maxTokensPropertyName">Provider-specific request field used for output token limits.</param>
    /// <param name="mergeSystemMessages">True to collapse multiple system messages into a single leading system message.</param>
    /// <returns>An async sequence of streamed response chunks.</returns>
    internal static async IAsyncEnumerable<string> StreamCompletionAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat, string? apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null, HttpClient? httpClient = null, int recursion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        string requestUri = DefaultChatCompletionsEndpoint, bool includeProviderExtensions = true,
        string maxTokensPropertyName = "max_completion_tokens", bool mergeSystemMessages = false)
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

        var request = CreateChatCompletionRequest(chat, options, includeProviderExtensions, maxTokensPropertyName, mergeSystemMessages);
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

        using var response = await httpClient.RepeatPostAsJsonForStreamAsync(requestUri, request, apiKey, options.MaxAttempts, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseReader = new StreamReader(responseContent);

        var streamingToolCalls = new SortedDictionary<int, StreamingToolCallState>();
        // Aggregate streamed text chunks so we can add a single chatbot message after the stream completes.
        var entireContent = new StringBuilder();
        string? lastFinishReason = null;

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

            AddUsage(chunkDocument.RootElement, usageTracker);

            if (!chunkDocument.RootElement.TryGetProperty("choices", out var choices))
            {
                continue;
            }

            if (choices.GetArrayLength() == 0)
            {
                continue;
            }

            var choice = choices[0];
            if (choice.TryGetProperty("finish_reason", out var finishReasonProperty)
                && finishReasonProperty.ValueKind != JsonValueKind.Null)
            {
                lastFinishReason = finishReasonProperty.GetString();
            }

            var delta = choice.GetProperty("delta");

            if (delta.TryGetProperty("content", out var contentProperty) && contentProperty.ValueKind != JsonValueKind.Null)
            {
                var content = ExtractMessageText(contentProperty);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    _ = entireContent.Append(content);
                    yield return content;
                }
            }

            if (delta.TryGetProperty("refusal", out var refusalProperty) && refusalProperty.ValueKind != JsonValueKind.Null)
            {
                var refusal = ExtractMessageText(refusalProperty);
                if (!string.IsNullOrWhiteSpace(refusal))
                {
                    _ = entireContent.Append(refusal);
                    yield return refusal;
                }
            }

            if (delta.TryGetProperty("tool_calls", out var toolCallsProperty))
            {
                for (var toolCallOffset = 0; toolCallOffset < toolCallsProperty.GetArrayLength(); toolCallOffset++)
                {
                    var toolCallProperty = toolCallsProperty[toolCallOffset];
                    var toolCallIndex = toolCallProperty.TryGetProperty("index", out var indexProperty)
                        ? indexProperty.GetInt32()
                        : toolCallOffset;

                    if (!streamingToolCalls.TryGetValue(toolCallIndex, out var streamingToolCall))
                    {
                        streamingToolCall = new StreamingToolCallState();
                        streamingToolCalls[toolCallIndex] = streamingToolCall;
                    }

                    if (toolCallProperty.TryGetProperty("id", out var toolCallIdProperty))
                    {
                        streamingToolCall.ToolCallId = toolCallIdProperty.GetString();
                    }

                    if (!toolCallProperty.TryGetProperty("function", out var functionProperty))
                    {
                        continue;
                    }

                    if (functionProperty.TryGetProperty("name", out var functionNameProperty))
                    {
                        streamingToolCall.Name = functionNameProperty.GetString();
                    }

                    if (functionProperty.TryGetProperty("arguments", out var functionArgumentsProperty))
                    {
                        _ = streamingToolCall.Arguments.Append(functionArgumentsProperty.GetString());
                    }
                }
            }
        }

        var functionCalls = streamingToolCalls
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value.Name))
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new TFunctionCall
            {
                ToolCallId = string.IsNullOrWhiteSpace(kvp.Value.ToolCallId)
                    ? $"chatcmpl_tool_{Guid.NewGuid():N}"
                    : kvp.Value.ToolCallId,
                Name = kvp.Value.Name!,
                Arguments = kvp.Value.Arguments.Length == 0
                    ? "{}"
                    : kvp.Value.Arguments.ToString()
            })
            .ToList();

        if (functionCalls.Count > 0)
        {
            var message1 = await chat.FromChatbotAsync(functionCalls);
            await options.AddMessageCallback(message1);
        }

        var shouldContinueConversation = false;
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
                    shouldContinueConversation = true;
                }
                else
                {
                    if (function.Callback is not null)
                    {
                        var functionValue = await function.Callback.InvokeForStringResultAsync(functionCall.Arguments, options.FunctionContext, cancellationToken);
                        var message3 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = functionCall.ToolCallId, Name = functionCall.Name, Value = functionValue });

                        await options.AddMessageCallback(message3);
                        if (!functionValue.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                        {
                            shouldContinueConversation = true;
                        }
                    }
                    else
                    {
                        var functionValue = await options.DefaultFunctionCallback(function.Name, functionCall.Arguments, cancellationToken);
                        if (functionValue is string stringValue)
                        {
                            var message4 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = functionCall.ToolCallId!, Name = functionCall.Name, Value = stringValue });
                            await options.AddMessageCallback(message4);
                            if (!stringValue.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                            {
                                shouldContinueConversation = true;
                            }
                        }
                        else
                        {
                            var message4 = await chat.FromFunctionAsync(new TFunctionResult { ToolCallId = functionCall.ToolCallId!, Name = functionCall.Name, Value = JsonSerializer.Serialize(functionValue, JsonOptions) });
                            await options.AddMessageCallback(message4);
                            shouldContinueConversation = true;
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

        if (entireContent.Length > 0)
        {
            var message6 = await chat.FromChatbotAsync(entireContent.ToString());
            await options.AddMessageCallback(message6);
        }

        if (functionCalls.Count > 0 && !shouldContinueConversation)
        {
            var fallback = await chat.FromChatbotAsync("No tool call succeeded; provide required parameters or respond directly.");
            await options.AddMessageCallback(fallback);

            if (!string.IsNullOrWhiteSpace(fallback.Content))
            {
                yield return fallback.Content;
            }

            yield break;
        }

        if (functionCalls.Count > 0)
        {
            // Re-enter streaming once tool results are in the transcript so the model can continue its reply.
            await foreach (var chunk2 in StreamCompletionAsync(chat, apiKey, options, usageTracker, httpClient, recursion + 1, cancellationToken, requestUri, includeProviderExtensions, maxTokensPropertyName, mergeSystemMessages))
            {
                yield return chunk2;
            }

            yield break;
        }

        if (entireContent.Length == 0)
        {
            ThrowForEmptyStreamResponse(lastFinishReason);
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
    /// <param name="includeProviderExtensions">True to emit OpenAI-only fields such as store, seed, reasoning effort, and verbosity.</param>
    /// <param name="maxTokensPropertyName">Provider-specific request field used for output token limits.</param>
    /// <param name="mergeSystemMessages">True to collapse multiple system messages into a single leading system message.</param>
    /// <returns>JSON request payload.</returns>
    private static JsonObject CreateChatCompletionRequest<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options,
        bool includeProviderExtensions,
        string maxTokensPropertyName,
        bool mergeSystemMessages)
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

        if (mergeSystemMessages)
        {
            MergeSystemMessages<TMessage, TFunctionCall, TFunctionResult>(messages);
        }

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
            requestObject[maxTokensPropertyName] = options.MaxOutputTokens.Value;
        }

        if (includeProviderExtensions && options.Seed.HasValue)
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

        if (includeProviderExtensions && options.ReasoningEffort != ReasoningEffort.None)
        {
            requestObject["reasoning_effort"] = options.ReasoningEffort.ToString().ToSnakeLower();
        }

        if (includeProviderExtensions && options.Verbosity != Verbosity.Medium)
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

        if (includeProviderExtensions && options.IsStoringOutputs)
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
        return role switch
        {
            ChatRole.System => UsesDeveloperRole(model) ? "developer" : "system",
            ChatRole.User => "user",
            ChatRole.Chatbot => "assistant",
            ChatRole.Function => "tool",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid role")
        };
    }

    /// <summary>
    /// Collapses all system messages into a single leading system message for providers that only accept one.
    /// </summary>
    private static void MergeSystemMessages<TMessage, TFunctionCall, TFunctionResult>(List<TMessage> messages)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        var systemMessages = messages
            .Where(message => message.Role == ChatRole.System && !string.IsNullOrWhiteSpace(message.Content))
            .ToList();

        if (systemMessages.Count <= 1)
        {
            return;
        }

        var mergedContent = string.Join("\n\n", systemMessages.Select(message => message.Content!.Trim()));
        _ = messages.RemoveAll(message => message.Role == ChatRole.System);
        messages.Insert(0, new TMessage
        {
            Role = ChatRole.System,
            Content = mergedContent,
            PinLocation = PinLocation.Begin
        });
    }

    /// <summary>
    /// Determines whether the selected OpenAI model family expects developer messages instead of legacy system messages.
    /// </summary>
    /// <remarks>
    /// OpenAI documents developer messages for o-series reasoning models and GPT-5-era chat models.
    /// Preserve legacy system-role behavior for older families such as GPT-4.x and GPT-4o.
    /// </remarks>
    private static bool UsesDeveloperRole(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        var normalizedModel = NormalizeModelName(model);
        if (normalizedModel.Length >= 2
            && normalizedModel[0] == 'o'
            && char.IsDigit(normalizedModel[1]))
        {
            return true;
        }

        if (!normalizedModel.StartsWith("gpt-", StringComparison.Ordinal))
        {
            return false;
        }

        var majorVersionStart = "gpt-".Length;
        var majorVersionLength = 0;
        while (majorVersionStart + majorVersionLength < normalizedModel.Length
               && char.IsDigit(normalizedModel[majorVersionStart + majorVersionLength]))
        {
            majorVersionLength++;
        }

        if (majorVersionLength == 0
            || !int.TryParse(normalizedModel.AsSpan(majorVersionStart, majorVersionLength), out var majorVersion))
        {
            return false;
        }

        return majorVersion >= 5;
    }

    private static string NormalizeModelName(string model)
    {
        var normalizedModel = model.Trim().ToLowerInvariant();

        var lastSlashIndex = normalizedModel.LastIndexOf('/');
        if (lastSlashIndex >= 0 && lastSlashIndex < normalizedModel.Length - 1)
        {
            normalizedModel = normalizedModel[(lastSlashIndex + 1)..];
        }

        if (normalizedModel.StartsWith("ft:", StringComparison.Ordinal))
        {
            var secondColonIndex = normalizedModel.IndexOf(':', "ft:".Length);
            normalizedModel = secondColonIndex > "ft:".Length
                ? normalizedModel["ft:".Length..secondColonIndex]
                : normalizedModel["ft:".Length..];
        }

        return normalizedModel;
    }

    private static JsonElement GetPrimaryChoice(JsonElement root)
    {
        if (!root.TryGetProperty("choices", out var choicesElement) || choicesElement.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("OpenAI returned no choices.");
        }

        return choicesElement[0];
    }

    private static void AddUsage(JsonElement root, TokenUsageTracker? usageTracker)
    {
        if (usageTracker is null || !root.TryGetProperty("usage", out var usageElement) || usageElement.ValueKind == JsonValueKind.Null)
        {
            return;
        }

        if (usageElement.TryGetProperty("prompt_tokens", out var promptTokensElement)
            && promptTokensElement.ValueKind == JsonValueKind.Number)
        {
            usageTracker.AddPromptTokens(promptTokensElement.GetInt32());
        }

        if (usageElement.TryGetProperty("prompt_tokens_details", out var promptTokenDetailsElement)
            && promptTokenDetailsElement.ValueKind == JsonValueKind.Object
            && promptTokenDetailsElement.TryGetProperty("cached_tokens", out var cachedTokensElement)
            && cachedTokensElement.ValueKind == JsonValueKind.Number)
        {
            usageTracker.AddCachedTokens(cachedTokensElement.GetInt32());
        }

        if (usageElement.TryGetProperty("completion_tokens", out var completionTokensElement)
            && completionTokensElement.ValueKind == JsonValueKind.Number)
        {
            usageTracker.AddCompletionTokens(completionTokensElement.GetInt32());
        }
    }

    private static string ExtractAssistantText(JsonElement message)
    {
        var builder = new StringBuilder();

        if (message.TryGetProperty("content", out var contentElement))
        {
            AppendMessageText(builder, contentElement);
        }

        if (builder.Length == 0
            && message.TryGetProperty("refusal", out var refusalElement)
            && refusalElement.ValueKind == JsonValueKind.String)
        {
            _ = builder.Append(refusalElement.GetString());
        }

        return builder.ToString();
    }

    private static string ExtractMessageText(JsonElement contentElement)
    {
        var builder = new StringBuilder();
        AppendMessageText(builder, contentElement);
        return builder.ToString();
    }

    private static void AppendMessageText(StringBuilder builder, JsonElement contentElement)
    {
        switch (contentElement.ValueKind)
        {
            case JsonValueKind.String:
            {
                var value = contentElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _ = builder.Append(value);
                }

                break;
            }

            case JsonValueKind.Array:
                foreach (var partElement in contentElement.EnumerateArray())
                {
                    if (partElement.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (partElement.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                    {
                        var text = textElement.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            _ = builder.Append(text);
                        }

                        continue;
                    }

                    if (partElement.TryGetProperty("refusal", out var refusalElement) && refusalElement.ValueKind == JsonValueKind.String)
                    {
                        var refusal = refusalElement.GetString();
                        if (!string.IsNullOrWhiteSpace(refusal))
                        {
                            _ = builder.Append(refusal);
                        }
                    }
                }

                break;
        }
    }

    private static void ThrowForEmptyChoice(JsonElement choice)
    {
        var finishReason = choice.TryGetProperty("finish_reason", out var finishReasonElement)
            && finishReasonElement.ValueKind == JsonValueKind.String
            ? finishReasonElement.GetString()
            : null;

        if (!string.IsNullOrWhiteSpace(finishReason))
        {
            throw new InvalidOperationException($"OpenAI returned no assistant content. Finish reason: {finishReason}.");
        }

        throw new InvalidOperationException("OpenAI returned an empty assistant message.");
    }

    private static void ThrowForEmptyStreamResponse(string? finishReason)
    {
        if (!string.IsNullOrWhiteSpace(finishReason))
        {
            throw new InvalidOperationException($"OpenAI returned no streamed assistant content. Finish reason: {finishReason}.");
        }

        throw new InvalidOperationException("OpenAI returned an empty streamed assistant message.");
    }

    private sealed class StreamingToolCallState
    {
        public string? ToolCallId { get; set; }

        public string? Name { get; set; }

        public StringBuilder Arguments { get; } = new();
    }
}
