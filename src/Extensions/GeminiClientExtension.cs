using ChatAIze.GenerativeCS.Clients;
using ChatAIze.GenerativeCS.Options.Gemini;
using Microsoft.Extensions.DependencyInjection;

namespace ChatAIze.GenerativeCS.Extensions;

public static class GeminiClientExtension
{
    public static IServiceCollection AddGeminiClient(this IServiceCollection services, Action<GeminiClientOptions>? options = null)
    {
        if (options != null)
        {
            _ = services.Configure(options);
        }

        _ = services.AddHttpClient<GeminiClient>();
        _ = services.AddSingleton<GeminiClient>();

        return services;
    }

    public static IServiceCollection AddGeminiClient(this IServiceCollection services, string apiKey, ChatCompletionOptions? defaultCompletionOptions = null)
    {
        return services.AddGeminiClient(o =>
        {
            o.ApiKey = apiKey;

            if (defaultCompletionOptions != null)
            {
                o.DefaultCompletionOptions = defaultCompletionOptions;
            }
        });
    }
}
