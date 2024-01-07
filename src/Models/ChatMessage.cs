using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Interfaces;

namespace ChatAIze.GenerativeCS.Models;

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
        Name = name;
        Content = content;
        PinLocation = pinLocation;
    }

    public ChatMessage(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Assistant;
        FunctionCalls = [functionCall];
        PinLocation = pinLocation;
    }

    public ChatMessage(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Assistant;
        FunctionCalls = functionCalls;
        PinLocation = pinLocation;
    }

    public ChatMessage(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Function;
        FunctionResult = functionResult;
        PinLocation = pinLocation;
    }

    public ChatRole Role { get; set; }

    public string? Name { get; set; }

    public string? Content { get; set; }

    public IEnumerable<FunctionCall> FunctionCalls { get; set; } = [];

    public FunctionResult? FunctionResult { get; set; }

    public PinLocation PinLocation { get; set; }

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;

    public static IChatMessage FromSystem(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(ChatRole.System, content, pinLocation);
    }

    public static IChatMessage FromUser(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(ChatRole.User, content, pinLocation);
    }

    public static IChatMessage FromUser(string name, string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(ChatRole.User, name, content, pinLocation);
    }

    public static IChatMessage FromAssistant(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(ChatRole.Assistant, content, pinLocation);
    }

    public static IChatMessage FromAssistant(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(functionCall, pinLocation);
    }

    public static IChatMessage FromAssistant(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(functionCalls, pinLocation);
    }

    public static IChatMessage FromFunction(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(functionResult, pinLocation);
    }
}
