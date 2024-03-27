using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureOpenAI;
/// <summary>
/// Defines a model that represents a request to the OpenAI service.
/// </summary>
public class OpenAIRequest : IWorkflowRequest
{
    /// <summary>
    /// Gets or sets the operation to perform with the OpenAI service.
    /// </summary>
    [JsonPropertyName("openAIOperation")]
    public OpenAIOperation OpenAIOperation { get; set; } = OpenAIOperation.Chat;
    /// <summary>
    /// Gets or sets the name of the gpt model deployment name to use for the operation.
    /// </summary>
    [JsonPropertyName("modelDeploymentName")]
    public string ModelDeploymentName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the chat completions options to use for the operation.
    /// </summary>
    [JsonPropertyName("chatCompletionsOptions")]
    public ChatCompletionsOptions? ChatOptions { get; set; }
    /// <summary>
    /// Gets or sets the name of the embedded deployment name to use for the operation.
    /// </summary>
    [JsonPropertyName("embeddedDeployment")]
    public string? EmbeddedDeployment { get; set; }
    /// <summary>
    /// Gets or sets the text to embed using the embedded deployment.
    /// </summary>
    [JsonPropertyName("text2embed")]
    public string? Text2Embed { get; set; }
    /// <summary>
    /// Gets or sets the observable properties for the request.
    /// </summary>
    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();

    /// <summary>
    /// Validates the request.
    /// </summary>
    /// <returns></returns>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        switch (OpenAIOperation)
        {
            case OpenAIOperation.Chat:
                if (string.IsNullOrWhiteSpace(ModelDeploymentName))
                {
                    result.AddErrorMessage($"ModelDeploymentName is missing.");
                }
                if (ChatOptions is null)
                {
                    result.AddErrorMessage($"ChatCompletionsOptions is missing.");
                }
                break;
            case OpenAIOperation.Embedding:
                if (string.IsNullOrWhiteSpace(EmbeddedDeployment))
                {
                    result.AddErrorMessage($"EmbeddedDeployment is missing.");
                }
                if (string.IsNullOrWhiteSpace(Text2Embed))
                {
                    result.AddErrorMessage($"Text2Embed is missing.");
                }
                break;
            default:
                result.AddErrorMessage($"OpenAIOperation is invalid.");
                break;
        }
        return result;
    }
}

/// <summary>
/// Defines the operations that can be performed with the OpenAI service.
/// </summary>
public enum OpenAIOperation
{
    Chat,
    Embedding
}