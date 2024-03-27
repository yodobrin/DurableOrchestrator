using System.Text.Json.Serialization;

namespace DurableOrchestrator.Core;

/// <summary>
/// Defines a model that represents a request to a workflow or activity.
/// </summary>
public abstract class BaseWorkflowRequest : IWorkflowRequest
{
    /// <inheritdoc />
    [JsonPropertyName("observableProperties")]
    public Dictionary<string, object> ObservabilityProperties { get; set; } = new();

    /// <inheritdoc />
    public abstract ValidationResult Validate();
}
