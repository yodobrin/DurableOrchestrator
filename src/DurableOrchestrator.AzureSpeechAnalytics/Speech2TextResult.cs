using System.Text.Json.Serialization;

namespace DurableOrchestrator.AzureSpeechAnalytics;

/// <summary>
/// Defines a model that represents the result of sentiment analysis on text.
/// </summary>
public class Speech2TextResult
{
    /// <summary>
    /// Extracted text from the audio.
    /// </summary>
    [JsonPropertyName("extractedText")]
    public string ExtractedText { get; set; } = string.Empty;

}
