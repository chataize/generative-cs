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
        Author = name;
        Content = content;
        PinLocation = pinLocation;
    }

    public ChatMessage(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = [functionCall];
        PinLocation = pinLocation;
    }

    public ChatMessage(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = functionCalls.ToList();
        PinLocation = pinLocation;
    }

    public ChatMessage(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Function;
        FunctionResult = functionResult;
        PinLocation = pinLocation;
    }

    public ChatRole Role { get; set; }

    public string? Author { get; set; }

    public string? Content { get; set; }

    public List<FunctionCall> FunctionCalls { get; set; } = [];

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

    public static IChatMessage FromChatbot(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(ChatRole.Chatbot, content, pinLocation);
    }

    public static IChatMessage FromChatbot(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(functionCall, pinLocation);
    }

    public static IChatMessage FromChatbot(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(functionCalls, pinLocation);
    }

    public static IChatMessage FromFunction(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage(functionResult, pinLocation);
    }
}
