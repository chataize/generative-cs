using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Gemini;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAIze.GenerativeCS.Extensions;

/// <summary>
/// Dependency injection extensions for registering <see cref="Clients.GeminiClient{TChat, TMessage, TFunctionCall, TFunctionResult}"/> instances.
/// </summary>
public static class GeminiClientExtension
{
    /// <summary>
    /// Registers a typed Gemini client using supplied options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="options">Optional configuration callback.</param>
    /// <returns>The same service collection.</returns>
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

        _ = services.AddHttpClient<GeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>>(c => c.Timeout = TimeSpan.FromMinutes(15));
        _ = services.AddHttpClient<GeminiClient>(c => c.Timeout = TimeSpan.FromMinutes(15));
        _ = services.AddSingleton<GeminiClient<TChat, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddSingleton<GeminiClient>();

        return services;
    }

    /// <summary>
    /// Registers the default Gemini client types using supplied options.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="options">Optional configuration callback.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddGeminiClient(this IServiceCollection services, Action<GeminiClientOptions<ChatMessage, FunctionCall, FunctionResult>>? options = null)
    {
        return services.AddGeminiClient<Chat, ChatMessage, FunctionCall, FunctionResult>(options);
    }

    /// <summary>
    /// Registers a typed Gemini client with an explicit API key and optional default completion options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">Gemini API key.</param>
    /// <param name="defaultCompletionOptions">Optional default completion options.</param>
    /// <returns>The same service collection.</returns>
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

    /// <summary>
    /// Registers the default Gemini client types with an explicit API key.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">Gemini API key.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddGeminiClient(this IServiceCollection services, string apiKey)
    {
        return services.AddGeminiClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey);
    }
}
