using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions.Chat;
using ChatAIze.Utilities.Extensions;

namespace ChatAIze.GenerativeCS.Models;

/// <summary>
/// Describes a callable function that can be surfaced to a language model.
/// </summary>
public record ChatFunction : IChatFunction
{
    /// <summary>
    /// Initializes an empty function definition.
    /// </summary>
    public ChatFunction() { }

    /// <summary>
    /// Initializes a function definition with a name.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresDoubleCheck = false)
    {
        Name = name;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = [];
    }

    /// <summary>
    /// Initializes a function definition with a name and description.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresDoubleCheck = false)
    {
        Name = name;
        Description = description;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = [];
    }

    /// <summary>
    /// Initializes a function definition that maps directly to a delegate callback.
    /// </summary>
    /// <param name="callback">Delegate implementing the function.</param>
    [SetsRequiredMembers]
    public ChatFunction(Delegate callback)
    {
        Name = callback.GetNormalizedMethodName();
        Callback = callback;
    }

    /// <summary>
    /// Initializes a function definition with a name mapped to a delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, Delegate callback)
    {
        Name = name;
        Callback = callback;
    }

    /// <summary>
    /// Initializes a function definition with explicit parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, params ICollection<IFunctionParameter> parameters)
    {
        Name = name;
        Parameters = parameters;
    }

    /// <summary>
    /// Initializes a function definition with a description and delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, Delegate callback)
    {
        Name = name;
        Description = description;
        Callback = callback;
    }

    /// <summary>
    /// Initializes a function definition with a description and parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, params ICollection<IFunctionParameter> parameters)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
    }

    /// <summary>
    /// Initializes a function definition that requires double checking and maps to a delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresDoubleCheck, Delegate callback)
    {
        Name = name;
        RequiresDoubleCheck = requiresDoubleCheck;
        Callback = callback;
    }

    /// <summary>
    /// Initializes a function definition that requires double checking and declares parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, bool requiresDoubleCheck, params ICollection<IFunctionParameter> parameters)
    {
        Name = name;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = parameters;
    }

    /// <summary>
    /// Initializes a function definition with a description, confirmation loop, and delegate callback.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="callback">Delegate implementing the function.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresDoubleCheck, Delegate callback)
    {
        Name = name;
        Description = description;
        RequiresDoubleCheck = requiresDoubleCheck;
        Callback = callback;
    }

    /// <summary>
    /// Initializes a function definition with a description, confirmation loop, and parameters.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="description">Function description.</param>
    /// <param name="requiresDoubleCheck">True to require a confirmation loop before execution.</param>
    /// <param name="parameters">Function parameters exposed to the model.</param>
    [SetsRequiredMembers]
    public ChatFunction(string name, string? description, bool requiresDoubleCheck, params ICollection<IFunctionParameter> parameters)
    {
        Name = name;
        Description = description;
        RequiresDoubleCheck = requiresDoubleCheck;
        Parameters = parameters;
    }

    /// <summary>
    /// Gets or sets the function name surfaced to the model.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets a human readable description of the function.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model must double check before executing.
    /// </summary>
    public bool RequiresDoubleCheck { get; set; }

    /// <summary>
    /// Gets or sets the delegate callback used to execute the function.
    /// </summary>
    public Delegate? Callback { get; set; }

    /// <summary>
    /// Gets or sets explicit function parameters available to the model.
    /// </summary>
    public ICollection<IFunctionParameter>? Parameters { get; set; }

    IReadOnlyCollection<IFunctionParameter>? IChatFunction.Parameters => (IReadOnlyCollection<IFunctionParameter>?)Parameters;
}
