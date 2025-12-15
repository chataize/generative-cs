using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

/// <summary>
/// Configures speech translation requests.
/// </summary>
public record TranslationOptions
{
    /// <summary>
    /// Initializes translation options.
    /// </summary>
    /// <param name="prompt">Optional prompt to guide translation.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public TranslationOptions(string? prompt = null, string? apiKey = null)
    {
        Prompt = prompt;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the model identifier used for translation.
    /// </summary>
    public string Model { get; set; } = DefaultModels.OpenAI.SpeechToText;

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets an optional prompt to guide translation.
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Gets or sets the sampling temperature.
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for a failed request.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the desired translation response format.
    /// </summary>
    public TranscriptionResponseFormat ResponseFormat { get; set; } = TranscriptionResponseFormat.Text;
}
