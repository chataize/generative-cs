using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

public record EmbeddingOptions
{
    public EmbeddingOptions(string model = DefaultModels.OpenAI.Embedding, string? apiKey = null)
    {
        Model = model;
        ApiKey = apiKey;
    }

    public string Model { get; set; } = DefaultModels.OpenAI.Embedding;

    public string? ApiKey { get; set; }

    public int? Dimensions { get; set; }

    public string? UserTrackingId { get; set; }

    public int MaxAttempts { get; set; } = 5;
}
