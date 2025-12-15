using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.Gemini;

/// <summary>
/// Groups default settings for the <see cref="Clients.GeminiClient{TChat, TMessage, TFunctionCall, TFunctionResult}"/>.
/// </summary>
/// <typeparam name="TMessage">Message type used in the chat.</typeparam>
/// <typeparam name="TFunctionCall">Function call type used in the chat.</typeparam>
/// <typeparam name="TFunctionResult">Function result type used in the chat.</typeparam>
public record GeminiClientOptions<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    /// <summary>
    /// Initializes client options with defaults.
    /// </summary>
    public GeminiClientOptions() { }

    /// <summary>
    /// Initializes client options with a specific API key.
    /// </summary>
    /// <param name="apiKey">Gemini API key.</param>
    public GeminiClientOptions(string? apiKey)
    {
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the API key applied to requests when not overridden per call.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the default chat completion options.
    /// </summary>
    public ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> DefaultCompletionOptions { get; set; } = new();
}

/// <summary>
/// Non-generic Gemini client options using the built-in chat models.
/// </summary>
public record GeminiClientOptions : GeminiClientOptions<ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes client options with defaults.
    /// </summary>
    public GeminiClientOptions() : base() { }

    /// <summary>
    /// Initializes client options with a specific API key.
    /// </summary>
    /// <param name="apiKey">Gemini API key.</param>
    public GeminiClientOptions(string? apiKey) : base(apiKey) { }
}
