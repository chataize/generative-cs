using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using GenerativeCS.Models;
using GenerativeCS.Options.OpenAI;
using GenerativeCS.Providers.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GenerativeCS.Clients;

public class OpenAIClient
{
    private readonly HttpClient _client = new();

    public OpenAIClient() { }

    [SetsRequiredMembers]
    public OpenAIClient(string apiKey)
    {
        ApiKey = apiKey;
    }

    [SetsRequiredMembers]
    [ActivatorUtilitiesConstructor]
    public OpenAIClient(IOptions<OpenAIClientOptions> options)
    {
        ApiKey = options.Value.ApiKey;
    }

    public required string ApiKey { get; set; }

    public static OpenAIClient CreateInstance(string apiKey)
    {
        return new OpenAIClient(apiKey);
    }

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await CompleteAsync(new ChatConversation(prompt), options, cancellationToken);
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(conversation, ApiKey, _client, options, cancellationToken);
    }

    public async IAsyncEnumerable<string> CompleteAsStreamAsync(ChatConversation conversation, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.CompleteAsStreamAsync(conversation, ApiKey, _client, options, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<List<float>> GetEmbeddingAsync(string text, EmbeddingOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Embeddings.GetEmbeddingAsync(text, ApiKey, _client, options, cancellationToken);
    }
}
