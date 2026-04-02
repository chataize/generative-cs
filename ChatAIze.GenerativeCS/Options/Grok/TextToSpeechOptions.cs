using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Options.Grok;

/// <summary>
/// Configures xAI text-to-speech synthesis requests.
/// </summary>
public record TextToSpeechOptions
{
    /// <summary>
    /// Initializes Grok text-to-speech options.
    /// </summary>
    /// <param name="voiceId">Preferred xAI voice identifier.</param>
    /// <param name="language">Requested language code.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public TextToSpeechOptions(string? voiceId = null, string language = "en", string? apiKey = null)
    {
        VoiceId = voiceId;
        Language = language;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets an optional model identifier for compatibility with OpenAI-shaped calling code.
    /// </summary>
    /// <remarks>xAI's <c>/v1/tts</c> endpoint currently does not expose model selection, so this property is ignored.</remarks>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the preferred xAI voice identifier.
    /// </summary>
    /// <remarks>If omitted, <see cref="GrokTextToSpeechVoices.Eve"/> is used.</remarks>
    public string? VoiceId { get; set; }

    /// <summary>
    /// Gets or sets an optional OpenAI-shaped voice preset for compatibility with existing calling code.
    /// </summary>
    /// <remarks>When <see cref="VoiceId"/> is also supplied, <see cref="VoiceId"/> wins.</remarks>
    public TextToSpeechVoice? Voice { get; set; }

    /// <summary>
    /// Gets or sets the requested language code.
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Gets or sets an OpenAI-shaped speed value for compatibility with existing calling code.
    /// </summary>
    /// <remarks>xAI's current text-to-speech endpoint does not expose playback speed control, so this property is ignored.</remarks>
    public double Speed { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets an OpenAI-shaped response format for compatibility with existing calling code.
    /// </summary>
    /// <remarks>Only <see cref="VoiceResponseFormat.Default"/> and <see cref="VoiceResponseFormat.MP3"/> map directly to xAI.</remarks>
    public VoiceResponseFormat ResponseFormat { get; set; }

    /// <summary>
    /// Gets or sets the explicit codec to request from xAI.
    /// </summary>
    /// <remarks>Supported codecs are <c>mp3</c>, <c>wav</c>, <c>pcm</c>, <c>mulaw</c>, and <c>alaw</c>.</remarks>
    public string? Codec { get; set; }

    /// <summary>
    /// Gets or sets the desired sample rate in Hz.
    /// </summary>
    public int? SampleRate { get; set; }

    /// <summary>
    /// Gets or sets the desired bit rate in bits per second for MP3 output.
    /// </summary>
    public int? BitRate { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for a failed request.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;
}
