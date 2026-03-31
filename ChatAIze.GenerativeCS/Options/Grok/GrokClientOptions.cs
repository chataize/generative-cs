using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.Grok;

/// <summary>
/// Groups default settings for the <see cref="Clients.GrokClient{TChat, TMessage, TFunctionCall, TFunctionResult}"/>.
/// </summary>
/// <typeparam name="TMessage">Message type used in the chat.</typeparam>
/// <typeparam name="TFunctionCall">Function call type used in the chat.</typeparam>
/// <typeparam name="TFunctionResult">Function result type used in the chat.</typeparam>
public record GrokClientOptions<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    /// <summary>
    /// Initializes client options with defaults.
    /// </summary>
    public GrokClientOptions() { }

    /// <summary>
    /// Initializes client options with a specific API key.
    /// </summary>
    /// <param name="apiKey">Grok API key.</param>
    public GrokClientOptions(string? apiKey)
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

    /// <summary>
    /// Gets or sets the default text-to-speech options.
    /// </summary>
    public TextToSpeechOptions DefaultTextToSpeechOptions { get; set; } = new();
}

/// <summary>
/// Non-generic Grok client options using the built-in chat models.
/// </summary>
public record GrokClientOptions : GrokClientOptions<ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes client options with defaults.
    /// </summary>
    public GrokClientOptions() : base() { }

    /// <summary>
    /// Initializes client options with a specific API key.
    /// </summary>
    /// <param name="apiKey">Grok API key.</param>
    public GrokClientOptions(string? apiKey) : base(apiKey) { }
}
