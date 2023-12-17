using System.Diagnostics.CodeAnalysis;
using GenerativeCS.Interfaces;

namespace GenerativeCS.Models;

public record CompletionRequest<TMessage> where TMessage : IChatMessage
{
    public CompletionRequest() { }

    [SetsRequiredMembers]
    public CompletionRequest(string model, IEnumerable<TMessage> messages)
    {
        Model = model;
        Messages = messages;
    }

    public required string Model { get; set; }

    public required IEnumerable<TMessage> Messages { get; set; }
}
