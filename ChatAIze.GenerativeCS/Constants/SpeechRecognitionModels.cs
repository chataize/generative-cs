namespace ChatAIze.GenerativeCS.Constants;

/// <summary>
/// Supported speech recognition model identifiers.
/// </summary>
public static class SpeechRecognitionModels
{
    /// <summary>
    /// OpenAI speech recognition models.
    /// </summary>
    public static class OpenAI
    {
        /// <summary>
        /// Whisper v1 model identifier.
        /// </summary>
        public const string Whisper1 = "whisper-1";

        /// <summary>
        /// GPT-4o transcription model identifier.
        /// </summary>
        public const string GPT4oTranscribe = "gpt-4o-transcribe";

        /// <summary>
        /// GPT-4o-mini transcription model identifier.
        /// </summary>
        public const string GPT4oMiniTranscribe = "gpt-4o-mini-transcribe";
    }
}
