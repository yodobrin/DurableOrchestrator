using System.Text;
using Azure;
using Azure.AI.DocumentIntelligence;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.AzureDocumentIntelligence;

/// <summary>
/// Defines a collection of activities for interacting with Azure AI Document Intelligence.
/// </summary>
/// <param name="client">The <see cref="DocumentIntelligenceClient"/> instance used to interact with Azure AI Document Intelligence.</param>
/// <param name="logger">The logger for capturing telemetry and diagnostic information.</param>
[ActivitySource(nameof(DocumentIntelligenceActivities))]
public class DocumentIntelligenceActivities(
    DocumentIntelligenceClient client,
    ILogger<DocumentIntelligenceActivities> logger)
    : BaseActivity(nameof(DocumentIntelligenceActivities))
{
    /// <summary>
    /// Extract the content of the provided document as markdown using Azure AI Document Intelligence.
    /// </summary>
    /// <param name="input">The document intelligence request containing the document content to analyze.</param>
    /// <param name="executionContext">The function execution context for execution-related functionality.</param>
    /// <returns>The document analysis result as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
    /// <exception cref="Exception">Thrown when an unhandled error occurs during the operation.</exception>
    [Function(nameof(AnalyzeDocumentToMarkdown))]
    public async Task<byte[]?> AnalyzeDocumentToMarkdown(
        [ActivityTrigger] DocumentIntelligenceRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(AnalyzeDocumentToMarkdown), input);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(AnalyzeDocumentToMarkdown)}::{nameof(input)} is invalid. {validationResult}");
        }

        // check how is the document passed (either via Uri or in-memory content)
        var content = input.ValueBy switch
        {
            DocumentIntelligenceRequestContentType.Uri => new AnalyzeDocumentContent
            {
                UrlSource = new Uri(input.ContentUri)
            },
            DocumentIntelligenceRequestContentType.InMemory => new AnalyzeDocumentContent
            {
                Base64Source = BinaryData.FromBytes(input.Content)
            },
            _ => throw new ArgumentException(
                $"{nameof(AnalyzeDocumentToMarkdown)}::{nameof(input)} has an invalid content type.")
        };

        try
        {
            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                input.ModelId,
                content,
                outputContentFormat: ContentFormat.Markdown);

            return Encoding.UTF8.GetBytes(operation.Value.Content);
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(AnalyzeDocumentToMarkdown), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }
}
