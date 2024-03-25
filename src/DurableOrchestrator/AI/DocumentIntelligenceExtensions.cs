// using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DurableOrchestrator.AI;

internal static class DocumentIntelligenceExtensions
{
    internal static IServiceCollection AddDocumentIntelligence(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var credentials = sp.GetRequiredService<DefaultAzureCredential>();

            var endpoint = Environment.GetEnvironmentVariable("DOCUMENT_INTELLIGENCE_ENDPOINT");
            // The role of either the user or spn needs to have 'Cognitive User' role to access the Document Intelligence service

            // in case keys authorization is preferred, the following code can be used
            // var key = Environment.GetEnvironmentVariable("DI_KEY");
            // AzureKeyCredential credentials = new AzureKeyCredential(key);

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException("Document Intelligence endpoint is not configured.");
            }

            return new DocumentIntelligenceClient(new Uri(endpoint), credentials);
        });

        return services;
    }
}
