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

    public void FromAssistant(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
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

    public void AddFunction(string name, bool requiresConfirmation = false)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation = false)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation));
    }

    public void AddFunction(Delegate callback)
    {
        Functions.Add(new ChatFunction(callback));
    }

    public void AddFunction(string name, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, callback));
    }

    public void AddFunction(string name, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, parameters));
    }

    public void AddFunction(string name, string? description, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, callback));
    }

    public void AddFunction(string name, string? description, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, string? description, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, parameters));
    }

    public void AddFunction(string name, bool requiresConfirmation, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, callback));
    }

    public void AddFunction(string name, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, Delegate callback)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, callback));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, IEnumerable<FunctionParameter> parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, parameters));
    }

    public void AddFunction(string name, string? description, bool requiresConfirmation, params FunctionParameter[] parameters)
    {
        Functions.Add(new ChatFunction(name, description, requiresConfirmation, parameters));
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

    public bool RemoveFunction(Delegate callback)
    {
        var function = Functions.LastOrDefault(f => f.Callback == callback);
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
