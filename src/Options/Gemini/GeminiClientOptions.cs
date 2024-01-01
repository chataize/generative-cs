namespace GenerativeCS.Options.Gemini;

public record GeminiClientOptions
{
    public required string ApiKey { get; set; }

    public ChatCompletionOptions? DefaultCompletionOptions { get; set; } = new();
}
