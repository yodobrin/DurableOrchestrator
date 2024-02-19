namespace DurableOrchestrator.Models;

public class WorkFlowInput
{

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("sourceBlobStorageInfo")]
    public BlobStorageInfo? SourceBlobStorageInfo { get; set; }
    
    [JsonPropertyName("targetBlobStorageInfo")]
    public BlobStorageInfo? TargetBlobStorageInfo { get; set; }

    [JsonPropertyName("textAnalyticsRequests")]
    public List<TextAnalyticsRequest>? TextAnalyticsRequests { get; set; } = new List<TextAnalyticsRequest>();
}
