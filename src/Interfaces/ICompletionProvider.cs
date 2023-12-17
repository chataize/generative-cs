namespace GenerativeCS.Interfaces;

public interface ICompletionProvider
{
    Task<string> CompleteAsync(string prompt);

    Task<string> CompleteAsync(IChatConversation conversation);
}
