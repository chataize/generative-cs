using System.Diagnostics.CodeAnalysis;

namespace ChatAIze.GenerativeCS.Models;

public record FunctionCall
{
    public FunctionCall() { }

    [SetsRequiredMembers]
    public FunctionCall(string name, string arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    [SetsRequiredMembers]
    public FunctionCall(string toolCallId, string name, string? arguments = null)
    {
        ToolCallId = toolCallId;
        Name = name;
        Arguments = arguments;
    }

    public string? ToolCallId { get; set; }

    public required string Name { get; set; }

    public string? Arguments { get; set; }
}
