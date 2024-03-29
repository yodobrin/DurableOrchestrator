using Azure;
using Azure.Data.Tables;
using DurableOrchestrator.Core;
using DurableOrchestrator.Core.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DurableOrchestrator.AzureStorage;

[ActivitySource]
public class TableStorageActivities(
    StorageClientFactory storageClientFactory,
    ILogger<TableStorageActivities> logger)
    : BaseActivity(nameof(TableStorageActivities))
{
    [Function(nameof(WriteEntityToTable))]
    public async Task<Response?> WriteEntityToTable(
        [ActivityTrigger] TableStorageRequest input,
        FunctionContext executionContext)
    {
        using var span = StartActiveSpan(nameof(WriteEntityToTable), input);

        var validationResult = input.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"{nameof(WriteEntityToTable)}::{nameof(input)} is invalid. {validationResult}");
        }

        try
        {
            var tableClient = storageClientFactory
                .GetTableServiceClient(input.StorageAccountName)
                .GetTableClient(input.TableName);

            // verify the table exists
            await tableClient.CreateIfNotExistsAsync();

            return await tableClient.AddEntityAsync(input.Entity!);
        }
        catch (Exception ex)
        {
            logger.LogError("{Activity} failed. {Error}", nameof(WriteEntityToTable), ex.Message);

            span.SetStatus(Status.Error);
            span.RecordException(ex);

            throw;
        }
    }
}
