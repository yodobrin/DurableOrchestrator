using DurableOrchestrator.Core;
using Azure.AI.OpenAI;
using System.Text;
using Azure;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.AzureOpenAI;

[ActivitySource]
public class OpenAIActivities(
    OpenAIClient client,
    ILogger<OpenAIActivities> log)
    : BaseActivity(nameof(OpenAIActivities))
{
    [Function(nameof(EmbeddText))]
    public async Task<float[]> EmbeddText(
        [ActivityTrigger] OpenAIRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(EmbeddText), input);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"OpenAIActivities::OpenAIRequest is invalid. {validationResult}");
        }
        try
        {            
            var embeddingsOption = new EmbeddingsOptions(input.EmbeddedDeployment, new List<string> {input.Text2Embed!});      
            var modelResponse = await client.GetEmbeddingsAsync(embeddingsOption);
            var response = modelResponse.Value.Data[0].Embedding.ToArray();
            log.LogInformation($"Embedding response: {response.Length}");
            return response;
        }
        catch (Exception ex)
        {
            log.LogError("Error in EmbeddText: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }


}
