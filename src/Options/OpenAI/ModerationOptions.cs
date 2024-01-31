using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

public record ModerationOptions
{
    private const string DEFAULT_MODEL = ModerationModels.TEXT_MODERATION_STABLE;

    public ModerationOptions(string model = DEFAULT_MODEL)
    {
        Model = model;
    }

    public string Model { get; set; } = DEFAULT_MODEL;

    public int MaxAttempts { get; set; } = 5;
}
