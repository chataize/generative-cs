namespace ChatAIze.GenerativeCS.Constants;

/// <summary>
/// Provides default model identifiers used across the library.
/// </summary>
public static class DefaultModels
{
    /// <summary>
    /// Default OpenAI model identifiers.
    /// </summary>
    public static class OpenAI
    {
        /// <summary>
        /// Default chat completion model.
        /// </summary>
        /// <remarks>Used when callers omit a model identifier to favor a generally capable, up-to-date model.</remarks>
        public const string ChatCompletion = ChatCompletionModels.OpenAI.GPT51;

        /// <summary>
        /// Default embedding model.
        /// </summary>
        /// <remarks>Balances cost and quality for general-purpose embeddings.</remarks>
        public const string Embedding = EmbeddingModels.OpenAI.TextEmbedding3Small;

        /// <summary>
        /// Default text-to-speech model.
        /// </summary>
        /// <remarks>Chosen for latency and voice quality in typical scenarios.</remarks>
        public const string TextToSpeech = TextToSpeechModels.OpenAI.GPT4oMiniTTS;

        /// <summary>
        /// Default speech-to-text model.
        /// </summary>
        public const string SpeechToText = SpeechRecognitionModels.OpenAI.GPT4oTranscribe;

        /// <summary>
        /// Default moderation model.
        /// </summary>
        public const string Moderation = ModerationModels.OpenAI.OmniModerationLatest;
    }

    /// <summary>
    /// Default Gemini model identifiers.
    /// </summary>
    public static class Gemini
    {
        /// <summary>
        /// Default chat completion model.
        /// </summary>
        public const string ChatCompletion = ChatCompletionModels.Gemini.Gemini15Flash;
    }
}
