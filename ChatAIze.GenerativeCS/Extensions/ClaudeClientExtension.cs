using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.Claude;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAIze.GenerativeCS.Extensions;

/// <summary>
/// Dependency injection extensions for registering <see cref="Clients.ClaudeClient{TChat, TMessage, TFunctionCall, TFunctionResult}"/> instances.
/// </summary>
public static class ClaudeClientExtension
{
    /// <summary>
    /// Registers a typed Claude client using supplied options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="options">Optional configuration callback.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddClaudeClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, Action<ClaudeClientOptions<TMessage, TFunctionCall, TFunctionResult>>? options = null)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        if (options is not null)
        {
            _ = services.Configure(options);

            if (options is Action<ClaudeClientOptions> options2)
            {
                _ = services.Configure(options2);
            }
        }

        _ = services.AddHttpClient<ClaudeClient<TChat, TMessage, TFunctionCall, TFunctionResult>>(c => c.Timeout = TimeSpan.FromMinutes(15));
        _ = services.AddHttpClient<ClaudeClient>(c => c.Timeout = TimeSpan.FromMinutes(15));
        _ = services.AddSingleton<ClaudeClient<TChat, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddSingleton<ClaudeClient>();

        return services;
    }

    /// <summary>
    /// Registers the default Claude client types using supplied options.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="options">Optional configuration callback.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddClaudeClient(this IServiceCollection services, Action<ClaudeClientOptions<ChatMessage, FunctionCall, FunctionResult>>? options = null)
    {
        return services.AddClaudeClient<Chat, ChatMessage, FunctionCall, FunctionResult>(options);
    }

    /// <summary>
    /// Registers a typed Claude client with an explicit API key and optional default completion options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">Claude API key.</param>
    /// <param name="defaultCompletionOptions">Optional default completion options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddClaudeClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? defaultCompletionOptions = null)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddClaudeClient<TChat, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultCompletionOptions is not null)
            {
                o.DefaultCompletionOptions = defaultCompletionOptions;
            }
        });
    }

    /// <summary>
    /// Registers the default Claude client types with an explicit API key.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">Claude API key.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddClaudeClient(this IServiceCollection services, string apiKey)
    {
        return services.AddClaudeClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey);
    }
}
