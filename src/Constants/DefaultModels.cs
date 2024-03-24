namespace ChatAIze.GenerativeCS.Constants;

public static class DefaultModels
{
    public static class OpenAI
    {
        public const string ChatCompletion = ChatCompletionModels.GPT_3_5_TURBO;
        public const string Embedding = EmbeddingModels.TEXT_EMBEDDING_3_LARGE;
        public const string TextToSpeech = TextToSpeechModels.TTS_1;
        public const string SpeechToText = SpeechRecognitionModels.WHISPER_1;
        public const string Moderation = ModerationModels.TEXT_MODERATION_STABLE;
    }

    public static class Gemini
    {
        public const string ChatCompletion = ChatCompletionModels.GEMINI_PRO;
    }
}
