using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

/// <summary>
/// Configures moderation requests.
/// </summary>
public record ModerationOptions
{
    /// <summary>
    /// Initializes moderation options.
    /// </summary>
    /// <param name="model">Moderation model identifier.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public ModerationOptions(string model = DefaultModels.OpenAI.Moderation, string? apiKey = null)
    {
        Model = model;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the model identifier used for moderation requests.
    /// </summary>
    public string Model { get; set; } = DefaultModels.OpenAI.Moderation;

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for a failed request.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;
}
