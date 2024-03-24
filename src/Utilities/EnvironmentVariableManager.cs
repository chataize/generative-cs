using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Utilities;

internal static class EnvironmentVariableManager
{
    internal static string GetOpenAIAPIKey()
    {
        var apiKey = Environment.GetEnvironmentVariable(EnvironmentVariables.OpenAIAPIKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("OPENAI_API_KEY environment variable is not set.");
        }

        return apiKey;
    }

    internal static string GetGeminiAPIKey()
    {
        var apiKey = Environment.GetEnvironmentVariable(EnvironmentVariables.GeminiAPIKey);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new Exception("GEMINI_API_KEY environment variable is not set.");
        }

        return apiKey;
    }
}
