namespace GenerativeCS.Interfaces;

public interface ICompletionProvider<TConversation, TMessage> where TConversation : IChatConversation<TMessage> where TMessage : IChatMessage, new()
{
    ICollection<Delegate> Functions { get; }

    Task<string> CompleteAsync(string prompt);

    Task<string> CompleteAsync(TConversation conversation);

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
