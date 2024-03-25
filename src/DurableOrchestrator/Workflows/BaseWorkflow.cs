using DurableOrchestrator.Models;

namespace DurableOrchestrator.Workflows;

/// <summary>
/// Initializes a new instance of the BaseWorkflow class with a specified workflow name. It sets up tracing for the workflow using the OpenTelemetry framework.
/// </summary>
/// <param name="workflowName">The name of the workflow for which tracing is to be set up.</param>
[ActivitySource(nameof(BaseWorkflow))]
public abstract class BaseWorkflow(string workflowName)
{
    protected readonly Tracer Tracer = TracerProvider.Default.GetTracer(workflowName);

    /// <summary>
    /// Validates the inputs provided to a workflow. It checks for null values, mandatory fields, and specific conditions that must be met for the workflow to proceed.
    /// </summary>
    /// <param name="workFlowInput">The WorkFlowInput object containing the input data for validation.</param>
    /// <returns>A ValidationResult object containing information about whether the input is valid, and if not, a list of error messages.</returns>
    protected static ValidationResult ValidateWorkFlowInputs(WorkFlowInput? workFlowInput)
    {
        var result = new ValidationResult { IsValid = true };

        if (workFlowInput == null)
        {
            result.AddMessage("WorkFlowInput is null. No additional checks where performed.");
            result.IsValid = false;
            return result;
        }

        // Source and Target Blobs while marked as optional are required for the workflow to proceed
        if (string.IsNullOrEmpty(workFlowInput.Name))
        {
            result.AddMessage("Workflow name is missing.");
        }

        if (workFlowInput!.SourceBlobStorageInfo == null)
        {
            result.AddMessage("Source blob storage info is missing.");
            result.IsValid = false;
        }
        else
        {
            if (string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.BlobName))
            {
                result.AddMessage("Source blob name is missing.");
                // could be missing - not breaking the validity of the request
            }

            if (string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.ContainerName))
            {
                result.AddMessage("Source container name is missing.");
                result.IsValid = false;
            }

            if (string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.StorageAccountName))
            {
                result.AddMessage("Source storage account name is missing.");
                result.IsValid = false;
            }
        }

        if (workFlowInput.TargetBlobStorageInfo == null)
        {
            result.AddMessage("Target blob storage info is missing.");
            result.IsValid = false;
        }
        else
        {
            if (string.IsNullOrEmpty(workFlowInput.TargetBlobStorageInfo.BlobName))
            {
                result.AddMessage("Target blob name is missing.");
            }

            if (string.IsNullOrEmpty(workFlowInput.TargetBlobStorageInfo.ContainerName))
            {
                result.AddMessage("Target container name is missing.");
                result.IsValid = false;
            }

            if (string.IsNullOrEmpty(workFlowInput.TargetBlobStorageInfo.StorageAccountName))
            {
                result.AddMessage("Target storage account name is missing.");
                result.IsValid = false;
            }
        }

        if (workFlowInput.TextAnalyticsRequests == null || workFlowInput.TextAnalyticsRequests.Count == 0)
        {
            result.AddMessage("TextAnalyticsRequests is missing or empty.");
        }
        else
        {
            // only if the individual list is empty -> request is not valid
            foreach (var request in workFlowInput.TextAnalyticsRequests)
            {
                if (string.IsNullOrEmpty(request.TextsToAnalyze))
                {
                    result.AddMessage("TextsToAnalyze is missing or empty.");
                    result.IsValid = false;
                }

                if (request.OperationTypes == null || request.OperationTypes.Count == 0)
                {
                    result.AddMessage("OperationTypes is missing or empty - what should be done with the text?");
                    result.IsValid = false;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts the workflow input from a JSON-formatted string. It deserializes the request body into a WorkFlowInput object.
    /// </summary>
    /// <param name="requestBody">The JSON-formatted string containing the workflow input data.</param>
    /// <returns>A WorkFlowInput object deserialized from the requestBody string.</returns>
    /// <exception cref="ArgumentException">Thrown when the requestBody is not a valid JSON representation of a WorkFlowInput object.</exception>
    protected static WorkFlowInput ExtractInput(string requestBody)
    {
        var workFlowInput = JsonSerializer.Deserialize<WorkFlowInput>(requestBody) ??
                            throw new ArgumentException("The request body is not a valid WorkFlowInput.",
                                nameof(requestBody));
        return workFlowInput;
    }

    protected TelemetrySpan StartActiveSpan(string name, IObservableContext? input = default)
    {
        var tracer = Tracer; //ActivityTracerProvider.GetTracer(workflowName);
        return input != default ? tracer.StartActiveSpan(name, SpanKind.Internal, input.ExtractTracingContext()) : tracer.StartActiveSpan(name);
    }
}
