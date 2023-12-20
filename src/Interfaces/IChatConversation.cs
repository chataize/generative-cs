namespace GenerativeCS.Interfaces;

public interface IChatConversation<TMessage> where TMessage : IChatMessage, new()
{
    ICollection<TMessage> Messages { get; }

    ICollection<Delegate> Functions { get; }

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

    void AddFunction(Delegate function)
    {
        Functions.Add(function);
    }

    void RemoveFunction(Delegate function)
    {
        Functions.Remove(function);
    }

    void ClearFunctions()
    {
        Functions.Clear();
    }
}
