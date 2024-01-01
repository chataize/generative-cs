using GenerativeCS.Options;
using GenerativeCS.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace GenerativeCS.Extensions;

public static class ChatGPTExtension
{
    public static IServiceCollection AddOpenAIClient(this IServiceCollection services, Action<ChatGPTOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<OpenAIClient>();

        return services;
    }
}
