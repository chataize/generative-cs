using GenerativeCS.Models;

namespace GenerativeCS.Interfaces;

public interface IChatConversation<TMessage, TFunction> where TMessage : IChatMessage, new() where TFunction : IChatFunction, new()
{
    ICollection<TMessage> Messages { get; }

    ICollection<TFunction> Functions { get; }

    void FromSystem(string message)
    {
        Messages.Add(IChatMessage.FromSystem<TMessage>(message));
    }

    void FromUser(string message)
    {
        Messages.Add(IChatMessage.FromUser<TMessage>(message));
    }

    void FromUser(string name, string message)
    {
        Messages.Add(IChatMessage.FromUser<TMessage>(name, message));
    }

    void FromAssistant(string message)
    {
        Messages.Add(IChatMessage.FromAssistant<TMessage>(message));
    }

    void FromAssistant(IFunctionCall functionCall)
    {
        Messages.Add(IChatMessage.FromAssistant<TMessage>(functionCall));
    }

    void FromFunction(IFunctionResult functionResult)
    {
        Messages.Add(IChatMessage.FromFunction<TMessage>(functionResult));
    }

    void AddFunction(TFunction function)
    {
        Functions.Add(function);
    }

    void AddFunction(Delegate function)
    {
        var chatFunction = new TFunction
        {
            Name = function.Method.Name,
            Function = function
        };

        Functions.Add(chatFunction);
    }

    void AddFunction(string name, Delegate function)
    {
        var chatFunction = new TFunction
        {
            Name = name,
            Function = function
        };

        Functions.Add(chatFunction);
    }

    void AddFunction(string name, string? description, Delegate function)
    {
        var chatFunction = new TFunction
        {
            Name = name,
            Description = description,
            Function = function
        };

        Functions.Add(chatFunction);
    }

    void RemoveFunction(TFunction function)
    {
        Functions.Remove(function);
    }

    void ClearFunctions()
    {
        Functions.Clear();
    }
}
