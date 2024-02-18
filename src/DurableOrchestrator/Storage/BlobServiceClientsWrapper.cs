using Azure.Storage.Blobs;

namespace DurableOrchestrator.Storage;
public class BlobServiceClientsWrapper
{
    public BlobServiceClient SourceClient { get; }
    public BlobServiceClient TargetClient { get; }

    public BlobServiceClientsWrapper(BlobServiceClient sourceClient, BlobServiceClient targetClient)
    {
        SourceClient = sourceClient;
        TargetClient = targetClient;
    }
 

}
