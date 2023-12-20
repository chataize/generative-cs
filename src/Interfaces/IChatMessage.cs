using GenerativeCS.Enums;

namespace GenerativeCS.Interfaces;

public interface IChatMessage
{
    ChatRole Role { get; set; }

    string? Name { get; set; }

    string? Content { get; set; }

    IFunctionCall? FunctionCall { get; set; }

    IFunctionResult? FunctionResult { get; set; }

    static T FromSystem<T>(string content) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.System,
            Content = content
        };
    }

    static T FromUser<T>(string content) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.User,
            Content = content
        };
    }

    static T FromUser<T>(string name, string content) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.User,
            Name = name,
            Content = content
        };
    }

    static T FromAssistant<T>(string content) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.Assistant,
            Content = content
        };
    }

    static T FromAssistant<T>(IFunctionCall functionCall) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.Assistant,
            FunctionCall = functionCall
        };
    }

    static T FromFunction<T>(IFunctionResult functionResult) where T : IChatMessage, new()
    {
        return new T
        {
            Role = ChatRole.Function,
            FunctionResult = functionResult
        };
    }
}
