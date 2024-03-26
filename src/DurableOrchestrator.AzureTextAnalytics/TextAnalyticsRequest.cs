using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureTextAnalytics;

/// <summary>
/// Defines a model that represents information about text to analyze using Azure AI Text Analytics.
/// </summary>
public class TextAnalyticsRequest : IWorkflowRequest
{
    /// <summary>
    /// Gets or sets the operation types to perform on the text.
    /// </summary>
    [JsonPropertyName("operationTypes")]
    public List<string>? OperationTypes { get; set; } = new(); // e.g., ["sentiment", "keyPhrases", "languageDetection"]

    /// <summary>
    /// Gets or sets the text to analyze.
    /// </summary>
    [JsonPropertyName("textsToAnalyze")]
    public string TextsToAnalyze { get; set; } = string.Empty;

    /// <inheritdoc />
    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(TextsToAnalyze))
        {
            result.AddErrorMessage($"{nameof(TextsToAnalyze)} is missing.");
        }

        return result;
    }
}
