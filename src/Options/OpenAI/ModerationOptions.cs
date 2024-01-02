using GenerativeCS.Constants;

namespace GenerativeCS.Options.OpenAI;

public record ModerationOptions
{
    private const string DEFAULT_MODEL = ModerationModels.TEXT_MODERATION_LATEST;

    public ModerationOptions(string model = DEFAULT_MODEL)
    {
        Model = model;
    }

    public string Model { get; set; } = DEFAULT_MODEL;

    public int MaxAttempts { get; set; } = 5;
}
