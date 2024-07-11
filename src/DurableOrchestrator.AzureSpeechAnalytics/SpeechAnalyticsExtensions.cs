using Azure.Core;
using Microsoft.CognitiveServices.Speech;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.AzureSpeechAnalytics;

/// <summary>
/// Defines a set of extension methods for configuring Azure AI Text Analytics services.
/// </summary>
public static class SpeechAnalyticsExtensions
{
    /// <summary>
    /// Configures the Azure AI Voice/Speech Analytics services for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Azure AI Text Analytics services to.</param>
    /// <param name="configuration">The application configuration to retrieve Azure AI Text Analytics settings from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddVoiceAnalytics(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = SpeechAnalyticsSettings.FromConfiguration(configuration);
    
        services.AddSingleton(settings);

        services.AddSingleton(sp =>
        {
            var credential = new DefaultAzureCredential();
            var tokenRequestContext = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" });
            var accessToken = credential.GetToken(tokenRequestContext);
            var resourceId = settings.SpeechResourceID;
            var authorizationToken = $"aad#{resourceId}#{accessToken.Token}";
            return SpeechConfig.FromAuthorizationToken(authorizationToken,settings.SpeechRegion);
        });

        return services;
    }
}
