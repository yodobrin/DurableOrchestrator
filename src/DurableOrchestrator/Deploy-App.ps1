param
(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    [Parameter(Mandatory = $true)]
    [string]$FunctionAppName
)

Write-Host "Deploying Durable Orchestrator..."

Set-Location -Path $PSScriptRoot

if (-not (Test-Path "../../artifacts")) {
    New-Item -ItemType Directory -Path "../../artifacts"
}
else {
    Remove-Item -Recurse -Force -Path "../../artifacts"
}

dotnet build ./DurableOrchestrator.csproj -c Release
dotnet publish ./DurableOrchestrator.csproj -c Release -o "../../artifacts/DurableOrchestrator"

$FunctionAppZipPath = "../../artifacts/DurableOrchestrator.zip"
Compress-Archive -Path "../../artifacts/DurableOrchestrator/*" -DestinationPath $FunctionAppZipPath -Force

az functionapp deployment source config-zip -g $ResourceGroupName -n $FunctionAppName --src $FunctionAppZipPath
