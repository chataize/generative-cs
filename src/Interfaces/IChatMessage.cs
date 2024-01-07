using ChatAIze.GenerativeCS.Enums;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Interfaces;

public interface IChatMessage
{
    ChatRole Role { get; set; }

    string? Name { get; set; }

    string? Content { get; set; }

    IEnumerable<FunctionCall> FunctionCalls { get; set; }

    FunctionResult? FunctionResult { get; set; }

    PinLocation PinLocation { get; set; }
}
