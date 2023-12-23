using GenerativeCS.Enums;
using GenerativeCS.Interfaces;

namespace GenerativeCS.Models;

public record ChatMessage : IChatMessage
{
    public ChatMessage() { }

    public ChatMessage(ChatRole role, string content)
    {
        Role = role;
        Content = content;
    }

    public ChatMessage(ChatRole role, string name, string content)
    {
        Role = role;
        Author = name;
        Content = content;
    }

    public ChatMessage(IFunctionCall functionCall)
    {
        Role = ChatRole.Assistant;
        FunctionCalls = new List<IFunctionCall> { functionCall };
    }

    public ChatMessage(ICollection<IFunctionCall> functionCalls)
    {
        Role = ChatRole.Assistant;
        FunctionCalls = functionCalls;
    }

    public ChatMessage(IFunctionResult functionResult)
    {
        Role = ChatRole.Function;
        FunctionResult = functionResult;
    }

    public ChatRole Role { get; set; }

    public string? Author { get; set; }

    public string? Content { get; set; }

    public ICollection<IFunctionCall> FunctionCalls { get; set; } = new List<IFunctionCall>();

    public IFunctionResult? FunctionResult { get; set; }

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    public static ChatMessage FromSystem(string content)
    {
        return new ChatMessage(ChatRole.System, content);
    }

    public static ChatMessage FromUser(string content)
    {
        return new ChatMessage(ChatRole.User, content);
    }

    public static ChatMessage FromUser(string name, string content)
    {
        return new ChatMessage(ChatRole.User, name, content);
    }

    public static ChatMessage FromAssistant(string content)
    {
        return new ChatMessage(ChatRole.Assistant, content);
    }

    public static ChatMessage FromAssistant(IFunctionCall functionCall)
    {
        return new ChatMessage(functionCall);
    }

    public static ChatMessage FromAssistant(ICollection<IFunctionCall> functionCalls)
    {
        return new ChatMessage(functionCalls);
    }

    public static ChatMessage FromFunction(IFunctionResult functionResult)
    {
        return new ChatMessage(functionResult);
    }
}
