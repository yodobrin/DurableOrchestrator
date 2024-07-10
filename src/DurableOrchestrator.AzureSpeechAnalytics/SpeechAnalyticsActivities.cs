using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.AzureSpeechAnalytics;


[ActivitySource]
public class SpeechAnalyticsActivities(
    SpeechConfig speechConfig,
    ILogger<SpeechAnalyticsActivities> logger)
    : BaseActivity(nameof(SpeechAnalyticsActivities))
{
     [Function(nameof(RecognizeSpeech))]
    public async Task<string?> RecognizeSpeech([ActivityTrigger] SpeechAnalyticsRequest input, FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(RecognizeSpeech), input);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"{nameof(SpeechAnalyticsActivities)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            using var audioInput = AudioConfig.FromWavFileInput(input.AudioFilePath);
            var recognizer = new SpeechRecognizer(speechConfig, audioInput);

            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                logger.LogInformation($"Recognized: {result.Text}");
                return result.Text;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                logger.LogWarning("No speech could be recognized.");
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(RecognizeSpeech), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }
}
