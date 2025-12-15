using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Gemini;
using ChatAIze.GenerativeCS.Providers.Gemini;
using ChatAIze.GenerativeCS.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatAIze.GenerativeCS.Clients;

/// <summary>
/// Client wrapper around the Gemini API for chat completions with optional function calling support.
/// </summary>
/// <typeparam name="TChat">Concrete chat container type.</typeparam>
/// <typeparam name="TMessage">Concrete chat message type.</typeparam>
/// <typeparam name="TFunctionCall">Concrete function call type.</typeparam>
/// <typeparam name="TFunctionResult">Concrete function result type.</typeparam>
public class GeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>
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
    /// Initializes a new instance of the client, reading the API key from the environment when available.
    /// </summary>
    public GeminiClient()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGeminiAPIKey();
        }
    }

    /// <summary>
    /// Initializes a new instance of the client with an explicit API key or falls back to the environment.
    /// </summary>
    /// <param name="apiKey">Gemini API key.</param>
    public GeminiClient(string apiKey)
    {
        ApiKey = apiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGeminiAPIKey();
        }
    }

    /// <summary>
    /// Initializes a new instance of the client with explicit default options.
    /// </summary>
    /// <param name="options">Typed client options.</param>
    public GeminiClient(GeminiClientOptions<TMessage, TFunctionCall, TFunctionResult> options)
    {
        ApiKey = options.ApiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGeminiAPIKey();
        }

        DefaultCompletionOptions = options.DefaultCompletionOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client using dependency injection managed options.
    /// </summary>
    /// <param name="httpClient">Preconfigured HTTP client.</param>
    /// <param name="options">Typed options snapshot from DI.</param>
    [ActivatorUtilitiesConstructor]
    public GeminiClient(HttpClient httpClient, IOptions<GeminiClientOptions<TMessage, TFunctionCall, TFunctionResult>> options)
    {
        _httpClient = httpClient;
        ApiKey = options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGeminiAPIKey();
        }

        DefaultCompletionOptions = options.Value.DefaultCompletionOptions;
    }

    /// <summary>
    /// Initializes a new instance of the client with default completion options.
    /// </summary>
    /// <param name="defaultCompletionOptions">Default chat completion options.</param>
    public GeminiClient(ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> defaultCompletionOptions)
    {
        DefaultCompletionOptions = defaultCompletionOptions;
    }

    /// <summary>
    /// Gets or sets the API key used for outbound Gemini requests.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the default chat completion options applied when none are supplied.
    /// </summary>
    public ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> DefaultCompletionOptions { get; set; } = new();

    /// <summary>
    /// Runs a one-off text completion for the supplied prompt.
    /// </summary>
    /// <param name="prompt">User text to send to Gemini.</param>
    /// <param name="options">Optional completion options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Generated text from the model.</returns>
    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(prompt, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Runs a completion using the provided chat transcript.
    /// </summary>
    /// <param name="chat">Chat history containing prior messages.</param>
    /// <param name="options">Optional completion options.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Generated text from the model.</returns>
    public async Task<string> CompleteAsync(TChat chat, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(chat, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }

    /// <summary>
    /// Registers a prebuilt function for inclusion in completions.
    /// </summary>
    /// <param name="function">Function metadata to register.</param>
    public void AddFunction(IChatFunction function)
    {
        DefaultCompletionOptions.Functions.Add(function);
    }

    /// <summary>
    /// Registers a function by name without a description.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    public void AddFunction(string name, bool requiresDoubleCheck = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck));
    }

    /// <summary>
    /// Registers a function by name with a description.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck));
    }

    /// <summary>
    /// Registers a function by delegate callback.
    /// </summary>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(callback));
    }

    /// <summary>
    /// Registers a named function by delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, callback));
    }

    /// <summary>
    /// Registers a named function with explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="parameters">Function parameters.</param>
    public void AddFunction(string name, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    /// <summary>
    /// Registers a named function with explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="parameters">Function parameters.</param>
    public void AddFunction(string name, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    /// <summary>
    /// Registers a named function with a description and delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, string? description, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, callback));
    }

    /// <summary>
    /// Registers a named function with a description and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="parameters">Function parameters.</param>
    public void AddFunction(string name, string? description, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    /// <summary>
    /// Registers a named function with a description and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="parameters">Function parameters.</param>
    public void AddFunction(string name, string? description, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    /// <summary>
    /// Registers a named function that requires double checking and maps to a delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, bool requiresDoubleCheck, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, callback));
    }

    /// <summary>
    /// Registers a named function that requires double checking and declares parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters.</param>
    public void AddFunction(string name, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Registers a named function that requires double checking and declares parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters.</param>
    public void AddFunction(string name, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Registers a named function with a description, confirmation loop, and delegate callback.
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
    /// Registers a named function with a description, confirmation loop, and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Registers a named function with a description, confirmation loop, and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Removes a registered function by reference.
    /// </summary>
    /// <param name="function">Function metadata instance to remove.</param>
    /// <returns>True when the function was removed.</returns>
    public bool RemoveFunction(ChatFunction function)
    {
        return DefaultCompletionOptions.Functions.Remove(function);
    }

    /// <summary>
    /// Removes a registered function by name.
    /// </summary>
    /// <param name="name">Function name to remove.</param>
    /// <returns>True when the function was removed.</returns>
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
    /// Removes a registered function that matches the supplied callback.
    /// </summary>
    /// <param name="callback">Delegate used to locate the function.</param>
    /// <returns>True when the function was removed.</returns>
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
public class GeminiClient : GeminiClient<Chat, ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes a new instance using environment configuration.
    /// </summary>
    public GeminiClient() : base() { }

    /// <summary>
    /// Initializes a new instance with an explicit API key.
    /// </summary>
    /// <param name="apiKey">Gemini API key.</param>
    public GeminiClient(string apiKey) : base(apiKey) { }

    /// <summary>
    /// Initializes a new instance with explicit default options.
    /// </summary>
    /// <param name="options">Gemini client options.</param>
    public GeminiClient(GeminiClientOptions options) : base(options) { }

    /// <summary>
    /// Initializes a new instance using dependency injection managed options.
    /// </summary>
    /// <param name="httpClient">Preconfigured HTTP client.</param>
    /// <param name="options">Typed options snapshot from DI.</param>
    [ActivatorUtilitiesConstructor]
    public GeminiClient(HttpClient httpClient, IOptions<GeminiClientOptions> options) : base(httpClient, options) { }

    /// <summary>
    /// Initializes a new instance with a default chat completion configuration.
    /// </summary>
    /// <param name="defaultCompletionOptions">Default chat completion options.</param>
    public GeminiClient(ChatCompletionOptions defaultCompletionOptions) : base(defaultCompletionOptions) { }
}
