namespace ChatAIze.GenerativeCS.Constants;

/// <summary>
/// Supported text-to-speech model identifiers.
/// </summary>
public static class TextToSpeechModels
{
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
    }
}
