using System.Net.Http.Json;
using DurableOrchestrator.Tests.Common;
using Flurl.Http;
using static Xunit.Assert;

namespace DurableOrchestrator.Tests;

[Collection(nameof(FunctionalTestFixtureCollection))]
public class CopyBlobWorkflowTests(FunctionalTestFixture fixture) : FunctionalTest(fixture)
{
    [Fact]
    public async Task ExtractDataFromDocument()
    {
        var client = new FlurlClient(new HttpClient());

        var document = new
        {
            id = Guid.NewGuid().ToString(),
            content = "Hello, World!"
        };

        // Arrange
        //var client = GetFlurlClient();

        //// Act
        //var response = await client.Request($"api/{CopyBlobWorkflow.OrchestrationTriggerName}")
        //    .PostJsonAsync(document);

        //// Assert
        //response.ResponseMessage.EnsureSuccessStatusCode();
    }
}
