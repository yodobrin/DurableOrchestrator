param
(
    [Parameter(Mandatory = $true)]
    [string]$DeploymentName,
    [Parameter(Mandatory = $true)]
    [string]$Location
)

$InfrastructureOutput = (./infra/Deploy-Infrastructure.ps1 -DeploymentName $DeploymentName -Location $Location)

$ResourceGroupName = $InfrastructureOutput.resourceGroupInfo.value.name
$FunctionAppName = $InfrastructureOutput.functionAppInfo.value.name

Write-Host "Building and deploying Durable Orchestrator..."

Set-Location -Path $PSScriptRoot

$FunctionAppOutput = (./src/DurableOrchestrator/Deploy-App.ps1 -ResourceGroupName $ResourceGroupName -FunctionAppName $FunctionAppName)

return @{
    infrastructureInfo = $InfrastructureOutput
    functionAppInfo    = $FunctionAppOutput
}
