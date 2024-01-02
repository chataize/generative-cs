using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Options.OpenAI;
using GenerativeCS.Utilities;

namespace GenerativeCS.Providers.OpenAI;

internal static class Embeddings
{
    internal static async Task<List<float>> GetEmbeddingAsync(string text, string apiKey, EmbeddingOptions? options = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new();

        var request = new JsonObject
        {
            { "input", text },
            { "model", options.Model }
        };

        if (options.User != null)
        {
            request.Add("user", options.User);
        }

        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/embeddings", request, apiKey, options.MaxAttempts, cancellationToken);
        _ = response.EnsureSuccessStatusCode();

        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);
        var embedding = new List<float>();

        foreach (var element in responseDocument.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray())
        {
            embedding.Add(element.GetSingle());
        }

        return embedding;
    }
}
