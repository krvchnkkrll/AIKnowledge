using System.ClientModel;
using Assistant.Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Assistant;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddAssistant(this IHostApplicationBuilder builder)
    {
        builder.Services.AddAssistantServices();
        return builder;
    }
    
    private static void AddAssistantServices(this IServiceCollection services)
    {
        services.AddSingleton<OpenAIClient>(sp =>
        {
            var assistantOptions = sp.GetRequiredService<IOptions<AssistantOptions>>().Value;

            return new OpenAIClient(
                new ApiKeyCredential("RandomKey"),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(assistantOptions.Url + "/v1"),
                }
            );
        });

        services.AddSingleton<IChatClient>(sp =>
        {
            var openAiClient = sp.GetRequiredService<OpenAIClient>();
            var assistantOptions = sp.GetRequiredService<IOptions<AssistantOptions>>().Value;
            return openAiClient.GetChatClient(assistantOptions.Model).AsIChatClient();
        });
    }
}