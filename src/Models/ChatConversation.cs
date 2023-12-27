using GenerativeCS.Events;

namespace GenerativeCS.Models;

public record ChatConversation
{
    public ChatConversation() { }

    public ChatConversation(string systemMessage)
    {
        FromSystem(systemMessage, PinLocation.Begin);
    }

    public event EventHandler<MessageAddedEventArgs>? MessageAdded;

    public List<ChatMessage> Messages { get; set; } = [];

    public List<ChatFunction> Functions { get; set; } = [];

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    public void FromSystem(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = ChatMessage.FromSystem(message, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs(chatMessage));
    }

    public void FromUser(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = ChatMessage.FromUser(message, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs(chatMessage));
    }

    public void FromUser(string name, string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = ChatMessage.FromUser(name, message, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs(chatMessage));
    }

    public void FromAssistant(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = ChatMessage.FromAssistant(message, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs(chatMessage));
    }

    public void FromAssistant(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = ChatMessage.FromAssistant(functionCall, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs(chatMessage));
    }

    public void FromAssistant(List<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = ChatMessage.FromAssistant(functionCalls, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs(chatMessage));
    }

    public void FromFunction(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = ChatMessage.FromFunction(functionResult, pinLocation);

        Messages.Add(chatMessage);
        MessageAdded?.Invoke(this, new MessageAddedEventArgs(chatMessage));
    }

    public void AddFunction(ChatFunction function)
    {
        Functions.Add(function);
    }

    public void AddFunction(Delegate operation)
    {
        var chatFunction = new ChatFunction(operation);
        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, Delegate operation)
    {
        var chatFunction = new ChatFunction(name, operation);
        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, string? description, Delegate operation)
    {
        var chatFunction = new ChatFunction(name, description, operation);
        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, bool requiresConfirmation, Delegate operation)
    {
        var chatFunction = new ChatFunction(name, requiresConfirmation, operation);
        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, Delegate operation)
    {
        var chatFunction = new ChatFunction(name, description, requiresConfirmation, operation);
        Functions.Add(chatFunction);
    }

    public bool RemoveFunction(ChatFunction function)
    {
        return Functions.Remove(function);
    }

    public bool RemoveFunction(string name)
    {
        var function = Functions.LastOrDefault(f => f.Name == name);
        if (function == null)
        {
            return false;
        }

        return Functions.Remove(function);
    }

    public bool RemoveFunction(Delegate operation)
    {
        var function = Functions.LastOrDefault(f => f.Operation == operation);
        if (function == null)
        {
            return false;
        }

        return Functions.Remove(function);
    }

    public void ClearFunctions()
    {
        Functions.Clear();
    }
}
