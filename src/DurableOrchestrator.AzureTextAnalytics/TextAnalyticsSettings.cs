using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.AzureTextAnalytics;

/// <summary>
/// Defines the settings for the Azure AI Text Analytics service.
/// </summary>
public class TextAnalyticsSettings(string textAnalyticsEndpoint)
{
    /// <summary>
    /// The configuration key for the Azure AI Text Analytics endpoint.
    /// </summary>
    public const string TextAnalyticsEndpointConfigKey = "TEXT_ANALYTICS_ENDPOINT";

    /// <summary>
    /// Gets the URL of the Azure AI Text Analytics endpoint.
    /// </summary>
    public string TextAnalyticsEndpoint { get; init; } = textAnalyticsEndpoint;

    /// <summary>
    /// Creates a new instance of the <see cref="TextAnalyticsSettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="TextAnalyticsSettings"/> class.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the TextAnalyticsEndpoint is not configured.</exception>
    public static TextAnalyticsSettings FromConfiguration(IConfiguration configuration)
    {
        var textAnalyticsEndpoint = configuration.GetValue<string>(TextAnalyticsEndpointConfigKey) ??
                                    throw new InvalidOperationException(
                                        $"{TextAnalyticsEndpointConfigKey} is not configured.");

        return new TextAnalyticsSettings(textAnalyticsEndpoint);
    }
}
