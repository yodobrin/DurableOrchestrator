@description('Name of the resource.')
param name string
@description('Location to deploy the resource. Defaults to the location of the resource group.')
param location string = resourceGroup().location
@description('Tags for the resource.')
param tags object = {}

type skuInfo = {
    name: 'CapacityReservation' | 'Free' | 'LACluster' | 'PerGB2018' | 'PerNode' | 'Premium' | 'Standalone' | 'Standard'
}

type keyVaultSecretsInfo = {
    keyVaultName: string
    primarySharedKeySecretName: string
}

@description('Log Analytics Workspace SKU. Defaults to PerGB2018.')
param sku skuInfo = {
    name: 'PerGB2018'
}
@description('Retention period (in days) for the Log Analytics Workspace. Defaults to 30.')
param retentionInDays int = 30
@description('Properties to store in a Key Vault.')
param keyVaultConfig keyVaultSecretsInfo = {
    keyVaultName: ''
    primarySharedKeySecretName: ''
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
    name: name
    location: location
    tags: tags
    properties: {
        retentionInDays: retentionInDays
        features: {
            enableLogAccessUsingOnlyResourcePermissions: true
        }
        sku: sku
        publicNetworkAccessForIngestion: 'Enabled'
        publicNetworkAccessForQuery: 'Enabled'
    }
}

module primarySharedKeySecret '../security/key-vault-secret.bicep' = if (!empty(keyVaultConfig.primarySharedKeySecretName)) {
    name: '${keyVaultConfig.primarySharedKeySecretName}-secret'
    params: {
        keyVaultName: keyVaultConfig.keyVaultName
        name: keyVaultConfig.primarySharedKeySecretName
        value: logAnalyticsWorkspace.listKeys().primarySharedKey
    }
}

@description('ID for the deployed Log Analytics Workspace resource.')
output id string = logAnalyticsWorkspace.id
@description('Name for the deployed Log Analytics Workspace resource.')
output name string = logAnalyticsWorkspace.name
@description('Customer ID for the deployed Log Analytics Workspace resource.')
output customerId string = logAnalyticsWorkspace.properties.customerId
