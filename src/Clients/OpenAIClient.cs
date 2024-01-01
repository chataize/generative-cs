using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using GenerativeCS.Models;
using GenerativeCS.Options.OpenAI;
using GenerativeCS.Providers.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GenerativeCS.Clients;

public class OpenAIClient
{
    private readonly HttpClient _httpClient = new();

    public OpenAIClient() { }

    [SetsRequiredMembers]
    public OpenAIClient(string apiKey, ChatCompletionOptions? defaultCompletionOptions = null)
    {
        ApiKey = apiKey;

        if (defaultCompletionOptions != null)
        {
            DefaultCompletionOptions = defaultCompletionOptions;
        }
    }

    [SetsRequiredMembers]
    [ActivatorUtilitiesConstructor]
    public OpenAIClient(HttpClient httpClient, IOptions<OpenAIClientOptions> options)
    {
        _httpClient = httpClient;

        ApiKey = options.Value.ApiKey;
        DefaultCompletionOptions = options.Value.DefaultCompletionOptions;
        DefaultEmbeddingOptions = options.Value.DefaultEmbeddingOptions;
    }

    public required string ApiKey { get; set; }

    public ChatCompletionOptions? DefaultCompletionOptions { get; set; } = new();

    public EmbeddingOptions? DefaultEmbeddingOptions { get; set; } = new();

    public static OpenAIClient CreateInstance(string apiKey)
    {
        return new OpenAIClient(apiKey);
    }

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await CompleteAsync(new ChatConversation(prompt), options ?? DefaultCompletionOptions, cancellationToken);
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(conversation, ApiKey, _httpClient, options ?? DefaultCompletionOptions, cancellationToken);
    }

    public async IAsyncEnumerable<string> CompleteAsStreamAsync(string prompt, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.CompleteAsStreamAsync(new ChatConversation(prompt), ApiKey, _httpClient, options ?? DefaultCompletionOptions, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async IAsyncEnumerable<string> CompleteAsStreamAsync(ChatConversation conversation, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.CompleteAsStreamAsync(conversation, ApiKey, _httpClient, options ?? DefaultCompletionOptions, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<List<float>> GetEmbeddingAsync(string text, EmbeddingOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Embeddings.GetEmbeddingAsync(text, ApiKey, _httpClient, options ?? DefaultEmbeddingOptions, cancellationToken);
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await TextToSpeech.SynthesizeSpeechAsync(text, ApiKey, options, _httpClient, cancellationToken);
    }
}
