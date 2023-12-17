namespace GenerativeCS.Interfaces;

public interface IChatConversation
{
    ICollection<IChatMessage> Messages { get; set; }
}
