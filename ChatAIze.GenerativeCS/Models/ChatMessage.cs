using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

/// <summary>
/// Represents an individual chat message that may contain text, function calls, function results, or images.
/// </summary>
/// <typeparam name="TFunctionCall">Function call type used in the message.</typeparam>
/// <typeparam name="TFunctionResult">Function result type used in the message.</typeparam>
public record ChatMessage<TFunctionCall, TFunctionResult> : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    /// <summary>
    /// Initializes a new message with default values.
    /// </summary>
    public ChatMessage() { }

    /// <summary>
    /// Initializes a message with a role, content, and optional images.
    /// </summary>
    /// <param name="role">Role of the sender.</param>
    /// <param name="content">Text content of the message.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <param name="imageUrls">Image URLs attached to the message.</param>
    public ChatMessage(ChatRole role, string content, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        Role = role;
        Content = content;
        PinLocation = pinLocation;
        ImageUrls = imageUrls;
    }

    /// <summary>
    /// Initializes a message with a role, username, content, and optional images.
    /// </summary>
    /// <param name="role">Role of the sender.</param>
    /// <param name="userName">Optional display name associated with the message (not a stable identifier).</param>
    /// <param name="content">Text content of the message.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <param name="imageUrls">Image URLs attached to the message.</param>
    public ChatMessage(ChatRole role, string userName, string content, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        Role = role;
        UserName = userName;
        Content = content;
        PinLocation = pinLocation;
        ImageUrls = imageUrls;
    }

    /// <summary>
    /// Initializes a chatbot message that issues a single function call.
    /// </summary>
    /// <param name="functionCall">Function call issued by the model.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public ChatMessage(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = [functionCall];
        PinLocation = pinLocation;
    }

    /// <summary>
    /// Initializes a chatbot message that issues multiple function calls.
    /// </summary>
    /// <param name="functionCalls">Function calls issued by the model.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public ChatMessage(ICollection<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Chatbot;
        FunctionCalls = functionCalls;
        PinLocation = pinLocation;
    }

    /// <summary>
    /// Initializes a function result message.
    /// </summary>
    /// <param name="functionResult">Result returned by executing a function.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public ChatMessage(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        Role = ChatRole.Function;
        FunctionResult = functionResult;
        PinLocation = pinLocation;
    }

    /// <summary>
    /// Gets or sets the role associated with the message.
    /// </summary>
    public ChatRole Role { get; set; }

    /// <summary>
    /// Gets or sets an optional display name associated with the message (not a stable identifier).
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the text content for the message.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets function calls produced by the model.
    /// </summary>
    public ICollection<TFunctionCall> FunctionCalls { get; set; } = [];

    /// <summary>
    /// Gets or sets the function result returned to the model.
    /// </summary>
    public TFunctionResult? FunctionResult { get; set; }

    /// <summary>
    /// Gets or sets the pin location that affects message ordering.
    /// </summary>
    public PinLocation PinLocation { get; set; }

    /// <summary>
    /// Gets or sets the UTC creation time of the message.
    /// </summary>
    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets image URLs attached to the message.
    /// </summary>
    public ICollection<string> ImageUrls { get; set; } = [];

    /// <summary>
    /// Creates a system message with the supplied content.
    /// </summary>
    /// <param name="content">System directive.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>A strongly-typed chat message.</returns>
    public static IChatMessage<TFunctionCall, TFunctionResult> FromSystem(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.System, content, pinLocation);
    }

    /// <summary>
    /// Creates a user message with the supplied content.
    /// </summary>
    /// <param name="content">User text.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>A strongly-typed chat message.</returns>
    public static IChatMessage<TFunctionCall, TFunctionResult> FromUser(string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.User, content, pinLocation);
    }

    /// <summary>
    /// Creates a user message with username context.
    /// </summary>
    /// <param name="userName">Display name associated with the message (not a stable identifier).</param>
    /// <param name="content">User text.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>A strongly-typed chat message.</returns>
    public static IChatMessage<TFunctionCall, TFunctionResult> FromUser(string userName, string content, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.User, userName, content, pinLocation);
    }

    /// <summary>
    /// Creates a chatbot message with text content.
    /// </summary>
    /// <param name="userName">Message content to assign (parameter name kept for compatibility; represents display text).</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>A strongly-typed chat message.</returns>
    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(string userName, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(ChatRole.Chatbot, userName, pinLocation);
    }

    /// <summary>
    /// Creates a chatbot message containing a single function call.
    /// </summary>
    /// <param name="functionCall">Function call to include.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>A strongly-typed chat message.</returns>
    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(functionCall, pinLocation);
    }

    /// <summary>
    /// Creates a chatbot message containing multiple function calls.
    /// </summary>
    /// <param name="functionCalls">Function calls to include.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>A strongly-typed chat message.</returns>
    public static IChatMessage<TFunctionCall, TFunctionResult> FromChatbot(ICollection<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(functionCalls, pinLocation);
    }

    /// <summary>
    /// Creates a message containing a function result.
    /// </summary>
    /// <param name="functionResult">Function result to include.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>A strongly-typed chat message.</returns>
    public static IChatMessage<TFunctionCall, TFunctionResult> FromFunction(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        return new ChatMessage<TFunctionCall, TFunctionResult>(functionResult, pinLocation);
    }
}

/// <summary>
/// Non-generic chat message type using the built-in function call and result models.
/// </summary>
public record ChatMessage : ChatMessage<FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes a new message with default values.
    /// </summary>
    public ChatMessage() : base() { }

    /// <summary>
    /// Initializes a message with a role and content.
    /// </summary>
    /// <param name="role">Role of the sender.</param>
    /// <param name="content">Text content of the message.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public ChatMessage(ChatRole role, string content, PinLocation pinLocation = PinLocation.None) : base(role, content, pinLocation) { }

    /// <summary>
    /// Initializes a message with a role, display name, and content.
    /// </summary>
    /// <param name="role">Role of the sender.</param>
    /// <param name="userName">Optional display name associated with the message (not a stable identifier).</param>
    /// <param name="content">Text content of the message.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public ChatMessage(ChatRole role, string userName, string content, PinLocation pinLocation = PinLocation.None) : base(role, userName, content, pinLocation) { }

    /// <summary>
    /// Initializes a chatbot message with a single function call.
    /// </summary>
    /// <param name="functionCall">Function call issued by the model.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public ChatMessage(FunctionCall functionCall, PinLocation pinLocation = PinLocation.None) : base(functionCall, pinLocation) { }

    /// <summary>
    /// Initializes a chatbot message with multiple function calls.
    /// </summary>
    /// <param name="functionCalls">Function calls issued by the model.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public ChatMessage(ICollection<FunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None) : base(functionCalls, pinLocation) { }

    /// <summary>
    /// Initializes a message containing a function result.
    /// </summary>
    /// <param name="functionResult">Function result to include.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public ChatMessage(FunctionResult functionResult, PinLocation pinLocation = PinLocation.None) : base(functionResult, pinLocation) { }
}
