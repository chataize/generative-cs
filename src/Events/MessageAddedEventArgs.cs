using System.Diagnostics.CodeAnalysis;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Events;

public class MessageAddedEventArgs : EventArgs
{
    public MessageAddedEventArgs() { }

    [SetsRequiredMembers]
    public MessageAddedEventArgs(ChatMessage message)
    {
        Message = message;
    }

    public required ChatMessage Message { get; init; }
}
