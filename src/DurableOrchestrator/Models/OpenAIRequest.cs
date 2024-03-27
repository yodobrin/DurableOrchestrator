using Azure.AI.OpenAI;

namespace DurableOrchestrator.Models;
public class OpenAIRequest : IObservableContext
{
    [JsonPropertyName("openAIOperation")]
    public OpenAIOperation OpenAIOperation { get; set; } = OpenAIOperation.Chat;

    [JsonPropertyName("modelDeploymentName")]
    public string ModelDeploymentName { get; set; } = string.Empty;

    // [JsonPropertyName("query")]
    // public string? Query { get; set; } = string.Empty;
    
    [JsonPropertyName("chatCompletionsOptions")]
    public ChatCompletionsOptions? ChatOptions { get; set; }

    [JsonPropertyName("embeddedDeployment")]
    public string? EmbeddedDeployment { get; set; }

    [JsonPropertyName("text2embed")]
    public string? Text2Embed { get; set; }

    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservableProperties { get; set; } = new();
}

public enum OpenAIOperation
{
    Chat,
    Embedding
}