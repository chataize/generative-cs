using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Constants;
using ChatAIze.GenerativeCS.Models;

namespace ChatAIze.GenerativeCS.Options.Gemini;

/// <summary>
/// Configures Gemini chat completion requests.
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
    /// Initializes a new set of Gemini completion options.
    /// </summary>
    /// <param name="model">Model identifier to target.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public ChatCompletionOptions(string model = DefaultModels.Gemini.ChatCompletion, string? apiKey = null)
        : base(model, apiKey)
    {
    }

    /// <summary>
    /// Gets or sets the top-k sampling cutoff used by Gemini models that support it.
    /// </summary>
    public int? TopK { get; set; }

    /// <summary>
    /// Gets or sets an optional Gemini thinking level override.
    /// </summary>
    /// <remarks>
    /// Supported values vary by model. When omitted, the provider maps <see cref="OpenAI.ChatCompletionOptions{TMessage, TFunctionCall, TFunctionResult}.ReasoningEffort"/>
    /// to an appropriate Gemini thinking level when possible.
    /// </remarks>
    public string? ThinkingLevel { get; set; }

    /// <summary>
    /// Gets or sets an optional Gemini thinking budget override.
    /// </summary>
    /// <remarks>Only use this when you intentionally need provider-specific control over hidden reasoning tokens.</remarks>
    public int? ThinkingBudget { get; set; }
}

/// <summary>
/// Non-generic Gemini chat completion options using the built-in chat models.
/// </summary>
public record ChatCompletionOptions : ChatCompletionOptions<ChatMessage, FunctionCall, FunctionResult>
{
    /// <summary>
    /// Initializes a new set of Gemini completion options.
    /// </summary>
    /// <param name="model">Model identifier to target.</param>
    /// <param name="apiKey">Optional API key overriding the client default.</param>
    public ChatCompletionOptions(string model = DefaultModels.Gemini.ChatCompletion, string? apiKey = null)
        : base(model, apiKey)
    {
    }
}
