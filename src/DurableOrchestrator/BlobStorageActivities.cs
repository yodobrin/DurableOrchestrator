using System.Text;
using Azure.Identity;
using Azure.Storage.Blobs;
using DurableOrchestrator.Observability;
using OpenTelemetry.Trace;

namespace DurableOrchestrator;

[ActivitySource(nameof(BlobStorageActivities))]
public class BlobStorageActivities(BlobServiceClient blobServiceClient, ILogger<BlobStorageActivities> log)
{
    private readonly Tracer _tracer = TracerProvider.Default.GetTracer(nameof(BlobStorageActivities));

    [Function(nameof(GetBlobContentAsString))]
    public async Task<string?> GetBlobContentAsString([ActivityTrigger] BlobStorageInfo input,
        FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(GetBlobContentAsString));

        if (!ValidateInput(input, log, checkContent: false))
        {
            return null;
        }

        try
        {
            var blobClient = GetBlobClient(input, log);

            var downloadResult = await blobClient.DownloadContentAsync();
            return downloadResult.Value.Content.ToString();
        }
        catch (Exception ex)
        {
            log.LogError("Error in GetBlobContentAsString: {Message}", ex.Message);
            return null;
        }
    }

    [Function(nameof(GetBlobContentAsBuffer))]
    public async Task<byte[]?> GetBlobContentAsBuffer([ActivityTrigger] BlobStorageInfo input,
        FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(GetBlobContentAsBuffer));

        if (!ValidateInput(input, log, checkContent: false))
        {
            return null;
        }

        try
        {
            var blobClient = GetBlobClient(input, log);

            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            log.LogError("Error in GetBlobContentAsBuffer: {Message}", ex.Message);
            return null;
        }
    }

    [Function(nameof(WriteStringToBlob))]
    public async Task WriteStringToBlob([ActivityTrigger] BlobStorageInfo input, FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(WriteStringToBlob));

        if (!ValidateInput(input, log))
        {
            return;
        }

        try
        {
            var blobClient = GetBlobClient(input, log);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input.Content));
            await blobClient.UploadAsync(stream, overwrite: true);
            log.LogInformation("Successfully uploaded content to blob: {BlobName} in container: {ContainerName}",
                input.BlobName, input.ContainerName);
        }
        catch (Exception ex)
        {
            log.LogError("Error in WriteStringToBlob: {Message}", ex.Message);
        }
    }

    [Function(nameof(WriteBufferToBlob))]
    public async Task WriteBufferToBlob([ActivityTrigger] BlobStorageInfo input, FunctionContext executionContext)
    {
        using var span = _tracer.StartActiveSpan(nameof(WriteStringToBlob));

        if (!ValidateInput(input, log, checkContent: false))
        {
            return;
        }

        try
        {
            var blobClient = GetBlobClient(input, log);

            using var stream = new MemoryStream(input.Buffer);
            await blobClient.UploadAsync(stream, overwrite: true);
            log.LogInformation("Successfully uploaded buffer to blob: {BlobName} in container: {ContainerName}",
                input.BlobName, input.ContainerName);
        }
        catch (Exception ex)
        {
            log.LogError("Error in WriteBufferToBlob: {Message}", ex.Message);
        }
    }

    private BlobClient GetBlobClient(BlobStorageInfo input, ILogger logger)
    {
        try
        {
            BlobClient blobClient;
            if (string.IsNullOrWhiteSpace(input.BlobUri))
            {
                if (string.IsNullOrWhiteSpace(input.ContainerName) || string.IsNullOrWhiteSpace(input.BlobName))
                {
                    logger.LogError("Container name or blob name is not provided, and no BlobUri is specified.");
                    throw new ArgumentException(
                        "Container name or blob name is not provided, and no BlobUri is specified.");
                }

                var blobContainerClient = blobServiceClient.GetBlobContainerClient(input.ContainerName);
                blobContainerClient.CreateIfNotExists();
                blobClient = blobContainerClient.GetBlobClient(input.BlobName);
            }
            else
            {
                blobClient = new BlobClient(new Uri(input.BlobUri), new DefaultAzureCredential());
            }

            return blobClient;
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to create BlobClient: {Message}", ex.Message);
            throw;
        }
    }

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
