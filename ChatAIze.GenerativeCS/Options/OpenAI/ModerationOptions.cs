using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

/// <summary>
/// Configures OpenAI moderation requests, including the model, API key override, and retry policy.
/// </summary>
public record ModerationOptions
{
    /// <summary>
    /// Initializes moderation options.
    /// </summary>
    /// <param name="model">Moderation model identifier to send to the provider (defaults to <see cref="DefaultModels.OpenAI.Moderation"/>).</param>
    /// <param name="apiKey">Optional API key that overrides the client-level default for moderation calls.</param>
    public ModerationOptions(string model = DefaultModels.OpenAI.Moderation, string? apiKey = null)
    {
        Model = model;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the model identifier used for moderation requests (defaults to <see cref="DefaultModels.OpenAI.Moderation"/>).
    /// </summary>
    public string Model { get; set; } = DefaultModels.OpenAI.Moderation;

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key when sending moderation requests.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of attempts (including the first call) before a moderation request is treated as failed.
    /// </summary>
    /// <remarks>Used by the retry helper; does not retry on client-side validation errors.</remarks>
    public int MaxAttempts { get; set; } = 5;
}
