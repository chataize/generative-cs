using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace ChatAIze.GenerativeCS.Providers.Gemini;

/// <summary>
/// Preserves Gemini provider-specific assistant parts such as thought signatures across multi-turn chats.
/// </summary>
internal static class GeminiMessagePartStore
{
    private static readonly ConditionalWeakTable<object, JsonArray> PartsByMessage = new();

    internal static void Set(object message, JsonArray parts)
    {
        PartsByMessage.Remove(message);
        PartsByMessage.Add(message, (JsonArray)JsonNode.Parse(parts.ToJsonString())!);
    }

    internal static bool TryGet(object message, out JsonArray parts)
    {
        if (PartsByMessage.TryGetValue(message, out var storedParts))
        {
            parts = (JsonArray)JsonNode.Parse(storedParts.ToJsonString())!;
            return true;
        }

        parts = [];
        return false;
    }
}
