using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Gemini;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAIze.GenerativeCS.Extensions;

public static class GeminiClientExtension
{
    public static IServiceCollection AddGeminiClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, Action<GeminiClientOptions<TMessage, TFunctionCall, TFunctionResult>>? options = null)
        where TConversation : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        if (options != null)
        {
            _ = services.Configure(options);

            if (options is Action<GeminiClientOptions> options2)
            {
                _ = services.Configure(options2);
            }
        }

        _ = services.AddHttpClient<GeminiClient<TConversation, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddHttpClient<GeminiClient>();
        _ = services.AddSingleton<GeminiClient<TConversation, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddSingleton<GeminiClient>();

        return services;
    }

    public static IServiceCollection AddGeminiClient(this IServiceCollection services, Action<GeminiClientOptions<ChatMessage, FunctionCall, FunctionResult>>? options = null)
    {
        return services.AddGeminiClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>(options);
    }

    public static IServiceCollection AddGeminiClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? defaultCompletionOptions = null)
        where TConversation : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddGeminiClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultCompletionOptions != null)
            {
                o.DefaultCompletionOptions = defaultCompletionOptions;
            }
        });
    }

    public static IServiceCollection AddGeminiClient(this IServiceCollection services, string apiKey)
    {
        return services.AddGeminiClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>(apiKey);
    }
}
