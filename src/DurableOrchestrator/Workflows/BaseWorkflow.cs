using OpenTelemetry.Trace;
using DurableOrchestrator.Models;

namespace DurableOrchestrator.Workflows;

public abstract class BaseWorkflow
{
    protected readonly Tracer Tracer;

    public BaseWorkflow(string workflowName)
    {
        Tracer = TracerProvider.Default.GetTracer(workflowName);
    }

   
    protected static ValidationResult ValidateWorkFlowInputs(WorkFlowInput workFlowInput)
    {
        var result = new ValidationResult { IsValid = true };

        if(workFlowInput == null)
        {
            result.AddError("WorkFlowInput is null. No additional checks where performed.");
            return result;
        }
            
        if(string.IsNullOrEmpty(workFlowInput?.Name))
        {
            result.AddError("Workflow name is missing.");
        }            
        if (workFlowInput!.SourceBlobStorageInfo == null)
        {
            result.AddError("Source blob storage info is missing.");
        }            
        else
        {
            if (string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.BlobName))
                {result.AddError("Source blob name is missing.");}
            if (string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.ContainerName))
                {result.AddError("Source container name is missing.");}
            if (string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.StorageAccountName))
                {result.AddError("Source storage account name is missing.");}
        }
        if(workFlowInput.TargetBlobStorageInfo == null)
        {
            result.AddError("Target blob storage info is missing.");
        }
        else
        {
            if (string.IsNullOrEmpty(workFlowInput.TargetBlobStorageInfo.BlobName))
                {result.AddError("Target blob name is missing.");}
            if (string.IsNullOrEmpty(workFlowInput.TargetBlobStorageInfo.ContainerName))
                {result.AddError("Target container name is missing.");}
            if (string.IsNullOrEmpty(workFlowInput.TargetBlobStorageInfo.StorageAccountName))
                {result.AddError("Target storage account name is missing.");}
        }

        return result;
    }

    protected static WorkFlowInput ExtractInput(string requestBody)
    {
        var workFlowInput = JsonSerializer.Deserialize<WorkFlowInput>(requestBody) ??
                    throw new ArgumentException("The request body is not a valid WorkFlowInput.", nameof(requestBody));
        return workFlowInput;
    }
}
