using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.Gemini;

public record ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    public ChatCompletionOptions(string model = DefaultModels.Gemini.ChatCompletion, string? apiKey = null)
    {
        Model = model;
        ApiKey = apiKey;
    }

    public string Model { get; set; } = DefaultModels.Gemini.ChatCompletion;

    public string? ApiKey { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public int? MessageLimit { get; set; }

    public int? CharacterLimit { get; set; }

    public bool IsTimeAware { get; set; }

    public bool IsDebugMode { get; set; }

    public List<IChatFunction> Functions { get; set; } = [];

    public Func<string?>? SystemMessageCallback { get; set; } = null;

    public Func<DateTime> TimeCallback { get; set; } = () => DateTime.Now;

    public Func<TMessage, ValueTask> AddMessageCallback { get; } = (_) => ValueTask.CompletedTask;

    public Func<string, string, CancellationToken, ValueTask<object?>> DefaultFunctionCallback { get; set; } = (_, _, _) => throw new NotImplementedException("Function callback has not been implemented.");

    public IFunctionContext? FunctionContext { get; set; }

    public void AddFunction(IChatFunction function)
    {
        Functions.Add(function);
    }

    public void AddFunction(string name, bool requiresDoubleCheck = false)
    {
        Functions.Add(new ChatFunction(name, requiresDoubleCheck));
    }

    public void AddFunction(string name, string? description, bool requiresDoubleCheck = false)
    {
        Functions.Add(new ChatFunction(name, description, requiresDoubleCheck));
    }

    public void AddFunction(Delegate callback)
    {
        Functions.Add(new ChatFunction(callback));
    }

    public void AddFunction(string name, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, callback));
    }

    public void AddFunction(string name, ICollection<IFunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, params IFunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, string? description, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, callback));
    }

    public void AddFunction(string name, string? description, ICollection<IFunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, string? description, params IFunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, bool requiresDoubleCheck, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, requiresDoubleCheck, callback));
    }

    public void AddFunction(string name, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    public void AddFunction(string name, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, requiresDoubleCheck, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresDoubleCheck, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, callback));
    }

    public void AddFunction(string name, string? description, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresDoubleCheck, params IFunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresDoubleCheck, parameters));
    }

    public bool RemoveFunction(ChatFunction function)
    {
        return Functions.Remove(function);
    }

    public bool RemoveFunction(string name)
    {
        var function = Functions.FirstOrDefault(f => f.Name == name);
        if (function is null)
        {
            return false;
        }

        return Functions.Remove(function);
    }

    public bool RemoveFunction(Delegate callback)
    {
        var function = Functions.FirstOrDefault(f => f.Callback == callback);
        if (function is null)
        {
            return false;
        }

        return Functions.Remove(function);
    }

    public void ClearFunctions()
    {
        Functions.Clear();
    }
}

public record ChatCompletionOptions : ChatCompletionOptions<ChatMessage, FunctionCall, FunctionResult>
{
    public ChatCompletionOptions() : base() { }

    public ChatCompletionOptions(string model = DefaultModels.Gemini.ChatCompletion) : base(model) { }
}
