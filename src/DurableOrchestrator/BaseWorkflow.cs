using OpenTelemetry.Trace;
using DurableOrchestrator.Storage;

namespace DurableOrchestrator;

public abstract class BaseWorkflow
{
    protected readonly Tracer Tracer;

    public BaseWorkflow(string workflowName)
    {
        Tracer = TracerProvider.Default.GetTracer(workflowName);
    }

   
    protected static bool ValidateWorkFlowInput(WorkFlowInput workFlowInput)
    {
        // Implement validation logic here
        // This is a simplified example, extend according to your needs
        return !string.IsNullOrEmpty(workFlowInput?.Name) &&
               workFlowInput.SourceBlobStorageInfo != null &&
               !string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.BlobName) &&
               !string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.ContainerName) &&
               !string.IsNullOrEmpty(workFlowInput.SourceBlobStorageInfo.StorageAccountName) &&
               workFlowInput.TargetBlobStorageInfo != null &&
               !string.IsNullOrEmpty(workFlowInput.TargetBlobStorageInfo.BlobName) &&
               !string.IsNullOrEmpty(workFlowInput.TargetBlobStorageInfo.ContainerName) &&
               !string.IsNullOrEmpty(workFlowInput.TargetBlobStorageInfo.StorageAccountName);
               
    }
    // protected static ExtractAndValidateResult ExtractAndValidateInput(WorkFlowInput workFlowInput)
    // {
    //     // Validate the extracted WorkFlowInput
    //     var validationResult = ValidateWorkFlowInputs(workFlowInput);

    //     // Return both the WorkFlowInput and ValidationResult
    //     return new ExtractAndValidateResult(workFlowInput, validationResult);
    // }

    protected static ValidationResult ValidateWorkFlowInputs(WorkFlowInput workFlowInput)
    {
        var result = new ValidationResult { IsValid = true };

        if(workFlowInput == null)
        {
            result.AddError("WorkFlowInput is null. No addtional checks where performed.");
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
    // protected abstract Task<List<string>> ExecuteWorkflowSteps(WorkFlowInput workFlowInput, TaskOrchestrationContext context);
}
