using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

public record TextToSpeechOptions
{
    public TextToSpeechOptions(string model = DefaultModels.OpenAI.TextToSpeech, TextToSpeechVoice voice = TextToSpeechVoice.Alloy, string? apiKey = null)
    {
        Model = model;
        Voice = voice;
        ApiKey = apiKey;
    }

    public string Model { get; set; } = DefaultModels.OpenAI.TextToSpeech;

    public TextToSpeechVoice Voice { get; set; }

    public string? ApiKey { get; set; }

    public double Speed { get; set; } = 1.0;

    public int MaxAttempts { get; set; } = 5;

    public VoiceResponseFormat ResponseFormat { get; set; }
}
