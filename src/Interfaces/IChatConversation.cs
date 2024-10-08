using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Interfaces;

public interface IChatConversation<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    string? UserTrackingId => null;

    ICollection<TMessage> Messages { get; }

    ValueTask<TMessage> FromSystemAsync(string message, PinLocation pinLocation = PinLocation.None);

    ValueTask<TMessage> FromUserAsync(string message, PinLocation pinLocation = PinLocation.None);

    ValueTask<TMessage> FromUserAsync(string author, string message, PinLocation pinLocation = PinLocation.None);

    ValueTask<TMessage> FromChatbotAsync(string message, PinLocation pinLocation = PinLocation.None);

    ValueTask<TMessage> FromChatbotAsync(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None);

    ValueTask<TMessage> FromChatbotAsync(IEnumerable<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None);

    ValueTask<TMessage> FromFunctionAsync(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None);
}
