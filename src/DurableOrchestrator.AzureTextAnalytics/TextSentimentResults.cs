using System.Text.Json.Serialization;

namespace DurableOrchestrator.AzureTextAnalytics;

/// <summary>
/// Defines a model that represents the results of sentiment analysis on text.
/// </summary>
public class TextSentimentResults
{
    /// <summary>
    /// Gets or sets the results of the sentiment analysis.
    /// </summary>
    [JsonPropertyName("results")]
    public List<TextSentimentResult> Results { get; set; } = new();
}
