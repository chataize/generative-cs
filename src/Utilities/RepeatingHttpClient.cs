using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace GenerativeCS.Utilities;

internal static class RepeatingHttpClient
{
    private static readonly TimeSpan[] Delays = [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(3),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    internal static async Task<HttpResponseMessage> RepeatPostAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string requestUri, TValue value, CancellationToken cancellationToken, int maxAttempts = 5)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var result = await client.PostAsJsonAsync(requestUri, value, cancellationToken);
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

                await Task.Delay(Delays[attempts < Delays.Length ? attempts : Delays.Length - 1]);
            }
        }
    }

    internal static async Task<HttpResponseMessage> RepeatPostAsJsonForStreamAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string requestUri, TValue value, CancellationToken cancellationToken, int maxAttempts = 5)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var requestContent = JsonSerializer.Serialize(value);
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(requestUri),
                    Content = new StringContent(requestContent, Encoding.UTF8, "application/json")
                };

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

                await Task.Delay(Delays[attempts < Delays.Length ? attempts : Delays.Length - 1]);
            }
        }
    }
}
