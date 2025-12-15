using System.Runtime.CompilerServices;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;
using ChatAIze.GenerativeCS.Providers.OpenAI;
using ChatAIze.GenerativeCS.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatAIze.GenerativeCS.Clients;

/// <summary>
/// Client wrapper around OpenAI endpoints for chat completions, embeddings, speech, transcription, translation, and moderation.
/// </summary>
/// <typeparam name="TChat">Concrete chat container type.</typeparam>
/// <typeparam name="TMessage">Concrete chat message type.</typeparam>
/// <typeparam name="TFunctionCall">Concrete function call type.</typeparam>
/// <typeparam name="TFunctionResult">Concrete function result type.</typeparam>
public class OpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>
    where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
    where TFunctionCall : IFunctionCall, new()
    where TFunctionResult : IFunctionResult, new()
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(15)
    };

    /// <summary>
    /// Initializes a new instance of the client using the API key from the environment when available.
    /// </summary>
    public OpenAIClient()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetOpenAIAPIKey();
        }
    }

    /// <summary>
    /// Initializes a new instance of the client with a specific API key or falls back to the environment.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key to use.</param>
    public OpenAIClient(string apiKey)
    {
        ApiKey = apiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetOpenAIAPIKey();
        }
    }

    /// <summary>
    /// Initializes a new instance of the client with explicit default options.
    /// </summary>
    /// <param name="options">Typed client options.</param>
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

    /// <summary>
    /// Initializes a new instance of the client using dependency injection managed options.
    /// </summary>
    /// <param name="httpClient">Preconfigured HTTP client.</param>
    /// <param name="options">Typed options snapshot from DI.</param>
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

    /// <summary>
    /// Initializes a new instance of the client with a default chat completion configuration.
    /// </summary>
    /// <param name="defaultCompletionOptions">Default chat completion options.</param>
    public OpenAIClient(ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> defaultCompletionOptions)
    {
        DefaultCompletionOptions = defaultCompletionOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client with default embedding options.
    /// </summary>
    /// <param name="defaultEmbeddingOptions">Default embedding options.</param>
    public OpenAIClient(EmbeddingOptions defaultEmbeddingOptions)
    {
        DefaultEmbeddingOptions = defaultEmbeddingOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client with default text-to-speech options.
    /// </summary>
    /// <param name="defaultTextToSpeechOptions">Default text-to-speech options.</param>
    public OpenAIClient(TextToSpeechOptions defaultTextToSpeechOptions)
    {
        DefaultTextToSpeechOptions = defaultTextToSpeechOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client with default transcription options.
    /// </summary>
    /// <param name="defaultTranscriptionOptions">Default transcription options.</param>
    public OpenAIClient(TranscriptionOptions defaultTranscriptionOptions)
    {
        DefaultTranscriptionOptions = defaultTranscriptionOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client with default translation options.
    /// </summary>
    /// <param name="defaultTranslationOptions">Default translation options.</param>
    public OpenAIClient(TranslationOptions defaultTranslationOptions)
    {
        DefaultTranslationOptions = defaultTranslationOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client with default moderation options.
    /// </summary>
    /// <param name="defaultModerationOptions">Default moderation options.</param>
    public OpenAIClient(ModerationOptions defaultModerationOptions)
    {
        DefaultModerationOptions = defaultModerationOptions;
    }

    /// <summary>
    /// Gets or sets the API key used for outbound OpenAI requests.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the default chat completion options applied when none are supplied.
    /// </summary>
    public ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> DefaultCompletionOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default embedding options applied when none are supplied.
    /// </summary>
    public EmbeddingOptions DefaultEmbeddingOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default text-to-speech options applied when none are supplied.
    /// </summary>
    public TextToSpeechOptions DefaultTextToSpeechOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default transcription options applied when none are supplied.
    /// </summary>
    public TranscriptionOptions DefaultTranscriptionOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default translation options applied when none are supplied.
    /// </summary>
    public TranslationOptions DefaultTranslationOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default moderation options applied when none are supplied.
    /// </summary>
    public ModerationOptions DefaultModerationOptions { get; set; } = new();

    /// <summary>
    /// Runs a one-off chat completion starting from a text prompt.
    /// </summary>
    /// <param name="prompt">User text to send to the model.</param>
    /// <param name="options">Optional per-request completion options.</param>
    /// <param name="usageTracker">Optional tracker that accumulates token usage from the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Generated text from the model.</returns>
    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        var chat = new TChat();
        _ = await chat.FromUserAsync(prompt);

        return await CompleteAsync(chat, options ?? DefaultCompletionOptions, usageTracker, cancellationToken);
    }

    /// <summary>
    /// Runs a one-off completion with an explicit system priming message and user message.
    /// </summary>
    /// <param name="systemMessage">The system or developer directive for the model.</param>
    /// <param name="userMessage">User text to send to the model.</param>
    /// <param name="options">Optional per-request completion options.</param>
    /// <param name="usageTracker">Optional tracker that accumulates token usage from the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Generated text from the model.</returns>
    public async Task<string> CompleteAsync(string systemMessage, string userMessage, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        var chat = new TChat();

        _ = await chat.FromSystemAsync(systemMessage);
        _ = await chat.FromUserAsync(userMessage);

        return await CompleteAsync(chat, options ?? DefaultCompletionOptions, usageTracker, cancellationToken);
    }

    /// <summary>
    /// Runs a completion based on a prebuilt chat transcript.
    /// </summary>
    /// <param name="chat">Chat history containing prior messages.</param>
    /// <param name="options">Optional per-request completion options.</param>
    /// <param name="usageTracker">Optional tracker that accumulates token usage from the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Generated text from the model.</returns>
    public async Task<string> CompleteAsync(TChat chat, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(chat, ApiKey, options ?? DefaultCompletionOptions, usageTracker, _httpClient, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Streams a completion response token-by-token for a plain text prompt.
    /// </summary>
    /// <param name="prompt">User text to send to the model.</param>
    /// <param name="options">Optional per-request completion options.</param>
    /// <param name="usageTracker">Optional tracker that accumulates token usage from the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async stream of response chunks.</returns>
    public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chat = new TChat();
        _ = await chat.FromUserAsync(prompt);

        await foreach (var chunk in ChatCompletion.StreamCompletionAsync(chat, ApiKey, options ?? DefaultCompletionOptions, usageTracker, _httpClient, cancellationToken: cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Streams a completion response token-by-token for an existing chat transcript.
    /// </summary>
    /// <param name="chat">Chat history containing prior messages.</param>
    /// <param name="options">Optional per-request completion options.</param>
    /// <param name="usageTracker">Optional tracker that accumulates token usage from the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>An async stream of response chunks.</returns>
    public async IAsyncEnumerable<string> StreamCompletionAsync(TChat chat, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.StreamCompletionAsync(chat, ApiKey, options ?? DefaultCompletionOptions, usageTracker, _httpClient, cancellationToken: cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Generates an embedding vector for the supplied text.
    /// </summary>
    /// <param name="text">Text to embed.</param>
    /// <param name="options">Optional per-request embedding options.</param>
    /// <param name="usageTracker">Optional tracker that accumulates token usage from the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Floating point embedding vector.</returns>
    public async Task<float[]> GetEmbeddingAsync(string text, EmbeddingOptions? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        return await Embeddings.GetEmbeddingAsync(text, ApiKey, options ?? DefaultEmbeddingOptions, usageTracker, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Generates an embedding encoded as Base64 for the supplied text.
    /// </summary>
    /// <param name="text">Text to embed.</param>
    /// <param name="options">Optional per-request embedding options.</param>
    /// <param name="usageTracker">Optional tracker that accumulates token usage from the request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Embedding encoded as a Base64 string.</returns>
    public async Task<string> GetBase64EmbeddingAsync(string text, EmbeddingOptions? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        return await Embeddings.GetBase64EmbeddingAsync(text, ApiKey, options ?? DefaultEmbeddingOptions, usageTracker, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Synthesizes speech audio bytes from text.
    /// </summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="options">Optional text-to-speech options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Raw audio bytes.</returns>
    public async Task<byte[]> SynthesizeSpeechAsync(string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await TextToSpeech.SynthesizeSpeechAsync(text, ApiKey, options ?? DefaultTextToSpeechOptions, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Synthesizes speech audio from text and saves it to disk.
    /// </summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="outputFilePath">File path where the audio file will be written.</param>
    /// <param name="options">Optional text-to-speech options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task SynthesizeSpeechAsync(string text, string outputFilePath, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        var audio = await TextToSpeech.SynthesizeSpeechAsync(text, ApiKey, options ?? DefaultTextToSpeechOptions, _httpClient, cancellationToken);
        await File.WriteAllBytesAsync(outputFilePath, audio, cancellationToken);
    }

    /// <summary>
    /// Generates a text transcript from audio bytes.
    /// </summary>
    /// <param name="audio">Audio payload to transcribe.</param>
    /// <param name="options">Optional transcription options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Text transcript of the audio.</returns>
    public async Task<string> TranscriptAsync(byte[] audio, TranscriptionOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await SpeechRecognition.TranscriptAsync(audio, ApiKey, options ?? DefaultTranscriptionOptions, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Generates a text transcript from an audio file.
    /// </summary>
    /// <param name="audioFilePath">File path to the audio content.</param>
    /// <param name="options">Optional transcription options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Text transcript of the audio.</returns>
    public async Task<string> TranscriptAsync(string audioFilePath, TranscriptionOptions? options = null, CancellationToken cancellationToken = default)
    {
        var audio = await File.ReadAllBytesAsync(audioFilePath, cancellationToken);
        return await SpeechRecognition.TranscriptAsync(audio, ApiKey, options ?? DefaultTranscriptionOptions, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Translates audio bytes to English text.
    /// </summary>
    /// <param name="audio">Audio payload to translate.</param>
    /// <param name="options">Optional translation options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Translated text.</returns>
    public async Task<string> TranslateAsync(byte[] audio, TranslationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await SpeechRecognition.TranslateAsync(audio, ApiKey, options ?? DefaultTranslationOptions, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Translates an audio file to English text.
    /// </summary>
    /// <param name="audioFilePath">File path to the audio content.</param>
    /// <param name="options">Optional translation options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Translated text.</returns>
    public async Task<string> TranslateAsync(string audioFilePath, TranslationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var audio = await File.ReadAllBytesAsync(audioFilePath, cancellationToken);
        return await SpeechRecognition.TranslateAsync(audio, ApiKey, options ?? DefaultTranslationOptions, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Runs text through the OpenAI moderation endpoint.
    /// </summary>
    /// <param name="text">Text to moderate.</param>
    /// <param name="options">Optional moderation options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Structured moderation result.</returns>
    public async Task<ModerationResult> ModerateAsync(string text, ModerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Moderation.ModerateAsync(text, ApiKey, options ?? DefaultModerationOptions, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Adds a prebuilt function to the default chat completion options.
    /// </summary>
    /// <param name="function">Function metadata to register.</param>
    public void AddFunction(IChatFunction function)
    {
        DefaultCompletionOptions.Functions.Add(function);
    }

    /// <summary>
    /// Adds a function by name without a description.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    public void AddFunction(string name, bool requiresDoubleCheck = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck));
    }

    /// <summary>
    /// Adds a function by name with a description.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck));
    }

    /// <summary>
    /// Adds a function that maps to a delegate callback.
    /// </summary>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(callback));
    }

    /// <summary>
    /// Adds a named function that maps to a delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, callback));
    }

    /// <summary>
    /// Adds a named function with explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="parameters">Collection of function parameters.</param>
    public void AddFunction(string name, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    /// <summary>
    /// Adds a named function with explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="parameters">Collection of function parameters.</param>
    public void AddFunction(string name, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    /// <summary>
    /// Adds a named function with a description and delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, string? description, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, callback));
    }

    /// <summary>
    /// Adds a named function with a description and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="parameters">Collection of function parameters.</param>
    public void AddFunction(string name, string? description, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    /// <summary>
    /// Adds a named function with a description and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="parameters">Collection of function parameters.</param>
    public void AddFunction(string name, string? description, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    /// <summary>
    /// Adds a named function that requires double checking and maps to a delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, bool requiresDoubleCheck, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, callback));
    }

    /// <summary>
    /// Adds a named function that requires double checking and declares parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Collection of function parameters.</param>
    public void AddFunction(string name, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Adds a named function that requires double checking and declares parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Collection of function parameters.</param>
    public void AddFunction(string name, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Adds a named function with description, confirmation loop, and delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, callback));
    }

    /// <summary>
    /// Adds a named function with description, confirmation loop, and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Collection of function parameters.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Adds a named function with description, confirmation loop, and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, params FunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Removes a function from the default options by reference.
    /// </summary>
    /// <param name="function">Function metadata instance to remove.</param>
    /// <returns>True when the function was removed.</returns>
    public bool RemoveFunction(ChatFunction function)
    {
        return DefaultCompletionOptions.Functions.Remove(function);
    }

    /// <summary>
    /// Removes a function from the default options by its name.
    /// </summary>
    /// <param name="name">Function name to remove.</param>
    /// <returns>True when a matching function was removed.</returns>
    public bool RemoveFunction(string name)
    {
        var function = DefaultCompletionOptions.Functions.FirstOrDefault(f => f.Name == name);
        if (function is null)
        {
            return false;
        }

        return DefaultCompletionOptions.Functions.Remove(function);
    }

    /// <summary>
    /// Removes a function that matches the supplied callback.
    /// </summary>
    /// <param name="callback">Delegate used to locate the function.</param>
    /// <returns>True when a matching function was removed.</returns>
    public bool RemoveFunction(Delegate callback)
    {
        var function = DefaultCompletionOptions.Functions.FirstOrDefault(f => f.Callback == callback);
        if (function is null)
        {
            return false;
        }

        return DefaultCompletionOptions.Functions.Remove(function);
    }

    /// <summary>
    /// Clears all registered functions from the default completion options.
    /// </summary>
    public void ClearFunctions()
    {
        DefaultCompletionOptions.Functions.Clear();
    }
}

/// <summary>
/// Non-generic convenience client that uses built-in chat, message, function call, and function result types.
/// </summary>
public class OpenAIClient : OpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes a new instance of the client using environment configuration.
    /// </summary>
    public OpenAIClient() : base() { }

    /// <summary>
    /// Initializes a new instance of the client with a specific API key.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key to use.</param>
    public OpenAIClient(string apiKey) : base(apiKey) { }

    /// <summary>
    /// Initializes a new instance of the client with explicit default options.
    /// </summary>
    /// <param name="options">OpenAI client options.</param>
    public OpenAIClient(OpenAIClientOptions options) : base(options) { }

    /// <summary>
    /// Initializes a new instance of the client using dependency injection managed options.
    /// </summary>
    /// <param name="httpClient">Preconfigured HTTP client.</param>
    /// <param name="options">Typed options snapshot from DI.</param>
    [ActivatorUtilitiesConstructor]
    public OpenAIClient(HttpClient httpClient, IOptions<OpenAIClientOptions> options) : base(httpClient, options) { }

    /// <summary>
    /// Initializes a new instance of the client with a default chat completion configuration.
    /// </summary>
    /// <param name="defaultCompletionOptions">Default chat completion options.</param>
    public OpenAIClient(ChatCompletionOptions defaultCompletionOptions) : base(defaultCompletionOptions) { }

    /// <summary>
    /// Initializes a new instance of the client with default embedding options.
    /// </summary>
    /// <param name="defaultEmbeddingOptions">Default embedding options.</param>
    public OpenAIClient(EmbeddingOptions defaultEmbeddingOptions) : base(defaultEmbeddingOptions) { }

    /// <summary>
    /// Initializes a new instance of the client with default text-to-speech options.
    /// </summary>
    /// <param name="defaultTextToSpeechOptions">Default text-to-speech options.</param>
    public OpenAIClient(TextToSpeechOptions defaultTextToSpeechOptions) : base(defaultTextToSpeechOptions) { }

    /// <summary>
    /// Initializes a new instance of the client with default transcription options.
    /// </summary>
    /// <param name="defaultTranscriptionOptions">Default transcription options.</param>
    public OpenAIClient(TranscriptionOptions defaultTranscriptionOptions) : base(defaultTranscriptionOptions) { }

    /// <summary>
    /// Initializes a new instance of the client with default translation options.
    /// </summary>
    /// <param name="defaultTranslationOptions">Default translation options.</param>
    public OpenAIClient(TranslationOptions defaultTranslationOptions) : base(defaultTranslationOptions) { }

    /// <summary>
    /// Initializes a new instance of the client with default moderation options.
    /// </summary>
    /// <param name="defaultModerationOptions">Default moderation options.</param>
    public OpenAIClient(ModerationOptions defaultModerationOptions) : base(defaultModerationOptions) { }
}
