using Azure.Identity;
using System.IO;
using System.Text;
using Azure.Storage.Blobs;




namespace event_processing
{
    public static class BlobStorageActivities
    {
        [FunctionName("GetBlobContentAsString")]
        public static async Task<string?> GetBlobContentAsString([ActivityTrigger] BlobStorageInfo input, ILogger log)
        {
            if (!ValidateInput(input, log, checkContent: false)) return null;

            try
            {
                var blobClient = GetBlobClient(input, log);
                if (blobClient == null) return null; // GetBlobClient logs the error

                var downloadResult = await blobClient.DownloadContentAsync();
                return downloadResult.Value.Content.ToString();
            }
            catch (Exception ex)
            {
                log.LogError($"Error in GetBlobContentAsString: {ex.Message}");
                return null;
            }
        }

        [FunctionName("GetBlobContentAsBuffer")]
        public static async Task<byte[]?> GetBlobContentAsBuffer([ActivityTrigger] BlobStorageInfo input, ILogger log)
        {
            if (!ValidateInput(input, log, checkContent: false)) return null;

            try
            {
                var blobClient = GetBlobClient(input, log);
                if (blobClient == null) return null; // GetBlobClient logs the error

                using (var memoryStream = new MemoryStream())
                {
                    await blobClient.DownloadToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in GetBlobContentAsBuffer: {ex.Message}");
                return null;
            }
        }

        [FunctionName("WriteStringToBlob")]
        public static async Task WriteStringToBlob([ActivityTrigger] BlobStorageInfo input, ILogger log)
        {
            if (!ValidateInput(input, log)) return;

            try
            {
                var blobClient = GetBlobClient(input, log);
                if (blobClient == null) return; // GetBlobClient logs the error

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(input.Content)))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                    log.LogInformation($"Successfully uploaded content to blob: {input.BlobName} in container: {input.ContainerName}");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in WriteStringToBlob: {ex.Message}");
            }
        }

        [FunctionName("WriteBufferToBlob")]
        public static async Task WriteBufferToBlob([ActivityTrigger] BlobStorageInfo input, ILogger log)
        {
            if (!ValidateInput(input, log, checkContent: false)) return;

            try
            {
                var blobClient = GetBlobClient(input, log);
                if (blobClient == null) return; // GetBlobClient logs the error

                using (var stream = new MemoryStream(input.Buffer))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                    log.LogInformation($"Successfully uploaded buffer to blob: {input.BlobName} in container: {input.ContainerName}");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in WriteBufferToBlob: {ex.Message}");
            }
        }
        private static BlobClient GetBlobClient(BlobStorageInfo input, ILogger log)
        {
            try
            {
                BlobClient blobClient;
                if (string.IsNullOrWhiteSpace(input.BlobUri))
                {
                    if (string.IsNullOrWhiteSpace(input.ContainerName) || string.IsNullOrWhiteSpace(input.BlobName))
                    {
                        log.LogError("Container name or blob name is not provided, and no BlobUri is specified.");
                        throw new ArgumentException("Container name or blob name is not provided, and no BlobUri is specified.");                        
                    }
                    var blobServiceClient = new BlobServiceClient(new Uri($"https://{input.StorageAccountName}.blob.core.windows.net"), new DefaultAzureCredential());
                    var blobContainerClient = blobServiceClient.GetBlobContainerClient(input.ContainerName);
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
                log.LogError($"Failed to create BlobClient: {ex.Message}");
                throw;

            }
        }
        private static bool ValidateInput(BlobStorageInfo input, ILogger log, bool checkContent = true)
        {
            if (input == null)
            {
                log.LogError("Input is null.");
                return false;
            }

            if (checkContent && string.IsNullOrWhiteSpace(input.Content))
            {
                log.LogWarning("Content is null or whitespace.");
                return false;
            }

            return true;
        }

    }
}
