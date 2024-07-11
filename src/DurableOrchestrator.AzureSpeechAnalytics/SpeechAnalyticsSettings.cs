using Microsoft.Extensions.Configuration;

namespace DurableOrchestrator.AzureSpeechAnalytics;

/// <summary>
/// Defines the settings for the Azure AI Text Analytics service.
/// </summary>
public class SpeechAnalyticsSettings(string speechResourceID, string speechRegion)
{
    /// <summary>
    /// The configuration key for the Azure AI Voice endpoint.
    /// </summary>
    public const string SpeechAnalyticsResourceID = "SPEECH_ANALYTICS_RESOURCE_ID";

    public const string SpeechAnalyticsRegion = "SPEECH_ANALYTICS_REGION";

    /// <summary>
    /// Gets the URL of the Azure AI Text Analytics endpoint.
    /// </summary>
    public string SpeechResourceID { get; init; } = speechResourceID;

    public string SpeechRegion { get; init; } = speechRegion;

    /// <summary>
    /// Creates a new instance of the <see cref="SpeechAnalyticsSettings"/> class from the specified configuration.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/> to use.</param>
    /// <returns>A new instance of the <see cref="SpeechAnalyticsSettings"/> class.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the TextAnalyticsEndpoint is not configured.</exception>
    public static SpeechAnalyticsSettings FromConfiguration(IConfiguration configuration)
    {
        var speechResourceId = configuration.GetValue<string>(SpeechAnalyticsResourceID) ??
                                    throw new InvalidOperationException(
                                        $"{SpeechAnalyticsResourceID} is not configured.");

        var speechRegion = configuration.GetValue<string>(SpeechAnalyticsRegion) ??
                                    throw new InvalidOperationException(
                                        $"{SpeechAnalyticsRegion} is not configured.");
        return new SpeechAnalyticsSettings(speechResourceId, speechRegion);
    }
}
