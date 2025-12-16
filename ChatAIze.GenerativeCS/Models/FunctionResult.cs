using System.Diagnostics.CodeAnalysis;
using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

/// <summary>
/// Represents the serialized value returned to the model after executing a function call.
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
    /// <param name="name">Function name the model requested.</param>
    /// <param name="value">Serialized result value (typically JSON or plain text the model can read).</param>
    [SetsRequiredMembers]
    public FunctionResult(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Initializes a function result with a tool call identifier.
    /// </summary>
    /// <param name="toolCallId">Provider-specific tool call identifier that pairs the result with the originating tool call.</param>
    /// <param name="name">Function name.</param>
    /// <param name="value">Serialized result value (typically JSON or plain text the model can read).</param>
    [SetsRequiredMembers]
    public FunctionResult(string toolCallId, string name, string value)
    {
        ToolCallId = toolCallId;
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets or sets the provider-issued tool call identifier used to correlate the result with a prior tool call.
    /// </summary>
    /// <remarks>Providers like OpenAI expect this to mirror the tool call id emitted in the original function call.</remarks>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Gets or sets the function name requested by the model.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the serialized result value returned to the model.
    /// </summary>
    public string Value { get; set; } = null!;
}
