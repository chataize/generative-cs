using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

public record ModerationOptions
{
    public ModerationOptions(string model = DefaultModels.OpenAI.Moderation, string? apiKey = null)
    {
        Model = model;
        ApiKey = apiKey;
    }

    public string Model { get; set; } = DefaultModels.OpenAI.Moderation;

    public string? ApiKey { get; set; }

    public int MaxAttempts { get; set; } = 5;
}
