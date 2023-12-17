namespace GenerativeCS.Interfaces;

public interface IChatConversation
{
    IEnumerable<IChatMessage> Messages { get; set; }
}
