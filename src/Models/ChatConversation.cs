using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Interfaces;

namespace ChatAIze.GenerativeCS.Models;

public record ChatConversation<T> : IChatConversation<T> where T : IChatMessage, new()
{
    public ChatConversation() { }

    public ChatConversation(IEnumerable<T> messages)
    {
        Messages = messages.ToList();
    }

    public string? User { get; set; }

    public ICollection<T> Messages { get; set; } = [];

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    public Task<T> FromSystemAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.System,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return Task.FromResult(chatMessage);
    }

    public async void FromSystem(string message, PinLocation pinLocation = PinLocation.None)
    {
        await FromSystemAsync(message, pinLocation);
    }

    public Task<T> FromUserAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.User,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return Task.FromResult(chatMessage);
    }

    public async void FromUser(string message, PinLocation pinLocation = PinLocation.None)
    {
        await FromUserAsync(message, pinLocation);
    }

    public Task<T> FromUserAsync(string name, string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.User,
            Name = name,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return Task.FromResult(chatMessage);
    }

    public async void FromUser(string name, string message, PinLocation pinLocation = PinLocation.None)
    {
        await FromUserAsync(name, message, pinLocation);
    }

    public Task<T> FromAssistantAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.Assistant,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return Task.FromResult(chatMessage);
    }

    public async void FromAssistant(string message, PinLocation pinLocation = PinLocation.None)
    {
        await FromAssistantAsync(message, pinLocation);
    }

    public Task<T> FromAssistantAsync(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.Assistant,
            FunctionCalls = [functionCall],
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return Task.FromResult(chatMessage);
    }

    public async void FromAssistant(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        await FromAssistantAsync(functionCall, pinLocation);
    }

    public Task<T> FromAssistantAsync(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.Assistant,
            FunctionCalls = functionCalls.ToList(),
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return Task.FromResult(chatMessage);
    }

    public async void FromAssistant(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        await FromAssistantAsync(functionCalls, pinLocation);
    }

    public Task<T> FromFunctionAsync(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.Function,
            FunctionResult = functionResult,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return Task.FromResult(chatMessage);
    }

    public async void FromFunction(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        await FromFunctionAsync(functionResult, pinLocation);
    }
}

public record ChatConversation : ChatConversation<ChatMessage>
{
    public ChatConversation() { }

    public ChatConversation(IEnumerable<ChatMessage> messages) : base(messages) { }
}
