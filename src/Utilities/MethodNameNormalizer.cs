namespace ChatAIze.GenerativeCS.Utilities;

internal static class MethodNameNormalizer
{
    internal static string NormalizeMethodName(string methodName)
    {
        if (!methodName.Contains("<<"))
        {
            return methodName;
        }

        var start = methodName.IndexOf("__");
        var end = methodName.IndexOf('|');

        if (start < 0 || end < 0 || start >= end)
        {
            throw new Exception($"Unable to infer method name from callback '{methodName}'. Specify explicit method name when adding the function.");
        }

        return methodName[(start + 2)..end];
    }
}
