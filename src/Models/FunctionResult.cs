using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions;

namespace ChatAIze.GenerativeCS.Models;

public record FunctionResult : IFunctionResult
{
    public FunctionResult() { }

    [SetsRequiredMembers]
    public FunctionResult(string name, string value)
    {
        Name = name;
        Value = value;
    }

    [SetsRequiredMembers]
    public FunctionResult(string toolCallId, string name, string value)
    {
        ToolCallId = toolCallId;
        Name = name;
        Value = value;
    }

    public string? ToolCallId { get; set; }

    public string Name { get; set; } = null!;

    public string Value { get; set; } = null!;
}
