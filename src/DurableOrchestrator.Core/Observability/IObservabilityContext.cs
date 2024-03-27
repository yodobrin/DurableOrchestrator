namespace DurableOrchestrator.Core.Observability;

/// <summary>
/// Defines an interface for sharing the observability context for objects passed between workflows and activities.
/// </summary>
public interface IObservabilityContext
{
    /// <summary>
    /// Gets or sets the properties storing the observability context.
    /// </summary>
    Dictionary<string, object> ObservabilityProperties { get; set; }
}
