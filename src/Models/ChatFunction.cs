using System.Diagnostics.CodeAnalysis;

namespace GenerativeCS.Models;

public record ChatFunction
{
    public ChatFunction() { }

    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresConfirmation = false)
    {
        Name = name;
        RequiresConfirmation = requiresConfirmation;
        Parameters = [];
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresConfirmation = false)
    {
        Name = name;
        Description = description;
        RequiresConfirmation = requiresConfirmation;
        Parameters = [];
    }

    [SetsRequiredMembers]
    public ChatFunction(Delegate callback)
    {
        Name = callback.Method.Name;
        Callback = callback;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, Delegate callback)
    {
        Name = name;
        Callback = callback;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, IEnumerable<FunctionParameter> parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, params FunctionParameter[] parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, Delegate callback)
    {
        Name = name;
        Description = description;
        Callback = callback;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, IEnumerable<FunctionParameter> parameters)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, params FunctionParameter[] parameters)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresConfirmation, Delegate callback)
    {
        Name = name;
        RequiresConfirmation = requiresConfirmation;
        Callback = callback;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        Name = name;
        RequiresConfirmation = requiresConfirmation;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        Name = name;
        RequiresConfirmation = requiresConfirmation;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresConfirmation, Delegate callback)
    {
        Name = name;
        Description = description;
        RequiresConfirmation = requiresConfirmation;
        Callback = callback;
    }


    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        Name = name;
        Description = description;
        RequiresConfirmation = requiresConfirmation;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        Name = name;
        Description = description;
        RequiresConfirmation = requiresConfirmation;
        Parameters = parameters;
    }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool RequiresConfirmation { get; set; }

    public IEnumerable<FunctionParameter>? Parameters { get; set; }

    public Delegate? Callback { get; set; }
}
