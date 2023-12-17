using GenerativeCS.Enums;

namespace GenerativeCS.Interfaces;

public interface IChatMessage
{
    ChatRole Role { get; set; }

    string? Name { get; set; }

    string? Content { get; set; }

    IFunctionCall? FunctionCall { get; set; }
}
