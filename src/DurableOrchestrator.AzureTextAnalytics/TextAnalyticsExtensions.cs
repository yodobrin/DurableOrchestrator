using Azure.AI.TextAnalytics;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.AzureTextAnalytics;

/// <summary>
/// Defines a set of extension methods for configuring Azure AI Text Analytics services.
/// </summary>
public static class TextAnalyticsExtensions
{
    /// <summary>
    /// Configures the Azure AI Text Analytics services for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Azure AI Text Analytics services to.</param>
    /// <param name="configuration">The application configuration to retrieve Azure AI Text Analytics settings from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddTextAnalytics(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = TextAnalyticsSettings.FromConfiguration(configuration);
        services.AddScoped(_ => settings);

        services.AddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();

            return new TextAnalyticsClient(new Uri(settings.TextAnalyticsEndpoint), credentials);
        });

        return services;
    }
}
