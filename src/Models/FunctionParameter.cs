using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

public record FunctionParameter : IFunctionParameter
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

    public ICollection<string> EnumValues { get; set; } = [];

    public bool IsRequired { get; set; } = true;
}
