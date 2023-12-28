using System.Diagnostics.CodeAnalysis;

namespace GenerativeCS.Models;

public record FunctionParameter
{
    public FunctionParameter() { }

    [SetsRequiredMembers]
    public FunctionParameter(Type type, string name, bool isOptional = false)
    {
        Type = type;
        Name = name;
        IsOptional = isOptional;
    }

    [SetsRequiredMembers]
    public FunctionParameter(Type type, string name, string? description, bool isOptional = false)
    {
        Type = type;
        Name = name;
        Description = description;
        IsOptional = isOptional;
    }

    public required Type Type { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsOptional { get; set; } = true;
}
