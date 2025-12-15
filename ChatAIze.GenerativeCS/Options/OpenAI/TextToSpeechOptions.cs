using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

/// <summary>
/// Configures text-to-speech synthesis requests.
/// </summary>
public record TextToSpeechOptions
{
    /// <summary>
    /// Initializes text-to-speech options.
    /// </summary>
    /// <param name="model">Text-to-speech model identifier.</param>
    /// <param name="voice">Voice preset.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public TextToSpeechOptions(string model = DefaultModels.OpenAI.TextToSpeech, TextToSpeechVoice voice = TextToSpeechVoice.Alloy, string? apiKey = null)
    {
        Model = model;
        Voice = voice;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the model identifier used for synthesis.
    /// </summary>
    public string Model { get; set; } = DefaultModels.OpenAI.TextToSpeech;

    /// <summary>
    /// Gets or sets the voice preset to use.
    /// </summary>
    public TextToSpeechVoice Voice { get; set; }

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    public double Speed { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for a failed request.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the desired audio container format.
    /// </summary>
    public VoiceResponseFormat ResponseFormat { get; set; }
}
