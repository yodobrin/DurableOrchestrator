using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureSpeechAnalytics;

/// <summary>
/// Defines a model that represents information about text to analyze using Azure AI Text Analytics.
/// </summary>
public class SpeechAnalyticsRequest : IWorkflowRequest
{
    /// <summary>
    /// Gets or sets the operation types to perform on the text.
    /// </summary>
    [JsonPropertyName("operationType")]
    public string? OperationType { get; set; } = string.Empty; // e.g., ["speech2text", "text2speech"]

    /// <summary>
    /// Gets or sets the text to process.
    /// </summary>
    [JsonPropertyName("textsToProcess")]
    public string ? TextsToProcess { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the audio bytes to process.
    /// </summary>

    [JsonPropertyName("audioFilePath")]
    public string? AudioFilePath { get; set; } = string.Empty;

    /// <inheritdoc />
    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        // check if the operation type is missing
        if (string.IsNullOrEmpty(OperationType))
        {
            result.AddErrorMessage($"{nameof(OperationType)} is missing.");
        }

        // check if its speech2text and the file path are empty
        if (OperationType.Equals("speech2text") && string.IsNullOrEmpty(AudioFilePath) )
        {
            result.AddErrorMessage($"{nameof(AudioFilePath)} is missing.");
        }
        // check if its text2speech and the text is empty
        else if (OperationType.Equals("text2speech") && string.IsNullOrWhiteSpace(TextsToProcess))
        {
            result.AddErrorMessage($"{nameof(TextsToProcess)} is missing.");
        }
        return result;
    }
}
