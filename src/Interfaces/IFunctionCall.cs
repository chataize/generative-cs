namespace ChatAIze.GenerativeCS.Interfaces;

public interface IFunctionCall
{
    string? ToolCallId { get; set; }

    string Name { get; set; }

    string Arguments { get; set; }
}
