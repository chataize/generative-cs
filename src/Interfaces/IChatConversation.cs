namespace GenerativeCS.Interfaces;

public interface IChatConversation<TMessageCollection, TMessage> where TMessageCollection : ICollection<TMessage> where TMessage : IChatMessage
{
    TMessageCollection Messages { get; }
}
