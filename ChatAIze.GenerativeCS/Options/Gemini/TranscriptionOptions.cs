using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Options.Gemini;

/// <summary>
/// Configures Gemini audio transcription requests built on top of audio understanding.
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
    public string Model { get; set; } = DefaultModels.Gemini.SpeechToText;

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the expected spoken language of the audio.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets an optional prompt to guide transcription.
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Gets or sets the sampling temperature.
    /// </summary>
    /// <remarks>Only applies when the selected Gemini model honors temperature for the request.</remarks>
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
