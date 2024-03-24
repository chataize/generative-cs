using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

public record ModerationOptions
{
    public ModerationOptions(string model = DefaultModels.OpenAI.Moderation)
    {
        Model = model;
    }

    public string Model { get; set; } = DefaultModels.OpenAI.Moderation;

    public int MaxAttempts { get; set; } = 5;
}
