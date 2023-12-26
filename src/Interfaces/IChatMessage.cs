using GenerativeCS.Enums;

namespace GenerativeCS.Interfaces;

public interface IChatMessage
{
    ChatRole Role { get; set; }

    string? Author { get; set; }

    string? Content { get; set; }

    ICollection<IFunctionCall> FunctionCalls { get; set; }

    IFunctionResult? FunctionResult { get; set; }

    PinLocation PinLocation { get; set; }

    static T FromSystem<T>(string content, PinLocation pinLocation = PinLocation.None) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.System,
            Content = content,
            PinLocation = pinLocation
        };
    }

    static T FromUser<T>(string content, PinLocation pinLocation = PinLocation.None) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.User,
            Content = content,
            PinLocation = pinLocation
        };
    }

    static T FromUser<T>(string name, string content, PinLocation pinLocation = PinLocation.None) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.User,
            Author = name,
            Content = content,
            PinLocation = pinLocation
        };
    }

    static T FromAssistant<T>(string content, PinLocation pinLocation = PinLocation.None) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.Assistant,
            Content = content,
            PinLocation = pinLocation
        };
    }

    static T FromAssistant<T>(IFunctionCall functionCall, PinLocation pinLocation = PinLocation.None) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.Assistant,
            FunctionCalls = new List<IFunctionCall> { functionCall },
            PinLocation = pinLocation
        };
    }

    static T FromAssistant<T>(ICollection<IFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.Assistant,
            FunctionCalls = functionCalls,
            PinLocation = pinLocation
        };
    }

    static T FromFunction<T>(IFunctionResult functionResult, PinLocation pinLocation = PinLocation.None) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.Function,
            FunctionResult = functionResult,
            PinLocation = pinLocation
        };
    }

    T Clone<T>() where T : IChatMessage, new()
    {
        return new T
        {
            Role = Role,
            Author = Author,
            Content = Content,
            FunctionCalls = FunctionCalls,
            FunctionResult = FunctionResult,
            PinLocation = PinLocation
        };
    }
}
