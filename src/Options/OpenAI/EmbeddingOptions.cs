using GenerativeCS.Constants;

namespace GenerativeCS.Options.OpenAI;

public record EmbeddingOptions
{
    private const string DefaultModel = EmbeddingModels.TEXT_EMBEDDING_ADA_002;

    public EmbeddingOptions(string model = DefaultModel)
    {
        Model = model;
    }

    public string Model { get; set; } = DefaultModel;

    public int MaxAttempts { get; set; } = 5;
}
