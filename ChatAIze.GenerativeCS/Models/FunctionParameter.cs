using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

public record FunctionParameter : IFunctionParameter
{
    public FunctionParameter() { }

    [SetsRequiredMembers]
    public FunctionParameter(Type type, string name, bool isRequired = true, params ICollection<string> enumValues)
    {
        Type = type;
        Name = name;
        IsRequired = isRequired;
        EnumValues = enumValues;
    }

    [SetsRequiredMembers]
    public FunctionParameter(Type type, string name, string? description, bool isRequired = true, params ICollection<string> enumValues)
    {
        Type = type;
        Name = name;
        Description = description;
        IsRequired = isRequired;
        EnumValues = enumValues;
    }

    public required Type Type { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsRequired { get; set; } = true;

    public ICollection<string> EnumValues { get; set; } = [];

    IReadOnlyCollection<string> IFunctionParameter.EnumValues => (IReadOnlyCollection<string>)EnumValues;
}
