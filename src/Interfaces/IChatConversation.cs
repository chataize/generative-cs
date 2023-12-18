namespace GenerativeCS.Interfaces;

public interface IChatConversation<TMessage> where TMessage : IChatMessage, new()
{
    ICollection<TMessage> Messages { get; }

    ICollection<Delegate> Functions { get; }

    void FromSystem(string content) 
    {
        Messages.Add(IChatMessage.FromSystem<TMessage>(content));
    }

    void FromUser(string content) 
    {
        Messages.Add(IChatMessage.FromUser<TMessage>(content));
    }

    void FromUser(string name, string content) 
    {
        Messages.Add(IChatMessage.FromUser<TMessage>(name, content));
    }

    void FromAssistant(string content) 
    {
        Messages.Add(IChatMessage.FromAssistant<TMessage>(content));
    }

    void FromAssistant(IFunctionCall functionCall) 
    {
        Messages.Add(IChatMessage.FromAssistant<TMessage>(functionCall));
    }

    void FromFunction(string name, string content) 
    {
        Messages.Add(IChatMessage.FromFunction<TMessage>(name, content));
    }
}
