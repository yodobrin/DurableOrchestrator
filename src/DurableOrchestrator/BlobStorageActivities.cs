using System.Text;
using Azure.Identity;
using Azure.Storage.Blobs;
using DurableOrchestrator.Observability;
using OpenTelemetry.Trace;
using DurableOrchestrator.Storage;

namespace DurableOrchestrator;

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

        // var sourceBlobClient = _blobServiceClientsWrapper.SourceClient.GetBlobContainerClient("sourceContainer").GetBlobClient("sourceBlobName");
        // var targetBlobClient = _blobServiceClientsWrapper.TargetClient.GetBlobContainerClient("targetContainer").GetBlobClient("targetBlobName");

    [Function(nameof(GetBlobContentAsString))]
    public async Task<string?> GetBlobContentAsString([ActivityTrigger] BlobStorageInfo input,
        FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(GetBlobContentAsString));

        if (!ValidateInput(input, _log, checkContent: false))
        {
            return null;
        }

        try
        {            
            var blobClient = _blobServiceClientsWrapper.SourceClient.GetBlobContainerClient(input.ContainerName).GetBlobClient(input.BlobName);

            var downloadResult = await blobClient.DownloadContentAsync();
            return downloadResult.Value.Content.ToString();
        }
        catch (Exception ex)
        {
            _log.LogError("Error in GetBlobContentAsString: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            return null;
        }
    }

    [Function(nameof(GetBlobContentAsBuffer))]
    public async Task<byte[]?> GetBlobContentAsBuffer([ActivityTrigger] BlobStorageInfo input,
        FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(GetBlobContentAsBuffer));

        if (!ValidateInput(input, _log, checkContent: false))
        {
            return null;
        }

        try
        {
            _log.LogInformation($"trying to read content of {input.BlobName} in container {input.ContainerName}");
            var blobClient = _blobServiceClientsWrapper.SourceClient.GetBlobContainerClient(input.ContainerName).GetBlobClient(input.BlobName);
            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _log.LogError("Error in GetBlobContentAsBuffer: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            return null;
        }
    }

    [Function(nameof(WriteStringToBlob))]
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
            _log.LogInformation("Successfully uploaded buffer to blob: {BlobName} in container: {ContainerName}",
                input.BlobName, input.ContainerName);
        }
        catch (Exception ex)
        {
            _log.LogError("Error in WriteBufferToBlob: {Message}", ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);
        }
    }

    // private BlobClient GetBlobClient(BlobStorageInfo input, ILogger logger)
    // {
    //     try
    //     {
    //         BlobClient blobClient;
    //         if (string.IsNullOrWhiteSpace(input.BlobUri))
    //         {
    //             if (string.IsNullOrWhiteSpace(input.ContainerName) || string.IsNullOrWhiteSpace(input.BlobName))
    //             {
    //                 logger.LogError("Container name or blob name is not provided, and no BlobUri is specified.");
    //                 throw new ArgumentException(
    //                     "Container name or blob name is not provided, and no BlobUri is specified.");
    //             }

    //             var blobContainerClient = blobServiceClient.GetBlobContainerClient(input.ContainerName);
    //             blobContainerClient.CreateIfNotExists();
    //             blobClient = blobContainerClient.GetBlobClient(input.BlobName);
    //         }
    //         else
    //         {
    //             blobClient = new BlobClient(new Uri(input.BlobUri), new DefaultAzureCredential());
    //         }

    //         return blobClient;
    //     }
    //     catch (Exception ex)
    //     {
    //         logger.LogError("Failed to create BlobClient: {Message}", ex.Message);
    //         throw;
    //     }
    // }

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