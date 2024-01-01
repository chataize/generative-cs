namespace GenerativeCS.Options.OpenAI;

public record EmbeddingOptions
{
    public EmbeddingOptions(string model = "text-embedding-ada-002")
    {
        Model = model;
    }

    public string Model { get; set; } = "text-embedding-ada-002";

    public int MaxAttempts { get; set; } = 5;
}
