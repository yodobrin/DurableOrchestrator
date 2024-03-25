using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Core;

/// <summary>
/// Defines an interface for the input to a workflow.
/// </summary>
public interface IWorkflowInput : IObservabilityContext
{
    /// <summary>
    /// Validates the input.
    /// </summary>
    /// <returns>A <see cref="ValidationResult"/> indicating whether the input is valid.</returns>
    ValidationResult Validate();
}
