using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

/// <summary>
/// Represents a function call issued by a language model.
/// </summary>
public record FunctionCall : IFunctionCall
{
    /// <summary>
    /// Initializes an empty function call.
    /// </summary>
    public FunctionCall() { }

    /// <summary>
    /// Initializes a function call with a name and argument payload.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="arguments">Arguments as a JSON string.</param>
    [SetsRequiredMembers]
    public FunctionCall(string name, string arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    /// <summary>
    /// Initializes a function call with a tool call identifier.
    /// </summary>
    /// <param name="toolCallId">Provider-specific tool call identifier.</param>
    /// <param name="name">Function name.</param>
    /// <param name="arguments">Arguments as a JSON string.</param>
    [SetsRequiredMembers]
    public FunctionCall(string toolCallId, string name, string arguments)
    {
        ToolCallId = toolCallId;
        Name = name;
        Arguments = arguments;
    }

    /// <summary>
    /// Gets or sets the provider-issued tool call identifier.
    /// </summary>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the serialized argument payload.
    /// </summary>
    public string Arguments { get; set; } = null!;
}
