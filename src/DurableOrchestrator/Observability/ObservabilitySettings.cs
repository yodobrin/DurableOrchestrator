using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.Observability;

/// <summary>
/// Defines the settings for configuring application observability.
/// </summary>
public class ObservabilitySettings(string? applicationInsightsConnectionString, string? zipkinEndpointUrl)
{
    /// <summary>
    /// The configuration key for the Application Insights connection string.
    /// </summary>
    public const string ApplicationInsightsConnectionStringConfigKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// The configuration key for the Zipkin endpoint URL.
    /// </summary>
    public const string ZipkinEndpointUrlConfigKey = "ZIPKIN_ENDPOINT_URL";

    /// <summary>
    /// Gets the Application Insights connection string.
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; init; } = applicationInsightsConnectionString;

    /// <summary>
    /// Gets the Zipkin endpoint URL.
    /// </summary>
    public string? ZipkinEndpointUrl { get; init; } = zipkinEndpointUrl;

    /// <summary>
    /// Creates a new instance of the <see cref="ObservabilitySettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="ObservabilitySettings"/> class.</returns>
    public static ObservabilitySettings FromConfiguration(IConfiguration configuration)
    {
        return new ObservabilitySettings(
            configuration[ApplicationInsightsConnectionStringConfigKey],
            configuration[ZipkinEndpointUrlConfigKey] ?? "http://localhost:9411/api/v2/spans");
    }
}
