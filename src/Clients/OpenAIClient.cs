using System.Runtime.CompilerServices;
using ChatAIze.GenerativeCS.Interfaces;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Providers.OpenAI;
using ChatAIze.GenerativeCS.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatAIze.GenerativeCS.Clients;

public class OpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>
    where TConversation : IChatConversation<TMessage, TFunctionCall, TFunctionResult>, new()
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
    where TFunctionCall : IFunctionCall, new()
    where TFunctionResult : IFunctionResult, new()
{
    private readonly HttpClient _httpClient = new();

    public OpenAIClient()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetOpenAIAPIKey();
        }
    }

    public OpenAIClient(string apiKey)
    {
        ApiKey = apiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetOpenAIAPIKey();
        }
    }

    public OpenAIClient(OpenAIClientOptions<TMessage, TFunctionCall, TFunctionResult> options)
    {
        ApiKey = options.ApiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetOpenAIAPIKey();
        }

        DefaultCompletionOptions = options.DefaultCompletionOptions;
        DefaultEmbeddingOptions = options.DefaultEmbeddingOptions;
        DefaultTextToSpeechOptions = options.DefaultTextToSpeechOptions;
        DefaultTranscriptionOptions = options.DefaultTranscriptionOptions;
        DefaultTranslationOptions = options.DefaultTranslationOptions;
        DefaultModerationOptions = options.DefaultModerationOptions;
    }

    [ActivatorUtilitiesConstructor]
    public OpenAIClient(HttpClient httpClient, IOptions<OpenAIClientOptions<TMessage, TFunctionCall, TFunctionResult>> options)
    {
        _httpClient = httpClient;
        ApiKey = options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetOpenAIAPIKey();
        }

        DefaultCompletionOptions = options.Value.DefaultCompletionOptions;
        DefaultEmbeddingOptions = options.Value.DefaultEmbeddingOptions;
        DefaultTextToSpeechOptions = options.Value.DefaultTextToSpeechOptions;
        DefaultTranscriptionOptions = options.Value.DefaultTranscriptionOptions;
        DefaultTranslationOptions = options.Value.DefaultTranslationOptions;
        DefaultModerationOptions = options.Value.DefaultModerationOptions;
    }

    public OpenAIClient(ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> defaultCompletionOptions)
    {
        DefaultCompletionOptions = defaultCompletionOptions;
    }

    public OpenAIClient(EmbeddingOptions defaultEmbeddingOptions)
    {
        DefaultEmbeddingOptions = defaultEmbeddingOptions;
    }

    public OpenAIClient(TextToSpeechOptions defaultTextToSpeechOptions)
    {
        DefaultTextToSpeechOptions = defaultTextToSpeechOptions;
    }

    public OpenAIClient(TranscriptionOptions defaultTranscriptionOptions)
    {
        DefaultTranscriptionOptions = defaultTranscriptionOptions;
    }

    public OpenAIClient(TranslationOptions defaultTranslationOptions)
    {
        DefaultTranslationOptions = defaultTranslationOptions;
    }

    public OpenAIClient(ModerationOptions defaultModerationOptions)
    {
        DefaultModerationOptions = defaultModerationOptions;
    }

    public string? ApiKey { get; set; }

    public ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> DefaultCompletionOptions { get; set; } = new();

    public EmbeddingOptions DefaultEmbeddingOptions { get; set; } = new();

    public TextToSpeechOptions DefaultTextToSpeechOptions { get; set; } = new();

    public TranscriptionOptions DefaultTranscriptionOptions { get; set; } = new();

    public TranslationOptions DefaultTranslationOptions { get; set; } = new();

    public ModerationOptions DefaultModerationOptions { get; set; } = new();

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        var conversation = new TConversation();
        _ = await conversation.FromUserAsync(prompt);

        return await CompleteAsync(conversation, options ?? DefaultCompletionOptions, usageTracker, cancellationToken);
    }

    public async Task<string> CompleteAsync(string systemMessage, string userMessage, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        var conversation = new TConversation();

        _ = await conversation.FromSystemAsync(systemMessage);
        _ = await conversation.FromUserAsync(userMessage);

        return await CompleteAsync(conversation, options ?? DefaultCompletionOptions, usageTracker, cancellationToken);
    }

    public async Task<string> CompleteAsync(TConversation conversation, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, usageTracker, _httpClient, cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var conversation = new TConversation();
        _ = await conversation.FromUserAsync(prompt);

        await foreach (var chunk in ChatCompletion.StreamCompletionAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, usageTracker, _httpClient, cancellationToken: cancellationToken))
        {
            yield return chunk;
        }
    }

    public async IAsyncEnumerable<string> StreamCompletionAsync(TConversation conversation, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.StreamCompletionAsync(conversation, ApiKey, options ?? DefaultCompletionOptions, usageTracker, _httpClient, cancellationToken: cancellationToken))
        {
            yield return chunk;
        }
    }

    public async Task<float[]> GetEmbeddingAsync(string text, EmbeddingOptions? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        return await Embeddings.GetEmbeddingAsync(text, ApiKey, options ?? DefaultEmbeddingOptions, usageTracker, _httpClient, cancellationToken);
    }

    public async Task<string> GetBase64EmbeddingAsync(string text, EmbeddingOptions? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        return await Embeddings.GetBase64EmbeddingAsync(text, ApiKey, options ?? DefaultEmbeddingOptions, usageTracker, _httpClient, cancellationToken);
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
        var function = DefaultCompletionOptions.Functions.FirstOrDefault(f => f.Name == name);
        if (function is null)
        {
            return false;
        }

        return DefaultCompletionOptions.Functions.Remove(function);
    }

    public bool RemoveFunction(Delegate callback)
    {
        var function = DefaultCompletionOptions.Functions.FirstOrDefault(f => f.Callback == callback);
        if (function is null)
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

public class OpenAIClient : OpenAIClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>
{
    public OpenAIClient() : base() { }

    public OpenAIClient(string apiKey) : base(apiKey) { }

    public OpenAIClient(OpenAIClientOptions options) : base(options) { }

    [ActivatorUtilitiesConstructor]
    public OpenAIClient(HttpClient httpClient, IOptions<OpenAIClientOptions> options) : base(httpClient, options) { }

    public OpenAIClient(ChatCompletionOptions defaultCompletionOptions) : base(defaultCompletionOptions) { }

    public OpenAIClient(EmbeddingOptions defaultEmbeddingOptions) : base(defaultEmbeddingOptions) { }

    public OpenAIClient(TextToSpeechOptions defaultTextToSpeechOptions) : base(defaultTextToSpeechOptions) { }

    public OpenAIClient(TranscriptionOptions defaultTranscriptionOptions) : base(defaultTranscriptionOptions) { }

    public OpenAIClient(TranslationOptions defaultTranslationOptions) : base(defaultTranslationOptions) { }

    public OpenAIClient(ModerationOptions defaultModerationOptions) : base(defaultModerationOptions) { }
}
