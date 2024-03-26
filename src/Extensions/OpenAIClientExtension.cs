using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Interfaces;
using ChatAIze.GenerativeCS.Models;
using ChatAIze.GenerativeCS.Options.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAIze.GenerativeCS.Extensions;

public static class OpenAIExtension
{
    public static IServiceCollection AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, Action<OpenAIClientOptions<TMessage, TFunctionCall, TFunctionResult>>? options = null)
        where TConversation : IChatConversation<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        if (options != null)
        {
            _ = services.Configure(options);

            if (options is Action<OpenAIClientOptions> options2)
            {
                _ = services.Configure(options2);
            }
        }

        _ = services.AddHttpClient<OpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddHttpClient<OpenAIClient>();
        _ = services.AddSingleton<OpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>>();
        _ = services.AddSingleton<OpenAIClient>();

        return services;
    }

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, Action<OpenAIClientOptions<ChatMessage, FunctionCall, FunctionResult>>? options = null)
    {
        return services.AddOpenAIClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>(options);
    }

    public static IServiceCollection AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey)
        where TConversation : IChatConversation<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;
        });
    }

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey)
    {
        return services.AddOpenAIClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>(apiKey);
    }

    public static IServiceCollection AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, ChatCompletionOptions<TMessage, TFunctionCall, TFunctionResult>? defaultCompletionOptions)
        where TConversation : IChatConversation<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultCompletionOptions != null)
            {
                o.DefaultCompletionOptions = defaultCompletionOptions;
            }
        });
    }

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, ChatCompletionOptions? defaultCompletionOptions)
    {
        return services.AddOpenAIClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultCompletionOptions);
    }

    public static IServiceCollection AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, EmbeddingOptions? defaultEmbeddingOptions)
        where TConversation : IChatConversation<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultEmbeddingOptions != null)
            {
                o.DefaultEmbeddingOptions = defaultEmbeddingOptions;
            }
        });
    }

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, EmbeddingOptions? defaultEmbeddingOptions)
    {
        return services.AddOpenAIClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultEmbeddingOptions);
    }

    public static IServiceCollection AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, TextToSpeechOptions? defaultTextToSpeechOptions)
        where TConversation : IChatConversation<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultTextToSpeechOptions != null)
            {
                o.DefaultTextToSpeechOptions = defaultTextToSpeechOptions;
            }
        });
    }

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, TextToSpeechOptions? defaultTextToSpeechOptions)
    {
        return services.AddOpenAIClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultTextToSpeechOptions);
    }

    public static IServiceCollection AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, TranscriptionOptions? defaultTranscriptionOptions)
        where TConversation : IChatConversation<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultTranscriptionOptions != null)
            {
                o.DefaultTranscriptionOptions = defaultTranscriptionOptions;
            }
        });
    }

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, TranscriptionOptions? defaultTranscriptionOptions)
    {
        return services.AddOpenAIClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultTranscriptionOptions);
    }

    public static IServiceCollection AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(this IServiceCollection services, string apiKey, TranslationOptions? defaultTranslationOptions)
        where TConversation : IChatConversation<TMessage, TFunctionCall, TFunctionResult>, new()
        where TMessage : IChatMessage<TFunctionCall, TFunctionResult>, new()
        where TFunctionCall : IFunctionCall, new()
        where TFunctionResult : IFunctionResult, new()
    {
        return services.AddOpenAIClient<TConversation, TMessage, TFunctionCall, TFunctionResult>(o =>
        {
            o.ApiKey = apiKey;

            if (defaultTranslationOptions != null)
            {
                o.DefaultTranslationOptions = defaultTranslationOptions;
            }
        });
    }

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, TranslationOptions? defaultTranslationOptions)
    {
        return services.AddOpenAIClient<ChatConversation, ChatMessage, FunctionCall, FunctionResult>(apiKey, defaultTranslationOptions);
    }
}
