namespace DurableOrchestrator.Models;

public class TextAnalyticsRequest
{
    [JsonPropertyName("operationTypes")]
    public List<string> OperationTypes { get; set; } = new List<string>(); // e.g., ["sentiment", "keyPhrases", "languageDetection"]

    [JsonPropertyName("textsToAnalyze")]
    public string TextsToAnalyze { get; set; } = string.Empty;
}