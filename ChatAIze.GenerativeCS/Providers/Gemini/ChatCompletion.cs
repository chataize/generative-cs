using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Options.Gemini;
using ChatAIze.GenerativeCS.Utilities;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.GenerativeCS.Providers.Gemini;

/// <summary>
/// Handles Gemini chat completion requests and function calling flows.
/// </summary>
public static class ChatCompletion
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
    /// Executes a text-only completion request using Gemini.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="prompt">User prompt text.</param>
    /// <param name="apiKey">API key used for the request.</param>
    /// <param name="options">Optional completion options.</param>
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Generated response text.</returns>
    internal static async Task<string> CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(string prompt, string? apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };
        
        options ??= new();

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        if (options.Functions.Count >= 1)
        {
            var chat = new TChat();
            _ = await chat.FromUserAsync(prompt);

            return await CompleteAsync(chat, apiKey, options, httpClient, cancellationToken);
        }

        var request = CreateCompletionRequest(prompt);
        if (options.IsDebugMode)
        {
            Console.WriteLine(request.ToString());
        }

        using var response = await httpClient.RepeatPostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{options.Model}:generateContent?key={apiKey}", request, null, options.MaxAttempts, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (options.IsDebugMode)
        {
            Console.WriteLine(responseDocument.RootElement.ToString());
        }

        var generatedMessage = responseDocument.RootElement.GetProperty("candidates")[0];
        var messageContent = generatedMessage.GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()!;

        return messageContent;
    }

    /// <summary>
    /// Executes a chat completion request based on the provided transcript.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="chat">Chat transcript to send.</param>
    /// <param name="apiKey">API key used for the request.</param>
    /// <param name="options">Optional completion options.</param>
    /// <param name="httpClient">HTTP client to use.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Generated response text.</returns>
    internal static async Task<string> CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(TChat chat, string? apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        options ??= new();

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        var request = CreateChatCompletionRequest(chat, options);
        if (options.IsDebugMode)
        {
            Console.WriteLine(request.ToString());
        }

        using var response = await httpClient.RepeatPostAsJsonAsync($"https://generativelanguage.googleapis.com/v1beta/models/{options.Model}:generateContent?key={apiKey}", request, null, options.MaxAttempts, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (options.IsDebugMode)
        {
            Console.WriteLine(responseDocument.RootElement.ToString());
        }

        var responseParts = responseDocument.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts");

        string messageContent = null!;
        foreach (var part in responseParts.EnumerateArray())
        {
            if (part.TryGetProperty("functionCall", out var functionCallElement) && functionCallElement.TryGetProperty("name", out var functionNameElement))
            {
                var functionName = functionNameElement.GetString()!;
                var functionArguments = functionCallElement.GetProperty("args").GetRawText()!;

                var message1 = await chat.FromChatbotAsync(new TFunctionCall { Name = functionName, Arguments = functionArguments });
                await options.AddMessageCallback(message1);

                var function = options.Functions.FirstOrDefault(f => f.Name.NormalizedEquals(functionName));
                if (function is not null)
                {
                    if (function.RequiresDoubleCheck && chat.Messages.Count(m => m.FunctionCalls.Any(c => c.Name == functionName)) % 2 != 0)
                    {
                        var message2 = await chat.FromFunctionAsync(new TFunctionResult { Name = functionName, Value = "Before executing, are you sure the user wants to run this function? If yes, call it again to confirm." });
                        await options.AddMessageCallback(message2);
                    }
                    else
                    {
                        if (function.Callback is not null)
                        {
                            var functionValue = await function.Callback.InvokeForStringResultAsync(functionArguments, options.FunctionContext, cancellationToken);
                            var message3 = await chat.FromFunctionAsync(new TFunctionResult { Name = functionName, Value = functionValue });

                            await options.AddMessageCallback(message3);
                        }
                        else
                        {
                            var functionValue = await options.DefaultFunctionCallback(functionName, functionArguments, cancellationToken);
                            var message4 = await chat.FromFunctionAsync(new TFunctionResult { Name = functionName, Value = JsonSerializer.Serialize(functionValue, JsonOptions) });

                            await options.AddMessageCallback(message4);
                        }
                    }
                }
                else
                {
                    var message5 = await chat.FromFunctionAsync(new TFunctionResult { Name = functionName, Value = $"Function '{functionName}' was not found." });
                    await options.AddMessageCallback(message5);
                }

                return await CompleteAsync(chat, apiKey, options, httpClient, cancellationToken);
            }
            else if (part.TryGetProperty("text", out var textElement))
            {
                messageContent = textElement.GetString()!;

                var message6 = await chat.FromChatbotAsync(messageContent);
                await options.AddMessageCallback(message6);
            }
            else
            {
                var message7 = await chat.FromFunctionAsync(new TFunctionResult { Name = "Error", Value = "Either call a function or respond with text." });
                await options.AddMessageCallback(message7);

                return await CompleteAsync(chat, apiKey, options, httpClient, cancellationToken);
            }
        }

        return messageContent;
    }

    /// <summary>
    /// Builds the JSON payload for a simple completion request.
    /// </summary>
    /// <param name="prompt">User prompt text.</param>
    /// <returns>JSON request payload.</returns>
    private static JsonObject CreateCompletionRequest(string prompt)
    {
        var partObject = new JsonObject
        {
            ["text"] = prompt
        };

        var partsArray = new JsonArray
        {
            partObject
        };

        var contentObject = new JsonObject
        {
            ["parts"] = partsArray
        };

        var contentsArray = new JsonArray
        {
            contentObject
        };

        var requestObject = new JsonObject
        {
            ["contents"] = contentsArray
        };

        return requestObject;
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
    private static JsonObject CreateChatCompletionRequest<TChat, TMessage, TFunctionCall, TFunctionResult>(TChat chat, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> options)
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
        MessageTools.LimitTokens<TMessage, TFunctionCall, TFunctionResult>(messages, options.MessageLimit, options.CharacterLimit);
        MessageTools.ReplaceSystemRole<TMessage, TFunctionCall, TFunctionResult>(messages);
        MessageTools.MergeMessages<TMessage, TFunctionCall, TFunctionResult>(messages);

        var contentsArray = new JsonArray();
        foreach (var message in messages)
        {
            var partObject = new JsonObject();
            var functionCall = message.FunctionCalls.FirstOrDefault();

            if (functionCall is not null)
            {
                var functionCallObject = new JsonObject
                {
                    ["name"] = functionCall.Name
                };

                if (functionCall.Arguments is not null)
                {
                    functionCallObject["args"] = JsonNode.Parse(functionCall.Arguments)!.AsObject();
                }

                partObject["functionCall"] = functionCallObject;
            }
            else if (message.FunctionResult is not null && !string.IsNullOrWhiteSpace(message.FunctionResult.Name))
            {
                var responseObject = new JsonObject
                {
                    ["name"] = message.FunctionResult.Name,
                    ["content"] = JsonSerializer.SerializeToNode(message.FunctionResult.Value, JsonOptions)
                };

                var functionResponseObject = new JsonObject
                {
                    ["name"] = message.FunctionResult.Name,
                    ["response"] = responseObject
                };

                partObject["functionResponse"] = functionResponseObject;
            }
            else
            {
                partObject["text"] = message.Content;
            }

            var partsArray = new JsonArray
            {
                partObject
            };

            var contentObject = new JsonObject
            {
                ["role"] = GetRoleName(message.Role),
                ["parts"] = partsArray
            };

            contentsArray.Add(contentObject);
        }

        var requestObject = new JsonObject
        {
            ["contents"] = contentsArray
        };

        if (options.Functions.Count > 0)
        {
            var functionsArray = new JsonArray();
            foreach (var function in options.Functions)
            {
                functionsArray.Add(SchemaSerializer.SerializeFunction(function, useOpenAIFeatures: false, isStrictModeOn: false));
            }

            var functionsObject = new JsonObject
            {
                ["function_declarations"] = functionsArray
            };

            var toolsArray = new JsonArray
            {
                functionsObject
            };

            requestObject["tools"] = toolsArray;
        }

        return requestObject;
    }

    /// <summary>
    /// Converts a chat role into the provider-specific role name.
    /// </summary>
    /// <param name="role">Role to convert.</param>
    /// <returns>Provider-specific role string.</returns>
    private static string GetRoleName(ChatRole role)
    {
        return role switch
        {
            ChatRole.System => "user",
            ChatRole.User => "user",
            ChatRole.Chatbot => "model",
            ChatRole.Function => "tool",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Invalid role")
        };
    }
}
