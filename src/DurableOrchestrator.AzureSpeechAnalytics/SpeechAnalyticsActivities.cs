using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System;
using System.Text;

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
            using FileStream audioStream = File.OpenRead(input.AudioFilePath);
            var pushStream = AudioInputStream.CreatePushStream();

            // Read audio data from file and push it to the push stream
            byte[] buffer = new byte[1024];
            int bytesRead;
            while ((bytesRead = await audioStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                pushStream.Write(buffer, bytesRead);
            }
            pushStream.Close();

            using var audioInput = AudioConfig.FromStreamInput(pushStream);
            using var recognizer = new SpeechRecognizer(speechConfig, audioInput);

            var transcription = new StringBuilder();
            var taskCompletionSource = new TaskCompletionSource<string>();
            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    transcription.Append(e.Result.Text);
                    transcription.Append(" ");
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine("No speech could be recognized.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                Console.WriteLine($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                    Console.WriteLine("CANCELED: Did you update the subscription info?");
                }

                taskCompletionSource.TrySetResult(transcription.ToString().Trim());
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("Session stopped.");
                taskCompletionSource.TrySetResult(transcription.ToString().Trim());
            };

            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            // Wait for the session to stop or cancel
            var result = await taskCompletionSource.Task;

            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

            return result;
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
