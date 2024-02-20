namespace DurableOrchestrator.Models;

/// <summary>
/// Defines an interface for sharing the context of an observable sequence through activities.
/// </summary>
public interface IObservableContext
{
    /// <summary>
    /// Gets or sets the properties storing the context.
    /// </summary>
    Dictionary<string, object> ObservableProperties { get; set; }
}
