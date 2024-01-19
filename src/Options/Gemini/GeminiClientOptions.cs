using System.Diagnostics.CodeAnalysis;

namespace ChatAIze.GenerativeCS.Options.Gemini;

public record GeminiClientOptions
{
    public GeminiClientOptions() { }

    [SetsRequiredMembers]
    public GeminiClientOptions(string apiKey)
    {
        ApiKey = apiKey;
    }

    public required string ApiKey { get; set; }

    public ChatCompletionOptions DefaultCompletionOptions { get; set; } = new();
}
