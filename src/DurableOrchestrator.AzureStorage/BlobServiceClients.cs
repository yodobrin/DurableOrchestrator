using Azure.Storage.Blobs;

namespace DurableOrchestrator.AzureStorage;

/// <summary>
/// Defines a wrapper for a source and target <see cref="BlobServiceClient"/>.
/// </summary>
/// <param name="source">The source <see cref="BlobServiceClient"/> used for retrieving blob content.</param>
/// <param name="target">The target <see cref="BlobServiceClient"/> used for writing blob content.</param>
public class BlobServiceClients(BlobServiceClient source, BlobServiceClient target)
{
    /// <summary>
    /// Gets the source <see cref="BlobServiceClient"/> used for retrieving blob content.
    /// </summary>
    public BlobServiceClient Source { get; } = source;

    /// <summary>
    /// Gets the target <see cref="BlobServiceClient"/> used for writing blob content.
    /// </summary>
    public BlobServiceClient Target { get; } = target;
}
