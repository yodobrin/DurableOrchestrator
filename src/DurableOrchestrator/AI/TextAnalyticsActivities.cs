using Azure.AI.TextAnalytics;
// using System.Text;
using DurableOrchestrator.Observability;
using OpenTelemetry.Trace;
using DurableOrchestrator.Models;

namespace DurableOrchestrator.AI;

[ActivitySource(nameof(TextAnalyticsActivities))]
public class TextAnalyticsActivities
{
    private readonly TextAnalyticsClient _client;
    private readonly ILogger<TextAnalyticsActivities> _log;
    private readonly Tracer _tracer = TracerProvider.Default.GetTracer(nameof(TextAnalyticsActivities));

    public TextAnalyticsActivities(TextAnalyticsClient client, ILogger<TextAnalyticsActivities> log)
    {
        _client = client;
        _log = log;
    }

    [Function(nameof(GetSentiment))]
    /// <summary>
    /// Analyzes the sentiment of the provided text using Azure AI Text Analytics.
    /// </summary>
    /// <param name="input">The text analytics request containing the text to analyze.</param>
    /// <param name="executionContext">The function execution context.</param>
    /// <returns>The sentiment analysis result as a string, or null if an error occurs.</returns>

    public async Task<string?> GetSentiment([ActivityTrigger] TextAnalyticsRequest input,
        FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(GetSentiment));

        if (!ValidateInput(input, _log))
        {
            return null;
        }

        try
        {
            var response = await _client.AnalyzeSentimentAsync(input.TextsToAnalyze);
            return response.Value.Sentiment.ToString();
        }
        catch (Exception ex)
        {
            _log.LogError("Error in GetSentiment: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            return null;
        }
    }

    /// <summary>
    /// Validates the input for the text analytics request.
    /// </summary>
    /// <param name="input">The text analytics request to validate.</param>
    /// <param name="log">The logger instance for logging validation errors.</param>
    /// <returns>true if the input is valid; otherwise, false.</returns>
    static bool ValidateInput(TextAnalyticsRequest input, ILogger<TextAnalyticsActivities> log)
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