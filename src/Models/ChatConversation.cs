using GenerativeCS.Interfaces;

namespace GenerativeCS.Models;

public record ChatConversation : IChatConversation<ChatMessage>
{
    public ChatConversation() { }

    public ChatConversation(string systemMessage)
    {
        Messages.Add(ChatMessage.FromSystem(systemMessage));
    }

    public ICollection<ChatMessage> Messages { get; set; } =  new List<ChatMessage>();

    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.Now;

    public void FromSystem(string message)
    {
        Messages.Add(ChatMessage.FromSystem(message));
    }

    public void FromUser(string message)
    {
        Messages.Add(ChatMessage.FromUser(message));
    }

    public void FromUser(string name, string message)
    {
        Messages.Add(ChatMessage.FromUser(name, message));
    }

    public void FromAssistant(string message)
    {
        Messages.Add(ChatMessage.FromAssistant(message));
    }

    public void FromAssistant(IFunctionCall functionCall)
    {
        Messages.Add(ChatMessage.FromAssistant(functionCall));
    }

    public void FromFunction(string name, string message)
    {
        Messages.Add(ChatMessage.FromFunction(name, message));
    }
}
