using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.OpenAI;

internal static class Embeddings
{
    internal static async Task<float[]> GetEmbeddingAsync(string text, string? apiKey, EmbeddingOptions? options = null, TokenUsageTracker? usageTracker = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new();

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        var request = CreateEmbeddingRequest(text, false, options);

        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/embeddings", request, apiKey, options.MaxAttempts, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (usageTracker is not null)
        {
            var usage = responseDocument.RootElement.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();

            usageTracker.AddPromptTokens(promptTokens);
        }

        var embedding = new List<float>();
        foreach (var element in responseDocument.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray())
        {
            embedding.Add(element.GetSingle());
        }

        return [.. embedding];
    }

    internal static async Task<string> GetBase64EmbeddingAsync(string text, string? apiKey = null, EmbeddingOptions? options = null, TokenUsageTracker? usageTracker = null, HttpClient? httpClient = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        httpClient ??= new();

        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            apiKey = options.ApiKey;
        }

        var request = CreateEmbeddingRequest(text, true, options);

        using var response = await httpClient.RepeatPostAsJsonAsync("https://api.openai.com/v1/embeddings", request, apiKey, options.MaxAttempts, cancellationToken);
        using var responseContent = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var responseDocument = await JsonDocument.ParseAsync(responseContent, cancellationToken: cancellationToken);

        if (usageTracker is not null)
        {
            var usage = responseDocument.RootElement.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();

            usageTracker.AddPromptTokens(promptTokens);
        }

        return responseDocument.RootElement.GetProperty("data")[0].GetProperty("embedding").GetString()!;
    }

    private static JsonObject CreateEmbeddingRequest(string text, bool isBase64Format, EmbeddingOptions options)
    {
        var request = new JsonObject
        {
            { "input", text },
            { "model", options.Model }
        };

        if (isBase64Format)
        {
            request.Add("encoding_format", "base64");
        }

        if (options.Dimensions.HasValue)
        {
            request.Add("dimensions", options.Dimensions);
        }

        if (options.UserTrackingId is not null)
        {
            request.Add("user", options.UserTrackingId);
        }

        return request;
    }
}
