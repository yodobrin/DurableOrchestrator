using DurableOrchestrator.Activities;
using DurableOrchestrator.Models;
using Azure.AI.OpenAI;

namespace DurableOrchestrator.AI;

[ActivitySource(nameof(OpenAIActivities))]
public class OpenAIActivities(
    OpenAIClient client,
    ILogger<DocumentIntelligenceActivities> log)
    : BaseActivity(nameof(OpenAIActivities))
{
    [Function(nameof(EmbeddText))]
    public async Task<float[]> EmbeddText(
        [ActivityTrigger] OpenAIRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(EmbeddText), input);

        if (!ValidateInput(input, log))
        {
            throw new ArgumentException("EmbeddText::Input is invalid.");
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

    private static bool ValidateInput(OpenAIRequest input, ILogger log)
    {
        if (input is null)
        {
            log.LogError("Input is null.");
            return false;
        }
        // switch based on input.OpenAIOperation
        switch (input.OpenAIOperation)
        {
            case OpenAIOperation.Embedding:
                return ValidateEmbeddingInput(input, log);
            case OpenAIOperation.Chat:
                return ValidateChatInput(input, log);
            default:
                log.LogError("Invalid OpenAIOperation.");
                return false;
        }

    }

    private static bool ValidateEmbeddingInput(OpenAIRequest input, ILogger log)
    {
        if (input is null)
        {
            log.LogError("OpenAIRequest is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(input.EmbeddedDeployment))
        {
            log.LogError("DeploymentName is null or empty.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(input.Text2Embed))
        {
            log.LogError("text2embed is null or empty.");
            return false;
        }
        // if (input.EmbeddingsOption_s. == null || input.EmbeddingsOption_s.TextsToEmbed.Count == 0)
        // {
        //     log.LogError("TextsToEmbed is null or empty.");
        //     return false;
        // }

        return true;
    }

    private static bool ValidateChatInput(OpenAIRequest input, ILogger log)
    {
        if (input.ChatOptions is null)
        {
            log.LogError("ChatOptions is null.");
            return false;
        }
        return true;
    }
}