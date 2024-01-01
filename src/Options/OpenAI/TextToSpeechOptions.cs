using GenerativeCS.Constants;
using GenerativeCS.Enums;

namespace GenerativeCS.Providers.OpenAI;

public record TextToSpeechOptions
{
    private const string DefaultModel = TextToSpeechModels.TTS_1;

    private const string DefaultVoice = TextToSpeechVoices.ALLOY;

    public TextToSpeechOptions(string model = DefaultModel, string voice = DefaultVoice)
    {
        Model = model;
        Voice = voice;
    }

    public string Model { get; set; } = DefaultModel;

    public string Voice { get; set; } = DefaultVoice;

    public double Speed { get; set; } = 1.0;

    public int MaxAttempts { get; set; } = 5;

    public VoiceResponseFormat ResponseFormat { get; set; }
}
