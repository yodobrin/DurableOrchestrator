using Azure.AI.TextAnalytics;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.AI;

internal static class TextAnalyticsExtensions
{
    internal static IServiceCollection AddTextAnalytics(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();

            var endpoint = Environment.GetEnvironmentVariable("TEXT_ANALYTICS_ENDPOINT");

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException("Text Analytics endpoint is not configured.");
            }

            return new TextAnalyticsClient(new Uri(endpoint), credentials);
        });

        return services;
    }
}
