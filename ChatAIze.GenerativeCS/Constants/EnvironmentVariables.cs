namespace ChatAIze.GenerativeCS.Constants;

/// <summary>
/// Names of environment variables used by the library.
/// </summary>
internal static class EnvironmentVariables
{
    /// <summary>
    /// Environment variable that stores the OpenAI API key.
    /// </summary>
    internal const string OpenAIAPIKey = "OPENAI_API_KEY";
    
    /// <summary>
    /// Environment variable that stores the Gemini API key.
    /// </summary>
    internal const string GeminiAPIKey = "GEMINI_API_KEY";

    /// <summary>
    /// Official Anthropic environment variable that stores the Claude API key.
    /// </summary>
    internal const string ClaudeAPIKey = "ANTHROPIC_API_KEY";

    /// <summary>
    /// Optional alias accepted for convenience when callers prefer a Claude-specific variable name.
    /// </summary>
    internal const string ClaudeAPIKeyAlias = "CLAUDE_API_KEY";
}
