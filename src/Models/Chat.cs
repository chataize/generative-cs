using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

public record Chat<TMessage, TFunctionCall, TFunctionResult> : IChat<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    public Chat() { }

    public Chat(string systemMessage)
    {
        FromSystem(systemMessage);
    }

    public Chat(IEnumerable<TMessage> messages)
    {
        Messages = messages.ToList();
    }

    public string? UserTrackingId { get; set; }

    public ICollection<TMessage> Messages { get; set; } = [];

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;

    public ValueTask<TMessage> FromSystemAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.System,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    public async void FromSystem(string message, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromSystemAsync(message, pinLocation);
    }

    public ValueTask<TMessage> FromUserAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.User,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    public async void FromUser(string message, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromUserAsync(message, pinLocation);
    }

    public ValueTask<TMessage> FromUserAsync(string userName, string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.User,
            UserName = userName,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    public async void FromUser(string userName, string message, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromUserAsync(userName, message, pinLocation);
    }

    public ValueTask<TMessage> FromChatbotAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.Chatbot,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    public async void FromChatbot(string message, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromChatbotAsync(message, pinLocation);
    }

    public ValueTask<TMessage> FromChatbotAsync(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.Chatbot,
            FunctionCalls = [functionCall],
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    public async void FromChatbot(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromChatbotAsync(functionCall, pinLocation);
    }

    public ValueTask<TMessage> FromChatbotAsync(IEnumerable<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.Chatbot,
            FunctionCalls = functionCalls.ToList(),
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    public async void FromChatbot(IEnumerable<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromChatbotAsync(functionCalls, pinLocation);
    }

    public ValueTask<TMessage> FromFunctionAsync(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.Function,
            FunctionResult = functionResult,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    public async void FromFunction(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromFunctionAsync(functionResult, pinLocation);
    }
}

public record Chat : Chat<ChatMessage, FunctionCall, FunctionResult>
{
    public Chat() : base() { }

    public Chat(string systemMessage) : base(systemMessage) { }

    public Chat(IEnumerable<ChatMessage> messages) : base(messages) { }
}
