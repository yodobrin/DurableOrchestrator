using System.Text;
using DurableOrchestrator.AzureDocumentIntelligence;
using DurableOrchestrator.AzureOpenAI;
using DurableOrchestrator.AzureStorage;
using DurableOrchestrator.Core.Observability;

namespace DurableOrchestrator.Examples.Invoices;

[ActivitySource]
public class InvoiceDataExtractionWorkflow(OpenAISettings openAISettings) : BaseWorkflow(OrchestrationName)
{
    private const string OrchestrationName = nameof(InvoiceDataExtractionWorkflow);
    private const string OrchestrationTriggerName = $"{OrchestrationName}_{nameof(QueueStart)}";

    [Function(OrchestrationName)]
    public async Task<List<string>> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // step 1: obtain input for the workflow
        var input = context.GetInput<InvoiceDataExtractionWorkflowRequest>() ??
                    throw new ArgumentNullException(nameof(context),
                        $"{nameof(InvoiceDataExtractionWorkflowRequest)} is null.");

        using var span = StartActiveSpan(OrchestrationName, input);
        var log = context.CreateReplaySafeLogger(OrchestrationName);

        var orchestrationResults = new WorkflowResult(OrchestrationName, log);

        // step 2: validate the input
        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            orchestrationResults.AddRange(
                nameof(InvoiceDataExtractionWorkflowRequest.Validate),
                $"{nameof(input)} is invalid.",
                validationResult.ValidationMessages,
                LogLevel.Error);
            return orchestrationResults.Results;
        }

        orchestrationResults.Add(nameof(InvoiceDataExtractionWorkflowRequest.Validate), $"{nameof(input)} is valid.");

        // step 3: get blob sas uri to be processed
        var invoiceUri = await CallActivityAsync<string?>(
            context,
            nameof(BlobStorageActivities.GetBlobSasUri),
            input.InvoiceSourceInfo!,
            span.Context);

        if (string.IsNullOrEmpty(invoiceUri))
        {
            orchestrationResults.Add(
                nameof(BlobStorageActivities.GetBlobSasUri),
                $"{nameof(invoiceUri)} is null or empty.",
                LogLevel.Error);
            return orchestrationResults.Results;
        }

        // step 4: convert content into markdown using document intelligence
        var markdownContent = await CallActivityAsync<byte[]?>(
            context,
            nameof(DocumentIntelligenceActivities.AnalyzeDocumentToMarkdown),
            new DocumentIntelligenceRequest
            {
                ContentUri = invoiceUri,
                ValueBy = DocumentIntelligenceRequestContentType.Uri,
                ModelId = "prebuilt-layout"
            },
            span.Context);

        if (markdownContent == null || markdownContent.Length == 0)
        {
            orchestrationResults.Add(
                nameof(DocumentIntelligenceActivities.AnalyzeDocumentToMarkdown),
                $"{nameof(markdownContent)} is null or empty.",
                LogLevel.Error);
            return orchestrationResults.Results;
        }

        // step 5: prompt invoice data extraction using openai
        var invoiceDataStructure = Invoice.Empty;

        var invoiceJsonData = await CallActivityAsync<string?>(
            context,
            nameof(OpenAIActivities.ExecuteChatCompletion),
            new OpenAICompletionsRequest
            {
                ModelDeploymentName = openAISettings.CompletionModelDeployment!,
                MaxTokens = 4096,
                Temperature = 0.1f,
                TopP = 0.1f,
                SystemPrompt =
                    "You are an AI assistant that extracts data from documents and returns them as structured JSON objects. Do not return as a code block.",
                Messages =
                [
                    $"Extract the data from this invoice. If a value is not present, provide null. Use the following structure: {JsonSerializer.Serialize(invoiceDataStructure)}",
                    Encoding.UTF8.GetString(markdownContent)
                ]
            },
            span.Context);

        if (string.IsNullOrEmpty(invoiceJsonData))
        {
            orchestrationResults.Add(
                nameof(OpenAIActivities.ExecuteChatCompletion),
                $"{nameof(invoiceJsonData)} is null or empty.",
                LogLevel.Error);
            return orchestrationResults.Results;
        }

        var invoiceData = JsonSerializer.Deserialize<Invoice>(invoiceJsonData);
        var invoiceEntity = InvoiceEntity.FromInvoice(
            input.TenantId,
            input.InvoiceSourceInfo!.BlobName,
            invoiceData!);

        // step 6: store the invoice data in blob storage
        input.InvoiceTargetInfo!.Buffer = JsonSerializer.SerializeToUtf8Bytes(invoiceEntity);

        await CallActivityAsync(
            context,
            nameof(BlobStorageActivities.WriteBufferToBlob),
            input.InvoiceTargetInfo!,
            span.Context);

        orchestrationResults.Add(nameof(BlobStorageActivities.WriteBufferToBlob),
            $"Invoice data for {invoiceEntity.InvoiceName} stored successfully in tenant {invoiceEntity.TenantId}.");

        return orchestrationResults.Results;
    }

    [Function(OrchestrationTriggerName)]
    public async Task QueueStart(
        [QueueTrigger("invoices", Connection = "AzureWebJobsStorage")]
        InvoiceDataExtractionWorkflowRequest? invoice,
        [DurableClient] DurableTaskClient starter,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(OrchestrationTriggerName);
        var log = executionContext.GetLogger(OrchestrationTriggerName);

        if (invoice == null)
        {
            throw new ArgumentNullException(nameof(invoice), $"{nameof(BlobStorageRequest)} is null.");
        }

        var instanceId = await StartWorkflowAsync(
            starter,
            invoice,
            span.Context);

        log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
    }

    public class InvoiceDataExtractionWorkflowRequest : BaseWorkflowRequest
    {
        /// <summary>
        /// Gets or sets the ID of the tenant associated with the invoice.
        /// </summary>
        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; } = Guid.Empty;

        /// <summary>
        /// Gets or sets the details of the invoice to retrieve from the blob storage.
        /// </summary>
        [JsonPropertyName("invoiceSourceInfo")]
        public BlobStorageRequest? InvoiceSourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the details of the blob storage to store the invoice data.
        /// </summary>
        [JsonPropertyName("invoiceTargetInfo")]
        public BlobStorageRequest? InvoiceTargetInfo { get; set; }

        /// <inheritdoc />
        public override ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (TenantId == Guid.Empty)
            {
                result.AddErrorMessage($"{nameof(TenantId)} is missing.");
            }

            result.Merge(InvoiceSourceInfo?.Validate(false), "Invoice source info is missing.");
            result.Merge(InvoiceTargetInfo?.Validate(false), "Invoice target info is missing.");

            return result;
        }
    }
}
