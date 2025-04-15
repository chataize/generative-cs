using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

public record ChatMessage<TFunctionCall, TFunctionResult> : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    public ChatMessage() { }

    public ChatMessage(ChatRole role, string content, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        Role = role;
        Content = content;
        PinLocation = pinLocation;
        ImageUrls = imageUrls;
    }

    public ChatMessage(ChatRole role, string userName, string content, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        Role = role;
        UserName = userName;
        Content = content;
        PinLocation = pinLocation;
        ImageUrls = imageUrls;
    }

    public ChatMessage(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = [functionCall];
        PinLocation = pinLocation;
    }

    public ChatMessage(ICollection<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = functionCalls;
        PinLocation = pinLocation;
    }

    public ChatMessage(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Function;
        FunctionResult = functionResult;
        PinLocation = pinLocation;
    }

    public ChatRole Role { get; set; }

    public string? UserName { get; set; }

    public string? Content { get; set; }

    public ICollection<TFunctionCall> FunctionCalls { get; set; } = [];

    public TFunctionResult? FunctionResult { get; set; }

    public PinLocation PinLocation { get; set; }

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<string> ImageUrls { get; set; } = [];

    public static IChatMessage<TFunctionCall, TFunctionResult> FromSystem(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.System, content, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromUser(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.User, content, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromUser(string userName, string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.User, userName, content, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(string userName, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.Chatbot, userName, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(functionCall, pinLocation);
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(ICollection<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
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

    public ChatMessage(ChatRole role, string userName, string content, PinLocation pinLocation = PinLocation.None) : base(role, userName, content, pinLocation) { }

    public ChatMessage(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None) : base(functionCall, pinLocation) { }

    public ChatMessage(ICollection<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None) : base(functionCalls, pinLocation) { }

    public ChatMessage(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None) : base(functionResult, pinLocation) { }
}
