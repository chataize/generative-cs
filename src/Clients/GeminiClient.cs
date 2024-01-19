using System.Diagnostics.CodeAnalysis;
using ChatAIze.GenerativeCS.Interfaces;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Gemini;
using ChatAIze.GenerativeCS.Providers.Gemini;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatAIze.GenerativeCS.Clients;

public class GeminiClient
{
    private readonly HttpClient _httpClient = new();

    public GeminiClient() { }

    [SetsRequiredMembers]
    public GeminiClient(string apiKey)
    {
        ApiKey = apiKey;
    }

    [SetsRequiredMembers]
    public GeminiClient(GeminiClientOptions options)
    {
        ApiKey = options.ApiKey;
        DefaultCompletionOptions = options.DefaultCompletionOptions;
    }

    [SetsRequiredMembers]
    [ActivatorUtilitiesConstructor]
    public GeminiClient(HttpClient httpClient, IOptions<GeminiClientOptions> options)
    {
        _httpClient = httpClient;

        ApiKey = options.Value.ApiKey;
        DefaultCompletionOptions = options.Value.DefaultCompletionOptions;
    }


    [SetsRequiredMembers]
    public GeminiClient(string apiKey, ChatCompletionOptions? defaultCompletionOptions = null)
    {
        ApiKey = apiKey;

        if (defaultCompletionOptions != null)
        {
            DefaultCompletionOptions = defaultCompletionOptions;
        }
    }

    public required string ApiKey { get; set; }

    public ChatCompletionOptions DefaultCompletionOptions { get; set; } = new();

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(prompt, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }

    public async Task<string> CompleteAsync<T>(IChatConversation<T> conversation, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default) where T : IChatMessage, new()
    {
        return await ChatCompletion.CompleteAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }
}
