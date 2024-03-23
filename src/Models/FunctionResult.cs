namespace ChatAIze.GenerativeCS.Models;

public record FunctionResult
{
    public FunctionResult() { }

    public FunctionResult(string name, string? value = null)
    {
        Name = name;
        Value = value;
    }

    public FunctionResult(string toolCallId, string name, string? value = null)
    {
        ToolCallId = toolCallId;
        Name = name;
        Value = value;
    }

    public string? ToolCallId { get; set; }

    public string? Name { get; set; }

    public string? Value { get; set; }
}
