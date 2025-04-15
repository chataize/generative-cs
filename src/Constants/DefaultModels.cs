namespace ChatAIze.GenerativeCS.Constants;

public static class DefaultModels
{
    public static class OpenAI
    {
        public const string ChatCompletion = ChatCompletionModels.OpenAI.GPT41;

        public const string Embedding = EmbeddingModels.OpenAI.TextEmbedding3Small;

        public const string TextToSpeech = TextToSpeechModels.OpenAI.GPT4oMiniTTS;

        public const string SpeechToText = SpeechRecognitionModels.OpenAI.GPT4oTranscribe;

        public const string Moderation = ModerationModels.OpenAI.OmniModerationLatest;
    }

    public static class Gemini
    {
        public const string ChatCompletion = ChatCompletionModels.Gemini.Gemini15Flash;
    }
}
