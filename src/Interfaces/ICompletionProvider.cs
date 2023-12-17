namespace GenerativeCS.Interfaces;

public interface ICompletionProvider<TConversation, TMessage> where TConversation : IChatConversation<TMessage> where TMessage : IChatMessage, new()
{
    Task<string> CompleteAsync(string prompt);

    Task<string> CompleteAsync(TConversation conversation);
}
