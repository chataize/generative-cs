using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ChatAIze.GenerativeCS.Utilities;

/// <summary>
/// Provides resilient HTTP helpers that retry transient failures.
/// </summary>
internal static class RepeatingHttpClient
{
    /// <summary>
    /// JSON serializer options used for outbound payloads.
    /// </summary>
    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    /// <summary>
    /// Backoff schedule applied between retries.
    /// </summary>
    private static readonly TimeSpan[] Delays = [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    /// <summary>
    /// Posts JSON content with retries and throws on non-success responses.
    /// </summary>
    /// <typeparam name="TValue">Payload type.</typeparam>
    /// <param name="client">HTTP client used for the request.</param>
    /// <param name="requestUri">Target URI.</param>
    /// <param name="value">Payload to serialize.</param>
    /// <param name="apiKey">Optional bearer token.</param>
    /// <param name="maxAttempts">Maximum retry attempts.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>HTTP response message.</returns>
    internal static async Task<HttpResponseMessage> RepeatPostAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string requestUri, TValue value, string? apiKey = null, int maxAttempts = 5, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var requestContent = JsonSerializer.Serialize(value, JsonOptions);
                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(requestUri),
                    Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
                };

                if (apiKey is not null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var result = await client.SendAsync(request, cancellationToken);
                if (!result.IsSuccessStatusCode)
                {
                    var errorContent = await result.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"StatusCode {(int)result.StatusCode}: {errorContent}");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                if (++attempts >= maxAttempts)
                {
                    throw;
                }

                await Task.Delay(Delays[attempts < Delays.Length ? attempts : Delays.Length - 1], cancellationToken);
            }
        }
    }

    /// <summary>
    /// Posts JSON content expecting a streamed response and retries on transient failures.
    /// </summary>
    /// <typeparam name="TValue">Payload type.</typeparam>
    /// <param name="client">HTTP client used for the request.</param>
    /// <param name="requestUri">Target URI.</param>
    /// <param name="value">Payload to serialize.</param>
    /// <param name="apiKey">Optional bearer token.</param>
    /// <param name="maxAttempts">Maximum retry attempts.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>HTTP response message.</returns>
    internal static async Task<HttpResponseMessage> RepeatPostAsJsonForStreamAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string requestUri, TValue value, string? apiKey = null, int maxAttempts = 5, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var requestContent = JsonSerializer.Serialize(value, JsonOptions);
                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(requestUri),
                    Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
                };

                if (apiKey is not null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var result = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                _ = result.EnsureSuccessStatusCode();

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                if (++attempts >= maxAttempts)
                {
                    throw;
                }

                await Task.Delay(Delays[attempts < Delays.Length ? attempts : Delays.Length - 1], cancellationToken);
            }
        }
    }

    /// <summary>
    /// Posts arbitrary HTTP content with retries and throws on non-success responses.
    /// </summary>
    /// <param name="client">HTTP client used for the request.</param>
    /// <param name="requestUri">Target URI.</param>
    /// <param name="content">Request content.</param>
    /// <param name="apiKey">Optional bearer token.</param>
    /// <param name="maxAttempts">Maximum retry attempts.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>HTTP response message.</returns>
    internal static async Task<HttpResponseMessage> RepeatPostAsync(this HttpClient client, [StringSyntax("Uri")] string requestUri, HttpContent content, string? apiKey = null, int maxAttempts = 5, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(requestUri),
                    Content = content
                };

                if (apiKey is not null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var result = await client.SendAsync(request, cancellationToken);
                _ = result.EnsureSuccessStatusCode();

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                if (++attempts >= maxAttempts)
                {
                    throw;
                }

                await Task.Delay(Delays[attempts < Delays.Length ? attempts : Delays.Length - 1], cancellationToken);
            }
        }
    }
}
