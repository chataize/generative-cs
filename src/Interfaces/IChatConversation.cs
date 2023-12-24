using GenerativeCS.Models;

namespace GenerativeCS.Interfaces;

public interface IChatConversation<TMessage, TFunction> where TMessage : IChatMessage, new() where TFunction : IChatFunction, new()
{
    ICollection<TMessage> Messages { get; }

    ICollection<TFunction> Functions { get; }

    void FromSystem(string message, PinLocation pinLocation = PinLocation.None)
    {
        Messages.Add(IChatMessage.FromSystem<TMessage>(message, pinLocation));
    }

    void FromUser(string message, PinLocation pinLocation = PinLocation.None)
    {
        Messages.Add(IChatMessage.FromUser<TMessage>(message, pinLocation));
    }

    void FromUser(string name, string message, PinLocation pinLocation = PinLocation.None)
    {
        Messages.Add(IChatMessage.FromUser<TMessage>(name, message, pinLocation));
    }

    void FromAssistant(string message, PinLocation pinLocation = PinLocation.None)
    {
        Messages.Add(IChatMessage.FromAssistant<TMessage>(message, pinLocation));
    }

    void FromAssistant(IFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        Messages.Add(IChatMessage.FromAssistant<TMessage>(functionCall, pinLocation));
    }

    void FromAssistant(ICollection<IFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        Messages.Add(IChatMessage.FromAssistant<TMessage>(functionCalls, pinLocation));
    }

    void FromFunction(IFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        Messages.Add(IChatMessage.FromFunction<TMessage>(functionResult, pinLocation));
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

    void RemoveFunction(string name)
    {
        var functionToRemove = Functions.LastOrDefault(f => f.Name == name);
        if (functionToRemove != null)
        {
            Functions.Remove(functionToRemove);
        }
    }

    void RemoveFunction(Delegate function)
    {
        var functionToRemove = Functions.LastOrDefault(f => f.Function == function);
        if (functionToRemove != null)
        {
            Functions.Remove(functionToRemove);
        }
    }

    void ClearFunctions()
    {
        Functions.Clear();
    }
}
