using DurableOrchestrator.Core;
using Azure.AI.OpenAI;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.AzureOpenAI;

[ActivitySource]
public class OpenAIActivities(OpenAIClient client, ILogger<OpenAIActivities> logger)
    : BaseActivity(nameof(OpenAIActivities))
{
    [Function(nameof(EmbedText))]
    public async Task<float[]?> EmbedText(
        [ActivityTrigger] OpenAIEmbeddingRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(EmbedText), input);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"{nameof(OpenAIActivities)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            var options = new EmbeddingsOptions(
                input.ModelDeploymentName,
                new List<string> { input.TextToEmbed! });

            var response = await client.GetEmbeddingsAsync(options);
            var embeddings = response.Value.Data[0].Embedding.ToArray();

            logger.LogInformation($"Text embedded successfully with {embeddings.Length} length.");
            return embeddings;
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(EmbedText), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }
}
