using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Interfaces;

namespace ChatAIze.GenerativeCS.Models;

public record ChatMessage<TFunctionCall, TFunctionResult> : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    public ChatMessage() { }

    public ChatMessage(ChatRole role, string content, PinLocation pinLocation = PinLocation.None)
    {
        Role = role;
        Content = content;
        PinLocation = pinLocation;
    }

    public ChatMessage(ChatRole role, string author, string content, PinLocation pinLocation = PinLocation.None)
    {
        Role = role;
        Author = author;
        Content = content;
        PinLocation = pinLocation;
    }

    public ChatMessage(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = [functionCall];
        PinLocation = pinLocation;
    }

    public ChatMessage(IEnumerable<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = functionCalls.ToList();
        PinLocation = pinLocation;
    }

    public ChatMessage(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Function;
        FunctionResult = functionResult;
        PinLocation = pinLocation;
    }

    public ChatRole Role { get; set; }

    public string? Author { get; set; }

    public string? Content { get; set; }

    public ICollection<TFunctionCall> FunctionCalls { get; set; } = [];

    public TFunctionResult? FunctionResult { get; set; }

    public PinLocation PinLocation { get; set; }

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;

    public static IChatMessage<TFunctionCall, TFunctionResult> FromSystem(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.System, content, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromUser(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.User, content, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromUser(string author, string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.User, author, content, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(string author, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.Chatbot, author, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(functionCall, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(IEnumerable<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(functionCalls, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromFunction(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(functionResult, pinLocation);
    }
}

public record ChatMessage : ChatMessage<FunctionCall, FunctionResult>
{
    public ChatMessage() : base() { }

    public ChatMessage(ChatRole role, string content, PinLocation pinLocation = PinLocation.None) : base(role, content, pinLocation) { }

    public ChatMessage(ChatRole role, string author, string content, PinLocation pinLocation = PinLocation.None) : base(role, author, content, pinLocation) { }

    public ChatMessage(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None) : base(functionCall, pinLocation) { }

    public ChatMessage(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None) : base(functionCalls, pinLocation) { }

    public ChatMessage(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None) : base(functionResult, pinLocation) { }
}
