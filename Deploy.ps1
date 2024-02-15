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

dotnet build ./src/DurableOrchestrator/DurableOrchestrator.csproj -c Release
dotnet publish ./src/DurableOrchestrator/DurableOrchestrator.csproj -c Release -o "./artifacts/DurableOrchestrator"

$FunctionAppZipPath = "./artifacts/DurableOrchestrator.zip"
Compress-Archive -Path "./artifacts/DurableOrchestrator/*" -DestinationPath $FunctionAppZipPath -Force

az functionapp deployment source config-zip -g $ResourceGroupName -n $FunctionAppName --src $FunctionAppZipPath
