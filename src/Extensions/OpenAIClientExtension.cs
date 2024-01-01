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

        services.AddSingleton<OpenAIClient>();
        return services;
    }
}
