using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Gemini;
using ChatAIze.GenerativeCS.Providers.Gemini;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAIze.GenerativeCS.Extensions;

public static class GeminiClientExtension
{
    public static IServiceCollection AddGeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, Action<GeminiClientOptions<TMessage, TFunctionCall, TFunctionResult>>? options = null)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        if (options is not null)
        {
            _ = services.Configure(options);

            if (options is Action<GeminiClientOptions> options2)
            {
                _ = services.Configure(options2);
            }
        }

        _ = services.AddHttpClient<GeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddHttpClient<GeminiClient>();
        _ = services.AddSingleton<GeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddSingleton<GeminiClient>();

        // Register IFileService to be resolved from the GeminiClient's Files property
        _ = services.AddSingleton<IFileService>(sp => 
        {
            var client = sp.GetRequiredService<GeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>>();
            return client.Files; 
        });

        return services;
    }

    public static IServiceCollection AddGeminiClient(this IServiceCollection services, Action<GeminiClientOptions<ChatMessage, FunctionCall, FunctionResult>>? options = null)
    {
        return services.AddGeminiClient<Chat, ChatMessage, FunctionCall, FunctionResult>(options);
    }

    public static IServiceCollection AddGeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? defaultCompletionOptions = null)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddGeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultCompletionOptions is not null)
            {
                o.DefaultCompletionOptions = defaultCompletionOptions;
            }
        });
    }

    public static IServiceCollection AddGeminiClient(this IServiceCollection services, string apiKey)
    {
        return services.AddGeminiClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey);
    }
}
