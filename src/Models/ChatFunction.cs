using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Utilities;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.GenerativeCS.Models;

public record ChatFunction : IChatFunction
{
    public ChatFunction() { }

    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresDoubleCheck = false)
    {
        Name = name;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = [];
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresDoubleCheck = false)
    {
        Name = name;
        Description = description;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = [];
    }

    [SetsRequiredMembers]
    public ChatFunction(Delegate callback)
    {
        Name = callback.GetNormalizedMethodName();
        Callback = callback;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, Delegate callback)
    {
        Name = name;
        Callback = callback;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, ICollection<IFunctionParameter> parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, params IFunctionParameter[] parameters)
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
    public ChatFunction(string name, string? description, ICollection<IFunctionParameter> parameters)
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
    public ChatFunction(string name, bool requiresDoubleCheck, Delegate callback)
    {
        Name = name;
        RequiresDoubleCheck = requiresDoubleCheck;
        Callback = callback;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        Name = name;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresDoubleCheck, params FunctionParameter[] parameters)
    {
        Name = name;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresDoubleCheck, Delegate callback)
    {
        Name = name;
        Description = description;
        RequiresDoubleCheck = requiresDoubleCheck;
        Callback = callback;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresDoubleCheck, ICollection<IFunctionParameter> parameters)
    {
        Name = name;
        Description = description;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = parameters;
    }

    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresDoubleCheck, params FunctionParameter[] parameters)
    {
        Name = name;
        Description = description;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = parameters;
    }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool RequiresDoubleCheck { get; set; }

    public ICollection<IFunctionParameter>? Parameters { get; set; }

    public Delegate? Callback { get; set; }
}
