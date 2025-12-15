using ChatAIze.Abstractions.Chat;

namespace ChatAIze.GenerativeCS.Models;

/// <summary>
/// Represents a chat conversation with typed messages, function calls, and function results.
/// </summary>
/// <typeparam name="TMessage">Message type used in the chat.</typeparam>
/// <typeparam name="TFunctionCall">Function call type used in the chat.</typeparam>
/// <typeparam name="TFunctionResult">Function result type used in the chat.</typeparam>
public record Chat<TMessage, TFunctionCall, TFunctionResult> : IChat<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    /// <summary>
    /// Initializes a new chat with no messages.
    /// </summary>
    public Chat() { }

    /// <summary>
    /// Initializes a chat with a single system message.
    /// </summary>
    /// <param name="systemMessage">System directive to seed the chat with.</param>
    public Chat(string systemMessage)
    {
        FromSystem(systemMessage);
    }

    /// <summary>
    /// Initializes a chat with a predefined collection of messages.
    /// </summary>
    /// <param name="messages">Messages to seed the chat with.</param>
    public Chat(IEnumerable<TMessage> messages)
    {
        Messages = messages.ToList();
    }

    /// <summary>
    /// Gets or sets an optional stable end-user identifier passed to providers for safety, abuse, or rate limiting.
    /// </summary>
    public string? UserTrackingId { get; set; }

    /// <summary>
    /// Gets or sets the collection of messages in the chat.
    /// </summary>
    public ICollection<TMessage> Messages { get; set; } = [];

    /// <summary>
    /// Gets or sets the UTC creation time of the chat.
    /// </summary>
    public DateTimeOffset CreationTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Adds a system message to the chat.
    /// </summary>
    /// <param name="message">Content of the system message.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>The created message.</returns>
    public ValueTask<TMessage> FromSystemAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.System,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    /// <summary>
    /// Adds a system message to the chat (fire-and-forget wrapper).
    /// </summary>
    /// <param name="message">Content of the system message.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public async void FromSystem(string message, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromSystemAsync(message, pinLocation);
    }

    /// <summary>
    /// Adds a user message with optional image URLs.
    /// </summary>
    /// <param name="message">User text.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <param name="imageUrls">Optional set of image URLs attached to the message.</param>
    /// <returns>The created message.</returns>
    public ValueTask<TMessage> FromUserAsync(string message, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.User,
            Content = message,
            PinLocation = pinLocation,
            ImageUrls = imageUrls
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    /// <summary>
    /// Adds a user message with optional image URLs (fire-and-forget wrapper).
    /// </summary>
    /// <param name="message">User text.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <param name="imageUrls">Optional set of image URLs attached to the message.</param>
    public async void FromUser(string message, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        _ = await FromUserAsync(message, pinLocation, imageUrls);
    }

    /// <summary>
    /// Adds a user message with username context and optional images.
    /// </summary>
    /// <param name="userName">Display name of the user the message originates from (not a stable identifier).</param>
    /// <param name="message">User text.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <param name="imageUrls">Optional set of image URLs attached to the message.</param>
    /// <returns>The created message.</returns>
    public ValueTask<TMessage> FromUserAsync(string userName, string message, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.User,
            UserName = userName,
            Content = message,
            PinLocation = pinLocation,
            ImageUrls = imageUrls
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    /// <summary>
    /// Adds a user message with username context (fire-and-forget wrapper).
    /// </summary>
    /// <param name="userName">Display name of the user the message originates from (not a stable identifier).</param>
    /// <param name="message">User text.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <param name="imageUrls">Optional set of image URLs attached to the message.</param>
    public async void FromUser(string userName, string message, PinLocation pinLocation = PinLocation.None, params ICollection<string> imageUrls)
    {
        _ = await FromUserAsync(userName, message, pinLocation, imageUrls);
    }

    /// <summary>
    /// Adds a chatbot text response to the chat.
    /// </summary>
    /// <param name="message">Response content.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>The created message.</returns>
    public ValueTask<TMessage> FromChatbotAsync(string message, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.Chatbot,
            Content = message,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    /// <summary>
    /// Adds a chatbot text response to the chat (fire-and-forget wrapper).
    /// </summary>
    /// <param name="message">Response content.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public async void FromChatbot(string message, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromChatbotAsync(message, pinLocation);
    }

    /// <summary>
    /// Adds a chatbot response containing a single function call.
    /// </summary>
    /// <param name="functionCall">Function call issued by the model.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>The created message.</returns>
    public ValueTask<TMessage> FromChatbotAsync(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.Chatbot,
            FunctionCalls = [functionCall],
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    /// <summary>
    /// Adds a chatbot response containing a single function call (fire-and-forget wrapper).
    /// </summary>
    /// <param name="functionCall">Function call issued by the model.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public async void FromChatbot(TFunctionCall functionCall, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromChatbotAsync(functionCall, pinLocation);
    }

    /// <summary>
    /// Adds a chatbot response that includes multiple function calls.
    /// </summary>
    /// <param name="functionCalls">Function calls issued by the model.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>The created message.</returns>
    public ValueTask<TMessage> FromChatbotAsync(IEnumerable<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.Chatbot,
            FunctionCalls = functionCalls.ToList(),
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    /// <summary>
    /// Adds a chatbot response that includes multiple function calls (fire-and-forget wrapper).
    /// </summary>
    /// <param name="functionCalls">Function calls issued by the model.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public async void FromChatbot(IEnumerable<TFunctionCall> functionCalls, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromChatbotAsync(functionCalls, pinLocation);
    }

    /// <summary>
    /// Adds a function result message to the chat.
    /// </summary>
    /// <param name="functionResult">Result produced by executing a function.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    /// <returns>The created message.</returns>
    public ValueTask<TMessage> FromFunctionAsync(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        var chatMessage = new TMessage
        {
            Role = ChatRole.Function,
            FunctionResult = functionResult,
            PinLocation = pinLocation
        };

        Messages.Add(chatMessage);
        return ValueTask.FromResult(chatMessage);
    }

    /// <summary>
    /// Adds a function result message to the chat (fire-and-forget wrapper).
    /// </summary>
    /// <param name="functionResult">Result produced by executing a function.</param>
    /// <param name="pinLocation">Optional pin location for message ordering.</param>
    public async void FromFunction(TFunctionResult functionResult, PinLocation pinLocation = PinLocation.None)
    {
        _ = await FromFunctionAsync(functionResult, pinLocation);
    }
}

/// <summary>
/// Non-generic chat model using the built-in chat message, function call, and function result types.
/// </summary>
public record Chat : Chat<ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes a new chat with no messages.
    /// </summary>
    public Chat() : base() { }

    /// <summary>
    /// Initializes a new chat with a system message.
    /// </summary>
    /// <param name="systemMessage">System directive to seed the chat with.</param>
    public Chat(string systemMessage) : base(systemMessage) { }

    /// <summary>
    /// Initializes a new chat with predefined messages.
    /// </summary>
    /// <param name="messages">Messages to seed the chat with.</param>
    public Chat(IEnumerable<ChatMessage> messages) : base(messages) { }
}
