using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.AzureDocumentIntelligence;

/// <summary>
/// Defines a set of extension methods for configuring Azure AI Document Intelligence services.
/// </summary>
public static class DocumentIntelligenceExtensions
{
    /// <summary>
    /// Configures the Azure AI Document Intelligence services for the application.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the Azure AI Document Intelligence services to.</param>
    /// <param name="configuration">The application configuration to retrieve Azure AI Document Intelligence settings from.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDocumentIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = DocumentIntelligenceSettings.FromConfiguration(configuration);
        services.AddScoped(_ => settings);

        services.AddSingleton(sp =>
        {
            // The role of either the user or spn needs to have 'Cognitive Services User' role to access the Document Intelligence service.
            // Where key-based authorization is preferred, the following code can be used:

            //var key = configuration["DOCUMENT_INTELLIGENCE_KEY"];
            //AzureKeyCredential credentials = new AzureKeyCredential(key);

            var credentials = sp.GetRequiredService<DefaultAzureCredential>();

            return new DocumentIntelligenceClient(new Uri(settings.DocumentIntelligenceEndpoint), credentials);
        });

        return services;
    }
}
