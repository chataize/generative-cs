using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAIze.GenerativeCS.Extensions;

/// <summary>
/// Dependency injection extensions for registering <see cref="Clients.OpenAIClient{TChat, TMessage, TFunctionCall, TFunctionResult}"/> instances.
/// </summary>
public static class OpenAIExtension
{
    /// <summary>
    /// Registers a typed OpenAI client using supplied options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="options">Optional configuration callback.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, Action<OpenAIClientOptions<TMessage, TFunctionCall, TFunctionResult>>? options = null)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        if (options is not null)
        {
            _ = services.Configure(options);

            if (options is Action<OpenAIClientOptions> options2)
            {
                _ = services.Configure(options2);
            }
        }

        _ = services.AddHttpClient<OpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>>(c => c.Timeout = TimeSpan.FromMinutes(15));
        _ = services.AddHttpClient<OpenAIClient>(c => c.Timeout = TimeSpan.FromMinutes(15));
        _ = services.AddSingleton<OpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddSingleton<OpenAIClient>();

        return services;
    }

    /// <summary>
    /// Registers the default OpenAI client types using supplied options.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="options">Optional configuration callback.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, Action<OpenAIClientOptions<ChatMessage, FunctionCall, FunctionResult>>? options = null)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(options);
    }

    /// <summary>
    /// Registers a typed OpenAI client with an explicit API key.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;
        });
    }

    /// <summary>
    /// Registers the default OpenAI client types with an explicit API key.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey);
    }

    /// <summary>
    /// Registers a typed OpenAI client with default completion options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultCompletionOptions">Default completion options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? defaultCompletionOptions)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultCompletionOptions is not null)
            {
                o.DefaultCompletionOptions = defaultCompletionOptions;
            }
        });
    }

    /// <summary>
    /// Registers the default OpenAI client types with default completion options.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultCompletionOptions">Default completion options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, ChatCompletionOptions? defaultCompletionOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultCompletionOptions);
    }

    /// <summary>
    /// Registers a typed OpenAI client with default embedding options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultEmbeddingOptions">Default embedding options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, EmbeddingOptions? defaultEmbeddingOptions)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultEmbeddingOptions is not null)
            {
                o.DefaultEmbeddingOptions = defaultEmbeddingOptions;
            }
        });
    }

    /// <summary>
    /// Registers the default OpenAI client types with default embedding options.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultEmbeddingOptions">Default embedding options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, EmbeddingOptions? defaultEmbeddingOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultEmbeddingOptions);
    }

    /// <summary>
    /// Registers a typed OpenAI client with default text-to-speech options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultTextToSpeechOptions">Default text-to-speech options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, TextToSpeechOptions? defaultTextToSpeechOptions)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultTextToSpeechOptions is not null)
            {
                o.DefaultTextToSpeechOptions = defaultTextToSpeechOptions;
            }
        });
    }

    /// <summary>
    /// Registers the default OpenAI client types with default text-to-speech options.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultTextToSpeechOptions">Default text-to-speech options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, TextToSpeechOptions? defaultTextToSpeechOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultTextToSpeechOptions);
    }

    /// <summary>
    /// Registers a typed OpenAI client with default transcription options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultTranscriptionOptions">Default transcription options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, TranscriptionOptions? defaultTranscriptionOptions)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultTranscriptionOptions is not null)
            {
                o.DefaultTranscriptionOptions = defaultTranscriptionOptions;
            }
        });
    }

    /// <summary>
    /// Registers the default OpenAI client types with default transcription options.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultTranscriptionOptions">Default transcription options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, TranscriptionOptions? defaultTranscriptionOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultTranscriptionOptions);
    }

    /// <summary>
    /// Registers a typed OpenAI client with default translation options.
    /// </summary>
    /// <typeparam name="TChat">Chat container type.</typeparam>
    /// <typeparam name="TMessage">Message type.</typeparam>
    /// <typeparam name="TFunctionCall">Function call type.</typeparam>
    /// <typeparam name="TFunctionResult">Function result type.</typeparam>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultTranslationOptions">Default translation options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, TranslationOptions? defaultTranslationOptions)
        where TChat : IChat<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TChat, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultTranslationOptions is not null)
            {
                o.DefaultTranslationOptions = defaultTranslationOptions;
            }
        });
    }

    /// <summary>
    /// Registers the default OpenAI client types with default translation options.
    /// </summary>
    /// <param name="services">Service collection to configure.</param>
    /// <param name="apiKey">API key used for requests.</param>
    /// <param name="defaultTranslationOptions">Default translation options.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, TranslationOptions? defaultTranslationOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultTranslationOptions);
    }
}
