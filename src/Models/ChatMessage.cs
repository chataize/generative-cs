using GenerativeCS.Enums;
using GenerativeCS.Interfaces;

namespace GenerativeCS.Models;

public record ChatMessage : IChatMessage
{
    public ChatMessage() { }

    public ChatMessage(ChatRole role, string content, PinLocation pinLocation = PinLocation.None)
    {
        Role = role;
        Content = content;
        PinLocation = pinLocation;
    }

    public ChatMessage(ChatRole role, string name, string content, PinLocation pinLocation = PinLocation.None)
    {
        Role = role;
        Author = name;
        Content = content;
        PinLocation = pinLocation;
    }

    public ChatMessage(IFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Assistant;
        FunctionCalls = new List<IFunctionCall> { functionCall };
        PinLocation = pinLocation;
    }

    public ChatMessage(ICollection<IFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Assistant;
        FunctionCalls = functionCalls;
        PinLocation = pinLocation;
    }

    public ChatMessage(IFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Function;
        FunctionResult = functionResult;
        PinLocation = pinLocation;
    }

    public ChatRole Role { get; set; }

    public string? Author { get; set; }

    public string? Content { get; set; }

    public ICollection<IFunctionCall> FunctionCalls { get; set; } = new List<IFunctionCall>();

    public IFunctionResult? FunctionResult { get; set; }

    public PinLocation PinLocation { get; set; }

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    public static ChatMessage FromSystem(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(ChatRole.System, content, pinLocation);
    }

    public static ChatMessage FromUser(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(ChatRole.User, content, pinLocation);
    }

    public static ChatMessage FromUser(string name, string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(ChatRole.User, name, content, pinLocation);
    }

    public static ChatMessage FromAssistant(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(ChatRole.Assistant, content, pinLocation);
    }

    public static ChatMessage FromAssistant(IFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(functionCall, pinLocation);
    }

    public static ChatMessage FromAssistant(ICollection<IFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(functionCalls, pinLocation);
    }

    public static ChatMessage FromFunction(IFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(functionResult, pinLocation);
    }
}
