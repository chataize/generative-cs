using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
                    var retryDelay = GetRetryDelay(result, errorContent);
                    result.Dispose();
                    throw CreateHttpRequestException(result.StatusCode, errorContent, retryDelay);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (++attempts >= maxAttempts)
                {
                    throw;
                }

                await Task.Delay(GetRetryDelayFromException(exception) ?? GetBackoffDelay(attempts), cancellationToken);
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
                if (!result.IsSuccessStatusCode)
                {
                    var errorContent = await result.Content.ReadAsStringAsync(cancellationToken);
                    var retryDelay = GetRetryDelay(result, errorContent);
                    result.Dispose();
                    throw CreateHttpRequestException(result.StatusCode, errorContent, retryDelay);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (++attempts >= maxAttempts)
                {
                    throw;
                }

                await Task.Delay(GetRetryDelayFromException(exception) ?? GetBackoffDelay(attempts), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Posts arbitrary HTTP content with retries and throws on non-success responses.
    /// </summary>
    /// <param name="client">HTTP client used for the request.</param>
    /// <param name="requestUri">Target URI.</param>
    /// <param name="contentFactory">Factory that creates fresh request content for each attempt.</param>
    /// <param name="apiKey">Optional bearer token.</param>
    /// <param name="maxAttempts">Maximum retry attempts.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>HTTP response message.</returns>
    internal static async Task<HttpResponseMessage> RepeatPostAsync(this HttpClient client, [StringSyntax("Uri")] string requestUri, Func<HttpContent> contentFactory, string? apiKey = null, int maxAttempts = 5, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                using var content = contentFactory();
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
                if (!result.IsSuccessStatusCode)
                {
                    var errorContent = await result.Content.ReadAsStringAsync(cancellationToken);
                    var retryDelay = GetRetryDelay(result, errorContent);
                    result.Dispose();
                    throw CreateHttpRequestException(result.StatusCode, errorContent, retryDelay);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (++attempts >= maxAttempts)
                {
                    throw;
                }

                await Task.Delay(GetRetryDelayFromException(exception) ?? GetBackoffDelay(attempts), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Sends a fully customized HTTP request with retries and surfaces non-success responses with their bodies.
    /// </summary>
    /// <param name="client">HTTP client used for the request.</param>
    /// <param name="requestFactory">Factory that creates a fresh request for each attempt.</param>
    /// <param name="responseHeadersRead">True to return after headers for streaming scenarios.</param>
    /// <param name="maxAttempts">Maximum retry attempts.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The successful HTTP response.</returns>
    internal static async Task<HttpResponseMessage> RepeatSendAsync(this HttpClient client, Func<HttpRequestMessage> requestFactory, bool responseHeadersRead = false, int maxAttempts = 5, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                using var request = requestFactory();

                var result = await client.SendAsync(
                    request,
                    responseHeadersRead ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
                    cancellationToken);

                if (!result.IsSuccessStatusCode)
                {
                    var errorContent = await result.Content.ReadAsStringAsync(cancellationToken);
                    var retryDelay = GetRetryDelay(result, errorContent);
                    result.Dispose();
                    throw CreateHttpRequestException(result.StatusCode, errorContent, retryDelay);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (++attempts >= maxAttempts)
                {
                    throw;
                }

                await Task.Delay(GetRetryDelayFromException(exception) ?? GetBackoffDelay(attempts), cancellationToken);
            }
        }
    }

    private static TimeSpan GetBackoffDelay(int attempts)
    {
        var delayIndex = Math.Clamp(attempts - 1, 0, Delays.Length - 1);
        return Delays[delayIndex];
    }

    private static HttpRequestException CreateHttpRequestException(System.Net.HttpStatusCode statusCode, string errorContent, TimeSpan? retryDelay)
    {
        var exception = new HttpRequestException($"StatusCode {(int)statusCode}: {errorContent}", null, statusCode);
        if (retryDelay.HasValue)
        {
            exception.Data["RetryDelay"] = retryDelay.Value;
        }

        return exception;
    }

    private static TimeSpan? GetRetryDelayFromException(Exception exception)
    {
        return exception.Data["RetryDelay"] is TimeSpan retryDelay
            ? retryDelay
            : null;
    }

    private static TimeSpan? GetRetryDelay(HttpResponseMessage response, string? errorContent)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            var until = date - DateTimeOffset.UtcNow;
            if (until > TimeSpan.Zero)
            {
                return until;
            }
        }

        return ParseRetryDelayFromErrorContent(errorContent);
    }

    private static TimeSpan? ParseRetryDelayFromErrorContent(string? errorContent)
    {
        if (string.IsNullOrWhiteSpace(errorContent))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(errorContent);
            if (!document.RootElement.TryGetProperty("error", out var errorElement))
            {
                return null;
            }

            if (errorElement.TryGetProperty("details", out var detailsElement))
            {
                foreach (var detail in detailsElement.EnumerateArray())
                {
                    if (detail.TryGetProperty("@type", out var typeElement)
                        && string.Equals(typeElement.GetString(), "type.googleapis.com/google.rpc.RetryInfo", StringComparison.Ordinal)
                        && detail.TryGetProperty("retryDelay", out var retryDelayElement))
                    {
                        var retryDelay = ParseGoogleDuration(retryDelayElement.GetString());
                        if (retryDelay > TimeSpan.Zero)
                        {
                            return retryDelay;
                        }
                    }
                }
            }

            if (errorElement.TryGetProperty("message", out var messageElement))
            {
                var message = messageElement.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    var marker = "Please retry in ";
                    var markerIndex = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                    if (markerIndex >= 0)
                    {
                        var suffix = message[(markerIndex + marker.Length)..];
                        var amountText = new string(suffix.TakeWhile(c => char.IsDigit(c) || c is '.' or ',').ToArray());
                        var unitText = suffix[amountText.Length..].TrimStart();
                        if (double.TryParse(amountText, NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
                        {
                            if (unitText.StartsWith("ms", StringComparison.OrdinalIgnoreCase)
                                || unitText.StartsWith("millisecond", StringComparison.OrdinalIgnoreCase))
                            {
                                return TimeSpan.FromMilliseconds(amount);
                            }

                            if (unitText.StartsWith("m", StringComparison.OrdinalIgnoreCase)
                                && !unitText.StartsWith("ms", StringComparison.OrdinalIgnoreCase)
                                && !unitText.StartsWith("millisecond", StringComparison.OrdinalIgnoreCase)
                                && !unitText.StartsWith("minute", StringComparison.OrdinalIgnoreCase))
                            {
                                return TimeSpan.FromMinutes(amount);
                            }

                            if (unitText.StartsWith("minute", StringComparison.OrdinalIgnoreCase))
                            {
                                return TimeSpan.FromMinutes(amount);
                            }

                            return TimeSpan.FromSeconds(amount);
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static TimeSpan? ParseGoogleDuration(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
        {
            return null;
        }

        if (duration.EndsWith("ms", StringComparison.OrdinalIgnoreCase)
            && double.TryParse(duration[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var milliseconds))
        {
            return TimeSpan.FromMilliseconds(milliseconds);
        }

        if (duration.EndsWith('s')
            && double.TryParse(duration[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (duration.EndsWith('m')
            && double.TryParse(duration[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes))
        {
            return TimeSpan.FromMinutes(minutes);
        }

        if (duration.EndsWith('h')
            && double.TryParse(duration[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var hours))
        {
            return TimeSpan.FromHours(hours);
        }

        return null;
    }
}
