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
}
