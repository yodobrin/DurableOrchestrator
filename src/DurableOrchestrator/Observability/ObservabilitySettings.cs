using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.Observability;

/// <summary>
/// Defines the settings for configuring application observability.
/// </summary>
public class ObservabilitySettings(string? applicationInsightsConnectionString)
{
    /// <summary>
    /// The configuration key for the Application Insights connection string.
    /// </summary>
    public const string ApplicationInsightsConnectionStringConfigKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    /// <summary>
    /// Gets the Application Insights connection string.
    /// </summary>
    public string? ApplicationInsightsConnectionString { get; init; } = applicationInsightsConnectionString;

    /// <summary>
    /// Creates a new instance of the <see cref="ObservabilitySettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="ObservabilitySettings"/> class.</returns>
    public static ObservabilitySettings FromConfiguration(IConfiguration configuration)
    {
        return new ObservabilitySettings(
            configuration[ApplicationInsightsConnectionStringConfigKey]);
    }
}
