namespace ChatAIze.GenerativeCS.Constants;

/// <summary>
/// Supported text-to-speech model identifiers.
/// </summary>
public static class TextToSpeechModels
{
    /// <summary>
    /// Gemini text-to-speech models.
    /// </summary>
    public static class Gemini
    {
        /// <summary>
        /// Preview text-to-speech model <c>gemini-2.5-flash-preview-tts</c>.
        /// </summary>
        public const string Gemini25FlashPreviewTTS = "gemini-2.5-flash-preview-tts";

        /// <summary>
        /// Preview text-to-speech model <c>gemini-2.5-pro-preview-tts</c>.
        /// </summary>
        public const string Gemini25ProPreviewTTS = "gemini-2.5-pro-preview-tts";
    }

    /// <summary>
    /// OpenAI text-to-speech models.
    /// </summary>
    public static class OpenAI
    {
        /// <summary>
        /// Text-to-speech model <c>tts-1</c>.
        /// </summary>
        public const string TTS1 = "tts-1";

        /// <summary>
        /// High-definition text-to-speech model <c>tts-1-hd</c>.
        /// </summary>
        public const string TTS1HD = "tts-1-hd";

        /// <summary>
        /// GPT-4o-mini text-to-speech model identifier.
        /// </summary>
        public const string GPT4oMiniTTS = "gpt-4o-mini-tts";

        /// <summary>
        /// GPT-4o-mini text-to-speech model identifier dated 2025-12-15.
        /// </summary>
        public const string GPT4oMiniTTS20251215 = "gpt-4o-mini-tts-2025-12-15";

        /// <summary>
        /// GPT-4o-mini text-to-speech model identifier dated 2025-03-20.
        /// </summary>
        public const string GPT4oMiniTTS20250320 = "gpt-4o-mini-tts-2025-03-20";
    }
}
