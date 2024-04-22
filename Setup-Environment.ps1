<#
.SYNOPSIS
    Deploys the infrastructure and applications required to run the solution.
.PARAMETER DeploymentName
	The name of the deployment.
.PARAMETER Location
    The location of the deployment.
.PARAMETER IsLocal
    Whether the deployment is for a local development environment or complete Azure deployment.
.PARAMETER SkipInfrastructure
    Whether to skip the infrastructure deployment. Requires InfrastructureOutputs.json to exist in the infra directory.
.EXAMPLE
    .\Setup-Environment.ps1 -DeploymentName 'my-deployment' -Location 'westeurope' -IsLocal $false -SkipInfrastructure $false
.NOTES
    Author: James Croft
    Date: 2024-04-22
#>

param
(
    [Parameter(Mandatory = $true)]
    [string]$DeploymentName,
    [Parameter(Mandatory = $true)]
    [string]$Location,
    [Parameter(Mandatory = $true)]
    [string]$IsLocal,
    [Parameter(Mandatory = $true)]
    [string]$SkipInfrastructure
)

Write-Host "Starting environment setup..."

if ($SkipInfrastructure -eq '$false' || -not (Test-Path -Path './infra/InfrastructureOutputs.json')) {
    Write-Host "Deploying infrastructure..."
    $InfrastructureOutputs = (./infra/Deploy-Infrastructure.ps1 `
            -DeploymentName $DeploymentName `
            -Location $Location `
            -ErrorAction Stop)
}
else {
    Write-Host "Skipping infrastructure deployment. Using existing outputs..."
    $InfrastructureOutputs = Get-Content -Path './infra/InfrastructureOutputs.json' -Raw | ConvertFrom-Json
}

if ($IsLocal -eq '$true') {
    $ResourceGroupName = $InfrastructureOutputs.resourceGroupInfo.value.name
    $KeyVaultUrl = $InfrastructureOutputs.keyVaultInfo.value.uri
    $DocumentIntelligenceEndpoint = $InfrastructureOutputs.documentIntelligenceInfo.value.endpoint
    $TextAnalyticsEndpoint = $InfrastructureOutputs.textAnalyticsInfo.value.endpoint
    $OpenAIEndpoint = $InfrastructureOutputs.openAIInfo.value.endpoint
    $OpenAIEmbeddingDeployment = $InfrastructureOutputs.openAIInfo.value.embeddingModelDeploymentName
    $OpenAICompletionDeployment = $InfrastructureOutputs.openAIInfo.value.completionModelDeploymentName
    $EventHubNamespaceName = $InfrastructureOutputs.eventHubNamespaceInfo.value.name

    $JsonToParquetEventHubConnectionString = (az eventhubs namespace authorization-rule keys list `
            --resource-group $ResourceGroupName `
            --namespace-name $EventHubNamespaceName `
            --name 'RootManageSharedAccessKey' `
            --query primaryConnectionString `
            --output tsv)

    Write-Host "Updating local settings..."

    $LocalSettingsPath = './src/DurableOrchestrator/local.settings.json'
    $LocalSettings = Get-Content -Path $LocalSettingsPath -Raw | ConvertFrom-Json
    $LocalSettings.Values.KEY_VAULT_URL = $KeyVaultUrl
    $LocalSettings.Values.DOCUMENT_INTELLIGENCE_ENDPOINT = $DocumentIntelligenceEndpoint
    $LocalSettings.Values.TEXT_ANALYTICS_ENDPOINT = $TextAnalyticsEndpoint
    $LocalSettings.Values.OPENAI_ENDPOINT = $OpenAIEndpoint
    $LocalSettings.Values.OPENAI_EMBEDDING_MODEL_DEPLOYMENT = $OpenAIEmbeddingDeployment
    $LocalSettings.Values.OPENAI_COMPLETION_MODEL_DEPLOYMENT = $OpenAICompletionDeployment
    $LocalSettings.Values.JSON2PARQUET_EVENTHUB = $JsonToParquetEventHubConnectionString
    $LocalSettings | ConvertTo-Json | Out-File -FilePath $LocalSettingsPath -Encoding utf8

    Write-Host "Starting local environment..."

    docker-compose up
}
else {
    Write-Host "Deploying Durable Orchestrator app..."

    ./infra/apps/DurableOrchestrator/Deploy-App.ps1 `
        -InfrastructureOutputsPath './infra/InfrastructureOutputs.json' `
        -ErrorAction Stop
}
