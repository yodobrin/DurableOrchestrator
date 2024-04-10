using System.Text.Json.Serialization;
using DurableOrchestrator.Core;

namespace DurableOrchestrator.AzureStorage;

public class BlobPagination : BlobStorageRequest
{
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 100;

    [JsonPropertyName("continuationToken")]
    public string ContinuationToken { get; set; } = string.Empty;

    [JsonPropertyName("blobNames")]
    public List<string> BlobNames { get; set; } = new List<string>();

    // public BlobPagination(string ContinuationToken, int PageSize, List<string> BlobNames)
    // {
    //     this.ContinuationToken = ContinuationToken;
    //     this.PageSize = PageSize;
    //     this.BlobNames = BlobNames;
    // }

    public override ValidationResult Validate()
    {
        var result = base.Validate();

        if (PageSize <= 0)
        {
            result.AddErrorMessage($"{nameof(PageSize)} must be greater than 0.");
        }

        return result;
    }

}