using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.Observability;

/// <summary>
/// Defines the settings for configuring application observability.
/// </summary>
public class ObservabilitySettings(string? applicationInsightsConnectionString, string? zipkinEndpointUrl, string? otlpExporterEndpoint)
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
    /// The configuration key for the OpenTelemetry Protocol (OTLP) exporter endpoint.
    /// </summary>
    public const string OtlpExporterEndpointConfigKey = "OTLP_EXPORTER_ENDPOINT";

    /// <summary>
    /// Gets the Application Insights connection string.
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; init; } = applicationInsightsConnectionString;

    /// <summary>
    /// Gets the Zipkin endpoint URL.
    /// </summary>
    public string? ZipkinEndpointUrl { get; init; } = zipkinEndpointUrl;

    /// <summary>
    /// Gets the OpenTelemetry Protocol (OTLP) exporter endpoint.
    /// </summary>
    public string? OtlpExporterEndpoint { get; init; } = otlpExporterEndpoint;

    /// <summary>
    /// Creates a new instance of the <see cref="ObservabilitySettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="ObservabilitySettings"/> class.</returns>
    public static ObservabilitySettings FromConfiguration(IConfiguration configuration)
    {
        return new ObservabilitySettings(
            configuration[ApplicationInsightsConnectionStringConfigKey],
            configuration[ZipkinEndpointUrlConfigKey],
            configuration[OtlpExporterEndpointConfigKey]);
    }
}
