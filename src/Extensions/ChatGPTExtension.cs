using GenerativeCS.Options;
using GenerativeCS.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace GenerativeCS.Extensions;

public static class ChatGPTExtension
{
    public static IServiceCollection AddChatGPT(this IServiceCollection services, Action<ChatGPTOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<ChatGPT>();

        return services;
    }
}
