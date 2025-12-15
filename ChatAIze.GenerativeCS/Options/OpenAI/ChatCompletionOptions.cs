using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.OpenAI;

/// <summary>
/// Configures how chat completion requests are built and executed for OpenAI models.
/// </summary>
/// <typeparam name="TMessage">Message type used in the chat.</typeparam>
/// <typeparam name="TFunctionCall">Function call type used in the chat.</typeparam>
/// <typeparam name="TFunctionResult">Function result type used in the chat.</typeparam>
public record ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    /// <summary>
    /// Initializes a new set of completion options.
    /// </summary>
    /// <param name="model">Model identifier to target.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public ChatCompletionOptions(string model = DefaultModels.OpenAI.ChatCompletion, string? apiKey = null)
    {
        Model = model;
        ApiKey = apiKey;
    }

    /// <summary>
    /// Gets or sets the model identifier used for chat completions.
    /// </summary>
    public string Model { get; set; } = DefaultModels.OpenAI.ChatCompletion;

    /// <summary>
    /// Gets or sets an optional API key that overrides the client-level key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets an optional stable end-user identifier passed to the provider for safety, abuse, or rate limiting.
    /// </summary>
    public string? UserTrackingId { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for a failed request.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of tokens the model may generate.
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of non-system messages to include.
    /// </summary>
    public int? MessageLimit { get; set; }

    /// <summary>
    /// Gets or sets the maximum total character count across user and assistant messages.
    /// </summary>
    public int? CharacterLimit { get; set; }

    /// <summary>
    /// Gets or sets an optional seed for deterministic responses.
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// Gets or sets the sampling temperature.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Gets or sets nucleus sampling probability mass.
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Gets or sets the frequency penalty applied to repeated tokens.
    /// </summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// Gets or sets the presence penalty applied to repeated tokens.
    /// </summary>
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// Gets or sets the desired reasoning effort for the response.
    /// </summary>
    public ReasoningEffort ReasoningEffort { get; set; } = ReasoningEffort.None;

    /// <summary>
    /// Gets or sets the expected verbosity level for the response.
    /// </summary>
    public Verbosity Verbosity { get; set; } = Verbosity.Medium;

    /// <summary>
    /// Gets or sets an optional CLR type for JSON schema response formatting.
    /// </summary>
    public Type? ResponseType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model should respond with JSON mode enabled.
    /// </summary>
    public bool IsJsonMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model may call multiple functions in parallel.
    /// </summary>
    public bool IsParallelFunctionCallingOn { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the model must follow strict function schemas.
    /// </summary>
    public bool IsStrictFunctionCallingOn { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether responses should be stored by the provider.
    /// </summary>
    public bool IsStoringOutputs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether time metadata should be appended automatically.
    /// </summary>
    public bool IsTimeAware { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether previous function calls should be removed before sending.
    /// </summary>
    public bool IsIgnoringPreviousFunctionCalls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether request and response payloads should be logged to the console.
    /// </summary>
    public bool IsDebugMode { get; set; }

    /// <summary>
    /// Gets or sets stop words that terminate generation.
    /// </summary>
    public List<string> StopWords { get; set; } = [];

    /// <summary>
    /// Gets or sets functions available to the model.
    /// </summary>
    public List<IChatFunction> Functions { get; set; } = [];

    /// <summary>
    /// Gets or sets a callback used to dynamically supply a system message.
    /// </summary>
    public Func<string?>? SystemMessageCallback { get; set; } = null;

    /// <summary>
    /// Gets or sets a callback that returns the current time when time awareness is enabled.
    /// </summary>
    public Func<DateTime> TimeCallback { get; set; } = () => DateTime.Now;

    /// <summary>
    /// Gets or sets a callback invoked whenever a message is added to the chat.
    /// </summary>
    public Func<TMessage, Task> AddMessageCallback { get; set; } = (_) => Task.CompletedTask;

    /// <summary>
    /// Gets or sets the fallback function callback used when a function does not have an explicit delegate.
    /// </summary>
    public Func<string, string, CancellationToken, ValueTask<object?>> DefaultFunctionCallback { get; set; } = (_, _, _) => throw new NotImplementedException("Function callback has not been implemented.");

    /// <summary>
    /// Gets or sets an optional function execution context passed to callbacks.
    /// </summary>
    public IFunctionContext? FunctionContext { get; set; }

    /// <summary>
    /// Adds a prebuilt function definition to the available set.
    /// </summary>
    /// <param name="function">Function metadata to add.</param>
    public void AddFunction(IChatFunction function)
    {
        Functions.Add(function);
    }

    /// <summary>
    /// Adds a function definition by name.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    public void AddFunction(string name, bool requiresDoubleCheck = false)
    {
        Functions.Add(new ChatFunction(name, requiresDoubleCheck));
    }

    /// <summary>
    /// Adds a function definition by name with a description.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck = false)
    {
        Functions.Add(new ChatFunction(name, description, requiresDoubleCheck));
    }

    /// <summary>
    /// Adds a function definition that maps directly to a delegate callback.
    /// </summary>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(Delegate callback)
    {
        Functions.Add(new ChatFunction(callback));
    }

    /// <summary>
    /// Adds a function definition with a name and delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, callback));
    }

    /// <summary>
    /// Adds a function definition with explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    public void AddFunction(string name, ICollection<IFunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    /// <summary>
    /// Adds a function definition with explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    public void AddFunction(string name, params IFunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    /// <summary>
    /// Adds a function definition with a description and delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, string? description, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, callback));
    }

    /// <summary>
    /// Adds a function definition with a description and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    public void AddFunction(string name, string? description, ICollection<IFunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    /// <summary>
    /// Adds a function definition with a description and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    public void AddFunction(string name, string? description, params IFunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    /// <summary>
    /// Adds a function definition that requires double checking and maps to a delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, bool requiresDoubleCheck, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, requiresDoubleCheck, callback));
    }

    /// <summary>
    /// Adds a function definition that requires double checking and declares parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    public void AddFunction(string name, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Adds a function definition that requires double checking and declares parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    public void AddFunction(string name, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Adds a function definition with description, confirmation loop, and delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, callback));
    }

    /// <summary>
    /// Adds a function definition with description, confirmation loop, and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Adds a function definition with description, confirmation loop, and explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    public void AddFunction(string name, string? description, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    /// <summary>
    /// Removes a function definition by reference.
    /// </summary>
    /// <param name="function">Function to remove.</param>
    /// <returns>True when the function was removed.</returns>
    public bool RemoveFunction(IChatFunction function)
    {
        return Functions.Remove(function);
    }

    /// <summary>
    /// Removes a function definition by its name.
    /// </summary>
    /// <param name="name">Function name to remove.</param>
    /// <returns>True when a function was removed.</returns>
    public bool RemoveFunction(string name)
    {
        var function = Functions.FirstOrDefault(f => f.Name == name);
        if (function is null)
        {
            return false;
        }

        return Functions.Remove(function);
    }

    /// <summary>
    /// Removes a function definition that matches the supplied callback.
    /// </summary>
    /// <param name="callback">Delegate used to locate the function.</param>
    /// <returns>True when a function was removed.</returns>
    public bool RemoveFunction(Delegate callback)
    {
        var function = Functions.FirstOrDefault(f => f.Callback == callback);
        if (function is null)
        {
            return false;
        }

        return Functions.Remove(function);
    }

    /// <summary>
    /// Clears all function definitions.
    /// </summary>
    public void ClearFunctions()
    {
        Functions.Clear();
    }
}

/// <summary>
/// Non-generic completion options using the built-in message, function call, and function result types.
/// </summary>
public record ChatCompletionOptions : ChatCompletionOptions<ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes a new set of completion options.
    /// </summary>
    /// <param name="model">Model identifier to target.</param>
    public ChatCompletionOptions(string model = DefaultModels.OpenAI.ChatCompletion) : base(model) { }
}
