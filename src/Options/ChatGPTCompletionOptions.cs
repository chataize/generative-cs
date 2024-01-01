using System.Text.Json;
using GenerativeCS.Models;

namespace GenerativeCS.Options;

public record ChatGPTCompletionOptions
{
    public string Model { get; set; } = "gpt-3.5-turbo";

    public string? User { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public int? MaxOutputTokens { get; set; }

    public int? MessageLimit { get; set; }

    public int? CharacterLimit { get; set; }

    public int? Seed { get; set; }

    public double? Temperature { get; set; }

    public double? TopP { get; set; }

    public double? FrequencyPenalty { get; set; }

    public double? PresencePenalty { get; set; }

    public bool IsJsonMode { get; set; }

    public bool IsTimeAware { get; set; }

    public List<string> StopWords { get; set; } = [];

    public List<ChatFunction> Functions { get; set; } = [];

    public Func<string, JsonElement, CancellationToken, Task<object?>> DefaultFunctionCallback { get; set; } = (_, _, _) => throw new NotImplementedException("Function callback has not been implemented.");

    public Func<DateTime> TimeCallback { get; set; } = () => DateTime.Now;

    public void AddFunction(ChatFunction function)
    {
        Functions.Add(function);
    }

    public void AddFunction(string name, bool requiresConfirmation = false)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation = false)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation));
    }

    public void AddFunction(Delegate callback)
    {
        Functions.Add(new ChatFunction(callback));
    }

    public void AddFunction(string name, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, callback));
    }

    public void AddFunction(string name, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, string? description, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, callback));
    }

    public void AddFunction(string name, string? description, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, string? description, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, bool requiresConfirmation, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, callback));
    }

    public void AddFunction(string name, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, callback));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, parameters));
    }

    public bool RemoveFunction(ChatFunction function)
    {
        return Functions.Remove(function);
    }

    public bool RemoveFunction(string name)
    {
        var function = Functions.LastOrDefault(f => f.Name == name);
        if (function == null)
        {
            return false;
        }

        return Functions.Remove(function);
    }

    public bool RemoveFunction(Delegate callback)
    {
        var function = Functions.LastOrDefault(f => f.Callback == callback);
        if (function == null)
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
