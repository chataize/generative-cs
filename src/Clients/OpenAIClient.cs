using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Providers.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatAIze.GenerativeCS.Clients;

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
        DefaultTextToSpeechOptions = options.Value.DefaultTextToSpeechOptions;
        DefaultTranscriptionOptions = options.Value.DefaultTranscriptionOptions;
        DefaultTranslationOptions = options.Value.DefaultTranslationOptions;
        DefaultModerationOptions = options.Value.DefaultModerationOptions;
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

    [SetsRequiredMembers]
    public OpenAIClient(string apiKey, ModerationOptions? defaultModerationOptions)
    {
        ApiKey = apiKey;

        if (defaultModerationOptions != null)
        {
            DefaultModerationOptions = defaultModerationOptions;
        }
    }

    public required string ApiKey { get; set; }

    public ChatCompletionOptions? DefaultCompletionOptions { get; set; } = new();

    public EmbeddingOptions? DefaultEmbeddingOptions { get; set; } = new();

    public TextToSpeechOptions? DefaultTextToSpeechOptions { get; set; } = new();

    public TranscriptionOptions? DefaultTranscriptionOptions { get; set; } = new();

    public TranslationOptions? DefaultTranslationOptions { get; set; } = new();

    public ModerationOptions? DefaultModerationOptions { get; set; } = new();

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

    public static OpenAIClient CreateInstance(string apiKey, ModerationOptions? defaultModerationOptions)
    {
        return new OpenAIClient(apiKey, defaultModerationOptions);
    }

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await CompleteAsync(new ChatConversation(prompt), options ?? DefaultCompletionOptions, cancellationToken);
    }

    public async Task<string> CompleteAsync(ChatConversation conversation, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.StreamCompletionAsync(new ChatConversation(prompt), ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(ChatConversation conversation, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.StreamCompletionAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<float[]> GetEmbeddingAsync(string text, EmbeddingOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Embeddings.GetEmbeddingAsync(text, ApiKey, options ?? DefaultEmbeddingOptions, _httpClient, cancellationToken);
    }

    public async Task<string> GetBase64EmbeddingAsync(string text, EmbeddingOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Embeddings.GetBase64EmbeddingAsync(text, ApiKey, options ?? DefaultEmbeddingOptions, _httpClient, cancellationToken);
    }

    public async Task<byte[]> SynthesizeSpeechAsync(string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await TextToSpeech.SynthesizeSpeechAsync(text, ApiKey, options ?? DefaultTextToSpeechOptions, _httpClient, cancellationToken);
    }

    public async Task SynthesizeSpeechAsync(string text, string outputFilePath, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        var audio = await TextToSpeech.SynthesizeSpeechAsync(text, ApiKey, options ?? DefaultTextToSpeechOptions, _httpClient, cancellationToken);
        await File.WriteAllBytesAsync(outputFilePath, audio, cancellationToken);
    }

    public async Task<string> TranscriptAsync(byte[] audio, TranscriptionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await SpeechRecognition.TranscriptAsync(audio, ApiKey, options ?? DefaultTranscriptionOptions, _httpClient, cancellationToken);
    }

    public async Task<string> TranscriptAsync(string audioFilePath, TranscriptionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var audio = await File.ReadAllBytesAsync(audioFilePath, cancellationToken);
        return await SpeechRecognition.TranscriptAsync(audio, ApiKey, options ?? DefaultTranscriptionOptions, _httpClient, cancellationToken);
    }

    public async Task<string> TranslateAsync(byte[] audio, TranslationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await SpeechRecognition.TranslateAsync(audio, ApiKey, options ?? DefaultTranslationOptions, _httpClient, cancellationToken);
    }

    public async Task<string> TranslateAsync(string audioFilePath, TranslationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var audio = await File.ReadAllBytesAsync(audioFilePath, cancellationToken);
        return await SpeechRecognition.TranslateAsync(audio, ApiKey, options ?? DefaultTranslationOptions, _httpClient, cancellationToken);
    }

    public async Task<ModerationResult> ModerateAsync(string text, ModerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Moderation.ModerateAsync(text, ApiKey, options ?? DefaultModerationOptions, _httpClient, cancellationToken);
    }
}
