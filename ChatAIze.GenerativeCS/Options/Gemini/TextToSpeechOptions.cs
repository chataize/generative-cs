using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Options.Gemini;

/// <summary>
/// Configures Gemini text-to-speech synthesis requests.
/// </summary>
public record TextToSpeechOptions
{
    /// <summary>
    /// Initializes Gemini text-to-speech options.
    /// </summary>
    /// <param name="model">Model identifier used for speech synthesis.</param>
    /// <param name="voiceName">Preferred Gemini voice name.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public TextToSpeechOptions(string model = DefaultModels.Gemini.TextToSpeech, string? voiceName = null, string? apiKey = null)
    {
        Model = model;
        VoiceName = voiceName;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the model identifier used for speech synthesis.
    /// </summary>
    public string Model { get; set; } = DefaultModels.Gemini.TextToSpeech;

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the explicit Gemini voice name.
    /// </summary>
    public string? VoiceName { get; set; }

    /// <summary>
    /// Gets or sets an OpenAI-shaped voice preset for compatibility with existing calling code.
    /// </summary>
    /// <remarks>When <see cref="VoiceName"/> is also supplied, <see cref="VoiceName"/> wins.</remarks>
    public TextToSpeechVoice? Voice { get; set; }

    /// <summary>
    /// Gets or sets the requested response format.
    /// </summary>
    /// <remarks>
    /// Gemini currently returns raw 24kHz 16-bit mono PCM. The provider wraps PCM as WAV when
    /// <see cref="VoiceResponseFormat.Default"/> is used so callers receive a file-friendly container by default.
    /// </remarks>
    public VoiceResponseFormat ResponseFormat { get; set; } = VoiceResponseFormat.Default;

    /// <summary>
    /// Gets or sets the expected PCM sample rate in Hz.
    /// </summary>
    /// <remarks>Gemini TTS currently emits 24kHz PCM.</remarks>
    public int SampleRate { get; set; } = 24_000;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for a failed request.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;
}
