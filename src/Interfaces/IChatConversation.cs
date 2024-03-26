using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Interfaces;

public interface IChatConversation<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    string? UserTrackingId { get; }

    ICollection<TMessage> Messages { get; }

    Task<TMessage> FromSystemAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task<TMessage> FromUserAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task<TMessage> FromUserAsync(string author, string message, PinLocation pinLocation = PinLocation.None);

    Task<TMessage> FromChatbotAsync(string message, PinLocation pinLocation = PinLocation.None);

    Task<TMessage> FromChatbotAsync(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None);

    Task<TMessage> FromChatbotAsync(IEnumerable<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None);

    Task<TMessage> FromFunctionAsync(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None);
}
