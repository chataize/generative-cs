using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Options.Gemini;
using ChatAIze.GenerativeCS.Utilities;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.GenerativeCS.Providers.Gemini;

/// <summary>
/// Handles Gemini content generation requests, including streaming, image inputs, structured outputs, and function calling.
/// </summary>
internal static class ChatCompletion
{
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    internal static async Task<string> CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        string prompt,
        string? apiKey,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        var chat = new TChat();
        _ = await chat.FromUserAsync(prompt);

        return await CompleteAsync(chat, apiKey, options, usageTracker, httpClient, cancellationToken: cancellationToken);
    }

    internal static async Task<string> CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat,
        string? apiKey,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null,
        HttpClient? httpClient = null,
        int recursion = 0,
        CancellationToken cancellationToken = default,
        bool forceStructuredFollowUp = false)
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
            throw new InvalidOperationException("Gemini API key was not provided.");
        }

        var request = await CreateChatCompletionRequestAsync(chat, options, httpClient, cancellationToken, forceStructuredFollowUp);
        if (options.IsDebugMode)
        {
            Console.WriteLine(request.ToJsonString(JsonOptions));
        }

        using var response = await GeminiHttp.SendGenerateContentRequestAsync(httpClient, options.Model, request, apiKey, options.MaxAttempts, isStreaming: false, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (options.IsDebugMode)
        {
            Console.WriteLine(responseDocument.RootElement.ToString());
        }

        HandlePromptFeedback(responseDocument.RootElement);
        AddUsage(responseDocument.RootElement, usageTracker);

        var candidate = GetPrimaryCandidate(responseDocument.RootElement);
        var finishReason = candidate.TryGetProperty("finishReason", out var finishReasonElement)
            ? finishReasonElement.GetString()
            : null;

        var textBuilder = new StringBuilder();
        var textParts = new JsonArray();
        var functionParts = new JsonArray();
        var toolCalls = new List<TFunctionCall>();

        if (candidate.TryGetProperty("content", out var contentElement)
            && contentElement.TryGetProperty("parts", out var partsElement))
        {
            foreach (var partElement in partsElement.EnumerateArray())
            {
                if (partElement.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _ = textBuilder.Append(text);
                        textParts.Add(CloneNode(partElement));
                    }

                    continue;
                }

                if (partElement.TryGetProperty("functionCall", out var functionCallElement)
                    && functionCallElement.TryGetProperty("name", out var functionNameElement))
                {
                    toolCalls.Add(new TFunctionCall
                    {
                        ToolCallId = functionCallElement.TryGetProperty("id", out var toolCallIdElement)
                            ? toolCallIdElement.GetString()
                            : $"gemini_call_{Guid.NewGuid():N}",
                        Name = functionNameElement.GetString()!,
                        Arguments = functionCallElement.TryGetProperty("args", out var argsElement)
                            ? argsElement.GetRawText()
                            : "{}"
                    });

                    functionParts.Add(CloneNode(partElement));
                }
            }
        }

        if (toolCalls.Count > 0 && string.Equals(finishReason, "MAX_TOKENS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Gemini stopped while producing function input. Increase MaxOutputTokens and retry.");
        }

        if (toolCalls.Count > 0 && string.Equals(finishReason, "MALFORMED_FUNCTION_CALL", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Gemini produced a malformed function call. Simplify the schema or retry with a less constrained prompt.");
        }

        var initialText = textBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(initialText))
        {
            var textMessage = await chat.FromChatbotAsync(initialText);
            GeminiMessagePartStore.Set(textMessage, textParts);
            await options.AddMessageCallback(textMessage);
        }

        if (toolCalls.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(initialText))
            {
                ThrowForEmptyCandidate(candidate);
            }

            return initialText;
        }

        var toolCallMessage = await chat.FromChatbotAsync(toolCalls);
        GeminiMessagePartStore.Set(toolCallMessage, functionParts);
        await options.AddMessageCallback(toolCallMessage);

        var shouldContinueConversation = await ExecuteToolCallsAsync(chat, toolCalls, options, cancellationToken);
        if (!shouldContinueConversation)
        {
            var fallback = await chat.FromChatbotAsync("No tool call succeeded; provide required parameters or respond directly.");
            await options.AddMessageCallback(fallback);
            return initialText + (fallback.Content ?? string.Empty);
        }

        var continuation = await CompleteAsync(
            chat,
            apiKey,
            options,
            usageTracker,
            httpClient,
            recursion + 1,
            cancellationToken,
            forceStructuredFollowUp || options.ResponseType is not null || options.IsJsonMode);

        return initialText + continuation;
    }

    internal static async IAsyncEnumerable<string> StreamCompletionAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat,
        string? apiKey,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null,
        HttpClient? httpClient = null,
        int recursion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        bool forceStructuredFollowUp = false)
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
            throw new InvalidOperationException("Gemini API key was not provided.");
        }

        var request = await CreateChatCompletionRequestAsync(chat, options, httpClient, cancellationToken, forceStructuredFollowUp);
        if (options.IsDebugMode)
        {
            Console.WriteLine(request.ToJsonString(JsonOptions));
        }

        using var response = await GeminiHttp.SendGenerateContentRequestAsync(httpClient, options.Model, request, apiKey, options.MaxAttempts, isStreaming: true, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseReader = new StreamReader(responseContent);

        var functionCallKeys = new HashSet<string>(StringComparer.Ordinal);
        var textBuilder = new StringBuilder();
        var textParts = new JsonArray();
        var functionParts = new JsonArray();
        var toolCalls = new List<TFunctionCall>();

        int? promptTokens = null;
        int cachedTokens = 0;
        int? completionTokens = null;
        string? lastFinishReason = null;

        string? line;
        while ((line = await responseReader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line["data: ".Length..].Trim();
            if (string.IsNullOrWhiteSpace(payload) || string.Equals(payload, "[DONE]", StringComparison.Ordinal))
            {
                continue;
            }

            using var chunkDocument = JsonDocument.Parse(payload);
            var root = chunkDocument.RootElement;

            if (options.IsDebugMode)
            {
                Console.WriteLine(root.ToString());
            }

            HandlePromptFeedback(root);

            if (root.TryGetProperty("usageMetadata", out var usageElement))
            {
                if (usageElement.TryGetProperty("promptTokenCount", out var promptTokensElement))
                {
                    promptTokens = promptTokensElement.GetInt32();
                }

                if (usageElement.TryGetProperty("cachedContentTokenCount", out var cachedTokensElement))
                {
                    cachedTokens = cachedTokensElement.GetInt32();
                }

                if (usageElement.TryGetProperty("candidatesTokenCount", out var completionTokensElement))
                {
                    completionTokens = completionTokensElement.GetInt32();
                }
            }

            if (!root.TryGetProperty("candidates", out var candidatesElement) || candidatesElement.GetArrayLength() == 0)
            {
                continue;
            }

            var candidate = candidatesElement[0];
            if (candidate.TryGetProperty("finishReason", out var finishReasonElement))
            {
                lastFinishReason = finishReasonElement.GetString();
            }

            if (!candidate.TryGetProperty("content", out var contentElement)
                || !contentElement.TryGetProperty("parts", out var partsElement))
            {
                continue;
            }

            foreach (var partElement in partsElement.EnumerateArray())
            {
                if (partElement.TryGetProperty("text", out var textElement))
                {
                    var text = textElement.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _ = textBuilder.Append(text);
                        textParts.Add(CloneNode(partElement));
                        yield return text;
                    }

                    continue;
                }

                if (partElement.TryGetProperty("functionCall", out var functionCallElement)
                    && functionCallElement.TryGetProperty("name", out var functionNameElement))
                {
                    var rawPart = partElement.GetRawText();
                    if (!functionCallKeys.Add(rawPart))
                    {
                        continue;
                    }

                    toolCalls.Add(new TFunctionCall
                    {
                        ToolCallId = functionCallElement.TryGetProperty("id", out var toolCallIdElement)
                            ? toolCallIdElement.GetString()
                            : $"gemini_call_{Guid.NewGuid():N}",
                        Name = functionNameElement.GetString()!,
                        Arguments = functionCallElement.TryGetProperty("args", out var argsElement)
                            ? argsElement.GetRawText()
                            : "{}"
                    });

                    functionParts.Add(CloneNode(partElement));
                }
            }
        }

        if (usageTracker is not null)
        {
            if (promptTokens.HasValue)
            {
                usageTracker.AddPromptTokens(promptTokens.Value);
            }

            usageTracker.AddCachedTokens(cachedTokens);

            if (completionTokens.HasValue)
            {
                usageTracker.AddCompletionTokens(completionTokens.Value);
            }
        }

        if (toolCalls.Count > 0 && string.Equals(lastFinishReason, "MAX_TOKENS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Gemini stopped while streaming function input. Increase MaxOutputTokens and retry.");
        }

        if (toolCalls.Count > 0 && string.Equals(lastFinishReason, "MALFORMED_FUNCTION_CALL", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Gemini produced a malformed function call while streaming.");
        }

        if (textBuilder.Length > 0)
        {
            var textMessage = await chat.FromChatbotAsync(textBuilder.ToString());
            GeminiMessagePartStore.Set(textMessage, textParts);
            await options.AddMessageCallback(textMessage);
        }

        if (toolCalls.Count == 0)
        {
            if (textBuilder.Length == 0)
            {
                ThrowForEmptyStreamCandidate(lastFinishReason);
            }

            yield break;
        }

        var toolCallMessage = await chat.FromChatbotAsync(toolCalls);
        GeminiMessagePartStore.Set(toolCallMessage, functionParts);
        await options.AddMessageCallback(toolCallMessage);

        var shouldContinueConversation = await ExecuteToolCallsAsync(chat, toolCalls, options, cancellationToken);
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

        await foreach (var continuationChunk in StreamCompletionAsync(
            chat,
            apiKey,
            options,
            usageTracker,
            httpClient,
            recursion + 1,
            cancellationToken,
            forceStructuredFollowUp || options.ResponseType is not null || options.IsJsonMode))
        {
            yield return continuationChunk;
        }
    }

    private static async Task<bool> ExecuteToolCallsAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat,
        IReadOnlyCollection<TFunctionCall> toolCalls,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options,
        CancellationToken cancellationToken)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        var shouldContinueConversation = false;
        foreach (var toolCall in toolCalls)
        {
            var function = options.Functions.FirstOrDefault(f => f.Name.NormalizedEquals(toolCall.Name));
            if (function is null)
            {
                var notFoundMessage = await chat.FromFunctionAsync(new TFunctionResult
                {
                    ToolCallId = toolCall.ToolCallId,
                    Name = toolCall.Name,
                    Value = $"Function '{toolCall.Name}' was not found."
                });

                await options.AddMessageCallback(notFoundMessage);
                continue;
            }

            if (function.RequiresDoubleCheck && chat.Messages.Count(m => m.FunctionCalls.Any(c => c.Name.NormalizedEquals(toolCall.Name))) % 2 != 0)
            {
                var confirmationMessage = await chat.FromFunctionAsync(new TFunctionResult
                {
                    ToolCallId = toolCall.ToolCallId,
                    Name = toolCall.Name,
                    Value = "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm."
                });

                await options.AddMessageCallback(confirmationMessage);
                shouldContinueConversation = true;
                continue;
            }

            object? functionValue;
            if (function.Callback is not null)
            {
                functionValue = await function.Callback.InvokeForStringResultAsync(toolCall.Arguments, options.FunctionContext, cancellationToken);
            }
            else
            {
                functionValue = await options.DefaultFunctionCallback(function.Name, toolCall.Arguments, cancellationToken);
            }

            var serializedValue = functionValue switch
            {
                null => "null",
                string stringValue => stringValue,
                _ => JsonSerializer.Serialize(functionValue, JsonOptions)
            };

            var resultMessage = await chat.FromFunctionAsync(new TFunctionResult
            {
                ToolCallId = toolCall.ToolCallId,
                Name = toolCall.Name,
                Value = serializedValue
            });

            await options.AddMessageCallback(resultMessage);
            if (!serializedValue.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
            {
                shouldContinueConversation = true;
            }
        }

        return shouldContinueConversation;
    }

    private static async Task<JsonObject> CreateChatCompletionRequestAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options,
        HttpClient httpClient,
        CancellationToken cancellationToken,
        bool forceStructuredFollowUp)
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

        var systemInstruction = string.Join(
            "\n\n",
            messages
                .Where(m => m.Role == ChatRole.System && !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => m.Content!.Trim()));

        var contentsArray = new JsonArray();
        foreach (var message in messages.Where(m => m.Role != ChatRole.System))
        {
            var parts = await CreatePartsAsync<TMessage, TFunctionCall, TFunctionResult>(message, httpClient, cancellationToken);
            if (parts.Count == 0)
            {
                continue;
            }

            contentsArray.Add(new JsonObject
            {
                ["role"] = GetRoleName(message.Role),
                ["parts"] = parts
            });
        }

        var requestObject = new JsonObject
        {
            ["contents"] = contentsArray
        };

        if (!string.IsNullOrWhiteSpace(systemInstruction))
        {
            requestObject["system_instruction"] = new JsonObject
            {
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["text"] = systemInstruction
                    }
                }
            };
        }

        var shouldIncludeTools = options.Functions.Count > 0 && !forceStructuredFollowUp;
        if (shouldIncludeTools)
        {
            var functionDeclarations = new JsonArray();
            foreach (var function in options.Functions)
            {
                functionDeclarations.Add(SchemaSerializer.SerializeFunction(function, useOpenAIFeatures: false, isStrictModeOn: false));
            }

            requestObject["tools"] = new JsonArray
            {
                new JsonObject
                {
                    ["function_declarations"] = functionDeclarations
                }
            };

            requestObject["tool_config"] = new JsonObject
            {
                ["function_calling_config"] = new JsonObject
                {
                    ["mode"] = options.IsStrictFunctionCallingOn ? "VALIDATED" : "AUTO"
                }
            };
        }

        var generationConfig = CreateGenerationConfig(options, forceStructuredFollowUp || options.Functions.Count == 0);
        if (generationConfig.Count > 0)
        {
            requestObject["generation_config"] = generationConfig;
        }

        if (options.IsStoringOutputs)
        {
            requestObject["store"] = true;
        }

        return requestObject;
    }

    private static JsonObject CreateGenerationConfig<TMessage, TFunctionCall, TFunctionResult>(
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options,
        bool canUseStructuredOutputs)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        var generationConfig = new JsonObject();

        if (options.MaxOutputTokens.HasValue)
        {
            generationConfig["max_output_tokens"] = Math.Max(options.MaxOutputTokens.Value, 1);
        }

        if (options.Temperature.HasValue)
        {
            generationConfig["temperature"] = options.Temperature.Value;
        }

        if (options.TopP.HasValue)
        {
            generationConfig["top_p"] = options.TopP.Value;
        }

        if (options.TopK.HasValue)
        {
            generationConfig["top_k"] = options.TopK.Value;
        }

        if (options.StopWords.Count > 0)
        {
            var stopSequences = new JsonArray();
            foreach (var stopWord in options.StopWords)
            {
                stopSequences.Add(stopWord);
            }

            generationConfig["stop_sequences"] = stopSequences;
        }

        var thinkingConfig = CreateThinkingConfig(options);
        if (thinkingConfig is not null)
        {
            generationConfig["thinking_config"] = thinkingConfig;
        }

        if (canUseStructuredOutputs && options.ResponseType is not null)
        {
            generationConfig["response_mime_type"] = "application/json";
            generationConfig["response_json_schema"] = SchemaSerializer.SerializeGeminiResponseSchema(options.ResponseType);
        }
        else if (canUseStructuredOutputs && options.IsJsonMode)
        {
            generationConfig["response_mime_type"] = "application/json";
        }

        return generationConfig;
    }

    private static JsonObject? CreateThinkingConfig<TMessage, TFunctionCall, TFunctionResult>(ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        var thinkingConfig = new JsonObject();
        var thinkingLevel = options.ThinkingLevel ?? options.ReasoningEffort switch
        {
            ReasoningEffort.Low => "low",
            ReasoningEffort.Medium => "medium",
            ReasoningEffort.High => "high",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(thinkingLevel))
        {
            thinkingConfig["thinking_level"] = thinkingLevel;
        }

        if (options.ThinkingBudget.HasValue)
        {
            thinkingConfig["thinking_budget"] = options.ThinkingBudget.Value;
        }

        return thinkingConfig.Count == 0 ? null : thinkingConfig;
    }

    private static async Task<JsonArray> CreatePartsAsync<TMessage, TFunctionCall, TFunctionResult>(
        TMessage message,
        HttpClient httpClient,
        CancellationToken cancellationToken)
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
        where TFunctionCall : IFunctionCall
        where TFunctionResult : IFunctionResult
    {
        if (message.Role == ChatRole.Chatbot && GeminiMessagePartStore.TryGet(message, out var storedParts))
        {
            return storedParts;
        }

        var parts = new JsonArray();

        if (message.FunctionCalls.Count > 0)
        {
            foreach (var functionCall in message.FunctionCalls)
            {
                var functionCallObject = new JsonObject
                {
                    ["name"] = functionCall.Name
                };

                if (!string.IsNullOrWhiteSpace(functionCall.Arguments))
                {
                    functionCallObject["args"] = ParseJsonOrString(functionCall.Arguments);
                }

                parts.Add(new JsonObject
                {
                    ["functionCall"] = functionCallObject
                });
            }

            return parts;
        }

        if (message.FunctionResult is not null && !string.IsNullOrWhiteSpace(message.FunctionResult.Name))
        {
            parts.Add(new JsonObject
            {
                ["functionResponse"] = new JsonObject
                {
                    ["name"] = message.FunctionResult.Name,
                    ["response"] = new JsonObject
                    {
                        ["name"] = message.FunctionResult.Name,
                        ["content"] = ParseJsonOrString(message.FunctionResult.Value)
                    }
                }
            });

            return parts;
        }

        if (!string.IsNullOrWhiteSpace(message.Content))
        {
            parts.Add(new JsonObject
            {
                ["text"] = message.Content
            });
        }

        foreach (var imageUrl in message.ImageUrls)
        {
            parts.Add(new JsonObject
            {
                ["inline_data"] = await GeminiHttp.DownloadRemoteFileAsInlineDataAsync(httpClient, imageUrl, cancellationToken)
            });
        }

        return parts;
    }

    private static void HandlePromptFeedback(JsonElement root)
    {
        if (!root.TryGetProperty("promptFeedback", out var promptFeedbackElement)
            || !promptFeedbackElement.TryGetProperty("blockReason", out var blockReasonElement))
        {
            return;
        }

        var blockReason = blockReasonElement.GetString();
        if (!string.IsNullOrWhiteSpace(blockReason))
        {
            throw new InvalidOperationException($"Gemini blocked the request prompt. Block reason: {blockReason}.");
        }
    }

    private static JsonElement GetPrimaryCandidate(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidatesElement) || candidatesElement.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini returned no candidates.");
        }

        return candidatesElement[0];
    }

    private static void ThrowForEmptyCandidate(JsonElement candidate)
    {
        var finishReason = candidate.TryGetProperty("finishReason", out var finishReasonElement)
            ? finishReasonElement.GetString()
            : null;

        if (!string.IsNullOrWhiteSpace(finishReason))
        {
            throw new InvalidOperationException($"Gemini returned no text content. Finish reason: {finishReason}.");
        }

        throw new InvalidOperationException("Gemini returned an empty candidate.");
    }

    private static void ThrowForEmptyStreamCandidate(string? finishReason)
    {
        if (!string.IsNullOrWhiteSpace(finishReason))
        {
            throw new InvalidOperationException($"Gemini returned no streamed content. Finish reason: {finishReason}.");
        }

        throw new InvalidOperationException("Gemini returned an empty streamed candidate.");
    }

    private static void AddUsage(JsonElement root, TokenUsageTracker? usageTracker)
    {
        if (usageTracker is null || !root.TryGetProperty("usageMetadata", out var usageElement))
        {
            return;
        }

        if (usageElement.TryGetProperty("promptTokenCount", out var promptTokensElement))
        {
            usageTracker.AddPromptTokens(promptTokensElement.GetInt32());
        }

        if (usageElement.TryGetProperty("cachedContentTokenCount", out var cachedTokensElement))
        {
            usageTracker.AddCachedTokens(cachedTokensElement.GetInt32());
        }

        if (usageElement.TryGetProperty("candidatesTokenCount", out var completionTokensElement))
        {
            usageTracker.AddCompletionTokens(completionTokensElement.GetInt32());
        }
    }

    private static string GetRoleName(ChatRole role)
    {
        return role switch
        {
            ChatRole.User => "user",
            ChatRole.Chatbot => "model",
            ChatRole.Function => "tool",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported Gemini role.")
        };
    }

    private static JsonNode ParseJsonOrString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return JsonValue.Create(string.Empty)!;
        }

        try
        {
            return JsonNode.Parse(value)!;
        }
        catch (JsonException)
        {
            return JsonValue.Create(value)!;
        }
    }

    private static JsonNode CloneNode(JsonElement element)
    {
        return JsonNode.Parse(element.GetRawText())!;
    }
}
