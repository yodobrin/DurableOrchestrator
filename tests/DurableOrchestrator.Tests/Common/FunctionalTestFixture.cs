using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;

namespace DurableOrchestrator.Tests.Common;

public class FunctionalTestFixture : IAsyncLifetime
{
    protected const string WorkloadName = "durable-orchestrator-tests";
    protected const string Location = "westeurope";

    public async Task InitializeAsync()
    {
        await CreateAzureEnvironmentAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task CreateAzureEnvironmentAsync()
    {
        var armClient = new ArmClient(new DefaultAzureCredential());

        var subscription = await armClient.GetDefaultSubscriptionAsync();

        var templateContent = await File.ReadAllTextAsync(Path.Combine("..", "infra", "main.bicep"));

        var deploymentContent = new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
        {
            Template = BinaryData.FromString(templateContent),
            Parameters = BinaryData.FromObjectAsJson(new
            {
                workloadName = new 
                {
                    value = WorkloadName
                },
                location = new
                {
                    value = Location
                }
            })
        });

        var response = await subscription.GetArmDeployments().CreateOrUpdateAsync(WaitUntil.Completed, WorkloadName, deploymentContent);

        if (response.HasCompleted && response.Value != null)
        {
            Console.WriteLine("Deployment completed successfully");
        }
        else
        {
            Console.WriteLine("Deployment failed");
        }
    }
}
