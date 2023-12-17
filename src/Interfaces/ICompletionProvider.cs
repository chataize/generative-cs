namespace GenerativeCS.Interfaces;

public interface ICompletionProvider<TMessageCollection, TMessage> where TMessageCollection : ICollection<TMessage> where TMessage : IChatMessage
{
    Task<string> CompleteAsync(string prompt);

    Task<string> CompleteAsync(IChatConversation<TMessageCollection, TMessage> conversation);
}
