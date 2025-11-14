using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

public record FunctionCall : IFunctionCall
{
    public FunctionCall() { }

    [SetsRequiredMembers]
    public FunctionCall(string name, string arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    [SetsRequiredMembers]
    public FunctionCall(string toolCallId, string name, string arguments)
    {
        ToolCallId = toolCallId;
        Name = name;
        Arguments = arguments;
    }

    public string? ToolCallId { get; set; }

    public string Name { get; set; } = null!;

    public string Arguments { get; set; } = null!;
}
