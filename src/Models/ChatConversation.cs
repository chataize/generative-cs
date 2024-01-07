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

    public ICollection<ChatFunction> Functions { get; set; } = [];

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    public Func<T, Task> AddMessageCallback { get; set; } = (_) => Task.CompletedTask;

    public async Task FromSystemAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.System,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        await AddMessageCallback(chatMessage);
    }

    public async void FromSystem(string message, PinLocation pinLocation = PinLocation.None)
    {
        await FromSystemAsync(message, pinLocation);
    }

    public async Task FromUserAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.User,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        await AddMessageCallback(chatMessage);
    }

    public async void FromUser(string message, PinLocation pinLocation = PinLocation.None)
    {
        await FromUserAsync(message, pinLocation);
    }

    public async Task FromUserAsync(string name, string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.User,
            Name = name,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        await AddMessageCallback(chatMessage);
    }

    public async void FromUser(string name, string message, PinLocation pinLocation = PinLocation.None)
    {
        await FromUserAsync(name, message, pinLocation);
    }

    public async Task FromAssistantAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.Assistant,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        await AddMessageCallback(chatMessage);
    }

    public async void FromAssistant(string message, PinLocation pinLocation = PinLocation.None)
    {
        await FromAssistantAsync(message, pinLocation);
    }

    public async Task FromAssistantAsync(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.Assistant,
            FunctionCalls = [functionCall],
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        await AddMessageCallback(chatMessage);
    }

    public async void FromAssistant(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        await FromAssistantAsync(functionCall, pinLocation);
    }

    public async Task FromAssistantAsync(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.Assistant,
            FunctionCalls = functionCalls,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        await AddMessageCallback(chatMessage);
    }

    public async void FromAssistant(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        await FromAssistantAsync(functionCalls, pinLocation);
    }

    public async Task FromFunctionAsync(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new T
        {
            Role = ChatRole.Function,
            FunctionResult = functionResult,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        await AddMessageCallback(chatMessage);
    }

    public async void FromFunction(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        await FromFunctionAsync(functionResult, pinLocation);
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

public record ChatConversation : ChatConversation<ChatMessage>
{
    public ChatConversation() { }

    public ChatConversation(IEnumerable<ChatMessage> messages) : base(messages) { }
}
