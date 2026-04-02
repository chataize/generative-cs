namespace ChatAIze.GenerativeCS.Constants;

/// <summary>
/// Supported speech recognition model identifiers.
/// </summary>
public static class SpeechRecognitionModels
{
    /// <summary>
    /// Gemini models commonly used for audio understanding.
    /// </summary>
    public static class Gemini
    {
        /// <summary>
        /// Stable model identifier for gemini-2.5-flash.
        /// </summary>
        public const string Gemini25Flash = ChatCompletionModels.Gemini.Gemini25Flash;

        /// <summary>
        /// Stable model identifier for gemini-2.0-flash.
        /// </summary>
        public const string Gemini20Flash = ChatCompletionModels.Gemini.Gemini20Flash;
    }

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
        /// GPT-4o transcription model identifier dated 2025-12-15.
        /// </summary>
        public const string GPT4oTranscribe20251215 = "gpt-4o-transcribe-2025-12-15";

        /// <summary>
        /// GPT-4o transcription model identifier dated 2025-03-20.
        /// </summary>
        public const string GPT4oTranscribe20250320 = "gpt-4o-transcribe-2025-03-20";

        /// <summary>
        /// GPT-4o diarization transcription model identifier.
        /// </summary>
        public const string GPT4oTranscribeDiarize = "gpt-4o-transcribe-diarize";

        /// <summary>
        /// GPT-4o-mini transcription model identifier.
        /// </summary>
        public const string GPT4oMiniTranscribe = "gpt-4o-mini-transcribe";

        /// <summary>
        /// GPT-4o-mini transcription model identifier dated 2025-12-15.
        /// </summary>
        public const string GPT4oMiniTranscribe20251215 = "gpt-4o-mini-transcribe-2025-12-15";

        /// <summary>
        /// GPT-4o-mini transcription model identifier dated 2025-03-20.
        /// </summary>
        public const string GPT4oMiniTranscribe20250320 = "gpt-4o-mini-transcribe-2025-03-20";
    }
}
