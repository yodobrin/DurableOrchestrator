using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureOpenAI;

/// <summary>
/// Defines a model that represents a request to the OpenAI service.
/// </summary>
public abstract class OpenAIRequest : BaseWorkflowRequest
{
    /// <summary>
    /// Gets or sets the name of the OpenAI model deployment name to use for the operation.
    /// </summary>
    [JsonPropertyName("modelDeploymentName")]
    public string ModelDeploymentName { get; set; } = string.Empty;

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(ModelDeploymentName))
        {
            result.AddErrorMessage($"{nameof(ModelDeploymentName)} is missing.");
        }

        return result;
    }
}
