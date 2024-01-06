using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ChatAIze.GenerativeCS.Utilities;

internal static class RepeatingHttpClient
{
    private static readonly TimeSpan[] Delays = [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    internal static async Task<HttpResponseMessage> RepeatPostAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string requestUri, TValue value, string? apiKey = null, int maxAttempts = 5, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var requestContent = JsonSerializer.Serialize(value);
                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(requestUri),
                    Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
                };

                if (apiKey != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var result = await client.SendAsync(request, cancellationToken);
                result.EnsureSuccessStatusCode();

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

    internal static async Task<HttpResponseMessage> RepeatPostAsJsonForStreamAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string requestUri, TValue value, string? apiKey = null, int maxAttempts = 5, CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var requestContent = JsonSerializer.Serialize(value);
                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(requestUri),
                    Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
                };

                if (apiKey != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var result = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                result.EnsureSuccessStatusCode();

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

                if (apiKey != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                var result = await client.SendAsync(request, cancellationToken);
                result.EnsureSuccessStatusCode();

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
