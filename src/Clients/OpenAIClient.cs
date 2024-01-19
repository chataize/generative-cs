using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using ChatAIze.GenerativeCS.Interfaces;
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
    public OpenAIClient(string apiKey)
    {
        ApiKey = apiKey;
    }

    [SetsRequiredMembers]
    public OpenAIClient(OpenAIClientOptions options)
    {
        ApiKey = options.ApiKey;
        DefaultCompletionOptions = options.DefaultCompletionOptions;
        DefaultEmbeddingOptions = options.DefaultEmbeddingOptions;
        DefaultTextToSpeechOptions = options.DefaultTextToSpeechOptions;
        DefaultTranscriptionOptions = options.DefaultTranscriptionOptions;
        DefaultTranslationOptions = options.DefaultTranslationOptions;
        DefaultModerationOptions = options.DefaultModerationOptions;
    }

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

    public ChatCompletionOptions DefaultCompletionOptions { get; set; } = new();

    public EmbeddingOptions DefaultEmbeddingOptions { get; set; } = new();

    public TextToSpeechOptions DefaultTextToSpeechOptions { get; set; } = new();

    public TranscriptionOptions DefaultTranscriptionOptions { get; set; } = new();

    public TranslationOptions DefaultTranslationOptions { get; set; } = new();

    public ModerationOptions DefaultModerationOptions { get; set; } = new();

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var conversation = new ChatConversation();
        conversation.FromUser(prompt);

        return await CompleteAsync(conversation, options ?? DefaultCompletionOptions, cancellationToken);
    }

    public async Task<string> CompleteAsync<T>(IChatConversation<T> conversation, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default) where T : IChatMessage, new()
    {
        return await ChatCompletion.CompleteAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversation = new ChatConversation();
        conversation.FromUser(prompt);

        await foreach (var chunk in ChatCompletion.StreamCompletionAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync<T>(IChatConversation<T> conversation, ChatCompletionOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : IChatMessage, new()
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

    public void AddFunction(ChatFunction function)
    {
        DefaultCompletionOptions.Functions.Add(function);
    }

    public void AddFunction(string name, bool requiresConfirmation = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresConfirmation));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresConfirmation));
    }

    public void AddFunction(Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(callback));
    }

    public void AddFunction(string name, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, callback));
    }

    public void AddFunction(string name, IEnumerable<FunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, params FunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, string? description, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, callback));
    }

    public void AddFunction(string name, string? description, IEnumerable<FunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, string? description, params FunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, bool requiresConfirmation, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresConfirmation, callback));
    }

    public void AddFunction(string name, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresConfirmation, callback));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresConfirmation, parameters));
    }

    public bool RemoveFunction(ChatFunction function)
    {
        return DefaultCompletionOptions.Functions.Remove(function);
    }

    public bool RemoveFunction(string name)
    {
        var function = DefaultCompletionOptions.Functions.LastOrDefault(f => f.Name == name);
        if (function == null)
        {
            return false;
        }

        return DefaultCompletionOptions.Functions.Remove(function);
    }

    public bool RemoveFunction(Delegate callback)
    {
        var function = DefaultCompletionOptions.Functions.LastOrDefault(f => f.Callback == callback);
        if (function == null)
        {
            return false;
        }

        return DefaultCompletionOptions.Functions.Remove(function);
    }

    public void ClearFunctions()
    {
        DefaultCompletionOptions.Functions.Clear();
    }
}
