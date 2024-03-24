namespace ChatAIze.GenerativeCS.Interfaces;

public interface IFunctionResult
{
    string? ToolCallId { get; set; }

    string Name { get; set; }

    string Value { get; set; }
}
