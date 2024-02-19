param
(
    [Parameter(Mandatory = $true)]
    [string]$DeploymentName,
    [Parameter(Mandatory = $true)]
    [string]$Location,
    [Parameter(Mandatory = $true)]
    [string]$ContainerImageName
)

$InfrastructureOutput = (./infra/Deploy-Infrastructure.ps1 -DeploymentName $DeploymentName -Location $Location)

$ResourceGroupName = $InfrastructureOutput.resourceGroupInfo.value.name
$FunctionAppName = $InfrastructureOutput.functionAppInfo.value.name
$FunctionAppOutput = (az functionapp config container set --image $ContainerImageName --name $FunctionAppName --resource-group $ResourceGroupName --registry-username "" --registry-password "")

return @{
    infrastructureInfo = $InfrastructureOutput
    functionAppInfo    = $FunctionAppOutput
}
