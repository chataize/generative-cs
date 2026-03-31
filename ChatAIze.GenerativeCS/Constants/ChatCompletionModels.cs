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
        // Keep this list scoped to model IDs OpenAI still documents for chat-completions use.
        // Exclude IDs that are Responses-only, legacy-Completions-only, or already shut down.
        /// <summary>
        /// Model identifier for gpt-5.4.
        /// </summary>
        public const string GPT54 = "gpt-5.4";

        /// <summary>
        /// Model identifier for gpt-5.4-2026-03-05.
        /// </summary>
        public const string GPT5420260305 = "gpt-5.4-2026-03-05";

        /// <summary>
        /// Model identifier for gpt-5.3-chat-latest.
        /// </summary>
        public const string GPT53Chat = "gpt-5.3-chat-latest";

        /// <summary>
        /// Model identifier for gpt-5.3-codex.
        /// </summary>
        public const string GPT53Codex = "gpt-5.3-codex";

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
        /// Model identifier for gpt-5.2-codex.
        /// </summary>
        public const string GPT52Codex = "gpt-5.2-codex";

        /// <summary>
        /// Model identifier for gpt-5.1.
        /// </summary>
        public const string GPT51 = "gpt-5.1";

        /// <summary>
        /// Model identifier for gpt-5.1-2025-11-13.
        /// </summary>
        public const string GPT5120251113 = "gpt-5.1-2025-11-13";

        /// <summary>
        /// Model identifier for gpt-5.1-chat-latest.
        /// </summary>
        public const string GPT51Chat = "gpt-5.1-chat-latest";

        /// <summary>
        /// Model identifier for gpt-5.1-codex-mini.
        /// </summary>
        public const string GPT51CodexMini = "gpt-5.1-codex-mini";

        /// <summary>
        /// Model identifier for gpt-5.
        /// </summary>
        public const string GPT5 = "gpt-5";

        /// <summary>
        /// Model identifier for gpt-5-2025-08-07.
        /// </summary>
        public const string GPT520250807 = "gpt-5-2025-08-07";

        /// <summary>
        /// Model identifier for gpt-5-chat-latest.
        /// </summary>
        public const string GPT5Chat = "gpt-5-chat-latest";

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
        /// Model identifier for gpt-4.1.
        /// </summary>
        public const string GPT41 = "gpt-4.1";

        /// <summary>
        /// Model identifier for gpt-4.1-2025-04-14.
        /// </summary>
        public const string GPT4120250414 = "gpt-4.1-2025-04-14";

        /// <summary>
        /// Model identifier for gpt-4.1-mini.
        /// </summary>
        public const string GPT41Mini = "gpt-4.1-mini";

        /// <summary>
        /// Model identifier for gpt-4.1-mini-2025-04-14.
        /// </summary>
        public const string GPT41Mini20250414 = "gpt-4.1-mini-2025-04-14";

        /// <summary>
        /// Model identifier for gpt-4.1-nano.
        /// </summary>
        public const string GPT41Nano = "gpt-4.1-nano";

        /// <summary>
        /// Model identifier for gpt-4.1-nano-2025-04-14.
        /// </summary>
        public const string GPT41Nano20250414 = "gpt-4.1-nano-2025-04-14";

        /// <summary>
        /// Model identifier for o3.
        /// </summary>
        public const string O3 = "o3";

        /// <summary>
        /// Model identifier for o3-2025-04-16.
        /// </summary>
        public const string O320250416 = "o3-2025-04-16";

        /// <summary>
        /// Model identifier for o3-mini.
        /// </summary>
        public const string O3Mini = "o3-mini";

        /// <summary>
        /// Model identifier for o3-mini-2025-01-31.
        /// </summary>
        public const string O3Mini20250131 = "o3-mini-2025-01-31";

        /// <summary>
        /// Model identifier for o3-deep-research.
        /// </summary>
        public const string O3DeepResearch = "o3-deep-research";

        /// <summary>
        /// Model identifier for o3-deep-research-2025-06-26.
        /// </summary>
        public const string O3DeepResearch20250626 = "o3-deep-research-2025-06-26";

        /// <summary>
        /// Model identifier for o4-mini.
        /// </summary>
        public const string O4Mini = "o4-mini";

        /// <summary>
        /// Model identifier for o4-mini-2025-04-16.
        /// </summary>
        public const string O4Mini20250416 = "o4-mini-2025-04-16";

        /// <summary>
        /// Model identifier for o4-mini-deep-research.
        /// </summary>
        public const string O4MiniDeepResearch = "o4-mini-deep-research";

        /// <summary>
        /// Model identifier for o4-mini-deep-research-2025-06-26.
        /// </summary>
        public const string O4MiniDeepResearch20250626 = "o4-mini-deep-research-2025-06-26";

        /// <summary>
        /// Model identifier for o1.
        /// </summary>
        public const string O1 = "o1";

        /// <summary>
        /// Model identifier for o1-2024-12-17.
        /// </summary>
        public const string O120241217 = "o1-2024-12-17";

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
        /// Model identifier for gpt-4o-search-preview.
        /// </summary>
        public const string GPT4oSearchPreview = "gpt-4o-search-preview";

        /// <summary>
        /// Model identifier for gpt-4o-search-preview-2025-03-11.
        /// </summary>
        public const string GPT4oSearchPreview20250311 = "gpt-4o-search-preview-2025-03-11";

        /// <summary>
        /// Model identifier for gpt-4o-mini.
        /// </summary>
        public const string GPT4oMini = "gpt-4o-mini";

        /// <summary>
        /// Model identifier for gpt-4o-mini-2024-07-18.
        /// </summary>
        public const string GPT4oMini20240718 = "gpt-4o-mini-2024-07-18";

        /// <summary>
        /// Model identifier for gpt-4o-mini-search-preview.
        /// </summary>
        public const string GPT4oMiniSearchPreview = "gpt-4o-mini-search-preview";

        /// <summary>
        /// Model identifier for gpt-4o-mini-search-preview-2025-03-11.
        /// </summary>
        public const string GPT4oMiniSearchPreview20250311 = "gpt-4o-mini-search-preview-2025-03-11";

        /// <summary>
        /// Model identifier for gpt-4o-audio-preview.
        /// </summary>
        public const string GPT4oAudioPreview = "gpt-4o-audio-preview";

        /// <summary>
        /// Model identifier for gpt-4o-audio-preview-2025-06-03.
        /// </summary>
        public const string GPT4oAudioPreview20250603 = "gpt-4o-audio-preview-2025-06-03";

        /// <summary>
        /// Model identifier for gpt-4o-audio-preview-2024-12-17.
        /// </summary>
        public const string GPT4oAudioPreview20241217 = "gpt-4o-audio-preview-2024-12-17";

        /// <summary>
        /// Model identifier for gpt-4o-mini-audio-preview.
        /// </summary>
        public const string GPT4oMiniAudioPreview = "gpt-4o-mini-audio-preview";

        /// <summary>
        /// Model identifier for gpt-4o-mini-audio-preview-2024-12-17.
        /// </summary>
        public const string GPT4oMiniAudioPreview20241217 = "gpt-4o-mini-audio-preview-2024-12-17";

        /// <summary>
        /// Model identifier for gpt-audio.
        /// </summary>
        public const string GPTAudio = "gpt-audio";

        /// <summary>
        /// Model identifier for gpt-audio-2025-08-28.
        /// </summary>
        public const string GPTAudio20250828 = "gpt-audio-2025-08-28";

        /// <summary>
        /// Model identifier for gpt-audio-1.5.
        /// </summary>
        public const string GPTAudio15 = "gpt-audio-1.5";

        /// <summary>
        /// Model identifier for gpt-audio-mini.
        /// </summary>
        public const string GPTAudioMini = "gpt-audio-mini";

        /// <summary>
        /// Model identifier for gpt-audio-mini-2025-12-15.
        /// </summary>
        public const string GPTAudioMini20251215 = "gpt-audio-mini-2025-12-15";

        /// <summary>
        /// Model identifier for gpt-audio-mini-2025-10-06.
        /// </summary>
        public const string GPTAudioMini20251006 = "gpt-audio-mini-2025-10-06";

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
        /// Model identifier for gpt-4.
        /// </summary>
        public const string GPT4 = "gpt-4";

        /// <summary>
        /// Model identifier for gpt-4-0613.
        /// </summary>
        public const string GPT40613 = "gpt-4-0613";

        /// <summary>
        /// Model identifier for gpt-4-0314.
        /// </summary>
        public const string GPT40314 = "gpt-4-0314";

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

    /// <summary>
    /// Claude chat completion model identifiers.
    /// </summary>
    public static class Claude
    {
        /// <summary>
        /// Model identifier for claude-opus-4-6.
        /// </summary>
        public const string Opus46 = "claude-opus-4-6";

        /// <summary>
        /// Model identifier for claude-sonnet-4-6.
        /// </summary>
        public const string Sonnet46 = "claude-sonnet-4-6";

        /// <summary>
        /// Snapshot model identifier for claude-haiku-4-5-20251001.
        /// </summary>
        public const string Haiku4520251001 = "claude-haiku-4-5-20251001";

        /// <summary>
        /// Alias model identifier for claude-haiku-4-5.
        /// </summary>
        public const string Haiku45 = "claude-haiku-4-5";
    }

    /// <summary>
    /// xAI Grok chat completion model identifiers.
    /// </summary>
    public static class Grok
    {
        /// <summary>
        /// Model identifier for grok-3.
        /// </summary>
        public const string Grok3 = "grok-3";

        /// <summary>
        /// Model identifier for grok-3-mini.
        /// </summary>
        public const string Grok3Mini = "grok-3-mini";

        /// <summary>
        /// Model identifier for grok-4-0709.
        /// </summary>
        public const string Grok40709 = "grok-4-0709";

        /// <summary>
        /// Model identifier for grok-4-1-fast-non-reasoning.
        /// </summary>
        public const string Grok41FastNonReasoning = "grok-4-1-fast-non-reasoning";

        /// <summary>
        /// Model identifier for grok-4-1-fast-reasoning.
        /// </summary>
        public const string Grok41FastReasoning = "grok-4-1-fast-reasoning";

        /// <summary>
        /// Model identifier for grok-4-fast-non-reasoning.
        /// </summary>
        public const string Grok4FastNonReasoning = "grok-4-fast-non-reasoning";

        /// <summary>
        /// Model identifier for grok-4-fast-reasoning.
        /// </summary>
        public const string Grok4FastReasoning = "grok-4-fast-reasoning";

        /// <summary>
        /// Model identifier for grok-4.20-0309-non-reasoning.
        /// </summary>
        public const string Grok4200309NonReasoning = "grok-4.20-0309-non-reasoning";

        /// <summary>
        /// Model identifier for grok-4.20-0309-reasoning.
        /// </summary>
        public const string Grok4200309Reasoning = "grok-4.20-0309-reasoning";

        /// <summary>
        /// Model identifier for grok-4.20-multi-agent-0309.
        /// </summary>
        public const string Grok420MultiAgent0309 = "grok-4.20-multi-agent-0309";

        /// <summary>
        /// Model identifier for grok-code-fast-1.
        /// </summary>
        public const string GrokCodeFast1 = "grok-code-fast-1";
    }
}
