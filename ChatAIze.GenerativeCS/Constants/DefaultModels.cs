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
        public const string ChatCompletion = ChatCompletionModels.OpenAI.GPT54;

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
        /// Default speech-translation model.
        /// </summary>
        /// <remarks>OpenAI currently only supports <c>whisper-1</c> on the dedicated translation endpoint.</remarks>
        public const string SpeechTranslation = SpeechRecognitionModels.OpenAI.Whisper1;

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

    /// <summary>
    /// Default Claude model identifiers.
    /// </summary>
    public static class Claude
    {
        /// <summary>
        /// Default chat completion model.
        /// </summary>
        /// <remarks>Favours Anthropic's current speed and capability balance for general-purpose chat workloads.</remarks>
        public const string ChatCompletion = ChatCompletionModels.Claude.Sonnet46;

        /// <summary>
        /// Default moderation model.
        /// </summary>
        public const string Moderation = ModerationModels.Claude.Sonnet46;
    }

    /// <summary>
    /// Default Grok model identifiers.
    /// </summary>
    public static class Grok
    {
        /// <summary>
        /// Default chat completion model.
        /// </summary>
        /// <remarks>Favours xAI's current fast non-reasoning Grok 4.1 variant for general-purpose assistant workloads.</remarks>
        public const string ChatCompletion = ChatCompletionModels.Grok.Grok41FastNonReasoning;
    }
}
