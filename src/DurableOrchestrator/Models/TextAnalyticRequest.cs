using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Models;

public class TextAnalyticsRequest : IObservabilityContext
{
    [JsonPropertyName("operationTypes")]
    public List<string>? OperationTypes { get; set; } = new List<string>(); // e.g., ["sentiment", "keyPhrases", "languageDetection"]

    [JsonPropertyName("textsToAnalyze")]
    public string TextsToAnalyze { get; set; } = string.Empty;

    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();
}
