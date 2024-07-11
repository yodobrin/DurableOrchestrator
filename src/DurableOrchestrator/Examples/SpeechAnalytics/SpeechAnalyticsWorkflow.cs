using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core.Observability;
using DurableOrchestrator.AzureSpeechAnalytics;
// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Extensions.Logging;
// using System.Collections.Generic;
// using System.Threading.Tasks;

namespace DurableOrchestrator.Examples.SpeechAnalytics;

    [ActivitySource]
    public class SpeechAnalyticsWorkflow : BaseWorkflow
    {
        private const string OrchestrationName = nameof(SpeechAnalyticsWorkflow);
        private const string OrchestrationTriggerName = $"{OrchestrationName}_HttpStart";

        public SpeechAnalyticsWorkflow() : base(OrchestrationName) { }

        [Function(OrchestrationName)]
        public async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var input = context.GetInput<SpeechAnalyticsWorkflowRequest>() ??
                        throw new ArgumentNullException(nameof(context), $"{nameof(SpeechAnalyticsWorkflowRequest)} is null.");

            using var span = StartActiveSpan(OrchestrationName, input);
            var log = context.CreateReplaySafeLogger(OrchestrationName);

            var orchestrationResults = new WorkflowResult(OrchestrationName, log);

            var validationResult = input.Validate();
            if (!validationResult.IsValid)
            {
                orchestrationResults.AddRange(
                    nameof(SpeechAnalyticsWorkflowRequest.Validate),
                    $"{nameof(input)} is invalid.",
                    validationResult.ValidationMessages,
                    LogLevel.Error);
                return orchestrationResults.Results;
            }

            orchestrationResults.Add(nameof(SpeechAnalyticsWorkflowRequest.Validate), $"{nameof(input)} is valid.");

            var recognizedText = await CallActivityAsync<string?>(
                context,
                nameof(SpeechAnalyticsActivities.RecognizeSpeech),
                input,
                span.Context);

            if (!string.IsNullOrEmpty(recognizedText))
            {
          // step : store the transcript data in blob storage
        input.TargetBlobStorageInfo!.Buffer = JsonSerializer.SerializeToUtf8Bytes(recognizedText, new JsonSerializerOptions { WriteIndented = true });

        await CallActivityAsync(
            context,
            nameof(BlobStorageActivities.WriteBufferToBlob),
            input.TargetBlobStorageInfo!,
            span.Context);

        orchestrationResults.Add(nameof(BlobStorageActivities.WriteBufferToBlob),
            $"transcription was stored successfully in storage.");
            }
            else
            {
                orchestrationResults.Add(nameof(SpeechAnalyticsActivities.RecognizeSpeech), "No speech recognized.");
            }

            return orchestrationResults.Results;
        }

        [Function(OrchestrationTriggerName)]
        public async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
            HttpRequestData req,
            [DurableClient] DurableTaskClient starter,
            FunctionContext executionContext)
        {
            using var span = StartActiveSpan(OrchestrationTriggerName);
            var log = executionContext.GetLogger(OrchestrationTriggerName);

            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrEmpty(requestBody))
            {
                throw new ArgumentException("The request body must not be null or empty.", nameof(req));
            }
log.LogInformation("Received request with body: {requestBody}", requestBody);
            var instanceId = await StartWorkflowAsync(
                starter,
                ExtractInput<SpeechAnalyticsWorkflowRequest>(requestBody),
                span.Context);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return await starter.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }

    public class SpeechAnalyticsWorkflowRequest : BaseWorkflowRequest
    {
        [JsonPropertyName("operationType")]
        public string? OperationType { get; set; }
        
        [JsonPropertyName("audioFilePath")]
        public string? AudioFilePath { get; set; }

        [JsonPropertyName("targetBlobStorageInfo")]
        public BlobStorageRequest? TargetBlobStorageInfo { get; set; }

        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(AudioFilePath))
            {
                result.AddErrorMessage($"{nameof(AudioFilePath)} is missing or empty.");
            }

            result.Merge(TargetBlobStorageInfo?.Validate(checkContent: false), "Target blob storage info is missing.");

            return result;
        }
    }

