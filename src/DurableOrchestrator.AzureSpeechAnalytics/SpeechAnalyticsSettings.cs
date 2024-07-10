using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.AzureSpeechAnalytics;

/// <summary>
/// Defines the settings for the Azure AI Text Analytics service.
/// </summary>
public class SpeechAnalyticsSettings(string speechEndpoint)
{
    /// <summary>
    /// The configuration key for the Azure AI Voice endpoint.
    /// </summary>
    public const string SpeechEndpointConfigKey = "SPEECH_ANALYTICS_ENDPOINT";

    /// <summary>
    /// Gets the URL of the Azure AI Text Analytics endpoint.
    /// </summary>
    public string SpeechEndpoint { get; init; } = speechEndpoint;

    /// <summary>
    /// Creates a new instance of the <see cref="SpeechAnalyticsSettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="SpeechAnalyticsSettings"/> class.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the TextAnalyticsEndpoint is not configured.</exception>
    public static SpeechAnalyticsSettings FromConfiguration(IConfiguration configuration)
    {
        var speechEndpoint = configuration.GetValue<string>(SpeechEndpointConfigKey) ??
                                    throw new InvalidOperationException(
                                        $"{SpeechEndpointConfigKey} is not configured.");

        return new SpeechAnalyticsSettings(speechEndpoint);
    }
}
