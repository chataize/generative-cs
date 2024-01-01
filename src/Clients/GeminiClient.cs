using System.Diagnostics.CodeAnalysis;
using GenerativeCS.Models;
using GenerativeCS.Options.Gemini;
using GenerativeCS.Providers.Gemini;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GenerativeCS.Clients;

public class GeminiClient
{
    private readonly HttpClient _httpClient = new();

    public GeminiClient() { }

    [SetsRequiredMembers]
    public GeminiClient(string apiKey, ChatCompletionOptions? defaultCompletionOptions = null)
    {
        ApiKey = apiKey;

        if (defaultCompletionOptions != null)
        {
            DefaultCompletionOptions = defaultCompletionOptions;
        }
    }

    [SetsRequiredMembers]
    [ActivatorUtilitiesConstructor]
    public GeminiClient(HttpClient httpClient, IOptions<GeminiClientOptions> options)
    {
        _httpClient = httpClient;

        ApiKey = options.Value.ApiKey;
        DefaultCompletionOptions = options.Value.DefaultCompletionOptions;
    }

    public required string ApiKey { get; set; }

    public ChatCompletionOptions? DefaultCompletionOptions { get; set; } = new();

    public static GeminiClient CreateInstance(string apiKey)
    {
        return new GeminiClient(apiKey);
    }

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(prompt, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }
}
