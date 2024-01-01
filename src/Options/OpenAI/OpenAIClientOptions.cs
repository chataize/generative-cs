namespace GenerativeCS.Options.OpenAI;

public record OpenAIClientOptions
{
    public required string ApiKey { get; set; }

    public ChatCompletionOptions? DefaultCompletionOptions { get; set; } = new();

    public EmbeddingOptions? DefaultEmbeddingOptions { get; set; } = new();
}
