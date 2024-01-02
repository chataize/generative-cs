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
    [ActivatorUtilitiesConstructor]
    public OpenAIClient(HttpClient httpClient, IOptions<OpenAIClientOptions> options)
    {
        _httpClient = httpClient;

        ApiKey = options.Value.ApiKey;
        DefaultCompletionOptions = options.Value.DefaultCompletionOptions;
        DefaultEmbeddingOptions = options.Value.DefaultEmbeddingOptions;
    }

    [SetsRequiredMembers]
    public OpenAIClient(string apiKey)
    {
        ApiKey = apiKey;
    }

    [SetsRequiredMembers]
    public OpenAIClient(string apiKey, ChatCompletionOptions? defaultCompletionOptions)
    {
        ApiKey = apiKey;

        if (defaultCompletionOptions != null)
        {
            DefaultCompletionOptions = defaultCompletionOptions;
        }
    }

    [SetsRequiredMembers]
    public OpenAIClient(string apiKey, EmbeddingOptions? defaultEmbeddingOptions)
    {
        ApiKey = apiKey;

        if (defaultEmbeddingOptions != null)
        {
            DefaultEmbeddingOptions = defaultEmbeddingOptions;
        }
    }

    [SetsRequiredMembers]
    public OpenAIClient(string apiKey, TextToSpeechOptions? defaultTextToSpeechOptions)
    {
        ApiKey = apiKey;

        if (defaultTextToSpeechOptions != null)
        {
            DefaultTextToSpeechOptions = defaultTextToSpeechOptions;
        }
    }

    [SetsRequiredMembers]
    public OpenAIClient(string apiKey, TranscriptionOptions? defaultTranscriptionOptions)
    {
        ApiKey = apiKey;

        if (defaultTranscriptionOptions != null)
        {
            DefaultTranscriptionOptions = defaultTranscriptionOptions;
        }
    }

    [SetsRequiredMembers]
    public OpenAIClient(string apiKey, TranslationOptions? defaultTranslationOptions)
    {
        ApiKey = apiKey;

        if (defaultTranslationOptions != null)
        {
            DefaultTranslationOptions = defaultTranslationOptions;
        }
    }

    public required string ApiKey { get; set; }

    public ChatCompletionOptions? DefaultCompletionOptions { get; set; } = new();

    public EmbeddingOptions? DefaultEmbeddingOptions { get; set; } = new();

    public TextToSpeechOptions? DefaultTextToSpeechOptions { get; set; } = new();

    public TranscriptionOptions? DefaultTranscriptionOptions { get; set; } = new();

    public TranslationOptions? DefaultTranslationOptions { get; set; } = new();

    public static OpenAIClient CreateInstance(string apiKey)
    {
        return new OpenAIClient(apiKey);
    }

    public static OpenAIClient CreateInstance(string apiKey, ChatCompletionOptions? defaultCompletionOptions)
    {
        return new OpenAIClient(apiKey, defaultCompletionOptions);
    }

    public static OpenAIClient CreateInstance(string apiKey, EmbeddingOptions? defaultEmbeddingOptions)
    {
        return new OpenAIClient(apiKey, defaultEmbeddingOptions);
    }

    public static OpenAIClient CreateInstance(string apiKey, TextToSpeechOptions? defaultTextToSpeechOptions)
    {
        return new OpenAIClient(apiKey, defaultTextToSpeechOptions);
    }

    public static OpenAIClient CreateInstance(string apiKey, TranscriptionOptions? defaultTranscriptionOptions)
    {
        return new OpenAIClient(apiKey, defaultTranscriptionOptions);
    }

    public static OpenAIClient CreateInstance(string apiKey, TranslationOptions? defaultTranslationOptions)
    {
        return new OpenAIClient(apiKey, defaultTranslationOptions);
    }

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await CompleteAsync(new ChatConversation(prompt), options ?? DefaultCompletionOptions, cancellationToken);
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }

    public async IAsyncEnumerable<string> CompleteAsStreamAsync(string prompt, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.CompleteAsStreamAsync(new ChatConversation(prompt), ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async IAsyncEnumerable<string> CompleteAsStreamAsync(ChatConversation conversation, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.CompleteAsStreamAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<List<float>> GetEmbeddingAsync(string text, EmbeddingOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Embeddings.GetEmbeddingAsync(text, ApiKey, options ?? DefaultEmbeddingOptions, _httpClient, cancellationToken);
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await TextToSpeech.SynthesizeSpeechAsync(text, ApiKey, options ?? DefaultTextToSpeechOptions, _httpClient, cancellationToken);
    }

    public async Task<string> TranscriptAsync(byte[] audio, TranscriptionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await SpeechRecognition.TranscriptAsync(audio, ApiKey, options ?? DefaultTranscriptionOptions, _httpClient, cancellationToken);
    }

    public async Task<string> TranslateAsync(byte[] audio, TranslationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await SpeechRecognition.TranslateAsync(audio, ApiKey, options ?? DefaultTranslationOptions, _httpClient, cancellationToken);
    }
}
