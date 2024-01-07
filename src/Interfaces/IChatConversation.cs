using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Interfaces;

public interface IChatConversation<T> where T : IChatMessage
{
    string? User { get; }

    ICollection<T> Messages { get; }

    ICollection<ChatFunction> Functions { get; }

    Func<T, Task> AddMessageCallback { get; }

    Task FromSystemAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task FromUserAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task FromUserAsync(string name, string message, PinLocation pinLocation = PinLocation.None);

    Task FromAssistantAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task FromAssistantAsync(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None);

    Task FromAssistantAsync(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None);

    Task FromFunctionAsync(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None);
}
