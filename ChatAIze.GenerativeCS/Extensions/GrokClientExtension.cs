using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Grok;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAIze.GenerativeCS.Extensions;

/// <summary>
/// Dependency injection extensions for registering <see cref="Clients.GrokClient{TChat, TMessage, TFunctionCall, TFunctionResult}"/> instances.
/// </summary>
public static class GrokClientExtension
{
    /// <summary>
    /// Registers a typed Grok client using supplied options.
    /// </summary>
    public static IServiceCollection AddGrokClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, Action<GrokClientOptions<TMessage, TFunctionCall, TFunctionResult>>? options = null)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        if (options is not null)
        {
            _ = services.Configure(options);

            if (options is Action<GrokClientOptions> options2)
            {
                _ = services.Configure(options2);
            }
        }

        _ = services.AddHttpClient<GrokClient<TChat, TMessage, TFunctionCall, TFunctionResult>>(c => c.Timeout = TimeSpan.FromMinutes(15));
        _ = services.AddHttpClient<GrokClient>(c => c.Timeout = TimeSpan.FromMinutes(15));
        _ = services.AddSingleton<GrokClient<TChat, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddSingleton<GrokClient>();

        return services;
    }

    /// <summary>
    /// Registers the default Grok client types using supplied options.
    /// </summary>
    public static IServiceCollection AddGrokClient(this IServiceCollection services, Action<GrokClientOptions<ChatMessage, FunctionCall, FunctionResult>>? options = null)
    {
        return services.AddGrokClient<Chat, ChatMessage, FunctionCall, FunctionResult>(options);
    }

    /// <summary>
    /// Registers a typed Grok client with an explicit API key and optional default completion options.
    /// </summary>
    public static IServiceCollection AddGrokClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? defaultCompletionOptions = null)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddGrokClient<TChat, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultCompletionOptions is not null)
            {
                o.DefaultCompletionOptions = defaultCompletionOptions;
            }
        });
    }

    /// <summary>
    /// Registers the default Grok client types with an explicit API key.
    /// </summary>
    public static IServiceCollection AddGrokClient(this IServiceCollection services, string apiKey)
    {
        return services.AddGrokClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey);
    }
}
