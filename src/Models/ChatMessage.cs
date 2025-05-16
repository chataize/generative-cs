using ChatAIze.Abstractions.Chat;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ChatAIze.GenerativeCS.Models;

public record ChatMessage<TFunctionCall, TFunctionResult> : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    public ChatMessage() 
    {
        Parts = new List<IChatContentPart>();
    }

    public ChatMessage(ChatRole role, string content, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        Role = role;
        PinLocation = pinLocation;
        ImageUrls = imageUrls;
        Parts = new List<IChatContentPart> { new TextPart(content) };
    }

    public ChatMessage(ChatRole role, string userName, string content, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        Role = role;
        UserName = userName;
        PinLocation = pinLocation;
        ImageUrls = imageUrls;
        Parts = new List<IChatContentPart> { new TextPart(content) };
    }

    public ChatMessage(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = [functionCall];
        PinLocation = pinLocation;
        Parts = new List<IChatContentPart>();
    }

    public ChatMessage(ICollection<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = functionCalls;
        PinLocation = pinLocation;
        Parts = new List<IChatContentPart>();
    }

    public ChatMessage(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Function;
        FunctionResult = functionResult;
        PinLocation = pinLocation;
        Parts = new List<IChatContentPart>();
    }

    public ChatRole Role { get; set; }

    public string? UserName { get; set; }

    [Obsolete("Use Parts to support multimodal content. This property interacts with the first TextPart among the Parts collection.")]
    public string? Content 
    { 
        get => Parts?.OfType<TextPart>().FirstOrDefault()?.Text;
        set
        {
            Parts ??= new List<IChatContentPart>();
            Parts.RemoveAll(p => p is TextPart);
            if (value != null)
            {
                Parts.Insert(0, new TextPart(value));
            }
        }
    }

    /// <summary>
    /// Represents the collection of content parts for the message (e.g., text, image, file data).
    /// For Gemini, this will be used to construct the 'parts' array.
    /// </summary>
    public List<IChatContentPart> Parts { get; set; }

    public ICollection<TFunctionCall> FunctionCalls { get; set; } = [];

    public TFunctionResult? FunctionResult { get; set; }

    public PinLocation PinLocation { get; set; }

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<string> ImageUrls { get; set; } = [];

    public static IChatMessage<TFunctionCall, TFunctionResult> FromSystem(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult> { Role = ChatRole.System, Parts = new List<IChatContentPart> { new TextPart(content) }, PinLocation = pinLocation };
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromUser(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult> { Role = ChatRole.User, Parts = new List<IChatContentPart> { new TextPart(content) }, PinLocation = pinLocation };
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromUser(string userName, string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult> { Role = ChatRole.User, UserName = userName, Parts = new List<IChatContentPart> { new TextPart(content) }, PinLocation = pinLocation };
    }

    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(string userName, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult> { Role = ChatRole.Chatbot, UserName = userName, PinLocation = pinLocation, Parts = new List<IChatContentPart>() };
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

    public ChatMessage(ChatRole role, string content, PinLocation pinLocation = PinLocation.None) 
        : base()
    {
        Role = role;
        PinLocation = pinLocation;
        Parts = new List<IChatContentPart> { new TextPart(content) };
    }

    public ChatMessage(ChatRole role, string userName, string content, PinLocation pinLocation = PinLocation.None) 
        : base()
    {
        Role = role;
        UserName = userName;
        PinLocation = pinLocation;
        Parts = new List<IChatContentPart> { new TextPart(content) };
    }

    public ChatMessage(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None) : base(functionCall, pinLocation) { }

    public ChatMessage(ICollection<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None) : base(functionCalls, pinLocation) { }

    public ChatMessage(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None) : base(functionResult, pinLocation) { }
}
