﻿using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Interfaces;

namespace ChatAIze.GenerativeCS.Models;

public record ChatConversation<TMessage, TFunctionCall, TFunctionResult> : IChatConversation<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    public ChatConversation() { }

    public ChatConversation(string systemMessage)
    {
        FromSystem(systemMessage);
    }

    public ChatConversation(IEnumerable<TMessage> messages)
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

    public ValueTask<TMessage> FromUserAsync(string author, string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.User,
            Author = author,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    public async void FromUser(string author, string message, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromUserAsync(author, message, pinLocation);
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

public record ChatConversation : ChatConversation<ChatMessage, FunctionCall, FunctionResult>
{
    public ChatConversation() : base() { }

    public ChatConversation(string systemMessage) : base(systemMessage) { }

    public ChatConversation(IEnumerable<ChatMessage> messages) : base(messages) { }
}
