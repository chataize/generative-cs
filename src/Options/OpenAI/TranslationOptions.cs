using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

public record TranslationOptions
{
    private const string DefaultModel = SpeechRecognitionModels.WHISPER_1;

    public TranslationOptions(string? prompt = null)
    {
        Prompt = prompt;
    }

    public string Model { get; set; } = DefaultModel;

    public string? Prompt { get; set; }

    public double Temperature { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public TranscriptionResponseFormat ResponseFormat { get; set; } = TranscriptionResponseFormat.Text;
}
