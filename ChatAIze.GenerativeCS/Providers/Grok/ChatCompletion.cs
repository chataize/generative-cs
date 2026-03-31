using System.Runtime.CompilerServices;
using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Options.Grok;
using ChatAIze.GenerativeCS.Utilities;

namespace ChatAIze.GenerativeCS.Providers.Grok;

/// <summary>
/// Handles xAI Grok chat completion requests using the provider's OpenAI-compatible endpoint.
/// </summary>
internal static class ChatCompletion
{
    private const string GrokChatCompletionsEndpoint = "https://api.x.ai/v1/chat/completions";

    /// <summary>
    /// Executes a Grok chat completion request and returns the full response text.
    /// </summary>
    internal static async Task<string> CompleteAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat, string? apiKey,
        ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null, HttpClient? httpClient = null,
        int recursion = 0, CancellationToken cancellationToken = default)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return await OpenAI.ChatCompletion.CompleteAsync(chat, apiKey, options, usageTracker, httpClient, recursion, cancellationToken, GrokChatCompletionsEndpoint, includeProviderExtensions: false);
    }

    /// <summary>
    /// Streams a Grok chat completion response, yielding chunks as they arrive.
    /// </summary>
    internal static async IAsyncEnumerable<string> StreamCompletionAsync<TChat, TMessage, TFunctionCall, TFunctionResult>(
        TChat chat, string? apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? options = null,
        TokenUsageTracker? usageTracker = null, HttpClient? httpClient = null, int recursion = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        await foreach (var chunk in OpenAI.ChatCompletion.StreamCompletionAsync(chat, apiKey, options, usageTracker, httpClient, recursion, cancellationToken, GrokChatCompletionsEndpoint, includeProviderExtensions: false))
        {
            yield return chunk;
        }
    }
}
