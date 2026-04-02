using System.Runtime.CompilerServices;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Grok;
using ChatAIze.GenerativeCS.Providers.Grok;
using ChatAIze.GenerativeCS.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatAIze.GenerativeCS.Clients;

/// <summary>
/// Client wrapper around xAI Grok APIs for chat completions and text-to-speech.
/// </summary>
/// <typeparam name="TChat">Concrete chat container type.</typeparam>
/// <typeparam name="TMessage">Concrete chat message type.</typeparam>
/// <typeparam name="TFunctionCall">Concrete function call type.</typeparam>
/// <typeparam name="TFunctionResult">Concrete function result type.</typeparam>
public class GrokClient<TChat, TMessage, TFunctionCall, TFunctionResult>
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
    public GrokClient()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGrokAPIKey();
        }
    }

    /// <summary>
    /// Initializes a new instance of the client with a specific API key or falls back to the environment.
    /// </summary>
    /// <param name="apiKey">The Grok API key to use.</param>
    public GrokClient(string apiKey)
    {
        ApiKey = apiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGrokAPIKey();
        }
    }

    /// <summary>
    /// Initializes a new instance of the client with explicit default options.
    /// </summary>
    /// <param name="options">Typed client options.</param>
    public GrokClient(GrokClientOptions<TMessage, TFunctionCall, TFunctionResult> options)
    {
        ApiKey = options.ApiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGrokAPIKey();
        }

        DefaultCompletionOptions = options.DefaultCompletionOptions;
        DefaultTextToSpeechOptions = options.DefaultTextToSpeechOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client using dependency injection managed options.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public GrokClient(HttpClient httpClient, IOptions<GrokClientOptions<TMessage, TFunctionCall, TFunctionResult>> options)
    {
        _httpClient = httpClient;
        ApiKey = options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGrokAPIKey();
        }

        DefaultCompletionOptions = options.Value.DefaultCompletionOptions;
        DefaultTextToSpeechOptions = options.Value.DefaultTextToSpeechOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client with default completion options.
    /// </summary>
    public GrokClient(ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> defaultCompletionOptions)
    {
        DefaultCompletionOptions = defaultCompletionOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client with default text-to-speech options.
    /// </summary>
    public GrokClient(TextToSpeechOptions defaultTextToSpeechOptions)
    {
        DefaultTextToSpeechOptions = defaultTextToSpeechOptions;
    }

    /// <summary>
    /// Gets or sets the API key used for outbound Grok requests.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the default chat completion options applied when none are supplied.
    /// </summary>
    public ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> DefaultCompletionOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets the default text-to-speech options applied when none are supplied.
    /// </summary>
    public TextToSpeechOptions DefaultTextToSpeechOptions { get; set; } = new();

    /// <summary>
    /// Runs a one-off text completion for the supplied prompt.
    /// </summary>
    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        var chat = new TChat();
        _ = await chat.FromUserAsync(prompt);

        return await CompleteAsync(chat, options ?? DefaultCompletionOptions, usageTracker, cancellationToken);
    }

    /// <summary>
    /// Runs a one-off completion with an explicit system priming message and user message.
    /// </summary>
    public async Task<string> CompleteAsync(string systemMessage, string userMessage, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        var chat = new TChat();
        _ = await chat.FromSystemAsync(systemMessage);
        _ = await chat.FromUserAsync(userMessage);

        return await CompleteAsync(chat, options ?? DefaultCompletionOptions, usageTracker, cancellationToken);
    }

    /// <summary>
    /// Runs a completion using the provided chat transcript.
    /// </summary>
    public async Task<string> CompleteAsync(TChat chat, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(chat, ApiKey, options ?? DefaultCompletionOptions, usageTracker, _httpClient, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Streams a completion for a single user prompt.
    /// </summary>
    public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chat = new TChat();
        _ = await chat.FromUserAsync(prompt);

        await foreach (var chunk in StreamCompletionAsync(chat, options, usageTracker, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Streams a completion using the provided chat transcript.
    /// </summary>
    public async IAsyncEnumerable<string> StreamCompletionAsync(TChat chat, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, TokenUsageTracker? usageTracker = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var chunk in ChatCompletion.StreamCompletionAsync(chat, ApiKey, options ?? DefaultCompletionOptions, usageTracker, _httpClient, cancellationToken: cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Synthesizes speech audio bytes from text.
    /// </summary>
    public async Task<byte[]> SynthesizeSpeechAsync(string text, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await TextToSpeech.SynthesizeSpeechAsync(text, ApiKey, options ?? DefaultTextToSpeechOptions, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Synthesizes speech audio from text and saves it to disk.
    /// </summary>
    public async Task SynthesizeSpeechAsync(string text, string outputFilePath, TextToSpeechOptions? options = null, CancellationToken cancellationToken = default)
    {
        var audio = await TextToSpeech.SynthesizeSpeechAsync(text, ApiKey, options ?? DefaultTextToSpeechOptions, _httpClient, cancellationToken);
        await File.WriteAllBytesAsync(outputFilePath, audio, cancellationToken);
    }

    /// <summary>
    /// Registers a prebuilt function for inclusion in completions.
    /// </summary>
    public void AddFunction(IChatFunction function)
    {
        DefaultCompletionOptions.Functions.Add(function);
    }

    /// <summary>
    /// Registers a function by name without a description.
    /// </summary>
    public void AddFunction(string name, bool requiresDoubleCheck = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck));
    }

    /// <summary>
    /// Registers a function by name with a description.
    /// </summary>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck));
    }

    /// <summary>
    /// Registers a function by delegate callback.
    /// </summary>
    public void AddFunction(Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(callback));
    }

    /// <summary>
    /// Registers a named function by delegate callback.
    /// </summary>
    public void AddFunction(string name, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, callback));
    }

    /// <summary>
    /// Registers a named function with explicit parameters.
    /// </summary>
    public void AddFunction(string name, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    /// <summary>
    /// Registers a named function with explicit parameters.
    /// </summary>
    public void AddFunction(string name, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    /// <summary>
    /// Registers a named function with a description and delegate callback.
    /// </summary>
    public void AddFunction(string name, string? description, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, callback));
    }

    /// <summary>
    /// Registers a named function with a description and explicit parameters.
    /// </summary>
    public void AddFunction(string name, string? description, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    /// <summary>
    /// Registers a named function with a description and explicit parameters.
    /// </summary>
    public void AddFunction(string name, string? description, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    /// <summary>
    /// Registers a named function that requires double checking and delegate execution.
    /// </summary>
    public void AddFunction(string name, bool requiresDoubleCheck, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, callback));
    }

    /// <summary>
    /// Registers a named function that requires double checking and explicit parameters.
    /// </summary>
    public void AddFunction(string name, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Registers a named function that requires double checking and explicit parameters.
    /// </summary>
    public void AddFunction(string name, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Registers a named function with description, confirmation loop, and delegate callback.
    /// </summary>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, callback));
    }

    /// <summary>
    /// Registers a named function with description, confirmation loop, and explicit parameters.
    /// </summary>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Registers a named function with description, confirmation loop, and explicit parameters.
    /// </summary>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, params FunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Removes a function from the default options by reference.
    /// </summary>
    public bool RemoveFunction(ChatFunction function)
    {
        return DefaultCompletionOptions.Functions.Remove(function);
    }

    /// <summary>
    /// Removes a function from the default options by its name.
    /// </summary>
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
/// Non-generic convenience client using built-in chat, message, function call, and function result types.
/// </summary>
public class GrokClient : GrokClient<Chat, ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes a new instance using environment configuration.
    /// </summary>
    public GrokClient() : base() { }

    /// <summary>
    /// Initializes a new instance with an explicit API key.
    /// </summary>
    public GrokClient(string apiKey) : base(apiKey) { }

    /// <summary>
    /// Initializes a new instance with explicit default options.
    /// </summary>
    public GrokClient(GrokClientOptions options) : base(options) { }

    /// <summary>
    /// Initializes a new instance using dependency injection managed options.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public GrokClient(HttpClient httpClient, IOptions<GrokClientOptions> options) : base(httpClient, options) { }

    /// <summary>
    /// Initializes a new instance with a default chat completion configuration.
    /// </summary>
    public GrokClient(ChatCompletionOptions defaultCompletionOptions) : base(defaultCompletionOptions) { }

    /// <summary>
    /// Initializes a new instance with default text-to-speech options.
    /// </summary>
    public GrokClient(TextToSpeechOptions defaultTextToSpeechOptions) : base(defaultTextToSpeechOptions) { }
}
