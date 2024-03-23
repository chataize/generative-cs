using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Interfaces;

public interface IChatConversation<T> where T : IChatMessage
{
    string? User { get; }

    ICollection<T> Messages { get; }

    Task<T> FromSystemAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task<T> FromUserAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task<T> FromUserAsync(string name, string message, PinLocation pinLocation = PinLocation.None);

    Task<T> FromChatbotAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task<T> FromChatbotAsync(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None);

    Task<T> FromChatbotAsync(IEnumerable<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None);

    Task<T> FromFunctionAsync(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None);
}
