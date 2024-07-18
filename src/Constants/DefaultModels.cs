namespace ChatAIze.GenerativeCS.Constants;

public static class DefaultModels
{
    public static class OpenAI
    {
        public const string ChatCompletion = ChatCompletionModels.OpenAI.GPT4oMini;

        public const string Embedding = EmbeddingModels.OpenAI.TextEmbedding3Small;

        public const string TextToSpeech = TextToSpeechModels.OpenAI.TTS1;

        public const string SpeechToText = SpeechRecognitionModels.OpenAI.Whisper1;

        public const string Moderation = ModerationModels.OpenAI.TextModerationStable;
    }

    public static class Gemini
    {
        public const string ChatCompletion = ChatCompletionModels.Gemini.Gemini15Flash;
    }
}
