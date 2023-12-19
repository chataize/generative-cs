using System.Diagnostics.CodeAnalysis;
using GenerativeCS.Interfaces;
using GenerativeCS.Models;

namespace GenerativeCS.Events;

public class MessageAddedEventArgs<TMessage> : EventArgs where TMessage : IChatMessage, new()
{
    public MessageAddedEventArgs() { }

    [SetsRequiredMembers]
    public MessageAddedEventArgs(TMessage message)
    {
        Message = message;
    }

    public required TMessage Message { get; init; }
}

public class MessageAddedEventArgs : MessageAddedEventArgs<ChatMessage>
{
    public MessageAddedEventArgs() { }

    [SetsRequiredMembers]
    public MessageAddedEventArgs(ChatMessage message) : base(message) { }
}
