using System.Text.Json;
using System.Text.Json.Nodes;
using GenerativeCS.Options.OpenAI;
using GenerativeCS.Utilities;

namespace GenerativeCS.Providers.OpenAI;

internal static class Embeddings
{
    internal static async Task<List<float>> GetEmbeddingAsync(string text, string apiKey, HttpClient? httpClient = null, EmbeddingOptions? options = null, CancellationToken cancellationToken = default)
    {
        httpClient ??= new HttpClient();
        options ??= new EmbeddingOptions();

        var request = new JsonObject
        {
            { "input", text },
            { "model", "text-embedding-ada-002" }
        };

        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/embeddings", request, cancellationToken, options.MaxAttempts);
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
