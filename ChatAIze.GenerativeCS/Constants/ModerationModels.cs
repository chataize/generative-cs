namespace ChatAIze.GenerativeCS.Constants;

/// <summary>
/// Supported moderation model identifiers.
/// </summary>
public static class ModerationModels
{
    /// <summary>
    /// OpenAI moderation models.
    /// </summary>
    public static class OpenAI
    {
        /// <summary>
        /// Latest omni moderation model.
        /// </summary>
        public const string OmniModerationLatest = "omni-moderation-latest";

        /// <summary>
        /// Omni moderation model dated 2024-09-26.
        /// </summary>
        public const string OmniModeration20240926 = "omni-moderation-2024-09-26";
    }

    /// <summary>
    /// Claude models recommended for moderation-style classification.
    /// </summary>
    public static class Claude
    {
        /// <summary>
        /// Claude Sonnet 4.6, used here as the default moderation model because it is already supported by the chat-completion integration.
        /// </summary>
        public const string Sonnet46 = ChatCompletionModels.Claude.Sonnet46;

        /// <summary>
        /// Claude Haiku 4.5, useful for lower-cost moderation scenarios.
        /// </summary>
        public const string Haiku45 = ChatCompletionModels.Claude.Haiku45;
    }
}
