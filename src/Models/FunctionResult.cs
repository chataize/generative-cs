using System.Diagnostics.CodeAnalysis;

namespace ChatAIze.GenerativeCS.Models;

public record FunctionResult
{
    public FunctionResult() { }

    [SetsRequiredMembers]
    public FunctionResult(string name, object? result)
    {
        Name = name;
        Result = result;
    }

    [SetsRequiredMembers]
    public FunctionResult(string id, string name, object? result)
    {
        Id = id;
        Name = name;
        Result = result;
    }

    public string? Id { get; set; }

    public required string Name { get; set; }

    public object? Result { get; set; }
}
