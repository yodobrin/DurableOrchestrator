using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using BlobSasBuilder = Azure.Storage.Sas.BlobSasBuilder;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a collection of activities for interacting with Azure Blob Storage.
/// </summary>
/// <param name="storageClientFactory">The <see cref="StorageClientFactory"/> instance used to interact with Azure Storage accounts.</param>
/// <param name="logger">The logger for capturing telemetry and diagnostic information.</param>
[ActivitySource]
public class BlobStorageActivities(
    StorageClientFactory storageClientFactory,
    ILogger<BlobStorageActivities> logger)
    : BaseActivity(nameof(BlobStorageActivities))
{
    [Function(nameof(GetBlobSasUri))]
    public async Task<string?> GetBlobSasUri(
        [ActivityTrigger] BlobStorageRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(GetBlobSasUri), input);

        var validationResult = input.Validate(checkContent: false);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(GetBlobSasUri)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            var blobServiceClient = storageClientFactory
                .GetBlobServiceClient(input.StorageAccountName);

            var blobClient = blobServiceClient
                .GetBlobContainerClient(input.ContainerName)
                .GetBlobClient(input.BlobName);

            if (!blobClient.CanGenerateSasUri)
            {
                // The blob client is constructed with managed identity and cannot generate SAS tokens, so we need to create one manually.
                var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = input.ContainerName,
                    BlobName = input.BlobName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow,
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };
                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
                {
                    Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName)
                };

                return blobUriBuilder.ToUri().ToString();
            }

            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));
            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(GetBlobSasUri), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }

    /// <summary>
    /// Retrieves the content of a blob as a string from Azure Storage.
    /// </summary>
    /// <param name="input">The blob storage information including storage account, container, and blob name.</param>
    /// <param name="executionContext">The function execution context for execution-related functionality.</param>
    /// <returns>The content of the specified blob as a string.</returns>
    /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
    /// <exception cref="Exception">Thrown when an unhandled error occurs during the operation.</exception>
    [Function(nameof(GetBlobContentAsString))]
    public async Task<string?> GetBlobContentAsString(
        [ActivityTrigger] BlobStorageRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(GetBlobContentAsString), input);

        var validationResult = input.Validate(checkContent: false);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(GetBlobContentAsString)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            var blobClient = storageClientFactory
                .GetBlobServiceClient(input.StorageAccountName)
                .GetBlobContainerClient(input.ContainerName)
                .GetBlobClient(input.BlobName);

            var downloadResult = await blobClient.DownloadContentAsync();
            return downloadResult.Value.Content.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(GetBlobContentAsString), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }

    /// <summary>
    /// Retrieves the content of a blob as a byte array from Azure Storage.
    /// </summary>
    /// <param name="input">The blob storage information including storage account, container, and blob name.</param>
    /// <param name="executionContext">The function execution context for execution-related functionality.</param>
    /// <returns>The content of the specified blob as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
    /// <exception cref="Exception">Thrown when an unhandled error occurs during the operation.</exception>
    [Function(nameof(GetBlobContentAsBuffer))]
    public async Task<byte[]?> GetBlobContentAsBuffer(
        [ActivityTrigger] BlobStorageRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(GetBlobContentAsBuffer), input);

        var validationResult = input.Validate(checkContent: false);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(GetBlobContentAsBuffer)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            logger.LogInformation(
                "Attempting to read content of {BlobName} in container {ContainerName}",
                input.BlobName,
                input.ContainerName);

            var blobClient = storageClientFactory
                .GetBlobServiceClient(input.StorageAccountName)
                .GetBlobContainerClient(input.ContainerName)
                .GetBlobClient(input.BlobName);

            using var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(GetBlobContentAsBuffer), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }

    /// <summary>
    /// Writes a string to a blob in Azure Storage.
    /// </summary>
    /// <remarks>
    /// Validates the input content is provided before writing the content to the blob.
    /// </remarks>
    /// <param name="input">The blob storage information including the content, storage account, container, and blob name.</param>
    /// <param name="executionContext">The function execution context for execution-related functionality.</param>
    /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
    [Function(nameof(WriteStringToBlob))]
    public async Task WriteStringToBlob([ActivityTrigger] BlobStorageRequest input, FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(WriteStringToBlob), input);

        var validationResult = input.Validate(checkContent: true);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(WriteStringToBlob)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            var blobContainerClient = storageClientFactory
                .GetBlobServiceClient(input.StorageAccountName)
                .GetBlobContainerClient(input.ContainerName);

            // verify the container exists
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(input.BlobName);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input.Content));
            await blobClient.UploadAsync(stream, overwrite: true);

            logger.LogInformation(
                "Successfully uploaded content to {BlobName} in container {ContainerName}",
                input.BlobName,
                input.ContainerName);
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(WriteStringToBlob), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);
        }
    }

    /// <summary>
    /// Writes a byte array to a blob in Azure Storage.
    /// </summary>
    /// <param name="input">The blob storage information including the buffer byte array, storage account, container, and blob name.</param>
    /// <param name="executionContext">The function execution context for execution-related functionality.</param>
    /// <exception cref="ArgumentException">Thrown when the input is invalid.</exception>
    [Function(nameof(WriteBufferToBlob))]
    public async Task WriteBufferToBlob([ActivityTrigger] BlobStorageRequest input, FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(WriteBufferToBlob), input);

        var validationResult = input.Validate(checkContent: false);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(WriteBufferToBlob)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            logger.LogInformation(
                "Attempting to write buffer to {BlobName} in container {ContainerName}",
                input.BlobName,
                input.ContainerName);

            var blobContainerClient = storageClientFactory
                .GetBlobServiceClient(input.StorageAccountName)
                .GetBlobContainerClient(input.ContainerName);

            // verify the container exists
            await blobContainerClient.CreateIfNotExistsAsync();

            var blobClient = blobContainerClient.GetBlobClient(input.BlobName);

            using var stream = new MemoryStream(input.Buffer);
            await blobClient.UploadAsync(stream, overwrite: true);

            logger.LogInformation(
                "Successfully uploaded buffer to {BlobName} in container {ContainerName}",
                input.BlobName,
                input.ContainerName);
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(WriteBufferToBlob), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);
        }
    }
}
