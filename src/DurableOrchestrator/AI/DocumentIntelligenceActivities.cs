using Azure.AI.DocumentIntelligence;
using DurableOrchestrator.Activities;
using DurableOrchestrator.Models;
using Azure;
using System.Text;

namespace DurableOrchestrator.AI;

[ActivitySource(nameof(DocumentIntelligenceActivities))]
public class DocumentIntelligenceActivities(
    DocumentIntelligenceClient client,
    ObservabilitySettings observabilitySettings,
    ILogger<DocumentIntelligenceActivities> log)
    : BaseActivity(nameof(DocumentIntelligenceActivities), observabilitySettings)
{
    /// <summary>
    /// Extract the content of the provided document using Azure AI Document Intelligence, output as markdown.
    /// </summary>
    /// <param name="input">The document intelligence request containing the document content to analyze.</param>
    /// <param name="executionContext">The function execution context.</param>
    /// <returns>The document analysis result as a string, or null if an error occurs.</returns>
    [Function(nameof(AnalyzeDocument2Markdown))]
    public async Task<byte []> AnalyzeDocument2Markdown(
        [ActivityTrigger] DocumentIntelligenceRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(AnalyzeDocument2Markdown), input);
        
        if (!ValidateInput(input, log))
        {
            // throw an exception to indicate that the input is invalid
            throw new ArgumentException("AnalyzeDocument2Markdown::Input is invalid.");
        }
        // check how is the document passed (either via Uri or in-memory content)
        AnalyzeDocumentContent content;
        switch (input.ValueBy)
        {
            case ContentType.Uri:
                content = new AnalyzeDocumentContent()
                {
                    UrlSource = new Uri(input.ContentUri)
                };
                break;
            case ContentType.InMemory:
                content = new AnalyzeDocumentContent () { Base64Source = BinaryData.FromBytes(input.Content)};                
                break;
            default:
                throw new ArgumentException("AnalyzeDocument2Markdown::Invalid ContentType.");
        }
        try
        {
            Operation<AnalyzeResult> operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed,input.ModelId, content, outputContentFormat: ContentFormat.Markdown );
            AnalyzeResult result = operation.Value;
            return Encoding.UTF8.GetBytes(result.Content.ToString());
        }
        catch (Exception ex)
        {
            log.LogError("Error in AnalyzeDocument2Markdown: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }

    /// <summary>
    /// Validates the input for the document intelligence request.
    /// </summary>
    /// <param name="input">The document intelligence request to validate.</param>
    /// <param name="log">The logger instance for logging validation errors.</param>
    /// <returns>true if the input is valid; otherwise, false.</returns>
    private static bool ValidateInput(DocumentIntelligenceRequest? input, ILogger<DocumentIntelligenceActivities> log)
    {
        if (input == null)
        {
            log.LogError("Input is null.");
            return false;
        }

        if (string.IsNullOrEmpty(input.ModelId))
        {
            log.LogError("ModelId is null or empty.");
            return false;
        }

        // switch on the ValueBy:
        switch (input.ValueBy)
        {
            case ContentType.Uri:
                if (string.IsNullOrEmpty(input.ContentUri))
                {
                    log.LogError("ContentUri is null or empty.");
                    return false;
                }
                break;
            case ContentType.InMemory:
                if (input.Content == null || input.Content.Length == 0)
                {
                    log.LogError("Content is null or empty.");
                    return false;
                }
                break;
            default:
                log.LogError("Invalid ContentType.");
                return false;
        }

        return true;
    }

}