using System.Diagnostics.CodeAnalysis;

namespace ChatAIze.GenerativeCS.Models;

public record FunctionParameter
{
    public FunctionParameter() { }

    [SetsRequiredMembers]
    public FunctionParameter(Type type, string name, bool IsRequired = true)
    {
        Type = type;
        Name = name;
        this.IsRequired = IsRequired;
    }

    [SetsRequiredMembers]
    public FunctionParameter(Type type, string name, string? description, bool isRequired = true)
    {
        Type = type;
        Name = name;
        Description = description;
        IsRequired = isRequired;
    }

    public required Type Type { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsRequired { get; set; } = true;
}
