namespace ChatAIze.GenerativeCS.Constants;

/// <summary>
/// Chat completion model identifiers grouped by provider.
/// </summary>
public static class ChatCompletionModels
{
    /// <summary>
    /// OpenAI chat completion model identifiers.
    /// </summary>
    public static class OpenAI
    {
        /// <summary>
        /// Model identifier for gpt-5.2.
        /// </summary>
        public const string GPT52 = "gpt-5.2";

        /// <summary>
        /// Model identifier for gpt-5.2-2025-12-11.
        /// </summary>
        public const string GPT5220251211 = "gpt-5.2-2025-12-11";

        /// <summary>
        /// Model identifier for gpt-5.2-chat-latest.
        /// </summary>
        public const string GPT52Chat = "gpt-5.2-chat-latest";

        /// <summary>
        /// Model identifier for gpt-5.2-pro.
        /// </summary>
        public const string GPT52Pro = "gpt-5.2-pro";

        /// <summary>
        /// Model identifier for gpt-5.2-pro-2025-12-11.
        /// </summary>
        public const string GPT52Pro20251211 = "gpt-5.2-pro-2025-12-11";

        /// <summary>
        /// Model identifier for gpt-5.1.
        /// </summary>
        public const string GPT51 = "gpt-5.1";

        /// <summary>
        /// Model identifier for gpt-5.1-2025-11-13.
        /// </summary>
        public const string GPT5120251113 = "gpt-5.1-2025-11-13";

        /// <summary>
        /// Model identifier for gpt-5.
        /// </summary>
        public const string GPT5 = "gpt-5";

        /// <summary>
        /// Model identifier for gpt-5-2025-08-07.
        /// </summary>
        public const string GPT520250807 = "gpt-5-2025-08-07";

        /// <summary>
        /// Model identifier for gpt-5-mini.
        /// </summary>
        public const string GPT5Mini = "gpt-5-mini";

        /// <summary>
        /// Model identifier for gpt-5-mini-2025-08-07.
        /// </summary>
        public const string GPT5Mini20250807 = "gpt-5-mini-2025-08-07";

        /// <summary>
        /// Model identifier for gpt-5-nano.
        /// </summary>
        public const string GPT5Nano = "gpt-5-nano";

        /// <summary>
        /// Model identifier for gpt-5-nano-2025-08-07.
        /// </summary>
        public const string GPT5Nano20250807 = "gpt-5-nano-2025-08-07";

        /// <summary>
        /// Model identifier for gpt-4.5-preview.
        /// </summary>
        public const string GPT45Preview = "gpt-4.5-preview";

        /// <summary>
        /// Model identifier for gpt-4.5-preview-2025-02-27.
        /// </summary>
        public const string GPT45Preview20250227 = "gpt-4.5-preview-2025-02-27";

        /// <summary>
        /// Model identifier for gpt-4.1.
        /// </summary>
        public const string GPT41 = "gpt-4.1";

        /// <summary>
        /// Model identifier for gpt-4.1-mini.
        /// </summary>
        public const string GPT41Mini = "gpt-4.1-mini";

        /// <summary>
        /// Model identifier for gpt-4.1-nano.
        /// </summary>
        public const string GPT41Nano = "gpt-4.1-nano";

        /// <summary>
        /// Model identifier for gpt-4o.
        /// </summary>
        public const string GPT4o = "gpt-4o";

        /// <summary>
        /// Model identifier for gpt-4o-2024-11-20.
        /// </summary>
        public const string GPT4o20241120 = "gpt-4o-2024-11-20";

        /// <summary>
        /// Model identifier for gpt-4o-2024-08-06.
        /// </summary>
        public const string GPT4o20240806 = "gpt-4o-2024-08-06";

        /// <summary>
        /// Model identifier for gpt-4o-2024-05-13.
        /// </summary>
        public const string GPT4o20240513 = "gpt-4o-2024-05-13";

        /// <summary>
        /// Model identifier for chatgpt-4o-latest.
        /// </summary>
        public const string ChatGPT4oLatest = "chatgpt-4o-latest";

        /// <summary>
        /// Model identifier for gpt-4o-mini.
        /// </summary>
        public const string GPT4oMini = "gpt-4o-mini";

        /// <summary>
        /// Model identifier for gpt-4o-mini-2024-07-18.
        /// </summary>
        public const string GPT4oMini20240718 = "gpt-4o-mini-2024-07-18";

        /// <summary>
        /// Model identifier for o1.
        /// </summary>
        public const string O1 = "o1";

        /// <summary>
        /// Model identifier for o1-2024-12-17.
        /// </summary>
        public const string O12024121 = "o1-2024-12-17";

        /// <summary>
        /// Model identifier for o1-preview.
        /// </summary>
        public const string O1Preview = "o1-preview";

        /// <summary>
        /// Model identifier for o1-preview-2024-09-12.
        /// </summary>
        public const string O1Preview20240912 = "o1-preview-2024-09-12";

        /// <summary>
        /// Model identifier for o3-mini.
        /// </summary>
        public const string O3Mini = "o3-mini";

        /// <summary>
        /// Model identifier for o3-mini-2025-01-31.
        /// </summary>
        public const string O3Mini20250131 = "o3-mini-2025-01-31";

        /// <summary>
        /// Model identifier for o1-mini.
        /// </summary>
        public const string O1Mini = "o1-mini";

        /// <summary>
        /// Model identifier for o1-mini-2024-09-12.
        /// </summary>
        public const string O1Mini20240912 = "o1-mini-2024-09-12";

        /// <summary>
        /// Model identifier for gpt-4-turbo.
        /// </summary>
        public const string GPT4Turbo = "gpt-4-turbo";

        /// <summary>
        /// Model identifier for gpt-4-turbo-2024-04-09.
        /// </summary>
        public const string GPT4Turbo20240409 = "gpt-4-turbo-2024-04-09";

        /// <summary>
        /// Model identifier for gpt-4-turbo-preview.
        /// </summary>
        public const string GPT4TurboPreview = "gpt-4-turbo-preview";

        /// <summary>
        /// Model identifier for gpt-4-0125-preview.
        /// </summary>
        public const string GPT40125Preview = "gpt-4-0125-preview";

        /// <summary>
        /// Model identifier for gpt-4-1106-preview.
        /// </summary>
        public const string GPT41106Preview = "gpt-4-1106-preview";

        /// <summary>
        /// Model identifier for gpt-4-vision-preview.
        /// </summary>
        public const string GPT4VisionPreview = "gpt-4-vision-preview";

        /// <summary>
        /// Model identifier for gpt-4-1106-vision-preview.
        /// </summary>
        public const string GPT41106VisionPreview = "gpt-4-1106-vision-preview";

        /// <summary>
        /// Model identifier for gpt-4.
        /// </summary>
        public const string GPT4 = "gpt-4";

        /// <summary>
        /// Model identifier for gpt-4-0613.
        /// </summary>
        public const string GPT40613 = "gpt-4-0613";

        /// <summary>
        /// Model identifier for gpt-4-32k.
        /// </summary>
        public const string GPT432k = "gpt-4-32k";

        /// <summary>
        /// Model identifier for gpt-4-32k-0613.
        /// </summary>
        public const string GPT432k0613 = "gpt-4-32k-0613";

        /// <summary>
        /// Model identifier for gpt-3.5-turbo-0125.
        /// </summary>
        public const string GPT35Turbo0125 = "gpt-3.5-turbo-0125";

        /// <summary>
        /// Model identifier for gpt-3.5-turbo.
        /// </summary>
        public const string GPT35Turbo = "gpt-3.5-turbo";

        /// <summary>
        /// Model identifier for gpt-3.5-turbo-1106.
        /// </summary>
        public const string GPT35Turbo1106 = "gpt-3.5-turbo-1106";

        /// <summary>
        /// Model identifier for gpt-3.5-turbo-instruct.
        /// </summary>
        public const string GPT35TurboInstruct = "gpt-3.5-turbo-instruct";

        /// <summary>
        /// Model identifier for gpt-3.5-turbo-16k.
        /// </summary>
        public const string GPT35Turbo16k = "gpt-3.5-turbo-16k";

        /// <summary>
        /// Model identifier for gpt-3.5-turbo-0613.
        /// </summary>
        public const string GPT35Turbo0613 = "gpt-3.5-turbo-0613";

        /// <summary>
        /// Model identifier for gpt-3.5-turbo-16k-0613.
        /// </summary>
        public const string GPT35Turbo16k0613 = "gpt-3.5-turbo-16k-0613";

        /// <summary>
        /// Model identifier for babbage-002.
        /// </summary>
        public const string Babbage002 = "babbage-002";

        /// <summary>
        /// Model identifier for davinci-002.
        /// </summary>
        public const string Davinci002 = "davinci-002";
    }

    /// <summary>
    /// Gemini chat completion model identifiers.
    /// </summary>
    public static class Gemini
    {
        /// <summary>
        /// Model identifier for gemini-1.5-pro-002.
        /// </summary>
        public const string Gemini15Pro002 = "gemini-1.5-pro-002";

        /// <summary>
        /// Model identifier for gemini-1.5-pro.
        /// </summary>
        public const string Gemini15Pro = "gemini-1.5-pro";

        /// <summary>
        /// Model identifier for gemini-1.5-flash.
        /// </summary>
        public const string Gemini15Flash = "gemini-1.5-flash";

        /// <summary>
        /// Model identifier for gemini-1.5-flash-002.
        /// </summary>
        public const string Gemini15Flash002 = "gemini-1.5-flash-002";

        /// <summary>
        /// Model identifier for gemini-1.5-flash-8b.
        /// </summary>
        public const string Gemini15Flash8B = "gemini-1.5-flash-8b";

        /// <summary>
        /// Model identifier for gemma-2-2b-it.
        /// </summary>
        public const string Gemma22B = "gemma-2-2b-it";

        /// <summary>
        /// Model identifier for gemma-2-9b-it.
        /// </summary>
        public const string Gemma29B = "gemma-2-9b-it";

        /// <summary>
        /// Model identifier for gemma-2-27b-it.
        /// </summary>
        public const string Gemma227B = "gemma-2-27b-it";

        /// <summary>
        /// Model identifier for gemini-1.5-pro-exp-0827.
        /// </summary>
        public const string Gemini15ProExperimental0827 = "gemini-1.5-pro-exp-0827";

        /// <summary>
        /// Model identifier for gemini-1.5-flash-exp-0827.
        /// </summary>
        public const string Gemini15FlashExperimental0827 = "gemini-1.5-flash-exp-0827";

        /// <summary>
        /// Model identifier for gemini-1.5-flash-8b-exp-0924.
        /// </summary>
        public const string Gemini15Flash8BExperimental0924 = "gemini-1.5-flash-8b-exp-0924";

        /// <summary>
        /// Model identifier for gemini-1.0-pro.
        /// </summary>
        public const string Gemini10Pro = "gemini-1.0-pro";
    }
}
