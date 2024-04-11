<#
.SYNOPSIS
    Builds and deploys the Durable Orchestrator Azure Functions app to a Container App in an existing Azure environment.
.DESCRIPTION
    This script initiates the deployment of the app.bicep template to the current default Azure subscription,
    determined by the Azure CLI. The infra/Deploy-Infrastructure.ps1 script must be run first to deploy the
    core infrastructure to the Azure subscription required by this script.

	Follow the instructions in the DeploymentGuide.md file at the root of this project to understand what this
    script will deploy to your Azure subscription, and the step-by-step on how to run it.
.PARAMETER InfrastructureOutputsPath
    The path to the deployments outputs file from the infra/Deploy-Infrastructure.ps1 script.
.EXAMPLE
    .\Deploy-App.ps1 -InfrastructureOutputsPath "../../InfrastructureOutputs.json"
.NOTES
    Author: James Croft
    Last Updated: 2024-02-23
#>

param
(
    [Parameter(Mandatory = $true)]
    [string]$InfrastructureOutputsPath
)

Set-Location -Path $PSScriptRoot

Write-Host "Deploying durable-orchestrator..."

$InfrastructureOutputs = Get-Content -Path $InfrastructureOutputsPath -Raw | ConvertFrom-Json

$Location = $InfrastructureOutputs.resourceGroupInfo.value.location
$ResourceGroupName = $InfrastructureOutputs.resourceGroupInfo.value.name
$WorkloadName = $InfrastructureOutputs.resourceGroupInfo.value.workloadName
$ContainerRegistryName = $InfrastructureOutputs.containerRegistryInfo.value.name
$CompletionModelDeploymentName = $InfrastructureOutputs.openAIInfo.value.completionModelDeploymentName
$EmbeddingModelDeploymentName = $InfrastructureOutputs.openAIInfo.value.embeddingModelDeploymentName

$ContainerName = "durable-orchestrator"
$ContainerVersion = (Get-Date -Format "yyMMddHHmm")
$ContainerImageName = "${ContainerName}:${ContainerVersion}"
$AzureContainerImageName = "${ContainerRegistryName}.azurecr.io/${ContainerImageName}"

az --version

Write-Host "Building ${ContainerImageName} image..."

az acr login --name $ContainerRegistryName

docker build -t $ContainerImageName -f ../../../src/DurableOrchestrator/Dockerfile ../../../src/.

Write-Host "Pushing ${ContainerImageName} image..."

docker tag $ContainerImageName $AzureContainerImageName
docker push $AzureContainerImageName

Write-Host "Deploying container app..."

$DeploymentOutputs = (az deployment group create --name durable-orchestrator-app --resource-group $ResourceGroupName --template-file './app.bicep' `
        --parameters '../../main.parameters.json' `
        --parameters workloadName=$WorkloadName `
        --parameters location=$Location `
        --parameters durableOrchestratorContainerImage=$ContainerImageName `
        --parameters openAICompletionModelName=$CompletionModelDeploymentName `
        --parameters openAIEmbeddingModelName=$EmbeddingModelDeploymentName `
        --query properties.outputs -o json) | ConvertFrom-Json
$DeploymentOutputs | ConvertTo-Json | Out-File -FilePath './AppOutputs.json' -Encoding utf8

Write-Host "Clean up old images..."

az acr run --cmd "acr purge --filter '${ContainerName}:.*' --untagged --ago 1h" --registry $ContainerRegistryName /dev/null

return $DeploymentOutputs
