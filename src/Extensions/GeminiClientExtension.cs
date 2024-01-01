using GenerativeCS.Clients;
using GenerativeCS.Options.Gemini;
using Microsoft.Extensions.DependencyInjection;

namespace GenerativeCS.Extensions;

public static class GeminiClientExtension
{
    public static IServiceCollection AddGeminiClient(this IServiceCollection services, Action<GeminiClientOptions>? options = null)
    {
        if (options != null)
        {
            services.Configure(options);
        }

        services.AddSingleton<GeminiClient>();
        return services;
    }
}
