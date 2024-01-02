using GenerativeCS.Constants;
using GenerativeCS.Enums;

namespace GenerativeCS.Options.OpenAI;

public record TextToSpeechOptions
{
    private const string DefaultModel = TextToSpeechModels.TTS_1;

    public TextToSpeechOptions(string model = DefaultModel, TextToSpeechVoice voice = TextToSpeechVoice.Alloy)
    {
        Model = model;
        Voice = voice;
    }

    public string Model { get; set; } = DefaultModel;

    public TextToSpeechVoice Voice { get; set; }

    public double Speed { get; set; } = 1.0;

    public int MaxAttempts { get; set; } = 5;

    public VoiceResponseFormat ResponseFormat { get; set; }
}
