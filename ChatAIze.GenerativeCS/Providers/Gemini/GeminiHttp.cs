using System.Net.Mime;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.Gemini;

/// <summary>
/// Shared Gemini HTTP helpers.
/// </summary>
internal static class GeminiHttp
{
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    internal static async Task<HttpResponseMessage> SendGenerateContentRequestAsync(
        HttpClient httpClient,
        string model,
        JsonObject request,
        string apiKey,
        int maxAttempts,
        bool isStreaming,
        CancellationToken cancellationToken)
    {
        var methodName = isStreaming ? "streamGenerateContent?alt=sse" : "generateContent";
        var requestUri = $"https://generativelanguage.googleapis.com/v1beta/{NormalizeModelName(model)}:{methodName}";

        return await httpClient.RepeatSendAsync(() =>
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(request.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.Add("x-goog-api-key", apiKey);
            return requestMessage;
        }, responseHeadersRead: isStreaming, maxAttempts: maxAttempts, cancellationToken: cancellationToken);
    }

    internal static async Task<HttpResponseMessage> SendJsonRequestAsync(
        HttpClient httpClient,
        string requestUri,
        JsonObject request,
        string apiKey,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        return await httpClient.RepeatSendAsync(() =>
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(request.ToJsonString(JsonOptions), Encoding.UTF8, "application/json")
            };
            requestMessage.Headers.Add("x-goog-api-key", apiKey);
            return requestMessage;
        }, responseHeadersRead: false, maxAttempts: maxAttempts, cancellationToken: cancellationToken);
    }

    internal static async Task<JsonObject> DownloadRemoteFileAsInlineDataAsync(HttpClient httpClient, string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Gemini image input received an empty URL.");
        }

        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return ParseDataUrl(url);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Failed to download Gemini image input '{url}'. StatusCode {(int)response.StatusCode}: {error}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var mimeType = response.Content.Headers.ContentType?.MediaType;
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            mimeType = GetMimeTypeFromFileName(url);
        }

        return new JsonObject
        {
            ["mime_type"] = mimeType,
            ["data"] = Convert.ToBase64String(bytes)
        };
    }

    internal static string NormalizeModelName(string model)
    {
        return model.StartsWith("models/", StringComparison.OrdinalIgnoreCase) ? model : $"models/{model}";
    }

    private static JsonObject ParseDataUrl(string dataUrl)
    {
        var separatorIndex = dataUrl.IndexOf(',', StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            throw new InvalidOperationException("Invalid data URL supplied to Gemini image input.");
        }

        var header = dataUrl[5..separatorIndex];
        var isBase64 = header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase);
        var mimeType = header.Split(';', 2)[0];
        var payload = dataUrl[(separatorIndex + 1)..];

        byte[] bytes = isBase64
            ? Convert.FromBase64String(payload)
            : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));

        return new JsonObject
        {
            ["mime_type"] = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType,
            ["data"] = Convert.ToBase64String(bytes)
        };
    }

    private static string GetMimeTypeFromFileName(string fileName)
    {
        var normalizedFileName = fileName.Split('?', '#')[0];
        var extension = Path.GetExtension(normalizedFileName).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".mp4" => "audio/mp4",
            ".webm" => "audio/webm",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            _ => MediaTypeNames.Application.Octet
        };
    }
}
