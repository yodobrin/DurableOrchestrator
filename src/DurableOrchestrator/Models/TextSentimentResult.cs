
namespace DurableOrchestrator.Models;
public class TextSentimentResult
{
    [JsonPropertyName("originalText")]
    public string OriginalText { get; set; } = string.Empty;

    [JsonPropertyName("sentiment")]
    public string Sentiment { get; set; } = string.Empty;
}

public class AnalysisResults
{
    [JsonPropertyName("results")]
    public List<TextSentimentResult> Results { get; set; } = new List<TextSentimentResult>();
}
