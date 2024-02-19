@description('Name of the resource.')
param name string
@description('Location to deploy the resource. Defaults to the location of the resource group.')
param location string = resourceGroup().location
@description('Tags for the resource.')
param tags object = {}

type skuInfo = {
    tier: string
    name: string
}

@description('App Service Plan SKU. Defaults to Y1.')
param sku skuInfo = {
    tier: 'Dynamic'
    name: 'Y1'
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
    name: name
    location: location
    tags: tags
    sku: sku
    kind: 'linux'
    properties: {
        reserved: true
    }
}

@description('ID for the deployed App Service Plan resource.')
output id string = appServicePlan.id
@description('Name for the deployed App Service Plan resource.')
output name string = appServicePlan.name
