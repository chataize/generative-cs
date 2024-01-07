using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Interfaces;

public interface IChatConversation<T> where T : IChatMessage
{
    string? User { get; }

    ICollection<T> Messages { get; }

    ICollection<ChatFunction> Functions { get; }

    Task<T> FromSystemAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task<T> FromUserAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task<T> FromUserAsync(string name, string message, PinLocation pinLocation = PinLocation.None);

    Task<T> FromAssistantAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task<T> FromAssistantAsync(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None);

    Task<T> FromAssistantAsync(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None);

    Task<T> FromFunctionAsync(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None);
}
