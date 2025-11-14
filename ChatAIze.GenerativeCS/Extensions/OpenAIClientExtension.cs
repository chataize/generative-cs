using ChatAIze.Abstractions.Chat;
using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAIze.GenerativeCS.Extensions;

public static class OpenAIExtension
{
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

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, Action<OpenAIClientOptions<ChatMessage, FunctionCall, FunctionResult>>? options = null)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(options);
    }

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

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey);
    }

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

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, ChatCompletionOptions? defaultCompletionOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultCompletionOptions);
    }

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

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, EmbeddingOptions? defaultEmbeddingOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultEmbeddingOptions);
    }

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

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, TextToSpeechOptions? defaultTextToSpeechOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultTextToSpeechOptions);
    }

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

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, TranscriptionOptions? defaultTranscriptionOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultTranscriptionOptions);
    }

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

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, TranslationOptions? defaultTranslationOptions)
    {
        return services.AddOpenAIClient<Chat, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultTranslationOptions);
    }
}
