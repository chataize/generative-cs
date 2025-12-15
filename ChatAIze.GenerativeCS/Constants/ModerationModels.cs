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

        /// <summary>
        /// Latest text moderation model.
        /// </summary>
        public const string TextModerationLatest = "moderation-latest";

        /// <summary>
        /// Stable text moderation model.
        /// </summary>
        public const string TextModerationStable = "text-moderation-stable";

        /// <summary>
        /// Text moderation model version 007.
        /// </summary>
        public const string TextModeration007 = "text-moderation-007";
    }
}
