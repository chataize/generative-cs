using GenerativeCS.Clients;
using GenerativeCS.Options.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace GenerativeCS.Extensions;

public static class ChatGPTExtension
{
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, Action<OpenAIClientOptions>? options = null)
    {
        if (options != null)
        {
            services.Configure(options);
        }

        services.AddHttpClient<OpenAIClient>();
        services.AddSingleton<OpenAIClient>();

        return services;
    }

    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, string apiKey, ChatCompletionOptions? defaultCompletionOptions = null)
    {
        return services.AddOpenAIClient(o =>
        {
            o.ApiKey = apiKey;

            if (defaultCompletionOptions != null)
            {
                o.DefaultCompletionOptions = defaultCompletionOptions;
            }
        });
    }
}
