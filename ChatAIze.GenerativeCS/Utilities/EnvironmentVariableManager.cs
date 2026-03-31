using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Utilities;

/// <summary>
/// Provides helpers for retrieving API keys from environment variables.
/// </summary>
internal static class EnvironmentVariableManager
{
    /// <summary>
    /// Reads the OpenAI API key from the <c>OPENAI_API_KEY</c> environment variable.
    /// </summary>
    /// <returns>The configured API key or null.</returns>
    internal static string? GetOpenAIAPIKey()
    {
        // Return null when the variable is unset so callers can supply explicit keys.
        return Environment.GetEnvironmentVariable(EnvironmentVariables.OpenAIAPIKey);
    }

    /// <summary>
    /// Reads the Gemini API key from the <c>GEMINI_API_KEY</c> environment variable.
    /// </summary>
    /// <returns>The configured API key or null.</returns>
    internal static string? GetGeminiAPIKey()
    {
        // Return null when the variable is unset so callers can supply explicit keys.
        return Environment.GetEnvironmentVariable(EnvironmentVariables.GeminiAPIKey);
    }

    /// <summary>
    /// Reads the Claude API key from the official <c>ANTHROPIC_API_KEY</c> environment variable, then falls back to <c>CLAUDE_API_KEY</c>.
    /// </summary>
    /// <returns>The configured API key or null.</returns>
    internal static string? GetClaudeAPIKey()
    {
        // Prefer Anthropic's documented variable name, but accept a Claude-specific alias for caller convenience.
        return Environment.GetEnvironmentVariable(EnvironmentVariables.ClaudeAPIKey)
            ?? Environment.GetEnvironmentVariable(EnvironmentVariables.ClaudeAPIKeyAlias);
    }

    /// <summary>
    /// Reads the Grok API key from the official <c>XAI_API_KEY</c> environment variable, then falls back to <c>GROK_API_KEY</c>.
    /// </summary>
    /// <returns>The configured API key or null.</returns>
    internal static string? GetGrokAPIKey()
    {
        // Prefer xAI's documented variable name, but accept a Grok-specific alias for caller convenience.
        return Environment.GetEnvironmentVariable(EnvironmentVariables.GrokAPIKey)
            ?? Environment.GetEnvironmentVariable(EnvironmentVariables.GrokAPIKeyAlias);
    }
}
