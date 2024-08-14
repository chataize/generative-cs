using ChatAIze.GenerativeCS.Enums;

namespace ChatAIze.GenerativeCS.Interfaces;

public interface IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    ChatRole Role { get; set; }

    string? Author
    {
        get => null;
        set
        {
            return;
        }
    }

    string? Content { get; set; }

    ICollection<TFunctionCall> FunctionCalls { get; set; }

    TFunctionResult? FunctionResult { get; set; }

    PinLocation PinLocation { get; set; }
}
