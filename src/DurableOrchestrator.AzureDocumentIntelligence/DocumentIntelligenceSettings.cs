using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.AzureDocumentIntelligence;

/// <summary>
/// Defines the settings for configuring Azure AI Document Intelligence.
/// </summary>
public class DocumentIntelligenceSettings(string documentIntelligenceEndpoint)
{
    /// <summary>
    /// The configuration key for the Azure AI Document Intelligence endpoint.
    /// </summary>
    public const string DocumentIntelligenceEndpointConfigKey = "DOCUMENT_INTELLIGENCE_ENDPOINT";

    /// <summary>
    /// Gets the URL of the Azure AI Document Intelligence endpoint.
    /// </summary>
    public string DocumentIntelligenceEndpoint { get; init; } = documentIntelligenceEndpoint;

    /// <summary>
    /// Creates a new instance of the <see cref="DocumentIntelligenceSettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="DocumentIntelligenceSettings"/> class.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the required configuration is not present.</exception>
    public static DocumentIntelligenceSettings FromConfiguration(IConfiguration configuration)
    {
        var documentIntelligenceEndpoint = configuration.GetValue<string>(DocumentIntelligenceEndpointConfigKey) ??
                                           throw new InvalidOperationException(
                                               $"{DocumentIntelligenceEndpointConfigKey} is not configured.");

        return new DocumentIntelligenceSettings(documentIntelligenceEndpoint);
    }
}
