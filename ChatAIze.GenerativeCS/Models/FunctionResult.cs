using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

/// <summary>
/// Represents the result returned after executing a function call.
/// </summary>
public record FunctionResult : IFunctionResult
{
    /// <summary>
    /// Initializes an empty function result.
    /// </summary>
    public FunctionResult() { }

    /// <summary>
    /// Initializes a function result with a name and value.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="value">Serialized result value.</param>
    [SetsRequiredMembers]
    public FunctionResult(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Initializes a function result with a tool call identifier.
    /// </summary>
    /// <param name="toolCallId">Provider-specific tool call identifier.</param>
    /// <param name="name">Function name.</param>
    /// <param name="value">Serialized result value.</param>
    [SetsRequiredMembers]
    public FunctionResult(string toolCallId, string name, string value)
    {
        ToolCallId = toolCallId;
        Name = name;
        Value = value;
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
    /// Gets or sets the serialized result value.
    /// </summary>
    public string Value { get; set; } = null!;
}
