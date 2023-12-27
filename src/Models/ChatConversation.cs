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

    public void AddFunction(Delegate function)
    {
        var chatFunction = new ChatFunction(function);
        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, Delegate function)
    {
        var chatFunction = new ChatFunction(name, function);
        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, string? description, Delegate function)
    {
        var chatFunction = new ChatFunction(name, description, function);
        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, bool requireConfirmation, Delegate function)
    {
        var chatFunction = new ChatFunction(name, requireConfirmation, function);
        Functions.Add(chatFunction);
    }

    public void AddFunction(string name, string? description, bool requireConfirmation, Delegate function)
    {
        var chatFunction = new ChatFunction(name, description, requireConfirmation, function);
        Functions.Add(chatFunction);
    }

    public void RemoveFunction(ChatFunction function)
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
