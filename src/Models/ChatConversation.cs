using GenerativeCS.Events;
using GenerativeCS.Interfaces;

namespace GenerativeCS.Models;

public record ChatConversation<TMessage, TFunction> : IChatConversation<TMessage, TFunction> where TMessage : IChatMessage, new() where TFunction : IChatFunction, new()
{
    public ChatConversation() { }

    public ChatConversation(string systemMessage)
    {
        Messages.Add(IChatMessage.FromSystem<TMessage>(systemMessage, PinLocation.Begin));
    }

    public event EventHandler<MessageAddedEventArgs<TMessage>>? MessageAdded;

    public ICollection<TMessage> Messages { get; set; } = new List<TMessage>();

    public ICollection<TFunction> Functions { get; set; } = new List<TFunction>();

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    public void FromSystem(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = IChatMessage.FromSystem<TMessage>(message, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs<TMessage>(chatMessage));
    }

    public void FromUser(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = IChatMessage.FromUser<TMessage>(message, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs<TMessage>(chatMessage));
    }

    public void FromUser(string name, string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = IChatMessage.FromUser<TMessage>(name, message, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs<TMessage>(chatMessage));
    }

    public void FromAssistant(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = IChatMessage.FromAssistant<TMessage>(message, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs<TMessage>(chatMessage));
    }

    public void FromAssistant(IFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = IChatMessage.FromAssistant<TMessage>(functionCall, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs<TMessage>(chatMessage));
    }

    public void FromAssistant(ICollection<IFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = IChatMessage.FromAssistant<TMessage>(functionCalls, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs<TMessage>(chatMessage));
    }

    public void FromFunction(IFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = IChatMessage.FromFunction<TMessage>(functionResult, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs<TMessage>(chatMessage));
    }

    public void AddFunction(TFunction function)
    {
        Functions.Add(function);
    }

    public void AddFunction(Delegate function)
    {
        var chatFunction = new TFunction
        {
            Name = function.Method.Name,
            Function = function
        };

        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, Delegate function)
    {
        var chatFunction = new TFunction
        {
            Name = name,
            Function = function
        };

        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, string? description, Delegate function)
    {
        var chatFunction = new TFunction
        {
            Name = name,
            Description = description,
            Function = function
        };

        Functions.Add(chatFunction);
    }

    public void RemoveFunction(TFunction function)
    {
        Functions.Remove(function);
    }

    public void RemoveFunction(string name)
    {
        var functionToRemove = Functions.LastOrDefault(f => f.Name == name);
        if (functionToRemove != null)
        {
            Functions.Remove(functionToRemove);
        }
    }

    public void RemoveFunction(Delegate function)
    {
        var functionToRemove = Functions.LastOrDefault(f => f.Function == function);
        if (functionToRemove != null)
        {
            Functions.Remove(functionToRemove);
        }
    }

    public void ClearFunctions()
    {
        Functions.Clear();
    }
}

public record ChatConversation : ChatConversation<ChatMessage, ChatFunction>
{
    public ChatConversation() { }

    public ChatConversation(string systemMessage) : base(systemMessage) { }
}
