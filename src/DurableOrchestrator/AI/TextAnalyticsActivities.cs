using Azure.AI.TextAnalytics;
using DurableOrchestrator.Activities;
using DurableOrchestrator.Models;
using DurableOrchestrator.Observability;

namespace DurableOrchestrator.AI;

[ActivitySource(nameof(TextAnalyticsActivities))]
public class TextAnalyticsActivities(TextAnalyticsClient client, ILogger<TextAnalyticsActivities> log) : BaseActivity(nameof(TextAnalyticsActivities))
{
    /// <summary>
    /// Analyzes the sentiment of the provided text using Azure AI Text Analytics.
    /// </summary>
    /// <param name="input">The text analytics request containing the text to analyze.</param>
    /// <param name="executionContext">The function execution context.</param>
    /// <returns>The sentiment analysis result as a string, or null if an error occurs.</returns>
    [Function(nameof(GetSentiment))]
    public async Task<string?> GetSentiment(
        [ActivityTrigger] TextAnalyticsRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(GetSentiment), input);

        if (!ValidateInput(input, log))
        {
            // throw an exception to indicate that the input is invalid
            throw new ArgumentException("GetSentiment::Input is invalid.");
        }

        try
        {
            var response = await client.AnalyzeSentimentAsync(input.TextsToAnalyze);
            return response.Value.Sentiment.ToString();
        }
        catch (Exception ex)
        {
            log.LogError("Error in GetSentiment: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }

    /// <summary>
    /// Validates the input for the text analytics request.
    /// </summary>
    /// <param name="input">The text analytics request to validate.</param>
    /// <param name="log">The logger instance for logging validation errors.</param>
    /// <returns>true if the input is valid; otherwise, false.</returns>
    private static bool ValidateInput(TextAnalyticsRequest? input, ILogger<TextAnalyticsActivities> log)
    {
        if (input == null)
        {
            log.LogError("Input is null.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(input.TextsToAnalyze))
        {
            log.LogError("TextsToAnalyze is null or whitespace.");
            return false;
        }
        return true;
    }
}
