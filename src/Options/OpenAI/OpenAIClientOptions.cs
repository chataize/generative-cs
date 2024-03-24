using System.Diagnostics.CodeAnalysis;
using ChatAIze.GenerativeCS.Interfaces;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

public record OpenAIClientOptions<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    public OpenAIClientOptions() { }

    [SetsRequiredMembers]
    public OpenAIClientOptions(string apiKey)
    {
        ApiKey = apiKey;
    }

    public required string ApiKey { get; set; }

    public ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> DefaultCompletionOptions { get; set; } = new();

    public EmbeddingOptions DefaultEmbeddingOptions { get; set; } = new();

    public TextToSpeechOptions DefaultTextToSpeechOptions { get; set; } = new();

    public TranscriptionOptions DefaultTranscriptionOptions { get; set; } = new();

    public TranslationOptions DefaultTranslationOptions { get; set; } = new();

    public ModerationOptions DefaultModerationOptions { get; set; } = new();
}

public record OpenAIClientOptions : OpenAIClientOptions<ChatMessage, FunctionCall, FunctionResult>
{
    public OpenAIClientOptions() { }

    [SetsRequiredMembers]
    public OpenAIClientOptions(string apiKey) : base(apiKey) { }
}
