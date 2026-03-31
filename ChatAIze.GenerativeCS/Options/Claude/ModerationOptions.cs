using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.Claude;

/// <summary>
/// Configures Claude moderation requests implemented on top of the Messages API.
/// </summary>
public record ModerationOptions
{
    /// <summary>
    /// Initializes moderation options.
    /// </summary>
    /// <param name="model">Claude model identifier to use for moderation classification.</param>
    /// <param name="apiKey">Optional API key overriding the client default for moderation calls.</param>
    public ModerationOptions(string model = DefaultModels.Claude.Moderation, string? apiKey = null)
    {
        Model = model;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the model identifier used for moderation requests.
    /// </summary>
    public string Model { get; set; } = DefaultModels.Claude.Moderation;

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key when sending moderation requests.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of attempts (including the first call) before a moderation request is treated as failed.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of output tokens reserved for the structured moderation result.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 512;
}
