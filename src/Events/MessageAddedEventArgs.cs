using System.Diagnostics.CodeAnalysis;
using ChatAIze.GenerativeCS.Interfaces;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Events;

public class MessageAddedEventArgs<T> : EventArgs where T : IChatMessage
{
    public MessageAddedEventArgs() { }

    [SetsRequiredMembers]
    public MessageAddedEventArgs(T message)
    {
        Message = message;
    }

    public required T Message { get; init; }
}
