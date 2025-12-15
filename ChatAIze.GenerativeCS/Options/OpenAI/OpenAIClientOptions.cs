using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

/// <summary>
/// Groups default settings consumed by <see cref="Clients.OpenAIClient{TChat, TMessage, TFunctionCall, TFunctionResult}"/> when per-call overrides are not supplied.
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
    /// Initializes client options with defaults for each operation type.
    /// </summary>
    public OpenAIClientOptions() { }

    /// <summary>
    /// Initializes client options with a specific API key.
    /// </summary>
    /// <param name="apiKey">API key to fall back to when a request does not provide one.</param>
    public OpenAIClientOptions(string? apiKey = null)
    {
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the API key applied to requests when no request-level override is provided.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the shared chat completion defaults used whenever a request omits explicit options.
    /// </summary>
    public ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> DefaultCompletionOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the shared embedding defaults used whenever a request omits explicit options.
    /// </summary>
    public EmbeddingOptions DefaultEmbeddingOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the shared text-to-speech defaults used whenever a request omits explicit options.
    /// </summary>
    public TextToSpeechOptions DefaultTextToSpeechOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the shared transcription defaults used whenever a request omits explicit options.
    /// </summary>
    public TranscriptionOptions DefaultTranscriptionOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the shared translation defaults used whenever a request omits explicit options.
    /// </summary>
    public TranslationOptions DefaultTranslationOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the shared moderation defaults used whenever a request omits explicit options.
    /// </summary>
    public ModerationOptions DefaultModerationOptions { get; set; } = new();
}

/// <summary>
/// Non-generic OpenAI client options using the built-in chat models.
/// </summary>
public record OpenAIClientOptions : OpenAIClientOptions<ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes client options with defaults for each operation type.
    /// </summary>
    public OpenAIClientOptions() : base() { }

    /// <summary>
    /// Initializes client options with a specific API key.
    /// </summary>
    /// <param name="apiKey">API key to fall back to when a request does not provide one.</param>
    public OpenAIClientOptions(string? apiKey) : base(apiKey) { }
}
