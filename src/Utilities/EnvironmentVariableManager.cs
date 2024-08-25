using ChatAIze.GenerativeCS.Constants;

namespace ChatAIze.GenerativeCS.Utilities;

internal static class EnvironmentVariableManager
{
    internal static string? GetOpenAIAPIKey()
    {
        return Environment.GetEnvironmentVariable(EnvironmentVariables.OpenAIAPIKey);
    }

    internal static string? GetGeminiAPIKey()
    {
        return Environment.GetEnvironmentVariable(EnvironmentVariables.GeminiAPIKey);
    }
}
