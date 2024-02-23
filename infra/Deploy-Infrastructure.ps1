<#
.SYNOPSIS
    Deploys the core infrastructure for the Durable Orchestrator to an Azure subscription.
.DESCRIPTION
    This script initiates the deployment of the main.bicep template to the current default Azure subscription,
    determined by the Azure CLI. The deployment name and location are required parameters.

	Follow the instructions in the DeploymentGuide.md file at the root of this project to understand what this
    script will deploy to your Azure subscription, and the step-by-step on how to run it.
.PARAMETER DeploymentName
    The name of the deployment to create in an Azure subscription.
.PARAMETER Location
    The location to deploy the Azure resources to.
.EXAMPLE
    .\Deploy-Infrastructure.ps1 -DeploymentName "my-workflows" -Location "westeurope"
.NOTES
    Author: James Croft
    Last Updated: 2024-02-23
#>

param
(
    [Parameter(Mandatory = $true)]
    [string]$DeploymentName,
    [Parameter(Mandatory = $true)]
    [string]$Location
)

Write-Host "Deploying infrastructure..."

Set-Location -Path $PSScriptRoot

az --version

$DeploymentOutputs = (az deployment sub create --name $DeploymentName --location $Location --template-file './main.bicep' --parameters './main.parameters.json' --parameters workloadName=$DeploymentName --parameters location=$Location --query properties.outputs -o json) | ConvertFrom-Json
$DeploymentOutputs | ConvertTo-Json | Out-File -FilePath './InfrastructureOutputs.json' -Encoding utf8

return $DeploymentOutputs
