using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

/// <summary>
/// Configures speech-to-text transcription requests.
/// </summary>
public record TranscriptionOptions
{
    /// <summary>
    /// Initializes transcription options.
    /// </summary>
    /// <param name="language">Optional expected language hint.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public TranscriptionOptions(string? language = null, string? apiKey = null)
    {
        Language = language;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the model identifier used for transcription.
    /// </summary>
    public string Model { get; set; } = DefaultModels.OpenAI.SpeechToText;

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the expected language of the audio.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets an optional prompt to guide transcription.
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
    /// Gets or sets the desired transcription response format.
    /// </summary>
    public TranscriptionResponseFormat ResponseFormat { get; set; } = TranscriptionResponseFormat.Text;
}
