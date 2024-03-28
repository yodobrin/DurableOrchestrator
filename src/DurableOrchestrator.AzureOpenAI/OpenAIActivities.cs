using System.Text;
using Azure.AI.OpenAI;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.AzureOpenAI;

[ActivitySource]
public class OpenAIActivities(OpenAIClient client, ILogger<OpenAIActivities> logger)
    : BaseActivity(nameof(OpenAIActivities))
{
    [Function(nameof(ExecuteChatCompletion))]
    public async Task<string?> ExecuteChatCompletion(
        [ActivityTrigger] OpenAICompletionsRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(ExecuteChatCompletion), input);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"{nameof(OpenAIActivities)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            var messages = new List<ChatRequestMessage>();

            if (!string.IsNullOrEmpty(input.SystemPrompt))
            {
                messages.Add(new ChatRequestSystemMessage(input.SystemPrompt));
            }

            messages.AddRange(input.Messages!.Select(message => new ChatRequestUserMessage(message)));

            var options = new ChatCompletionsOptions(input.ModelDeploymentName, messages)
            {
                MaxTokens = input.MaxTokens,
                Temperature = input.Temperature,
                NucleusSamplingFactor = input.TopP,
            };

            var response = await client.GetChatCompletionsAsync(options);

            var stringBuilder = new StringBuilder();
            foreach (var completion in response.Value.Choices)
            {
                stringBuilder.Append(completion.Message.Content);
            }

            return stringBuilder.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(ExecuteChatCompletion), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }

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
