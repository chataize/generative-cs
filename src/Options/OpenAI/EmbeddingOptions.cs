using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

public record EmbeddingOptions
{
    private const string DefaultModel = EmbeddingModels.TEXT_EMBEDDING_3_LARGE;

    public EmbeddingOptions(string model = DefaultModel, string? user = null)
    {
        Model = model;
        User = user;
    }

    public string Model { get; set; } = DefaultModel;

    public int? Dimensions { get; set; }

    public string? User { get; set; }

    public int MaxAttempts { get; set; } = 5;
}
