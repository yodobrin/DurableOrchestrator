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
