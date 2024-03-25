using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.AI;

internal static class OpenAIExtensions
{
    internal static IServiceCollection AddOpenAI(this IServiceCollection services, IConfiguration configuration)
    {
        var openAISettings = OpenAISettings.FromConfiguration(configuration);
        services.AddScoped(_ => openAISettings);

        services.AddScoped(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();
            return new OpenAIClient(new Uri(openAISettings.EndpointUrl), credentials);
        });

        return services;
    }
}
