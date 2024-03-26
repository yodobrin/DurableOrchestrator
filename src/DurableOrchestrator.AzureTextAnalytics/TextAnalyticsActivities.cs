using Azure.AI.TextAnalytics;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.AzureTextAnalytics;

/// <summary>
/// Defines a collection of activities for interacting with Azure AI Text Analytics.
/// </summary>
/// <param name="client">The <see cref="TextAnalyticsClient"/> instance used to interact with Azure AI Text Analytics.</param>
/// <param name="logger">The logger for capturing telemetry and diagnostic information.</param>
[ActivitySource(nameof(TextAnalyticsActivities))]
public class TextAnalyticsActivities(
    TextAnalyticsClient client,
    ILogger<TextAnalyticsActivities> logger)
    : BaseActivity(nameof(TextAnalyticsActivities))
{
    /// <summary>
    /// Analyzes the sentiment of the provided text using Azure AI Text Analytics.
    /// </summary>
    /// <param name="input">The text analytics request containing the text to analyze.</param>
    /// <param name="executionContext">The function execution context for execution-related functionality.</param>
    /// <returns>The sentiment analysis result as a string, or null if an error occurs.</returns>
    [Function(nameof(GetSentiment))]
    public async Task<string?> GetSentiment(
        [ActivityTrigger] TextAnalyticsRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(GetSentiment), input);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(GetSentiment)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            var response = await client.AnalyzeSentimentAsync(input.TextsToAnalyze);
            return response.Value.Sentiment.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(GetSentiment), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }
}
