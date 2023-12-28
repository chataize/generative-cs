using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;

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

    internal static async Task<HttpResponseMessage> RepeatPostAsJsonAsync<TValue>(this HttpClient client, [StringSyntax("Uri")] string? requestUri, TValue value, CancellationToken cancellationToke, int maxAttempts = 5)
    {
        var attempts = 0;
        while (true)
        {
            try
            {
                var result = await client.PostAsJsonAsync(requestUri, value, cancellationToke);
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
