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

$ContainerName = "durable-orchestrator"
$ContainerVersion = (Get-Date -Format "yyMMddHHmm")
$ContainerImageName = "${ContainerName}:${ContainerVersion}"
$AzureContainerImageName = "${ContainerRegistryName}.azurecr.io/${ContainerImageName}"

az --version

Write-Host "Building ${ContainerImageName} image..."

az acr login --name $ContainerRegistryName

docker build -t $ContainerImageName -f ../../../src/DurableOrchestrator/Dockerfile ../../../src/DurableOrchestrator/.

Write-Host "Pushing ${ContainerImageName} image..."

docker tag $ContainerImageName $AzureContainerImageName
docker push $AzureContainerImageName

Write-Host "Clean up old images..."

az acr run --cmd "acr purge --filter '${ContainerName}:.*' --untagged --ago 1h" --registry $ContainerRegistryName /dev/null

Write-Host "Deploying container app..."

$DeploymentOutputs = (az deployment group create --name durable-orchestrator-app --resource-group $ResourceGroupName --template-file './app.bicep' --parameters '../../main.parameters.json' --parameters workloadName=$WorkloadName --parameters location=$Location --parameters durableOrchestratorContainerImage=$ContainerImageName --query properties.outputs -o json) | ConvertFrom-Json
$DeploymentOutputs | ConvertTo-Json | Out-File -FilePath './AppOutputs.json' -Encoding utf8

return $DeploymentOutputs
