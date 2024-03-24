using System.Diagnostics.CodeAnalysis;
using ChatAIze.GenerativeCS.Interfaces;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.Gemini;

public record GeminiClientOptions<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    public GeminiClientOptions() { }

    [SetsRequiredMembers]
    public GeminiClientOptions(string apiKey)
    {
        ApiKey = apiKey;
    }

    public required string ApiKey { get; set; }

    public ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> DefaultCompletionOptions { get; set; } = new();
}

public record GeminiClientOptions : GeminiClientOptions<ChatMessage, FunctionCall, FunctionResult>
{
    public GeminiClientOptions() { }

    [SetsRequiredMembers]
    public GeminiClientOptions(string apiKey) : base(apiKey) { }
}
