using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.Claude;

/// <summary>
/// Claude chat completion options that intentionally inherit the OpenAI-shaped option surface so callers can switch providers with minimal code changes.
/// </summary>
/// <typeparam name="TMessage">Message type used in the chat.</typeparam>
/// <typeparam name="TFunctionCall">Function call type used in the chat.</typeparam>
/// <typeparam name="TFunctionResult">Function result type used in the chat.</typeparam>
public record ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>
    : OpenAI.ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    /// <summary>
    /// Default maximum output tokens used because Anthropic requires <c>max_tokens</c> on every request.
    /// </summary>
    public const int DefaultMaxOutputTokens = 4096;

    /// <summary>
    /// Initializes Claude completion options.
    /// </summary>
    /// <param name="model">Model identifier to target.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public ChatCompletionOptions(string model = DefaultModels.Claude.ChatCompletion, string? apiKey = null)
        : base(model, apiKey)
    {
        MaxOutputTokens ??= DefaultMaxOutputTokens;
    }
}

/// <summary>
/// Non-generic Claude completion options using the built-in chat models.
/// </summary>
public record ChatCompletionOptions : ChatCompletionOptions<Models.ChatMessage, Models.FunctionCall, Models.FunctionResult>
{
    /// <summary>
    /// Initializes Claude completion options.
    /// </summary>
    /// <param name="model">Model identifier to target.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public ChatCompletionOptions(string model = DefaultModels.Claude.ChatCompletion, string? apiKey = null)
        : base(model, apiKey) { }
}
