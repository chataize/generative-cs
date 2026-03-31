using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.Grok;

/// <summary>
/// Configures Grok chat completion requests.
/// </summary>
/// <typeparam name="TMessage">Message type used in the chat.</typeparam>
/// <typeparam name="TFunctionCall">Function call type used in the chat.</typeparam>
/// <typeparam name="TFunctionResult">Function result type used in the chat.</typeparam>
public record ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>
    : OpenAI.ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>
    where TMessage : IChatMessage<TFunctionCall, TFunctionResult>
    where TFunctionCall : IFunctionCall
    where TFunctionResult : IFunctionResult
{
    /// <summary>
    /// Initializes a new set of Grok completion options.
    /// </summary>
    /// <param name="model">Model identifier to target.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public ChatCompletionOptions(string model = DefaultModels.Grok.ChatCompletion, string? apiKey = null)
        : base(model, apiKey)
    {
    }
}

/// <summary>
/// Non-generic Grok chat completion options using the built-in chat models.
/// </summary>
public record ChatCompletionOptions : ChatCompletionOptions<ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes a new set of Grok completion options.
    /// </summary>
    /// <param name="model">Model identifier to target.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public ChatCompletionOptions(string model = DefaultModels.Grok.ChatCompletion, string? apiKey = null)
        : base(model, apiKey)
    {
    }
}
