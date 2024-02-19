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