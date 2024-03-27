using System.Text.Json.Serialization;

namespace DurableOrchestrator.AzureTextAnalytics;

/// <summary>
/// Defines a model that represents the result of sentiment analysis on text.
/// </summary>
public class TextSentimentResult
{
    /// <summary>
    /// Gets or sets the original text that was analyzed.
    /// </summary>
    [JsonPropertyName("originalText")]
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sentiment of the text.
    /// </summary>
    [JsonPropertyName("sentiment")]
    public string Sentiment { get; set; } = string.Empty;
}
