using System.Diagnostics.CodeAnalysis;
using GenerativeCS.Models;
using GenerativeCS.Options.Gemini;
using GenerativeCS.Providers.Gemini;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GenerativeCS.Clients;

public class GeminiClient
{
    private readonly HttpClient _client = new();

    public GeminiClient() { }

    [SetsRequiredMembers]
    public GeminiClient(string apiKey)
    {
        ApiKey = apiKey;
    }

    [SetsRequiredMembers]
    [ActivatorUtilitiesConstructor]
    public GeminiClient(IOptions<GeminiClientOptions> options)
    {
        ApiKey = options.Value.ApiKey;
    }

    public required string ApiKey { get; set; }

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletions.CompleteAsync(prompt, ApiKey, _client, options, cancellationToken);
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletions.CompleteAsync(conversation, ApiKey, _client, options, cancellationToken);
    }
}
