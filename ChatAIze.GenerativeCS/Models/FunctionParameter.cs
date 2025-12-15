using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

/// <summary>
/// Represents a parameter definition for a callable function.
/// </summary>
public record FunctionParameter : IFunctionParameter
{
    /// <summary>
    /// Initializes an empty function parameter definition.
    /// </summary>
    public FunctionParameter() { }

    /// <summary>
    /// Initializes a parameter definition with a type, name, and optional enum values.
    /// </summary>
    /// <param name="type">Parameter CLR type.</param>
    /// <param name="name">Parameter name.</param>
    /// <param name="isRequired">True when the parameter must be supplied.</param>
    /// <param name="enumValues">Optional allowed values for enum-like parameters.</param>
    [SetsRequiredMembers]
    public FunctionParameter(Type type, string name, bool isRequired = true, params ICollection<string> enumValues)
    {
        Type = type;
        Name = name;
        IsRequired = isRequired;
        EnumValues = enumValues;
    }

    /// <summary>
    /// Initializes a parameter definition with a description and optional enum values.
    /// </summary>
    /// <param name="type">Parameter CLR type.</param>
    /// <param name="name">Parameter name.</param>
    /// <param name="description">Human readable description.</param>
    /// <param name="isRequired">True when the parameter must be supplied.</param>
    /// <param name="enumValues">Optional allowed values for enum-like parameters.</param>
    [SetsRequiredMembers]
    public FunctionParameter(Type type, string name, string? description, bool isRequired = true, params ICollection<string> enumValues)
    {
        Type = type;
        Name = name;
        Description = description;
        IsRequired = isRequired;
        EnumValues = enumValues;
    }

    /// <summary>
    /// Gets or sets the CLR type of the parameter.
    /// </summary>
    public required Type Type { get; set; }

    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Gets or sets enum-like allowed values for the parameter.
    /// </summary>
    public ICollection<string> EnumValues { get; set; } = [];

    IReadOnlyCollection<string> IFunctionParameter.EnumValues => (IReadOnlyCollection<string>)EnumValues;
}
