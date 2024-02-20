using System.Text;
using DurableOrchestrator.Observability;
using OpenTelemetry.Trace;
using DurableOrchestrator.Models;

namespace DurableOrchestrator.Storage;

[ActivitySource(nameof(BlobStorageActivities))]
public class BlobStorageActivities
{
    private readonly BlobServiceClientsWrapper _blobServiceClientsWrapper;
    private readonly ILogger<BlobStorageActivities> _log;
    private readonly Tracer _tracer = TracerProvider.Default.GetTracer(nameof(BlobStorageActivities));

    public BlobStorageActivities(BlobServiceClientsWrapper blobServiceClientsWrapper, ILogger<BlobStorageActivities> log)
    {
        _blobServiceClientsWrapper = blobServiceClientsWrapper;
        _log = log;
    }

    [Function(nameof(GetBlobContentAsString))]
    /// <summary>
    /// Retrieves the content of a blob as a string. Validates the input before attempting to read the blob's content.
    /// </summary>
    /// <param name="input">Blob storage information including container and blob names.</param>
    /// <param name="executionContext">Function execution context for logging and telemetry.</param>
    /// <returns>The content of the specified blob as a string, or null if the operation fails.</returns>
    public async Task<string?> GetBlobContentAsString([ActivityTrigger] BlobStorageInfo input,
        FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(GetBlobContentAsString));

        if (!ValidateInput(input, _log, checkContent: false))
        {
            throw new ArgumentException("Invalid input", nameof(input));
        }

        try
        {
            var blobContainerClient = _blobServiceClientsWrapper.SourceClient.GetBlobContainerClient(input.ContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(input.BlobName);

            var downloadResult = await blobClient.DownloadContentAsync();
            return downloadResult.Value.Content.ToString();
        }
        catch (Exception ex)
        {
            _log.LogError("Error in GetBlobContentAsString: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }

    [Function(nameof(GetBlobContentAsBuffer))]
    /// <summary>
    /// Retrieves the content of a blob as a byte array. Validates the input before attempting to read the blob's content.
    /// </summary>
    /// <param name="input">Blob storage information including container and blob names.</param>
    /// <param name="executionContext">Function execution context for logging and telemetry.</param>
    /// <returns>The content of the specified blob as a byte array, or null if the operation fails.</returns>
    public async Task<byte[]?> GetBlobContentAsBuffer([ActivityTrigger] BlobStorageInfo input,
        FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(GetBlobContentAsBuffer));

        if (!ValidateInput(input, _log, checkContent: false))
        {
            throw new ArgumentException("Invalid input", nameof(input));
        }

        try
        {
            _log.LogInformation($"trying to read content of {input.BlobName} in container {input.ContainerName}");

            var blobContainerClient = _blobServiceClientsWrapper.SourceClient.GetBlobContainerClient(input.ContainerName);
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(input.BlobName);

            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _log.LogError("Error in GetBlobContentAsBuffer: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }

    [Function(nameof(WriteStringToBlob))]
    /// <summary>
    /// Writes a string to a blob. Validates the input before writing the content to the blob.
    /// </summary>
    /// <param name="input">Blob storage information including container and blob names, along with the content to write.</param>
    /// <param name="executionContext">Function execution context for logging and telemetry.</param>
    public async Task WriteStringToBlob([ActivityTrigger] BlobStorageInfo input, FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(WriteStringToBlob));

        if (!ValidateInput(input, _log))
        {
            return;
        }

        try
        {
            var blobClient = _blobServiceClientsWrapper.TargetClient.GetBlobContainerClient(input.ContainerName).GetBlobClient(input.BlobName);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input.Content));
            await blobClient.UploadAsync(stream, overwrite: true);
            _log.LogInformation("Successfully uploaded content to blob: {BlobName} in container: {ContainerName}",
                input.BlobName, input.ContainerName);
        }
        catch (Exception ex)
        {
            _log.LogError("Error in WriteStringToBlob: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);
        }
    }

    [Function(nameof(WriteBufferToBlob))]
    /// <summary>
    /// Writes a byte array to a blob. Validates the input before writing the buffer to the blob.
    /// </summary>
    /// <param name="input">Blob storage information including container and blob names, along with the buffer to write.</param>
    /// <param name="executionContext">Function execution context for logging and telemetry.</param>
    public async Task WriteBufferToBlob([ActivityTrigger] BlobStorageInfo input, FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(WriteStringToBlob));

        if (!ValidateInput(input, _log, checkContent: false))
        {
            return;
        }

        try
        {
            _log.LogInformation($"trying to write to {input.ContainerName} to a file named: {input.BlobName}");
            var blobClient = _blobServiceClientsWrapper.TargetClient.GetBlobContainerClient(input.ContainerName).GetBlobClient(input.BlobName);

            using var stream = new MemoryStream(input.Buffer);
            await blobClient.UploadAsync(stream, overwrite: true);
            _log.LogInformation($"Successfully uploaded buffer to blob: {input.BlobName} in container: {input.ContainerName}",
                input.BlobName, input.ContainerName);
        }
        catch (Exception ex)
        {
            _log.LogError("Error in WriteBufferToBlob: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);
        }
    }
    /// <summary>
    /// Validates the blob storage input, optionally checking if the content is not null or whitespace when required.
    /// </summary>
    /// <param name="input">The blob storage information to validate.</param>
    /// <param name="log">Logger for logging warnings in case of invalid inputs.</param>
    /// <param name="checkContent">Flag indicating whether to check the content for null or whitespace.</param>
    /// <returns>True if the input is valid, otherwise false.</returns>
    private static bool ValidateInput(BlobStorageInfo input, ILogger log, bool checkContent = true)
    {
        if (!checkContent || !string.IsNullOrWhiteSpace(input.Content))
        {
            return true;
        }

        log.LogWarning("Content is null or whitespace.");
        return false;
    }
}
