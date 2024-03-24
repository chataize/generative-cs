namespace ChatAIze.GenerativeCS.Constants;

public static class DefaultModels
{
    public static class OpenAI
    {
        public const string ChatCompletion = ChatCompletionModels.OpenAI.GPT_3_5_TURBO;
        public const string Embedding = EmbeddingModels.OpenAI.TEXT_EMBEDDING_3_LARGE;
        public const string TextToSpeech = TextToSpeechModels.OpenAI.TTS_1;
        public const string SpeechToText = SpeechRecognitionModels.OpenAI.WHISPER_1;
        public const string Moderation = ModerationModels.OpenAI.TEXT_MODERATION_STABLE;
    }

    public static class Gemini
    {
        public const string ChatCompletion = ChatCompletionModels.Gemini.GEMINI_PRO;
    }
}
