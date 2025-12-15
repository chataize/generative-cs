using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

/// <summary>
/// Groups default settings for the <see cref="Clients.OpenAIClient{TChat, TMessage, TFunctionCall, TFunctionResult}"/>.
/// </summary>
/// <typeparam name="TMessage">Message type used in the chat.</typeparam>
/// <typeparam name="TFunctionCall">Function call type used in the chat.</typeparam>
/// <typeparam name="TFunctionResult">Function result type used in the chat.</typeparam>
public record OpenAIClientOptions<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    /// <summary>
    /// Initializes client options with defaults.
    /// </summary>
    public OpenAIClientOptions() { }

    /// <summary>
    /// Initializes client options with a specific API key.
    /// </summary>
    /// <param name="apiKey">API key to use by default.</param>
    public OpenAIClientOptions(string? apiKey = null)
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
    /// Gets or sets the default embedding options.
    /// </summary>
    public EmbeddingOptions DefaultEmbeddingOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default text-to-speech options.
    /// </summary>
    public TextToSpeechOptions DefaultTextToSpeechOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default transcription options.
    /// </summary>
    public TranscriptionOptions DefaultTranscriptionOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default translation options.
    /// </summary>
    public TranslationOptions DefaultTranslationOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default moderation options.
    /// </summary>
    public ModerationOptions DefaultModerationOptions { get; set; } = new();
}

/// <summary>
/// Non-generic OpenAI client options using the built-in chat models.
/// </summary>
public record OpenAIClientOptions : OpenAIClientOptions<ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes client options with defaults.
    /// </summary>
    public OpenAIClientOptions() : base() { }

    /// <summary>
    /// Initializes client options with a specific API key.
    /// </summary>
    /// <param name="apiKey">API key to use by default.</param>
    public OpenAIClientOptions(string? apiKey) : base(apiKey) { }
}
