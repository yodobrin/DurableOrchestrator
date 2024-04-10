using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureStorage;

public class CompoundStorageRequest : BaseWorkflowRequest
{
    [JsonPropertyName("sourceStorageRequest")]
    public BlobStorageRequest SourceStorageRequest { get; set; } = new BlobStorageRequest();

    [JsonPropertyName("destinationStorageRequest")]
    public BlobStorageRequest DestinationStorageRequest { get; set; } = new BlobStorageRequest();

    [JsonPropertyName("blobNames")]
    public List<string> BlobNames { get; set; } = new List<string>();

    public override ValidationResult Validate()
    {
        var result = new ValidationResult();

        if (SourceStorageRequest == null)
        {
            result.AddErrorMessage($"{nameof(SourceStorageRequest)} is missing.");
        }
        else
        {
            result.Merge(SourceStorageRequest.Validate(false), "Source storage request is missing.");            
        }

        if (DestinationStorageRequest == null)
        {
            result.AddErrorMessage($"{nameof(DestinationStorageRequest)} is missing.");
        }
        else
        {            
            result.Merge(DestinationStorageRequest.Validate(false), "Destination storage request is missing.");
        }
        if (BlobNames == null || BlobNames.Count == 0)
        {
            result.AddErrorMessage($"{nameof(BlobNames)} is missing or empty.");
        }

        return result;
    }
}