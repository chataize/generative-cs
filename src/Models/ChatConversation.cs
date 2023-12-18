using GenerativeCS.Interfaces;

namespace GenerativeCS.Models;

public record ChatConversation<TMessage> : IChatConversation<TMessage> where TMessage : IChatMessage, new()
{
    public ChatConversation() { }

    public ChatConversation(string systemMessage)
    {
        Messages.Add(IChatMessage.FromSystem<TMessage>(systemMessage));
    }

    public ICollection<TMessage> Messages { get; set; } = new List<TMessage>();

    public ICollection<Delegate> Functions { get; set; } = new List<Delegate>();

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    public void FromSystem(string message)
    {
        Messages.Add(IChatMessage.FromSystem<TMessage>(message));
    }

    public void FromUser(string message)
    {
        Messages.Add(IChatMessage.FromSystem<TMessage>(message));
    }

    public void FromUser(string name, string message)
    {
        Messages.Add(IChatMessage.FromUser<TMessage>(name, message));
    }

    public void FromAssistant(string message)
    {
        Messages.Add(IChatMessage.FromAssistant<TMessage>(message));
    }

    public void FromAssistant(IFunctionCall functionCall)
    {
        Messages.Add(IChatMessage.FromAssistant<TMessage>(functionCall));
    }

    public void FromFunction(string name, string message)
    {
        Messages.Add(IChatMessage.FromFunction<TMessage>(name, message));
    }
}

public record ChatConversation : ChatConversation<ChatMessage>
{
    public ChatConversation() { }

    public ChatConversation(string systemMessage) : base(systemMessage) { }
}
