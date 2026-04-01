using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Options.Gemini;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.Gemini;

/// <summary>
/// Handles Gemini embedding requests.
/// </summary>
internal static class Embeddings
{
    internal static async Task<float[]> GetEmbeddingAsync(
        string text,
        string? apiKey,
        EmbeddingOptions? options = null,
        TokenUsageTracker? usageTracker = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        httpClient ??= new()
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        options ??= new();
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key was not provided.");
        }

        var modelName = GeminiHttp.NormalizeModelName(options.Model);
        var request = new JsonObject
        {
            ["model"] = modelName,
            ["content"] = new JsonObject
            {
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["text"] = text
                    }
                }
            }
        };

        if (options.Dimensions.HasValue)
        {
            request["output_dimensionality"] = options.Dimensions.Value;
        }

        using var response = await GeminiHttp.SendJsonRequestAsync(
            httpClient,
            $"https://generativelanguage.googleapis.com/v1beta/{modelName}:embedContent",
            request,
            apiKey,
            options.MaxAttempts,
            cancellationToken);

        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (usageTracker is not null
            && responseDocument.RootElement.TryGetProperty("usageMetadata", out var usageElement)
            && usageElement.TryGetProperty("promptTokenCount", out var promptTokensElement))
        {
            usageTracker.AddPromptTokens(promptTokensElement.GetInt32());
        }

        return responseDocument.RootElement.GetProperty("embedding").GetProperty("values").EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();
    }

    internal static async Task<string> GetBase64EmbeddingAsync(
        string text,
        string? apiKey,
        EmbeddingOptions? options = null,
        TokenUsageTracker? usageTracker = null,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await GetEmbeddingAsync(text, apiKey, options, usageTracker, httpClient, cancellationToken);
        var bytes = new byte[embedding.Length * sizeof(float)];
        for (var i = 0; i < embedding.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float), sizeof(float)), embedding[i]);
        }

        return Convert.ToBase64String(bytes);
    }
}
