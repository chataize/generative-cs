using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Gemini;
using ChatAIze.GenerativeCS.Providers.Gemini;
using ChatAIze.GenerativeCS.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ChatAIze.GenerativeCS.Clients;

public class GeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>
    where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
    where TFunctionCall : IFunctionCall, new()
    where TFunctionResult : IFunctionResult, new()
{
    private readonly HttpClient _httpClient = new();
    public string? ApiKey { get; set; }
    public IFileService Files { get; }

    public GeminiClient()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGeminiAPIKey();
        }
        var geminiOptions = new GeminiOptions { ApiKey = this.ApiKey };
        Files = new FileService(_httpClient, Microsoft.Extensions.Options.Options.Create(geminiOptions));
    }

    public GeminiClient(string apiKey)
    {
        ApiKey = apiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGeminiAPIKey();
        }
        var geminiOptions = new GeminiOptions { ApiKey = this.ApiKey };
        Files = new FileService(_httpClient, Microsoft.Extensions.Options.Options.Create(geminiOptions));
    }

    public GeminiClient(GeminiClientOptions<TMessage, TFunctionCall, TFunctionResult> options)
    {
        ApiKey = options.ApiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGeminiAPIKey();
        }

        DefaultCompletionOptions = options.DefaultCompletionOptions;
        var geminiOptions = new GeminiOptions { ApiKey = this.ApiKey };
        Files = new FileService(_httpClient, Microsoft.Extensions.Options.Options.Create(geminiOptions));
    }

    [ActivatorUtilitiesConstructor]
    public GeminiClient(HttpClient httpClient, IOptions<GeminiClientOptions<TMessage, TFunctionCall, TFunctionResult>> clientOptions)
    {
        _httpClient = httpClient;
        ApiKey = clientOptions.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGeminiAPIKey();
        }

        DefaultCompletionOptions = clientOptions.Value.DefaultCompletionOptions;

        var fileServiceOptions = new GeminiOptions 
        {
            ApiKey = clientOptions.Value.ApiKey 
        };
        Files = new FileService(_httpClient, Microsoft.Extensions.Options.Options.Create(fileServiceOptions));
    }

    public GeminiClient(ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> defaultCompletionOptions)
    {
        DefaultCompletionOptions = defaultCompletionOptions;
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ApiKey = EnvironmentVariableManager.GetGeminiAPIKey();
        }
        var geminiOptions = new GeminiOptions { ApiKey = this.ApiKey };
        Files = new FileService(_httpClient, Microsoft.Extensions.Options.Options.Create(geminiOptions));
    }

    public ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult> DefaultCompletionOptions { get; set; } = new();

    public async Task<string> CompleteAsync(string prompt, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(prompt, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }

    public async Task<string> CompleteAsync(TChat chat, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null, CancellationToken cancellationToken = default)
    {
        return await ChatCompletion.CompleteAsync(chat, ApiKey, options ?? DefaultCompletionOptions, _httpClient, cancellationToken);
    }

    public void AddFunction(IChatFunction function)
    {
        DefaultCompletionOptions.Functions.Add(function);
    }

    public void AddFunction(string name, bool requiresDoubleCheck = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck));
    }

    public void AddFunction(string name, string? description, bool requiresDoubleCheck = false)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck));
    }

    public void AddFunction(Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(callback));
    }

    public void AddFunction(string name, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, callback));
    }

    public void AddFunction(string name, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, string? description, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, callback));
    }

    public void AddFunction(string name, string? description, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, string? description, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, bool requiresDoubleCheck, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, callback));
    }

    public void AddFunction(string name, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    public void AddFunction(string name, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresDoubleCheck, Delegate callback)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, callback));
    }

    public void AddFunction(string name, string? description, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        DefaultCompletionOptions.Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
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

public class GeminiClient : GeminiClient<Chat, ChatMessage, FunctionCall, FunctionResult>
{
    public GeminiClient() : base() { }

    public GeminiClient(string apiKey) : base(apiKey) { }

    public GeminiClient(GeminiClientOptions options) : base(options) { }

    [ActivatorUtilitiesConstructor]
    public GeminiClient(HttpClient httpClient, IOptions<GeminiClientOptions> options) : base(httpClient, options) { }

    public GeminiClient(ChatCompletionOptions defaultCompletionOptions) : base(defaultCompletionOptions) { }
}
