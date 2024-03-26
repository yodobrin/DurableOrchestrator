using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureDocumentIntelligence;

/// <summary>
/// Defines a model that represents information about a document to analyze using Azure AI Document Intelligence.
/// </summary>
public class DocumentIntelligenceRequest : IWorkflowRequest
{
    /// <summary>
    /// Gets or sets the ID of the model to use for document analysis.
    /// </summary>
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the method by which the document content is provided.
    /// </summary>
    [JsonPropertyName("valueBy")]
    public DocumentIntelligenceRequestContentType ValueBy { get; set; } = DocumentIntelligenceRequestContentType.Uri;

    /// <summary>
    /// Gets or sets the URI of the document content to analyze if <see cref="ValueBy"/> is set to <see cref="DocumentIntelligenceRequestContentType.Uri"/>.
    /// </summary>
    [JsonPropertyName("contentUri")]
    public string ContentUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content of the document to analyze if <see cref="ValueBy"/> is set to <see cref="DocumentIntelligenceRequestContentType.InMemory"/>.
    /// </summary>
    [JsonPropertyName("content")]
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <inheritdoc />
    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();

    /// <inheritdoc />
    public ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(ModelId))
        {
            result.AddErrorMessage($"{nameof(ModelId)} is missing.");
        }

        switch (ValueBy)
        {
            case DocumentIntelligenceRequestContentType.Uri:
                if (string.IsNullOrEmpty(ContentUri))
                {
                    result.AddErrorMessage($"{nameof(ContentUri)} is missing.");
                }
                break;
            case DocumentIntelligenceRequestContentType.InMemory:
                if (Content.Length == 0)
                {
                    result.AddErrorMessage($"{nameof(Content)} is missing.");
                }
                break;
            default:
                result.AddErrorMessage($"Invalid {nameof(DocumentIntelligenceRequestContentType)}.");
                break;
        }

        return result;
    }
}
