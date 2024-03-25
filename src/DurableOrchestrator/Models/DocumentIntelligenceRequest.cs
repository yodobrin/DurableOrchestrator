namespace DurableOrchestrator.Models;

public class DocumentIntelligenceRequest : IObservableContext
{
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    [JsonPropertyName("valueBy")]
    public ContentType ValueBy { get; set; } = ContentType.Uri;

    [JsonPropertyName("contentUri")]
    public string ContentUri { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public byte[] Content { get; set; } = Array.Empty<byte>();

    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservableProperties { get; set; } = new();
}

public enum ContentType
{
    Uri,
    InMemory
}