using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.Claude;
using ChatAIze.GenerativeCS.Utilities;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.GenerativeCS.Providers.Claude;

/// <summary>
/// Handles Claude Messages API requests, including streaming and function calling flows.
/// </summary>
internal static class ChatCompletion
{
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

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
    internal static async Task<string> CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat,
        string? apiKey,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null,
        HttpClient? httpClient = null,
        int recursion = 0,
        CancellationToken cancellationToken = default)
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

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Claude API key was not provided.");
        }

        var request = CreateChatCompletionRequest(chat, options);
        if (options.IsDebugMode)
        {
            Console.WriteLine(request.ToJsonString(JsonOptions));
        }

        using var response = await SendClaudeRequestAsync(httpClient, request, apiKey, options.MaxAttempts, isStreaming: false, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (options.IsDebugMode)
        {
            Console.WriteLine(responseDocument.RootElement.ToString());
        }

        AddUsage(responseDocument.RootElement, usageTracker);

        var toolCalls = new List<TFunctionCall>();
        var textBuilder = new StringBuilder();

        foreach (var contentBlock in responseDocument.RootElement.GetProperty("content").EnumerateArray())
        {
            if (!contentBlock.TryGetProperty("type", out var typeElement))
            {
                continue;
            }

            switch (typeElement.GetString())
            {
                case "text":
                    if (contentBlock.TryGetProperty("text", out var textElement) && !string.IsNullOrWhiteSpace(textElement.GetString()))
                    {
                        _ = textBuilder.Append(textElement.GetString());
                    }

                    break;

                case "tool_use":
                    toolCalls.Add(new TFunctionCall
                    {
                        ToolCallId = contentBlock.GetProperty("id").GetString(),
                        Name = contentBlock.GetProperty("name").GetString()!,
                        Arguments = contentBlock.TryGetProperty("input", out var inputElement)
                            ? inputElement.GetRawText()
                            : "{}"
                    });
                    break;
            }
        }

        var stopReason = responseDocument.RootElement.TryGetProperty("stop_reason", out var stopReasonElement)
            ? stopReasonElement.GetString()
            : null;

        if (toolCalls.Count > 0 && string.Equals(stopReason, "max_tokens", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Claude stopped while producing tool input. Increase MaxOutputTokens and retry.");
        }

        var initialText = textBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(initialText))
        {
            var textMessage = await chat.FromChatbotAsync(initialText);
            await options.AddMessageCallback(textMessage);
        }

        if (toolCalls.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(initialText))
            {
                ThrowForEmptyResponse(stopReason, isStreaming: false);
            }

            return initialText;
        }

        var toolCallMessage = await chat.FromChatbotAsync(toolCalls);
        await options.AddMessageCallback(toolCallMessage);

        var shouldContinueConversation = await ExecuteToolCallsAsync(chat, toolCalls, options, cancellationToken);
        if (!shouldContinueConversation)
        {
            var fallback = await chat.FromChatbotAsync("No tool call succeeded; provide required parameters or respond directly.");
            await options.AddMessageCallback(fallback);
            return initialText + (fallback.Content ?? string.Empty);
        }

        var continuation = await CompleteAsync(chat, apiKey, options, usageTracker, httpClient, recursion + 1, cancellationToken);

        return initialText + continuation;
    }

    /// <summary>
    /// Streams a chat completion response, yielding tokens as they arrive.
    /// </summary>
    internal static async IAsyncEnumerable<string> StreamCompletionAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat,
        string? apiKey,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null,
        HttpClient? httpClient = null,
        int recursion = 0,
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

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Claude API key was not provided.");
        }

        var request = CreateChatCompletionRequest(chat, options);
        request["stream"] = true;

        if (options.IsDebugMode)
        {
            Console.WriteLine(request.ToJsonString(JsonOptions));
        }

        using var response = await SendClaudeRequestAsync(httpClient, request, apiKey, options.MaxAttempts, isStreaming: true, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseReader = new StreamReader(responseContent);

        var textBlocks = new SortedDictionary<int, StringBuilder>();
        var toolCalls = new SortedDictionary<int, StreamingToolCall>();
        string? stopReason = null;
        var promptTokensRecorded = false;

        string? line;
        while ((line = await responseReader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line["data: ".Length..];
            using var eventDocument = JsonDocument.Parse(payload);
            var root = eventDocument.RootElement;

            if (options.IsDebugMode)
            {
                Console.WriteLine(root.ToString());
            }

            var eventType = root.GetProperty("type").GetString();
            switch (eventType)
            {
                case "message_start":
                    if (!promptTokensRecorded && usageTracker is not null && root.GetProperty("message").TryGetProperty("usage", out var startUsage))
                    {
                        AddPromptUsage(startUsage, usageTracker);
                        promptTokensRecorded = true;
                    }

                    break;

                case "content_block_start":
                {
                    var index = root.GetProperty("index").GetInt32();
                    var contentBlock = root.GetProperty("content_block");
                    var blockType = contentBlock.GetProperty("type").GetString();

                    if (blockType == "text")
                    {
                        textBlocks[index] = new StringBuilder();
                    }
                    else if (blockType == "tool_use")
                    {
                        toolCalls[index] = new StreamingToolCall(
                            contentBlock.GetProperty("id").GetString()!,
                            contentBlock.GetProperty("name").GetString()!,
                            new StringBuilder());
                    }

                    break;
                }

                case "content_block_delta":
                {
                    var index = root.GetProperty("index").GetInt32();
                    var delta = root.GetProperty("delta");
                    var deltaType = delta.GetProperty("type").GetString();

                    if (deltaType == "text_delta")
                    {
                        var text = delta.GetProperty("text").GetString() ?? string.Empty;
                        if (!textBlocks.TryGetValue(index, out var blockBuilder))
                        {
                            blockBuilder = new StringBuilder();
                            textBlocks[index] = blockBuilder;
                        }

                        _ = blockBuilder.Append(text);
                        yield return text;
                    }
                    else if (deltaType == "input_json_delta" && toolCalls.TryGetValue(index, out var toolCall))
                    {
                        _ = toolCall.Arguments.Append(delta.GetProperty("partial_json").GetString());
                    }

                    break;
                }

                case "message_delta":
                    stopReason = root.GetProperty("delta").TryGetProperty("stop_reason", out var stopReasonElement)
                        ? stopReasonElement.GetString()
                        : stopReason;

                    if (usageTracker is not null && root.TryGetProperty("usage", out var deltaUsage))
                    {
                        AddCompletionUsage(deltaUsage, usageTracker);
                    }

                    break;

                case "error":
                    throw new HttpRequestException(root.ToString());
            }
        }

        var textContent = string.Concat(textBlocks.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value.ToString()));
        var completedToolCalls = toolCalls
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new TFunctionCall
            {
                ToolCallId = kvp.Value.Id,
                Name = kvp.Value.Name,
                Arguments = string.IsNullOrWhiteSpace(kvp.Value.Arguments.ToString()) ? "{}" : kvp.Value.Arguments.ToString()
            })
            .ToList();

        if (completedToolCalls.Count > 0 && string.Equals(stopReason, "max_tokens", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Claude stopped while streaming tool input. Increase MaxOutputTokens and retry.");
        }

        if (!string.IsNullOrWhiteSpace(textContent))
        {
            var textMessage = await chat.FromChatbotAsync(textContent);
            await options.AddMessageCallback(textMessage);
        }

        if (completedToolCalls.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(textContent))
            {
                ThrowForEmptyResponse(stopReason, isStreaming: true);
            }

            yield break;
        }

        var toolCallMessage = await chat.FromChatbotAsync(completedToolCalls);
        await options.AddMessageCallback(toolCallMessage);

        var shouldContinueConversation = await ExecuteToolCallsAsync(chat, completedToolCalls, options, cancellationToken);
        if (!shouldContinueConversation)
        {
            var fallback = await chat.FromChatbotAsync("No tool call succeeded; provide required parameters or respond directly.");
            await options.AddMessageCallback(fallback);

            if (!string.IsNullOrWhiteSpace(fallback.Content))
            {
                yield return fallback.Content;
            }

            yield break;
        }

        await foreach (var continuationChunk in StreamCompletionAsync(chat, apiKey, options, usageTracker, httpClient, recursion + 1, cancellationToken))
        {
            yield return continuationChunk;
        }
    }

    private static async Task<HttpResponseMessage> SendClaudeRequestAsync(HttpClient httpClient, JsonObject request, string apiKey, int maxAttempts, bool isStreaming, CancellationToken cancellationToken)
    {
        return await httpClient.RepeatSendAsync(() =>
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(request.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Add("x-api-key", apiKey);
            httpRequest.Headers.Add("anthropic-version", AnthropicVersion);

            return httpRequest;
        }, responseHeadersRead: isStreaming, maxAttempts: maxAttempts, cancellationToken: cancellationToken);
    }

    private static void AddUsage(JsonElement responseRoot, TokenUsageTracker? usageTracker)
    {
        if (usageTracker is null || !responseRoot.TryGetProperty("usage", out var usage))
        {
            return;
        }

        AddPromptUsage(usage, usageTracker);

        if (usage.TryGetProperty("output_tokens", out var outputTokensElement) && outputTokensElement.ValueKind == JsonValueKind.Number)
        {
            usageTracker.AddCompletionTokens(outputTokensElement.GetInt32());
        }
    }

    private static void AddPromptUsage(JsonElement usage, TokenUsageTracker usageTracker)
    {
        if (usage.TryGetProperty("input_tokens", out var inputTokensElement) && inputTokensElement.ValueKind == JsonValueKind.Number)
        {
            usageTracker.AddPromptTokens(inputTokensElement.GetInt32());
        }

        if (usage.TryGetProperty("cache_creation_input_tokens", out var cacheCreationElement) && cacheCreationElement.ValueKind == JsonValueKind.Number)
        {
            usageTracker.AddPromptTokens(cacheCreationElement.GetInt32());
        }

        if (usage.TryGetProperty("cache_read_input_tokens", out var cacheReadElement) && cacheReadElement.ValueKind == JsonValueKind.Number)
        {
            usageTracker.AddCachedTokens(cacheReadElement.GetInt32());
        }
    }

    private static void AddCompletionUsage(JsonElement usage, TokenUsageTracker usageTracker)
    {
        if (usage.TryGetProperty("output_tokens", out var outputTokensElement) && outputTokensElement.ValueKind == JsonValueKind.Number)
        {
            usageTracker.AddCompletionTokens(outputTokensElement.GetInt32());
        }
    }

    private static async Task<bool> ExecuteToolCallsAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat,
        IEnumerable<TFunctionCall> functionCalls,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options,
        CancellationToken cancellationToken)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult, new()
    {
        var shouldContinueConversation = false;
        foreach (var functionCall in functionCalls)
        {
            var function = options.Functions.FirstOrDefault(f => f.Name.NormalizedEquals(functionCall.Name));
            if (function is not null)
            {
                if (function.RequiresDoubleCheck && chat.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionCall.Name)) % 2 != 0)
                {
                    var confirmationMessage = await chat.FromFunctionAsync(new TFunctionResult
                    {
                        ToolCallId = functionCall.ToolCallId,
                        Name = functionCall.Name,
                        Value = "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm."
                    });

                    await options.AddMessageCallback(confirmationMessage);
                    shouldContinueConversation = true;
                    continue;
                }

                if (function.Callback is not null)
                {
                    var functionValue = await function.Callback.InvokeForStringResultAsync(functionCall.Arguments, options.FunctionContext, cancellationToken);
                    var resultMessage = await chat.FromFunctionAsync(new TFunctionResult
                    {
                        ToolCallId = functionCall.ToolCallId,
                        Name = functionCall.Name,
                        Value = functionValue
                    });

                    await options.AddMessageCallback(resultMessage);
                    if (!functionValue.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                    {
                        shouldContinueConversation = true;
                    }
                    continue;
                }

                var fallbackValue = await options.DefaultFunctionCallback(function.Name, functionCall.Arguments, cancellationToken);
                var serializedValue = fallbackValue is string stringValue
                    ? stringValue
                    : JsonSerializer.Serialize(fallbackValue, JsonOptions);

                var fallbackMessage = await chat.FromFunctionAsync(new TFunctionResult
                {
                    ToolCallId = functionCall.ToolCallId,
                    Name = functionCall.Name,
                    Value = serializedValue
                });

                await options.AddMessageCallback(fallbackMessage);
                if (!serializedValue.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                {
                    shouldContinueConversation = true;
                }
                continue;
            }

            var notFoundMessage = await chat.FromFunctionAsync(new TFunctionResult
            {
                ToolCallId = functionCall.ToolCallId,
                Name = functionCall.Name,
                Value = $"Function '{functionCall.Name}' was not found."
            });

            await options.AddMessageCallback(notFoundMessage);
        }

        return shouldContinueConversation;
    }

    private static JsonObject CreateChatCompletionRequest<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        var messages = chat.Messages.ToList();
        var hasExistingToolTranscript = messages.Any(m => m.FunctionCalls.Count > 0 || m.FunctionResult is not null);

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

        var systemSegments = messages
            .Where(message => message.Role == ChatRole.System && !string.IsNullOrWhiteSpace(message.Content))
            .Select(message => message.Content!)
            .ToList();

        var verbosityInstruction = GetVerbosityInstruction(options.Verbosity);
        if (verbosityInstruction is not null)
        {
            systemSegments.Add(verbosityInstruction);
        }

        if (options.IsJsonMode && options.ResponseType is null)
        {
            // Anthropic structured outputs require a concrete closed schema, so generic JSON mode
            // is best expressed as an explicit system instruction instead of output_config.format.
            systemSegments.Add("Respond with a single valid JSON object and no surrounding prose or code fences.");
        }

        var messagesArray = new JsonArray();
        JsonObject? currentMessage = null;
        string? currentRole = null;
        JsonArray? currentContent = null;

        foreach (var message in messages.Where(message => message.Role != ChatRole.System))
        {
            var contentBlocks = CreateContentBlocks<TMessage, TFunctionCall, TFunctionResult>(message);
            if (contentBlocks.Count == 0)
            {
                continue;
            }

            var role = GetRoleName(message.Role);
            var startsWithToolResult = contentBlocks[0] is JsonObject firstBlock
                && string.Equals(firstBlock["type"]?.GetValue<string>(), "tool_result", StringComparison.Ordinal);

            var currentStartsWithToolResult = currentContent is not null
                && currentContent.Count > 0
                && currentContent[0] is JsonObject currentFirstBlock
                && string.Equals(currentFirstBlock["type"]?.GetValue<string>(), "tool_result", StringComparison.Ordinal);

            var shouldStartNewMessage = currentMessage is null
                || currentRole != role
                || startsWithToolResult != currentStartsWithToolResult;

            if (shouldStartNewMessage)
            {
                if (currentMessage is not null)
                {
                    messagesArray.Add(currentMessage);
                }

                currentContent = new JsonArray();
                currentMessage = new JsonObject
                {
                    ["role"] = role,
                    ["content"] = currentContent
                };

                currentRole = role;
            }

            foreach (var contentBlock in contentBlocks)
            {
                currentContent!.Add(contentBlock?.DeepClone());
            }
        }

        if (currentMessage is not null)
        {
            messagesArray.Add(currentMessage);
        }

        if (messagesArray.Count == 0)
        {
            throw new InvalidOperationException("Claude requires at least one non-system message in the conversation.");
        }

        var requestObject = new JsonObject
        {
            ["model"] = options.Model,
            ["messages"] = messagesArray,
            ["max_tokens"] = Math.Max(options.MaxOutputTokens ?? ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>.DefaultMaxOutputTokens, 1)
        };

        var userTrackingId = chat.UserTrackingId ?? options.UserTrackingId;
        if (!string.IsNullOrWhiteSpace(userTrackingId))
        {
            requestObject["metadata"] = new JsonObject
            {
                ["user_id"] = userTrackingId
            };
        }

        if (systemSegments.Count > 0)
        {
            requestObject["system"] = string.Join("\n\n", systemSegments);
        }

        if (options.Temperature.HasValue)
        {
            requestObject["temperature"] = Math.Clamp(options.Temperature.Value, 0.0, 1.0);
        }
        else if (options.TopP.HasValue)
        {
            requestObject["top_p"] = Math.Clamp(options.TopP.Value, 0.0, 1.0);
        }

        if (options.StopWords.Count > 0)
        {
            var stopSequencesArray = new JsonArray();
            foreach (var stopWord in options.StopWords.Where(stopWord => !string.IsNullOrWhiteSpace(stopWord)))
            {
                stopSequencesArray.Add(stopWord);
            }

            if (stopSequencesArray.Count > 0)
            {
                requestObject["stop_sequences"] = stopSequencesArray;
            }
        }

        if (options.Functions.Count > 0)
        {
            var toolsArray = new JsonArray();
            foreach (var function in options.Functions)
            {
                var functionObject = SchemaSerializer.SerializeFunction(function, useOpenAIFeatures: false, isStrictModeOn: false);
                var inputSchema = functionObject["parameters"];
                _ = functionObject.Remove("parameters");

                if (inputSchema is JsonObject inputSchemaObject)
                {
                    SchemaSerializer.SanitizeForClaudeStructuredOutputs(inputSchemaObject);
                    functionObject["input_schema"] = inputSchemaObject;
                }

                if (options.IsStrictFunctionCallingOn)
                {
                    functionObject["strict"] = true;
                }

                toolsArray.Add(functionObject);
            }

            if (toolsArray.Count > 0)
            {
                requestObject["tools"] = toolsArray;
            }

            var shouldForceInitialToolUse = options.IsStrictFunctionCallingOn && !hasExistingToolTranscript;
            if (!options.IsParallelFunctionCallingOn || shouldForceInitialToolUse)
            {
                var toolChoice = new JsonObject
                {
                    // Anthropic's "any" mode forces at least one tool use, which is the closest
                    // provider-native behavior to the library's strict function-calling intent.
                    // Once tool calls/results are already in the transcript, switch back to auto so
                    // Claude can synthesize a final answer instead of being forced into another tool loop.
                    ["type"] = shouldForceInitialToolUse ? "any" : "auto"
                };

                if (!options.IsParallelFunctionCallingOn)
                {
                    toolChoice["disable_parallel_tool_use"] = true;
                }

                requestObject["tool_choice"] = toolChoice;
            }
        }

        var outputConfig = CreateOutputConfig(options);
        if (outputConfig is not null)
        {
            requestObject["output_config"] = outputConfig;
        }

        return requestObject;
    }

    private static JsonObject? CreateOutputConfig<TMessage, TFunctionCall, TFunctionResult>(ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        JsonObject? outputConfig = null;

        var effort = MapReasoningEffort(options.ReasoningEffort, options.Model);
        if (effort is not null)
        {
            outputConfig ??= new JsonObject();
            outputConfig["effort"] = effort;
        }

        if (options.ResponseType is not null)
        {
            outputConfig ??= new JsonObject();
            outputConfig["format"] = SchemaSerializer.SerializeClaudeResponseFormat(options.ResponseType);
        }

        return outputConfig;
    }

    private static string? MapReasoningEffort(ReasoningEffort effort, string model)
    {
        if (effort == ReasoningEffort.None || !SupportsEffort(model))
        {
            return null;
        }

        return effort switch
        {
            ReasoningEffort.Minimal => "low",
            ReasoningEffort.Low => "low",
            ReasoningEffort.Medium => "medium",
            ReasoningEffort.High => "high",
            _ => null
        };
    }

    private static bool SupportsEffort(string model)
    {
        return model.StartsWith("claude-opus-4-6", StringComparison.OrdinalIgnoreCase)
            || model.StartsWith("claude-sonnet-4-6", StringComparison.OrdinalIgnoreCase)
            || model.StartsWith("claude-opus-4-5", StringComparison.OrdinalIgnoreCase)
            || model.StartsWith("claude-sonnet-4-5", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetVerbosityInstruction(Verbosity verbosity)
    {
        return verbosity switch
        {
            Verbosity.Low => "Keep responses concise unless the user explicitly asks for depth.",
            Verbosity.High => "Provide detailed and thorough responses unless the user explicitly asks for brevity.",
            _ => null
        };
    }

    private static JsonArray CreateContentBlocks<TMessage, TFunctionCall, TFunctionResult>(TMessage message)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        var contentBlocks = new JsonArray();

        if (message.FunctionResult is not null && !string.IsNullOrWhiteSpace(message.FunctionResult.Name))
        {
            var toolResultObject = new JsonObject
            {
                ["type"] = "tool_result",
                ["tool_use_id"] = message.FunctionResult.ToolCallId,
                ["content"] = message.FunctionResult.Value
            };

            if (message.FunctionResult.Value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
            {
                toolResultObject["is_error"] = true;
            }

            contentBlocks.Add(toolResultObject);
            return contentBlocks;
        }

        if (message.Role == ChatRole.User)
        {
            foreach (var imageUrl in message.ImageUrls)
            {
                var imageObject = new JsonObject
                {
                    ["type"] = "image",
                    ["source"] = new JsonObject
                    {
                        ["type"] = "url",
                        ["url"] = imageUrl
                    }
                };

                contentBlocks.Add(imageObject);
            }
        }

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            contentBlocks.Add(new JsonObject
            {
                ["type"] = "text",
                ["text"] = message.Content
            });
        }

        foreach (var functionCall in message.FunctionCalls)
        {
            contentBlocks.Add(new JsonObject
            {
                ["type"] = "tool_use",
                ["id"] = functionCall.ToolCallId ?? $"toolu_{Guid.NewGuid():N}",
                ["name"] = functionCall.Name.ToSnakeLower(),
                ["input"] = ParseToolInput(functionCall.Arguments, functionCall.Name)
            });
        }

        return contentBlocks;
    }

    private static JsonNode ParseToolInput(string? arguments, string functionName)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(arguments) ?? new JsonObject();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Function arguments for '{functionName}' were not valid JSON.", exception);
        }
    }

    private static string GetRoleName(ChatRole role)
    {
        return role switch
        {
            ChatRole.System => "user",
            ChatRole.User => "user",
            ChatRole.Chatbot => "assistant",
            ChatRole.Function => "user",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid role")
        };
    }

    private static void ThrowForEmptyResponse(string? stopReason, bool isStreaming)
    {
        if (!string.IsNullOrWhiteSpace(stopReason))
        {
            var responseKind = isStreaming ? "streamed assistant content" : "assistant content";
            throw new InvalidOperationException($"Claude returned no {responseKind}. Stop reason: {stopReason}.");
        }

        if (isStreaming)
        {
            throw new InvalidOperationException("Claude returned an empty streamed assistant message.");
        }

        throw new InvalidOperationException("Claude returned an empty assistant message.");
    }

    private sealed record StreamingToolCall(string Id, string Name, StringBuilder Arguments);
}
