using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

public record TranscriptionOptions
{
    private const string DefaultModel = SpeechRecognitionModels.WHISPER_1;

    public TranscriptionOptions(string? language = null, string? prompt = null)
    {
        Language = language;
        Prompt = prompt;
    }

    public string Model { get; set; } = DefaultModel;

    public string? Language { get; set; }

    public string? Prompt { get; set; }

    public double Temperature { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public TranscriptionResponseFormat ResponseFormat { get; set; } = TranscriptionResponseFormat.Text;
}
