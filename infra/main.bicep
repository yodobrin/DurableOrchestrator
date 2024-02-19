targetScope = 'subscription'

type appSettingInfo = {
  name: string
  value: string
}

type appSettingsInfo = {
  values: appSettingInfo[]
  functionApp: string[]
}

@minLength(1)
@maxLength(64)
@description('Name of the workload which is used to generate a short unique hash used in all resources.')
param workloadName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Name of the resource group. If empty, a unique name will be generated.')
param resourceGroupName string = ''

@description('Tags for all resources.')
param tags object = {}

@description('Name of the Managed Identity. If empty, a unique name will be generated.')
param managedIdentityName string = ''
@description('Name of the Key Vault. If empty, a unique name will be generated.')
param keyVaultName string = ''
@description('Name of the Log Analytics Workspace. If empty, a unique name will be generated.')
param logAnalyticsWorkspaceName string = ''
@description('Name of the Application Insights. If empty, a unique name will be generated.')
param applicationInsightsName string = ''
@description('Name of the Storage Account. If empty, a unique name will be generated.')
param storageAccountName string = ''
@description('Name of the App Service Plan. If empty, a unique name will be generated.')
param appServicePlanName string = ''
@description('Name of the Function App. If empty, a unique name will be generated.')
param functionAppName string = ''

var abbrs = loadJsonContent('./abbreviations.json')
var roles = loadJsonContent('./roles.json')
var resourceToken = toLower(uniqueString(subscription().id, workloadName, location))

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourceGroup}${workloadName}'
  location: location
  tags: union(tags, {})
}

module managedIdentity './security/managed-identity.bicep' = {
  name: !empty(managedIdentityName) ? managedIdentityName : '${abbrs.managedIdentity}${resourceToken}'
  scope: resourceGroup
  params: {
    name: !empty(managedIdentityName) ? managedIdentityName : '${abbrs.managedIdentity}${resourceToken}'
    location: location
    tags: union(tags, {})
  }
}

resource keyVaultSecretsOfficer 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.keyVaultSecretsOfficer
}

module keyVault './security/key-vault.bicep' = {
  name: !empty(keyVaultName) ? keyVaultName : '${abbrs.keyVault}${resourceToken}'
  scope: resourceGroup
  params: {
    name: !empty(keyVaultName) ? keyVaultName : '${abbrs.keyVault}${resourceToken}'
    location: location
    tags: union(tags, {})
    roleAssignments: [
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: keyVaultSecretsOfficer.id
      }
    ]
  }
}

module logAnalyticsWorkspace './management_governance/log-analytics-workspace.bicep' = {
  name: !empty(logAnalyticsWorkspaceName) ? logAnalyticsWorkspaceName : '${abbrs.logAnalyticsWorkspace}${resourceToken}'
  scope: resourceGroup
  params: {
    name: !empty(logAnalyticsWorkspaceName) ? logAnalyticsWorkspaceName : '${abbrs.logAnalyticsWorkspace}${resourceToken}'
    location: location
    tags: union(tags, {})
  }
}

module applicationInsights './management_governance/application-insights.bicep' = {
  name: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.applicationInsights}${resourceToken}'
  scope: resourceGroup
  params: {
    name: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.applicationInsights}${resourceToken}'
    location: location
    tags: union(tags, {})
    logAnalyticsWorkspaceName: logAnalyticsWorkspace.outputs.name
  }
}

resource storageBlobDataContributor 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.storageBlobDataContributor
}

module storageAccount './storage/storage-account.bicep' = {
  name: !empty(storageAccountName) ? storageAccountName : '${abbrs.storageAccount}${resourceToken}'
  scope: resourceGroup
  params: {
    name: !empty(storageAccountName) ? storageAccountName : '${abbrs.storageAccount}${resourceToken}'
    location: location
    tags: union(tags, {})
    sku: {
      name: 'Standard_LRS'
    }
    keyVaultConfig: {
      keyVaultName: keyVault.outputs.name
      primaryKeySecretName: 'StorageAccountPrimaryKey'
      connectionStringSecretName: 'StorageAccountConnectionString'
    }
    roleAssignments: [
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: storageBlobDataContributor.id
      }
    ]
  }
}

resource storageAccountRef 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: storageAccount.name
  scope: resourceGroup
}

module appServicePlan './compute/app-service-plan.bicep' = {
  name: !empty(appServicePlanName) ? appServicePlanName : '${abbrs.appServicePlan}${resourceToken}'
  scope: resourceGroup
  params: {
    name: !empty(appServicePlanName) ? appServicePlanName : '${abbrs.appServicePlan}${resourceToken}'
    location: location
    tags: union(tags, {})
    sku: {
      tier: 'PremiumV3'
      name: 'P0V3'
    }
  }
}

module functionAppSettings './security/key-vault-secret-environment-variables.bicep' = {
  name: !empty(functionAppName) ? '${functionAppName}-settings' : '${abbrs.functionApp}${resourceToken}-settings'
  scope: resourceGroup
  params: {
    keyVaultVariables: [
      {
        name: 'StorageAccountConnectionString'
        keyVaultSecretUri: storageAccount.outputs.connectionStringSecretUri
      }
    ]
  }
}

module functionApp './compute/function-app.bicep' = {
  name: !empty(functionAppName) ? functionAppName : '${abbrs.functionApp}${resourceToken}'
  scope: resourceGroup
  params: {
    name: !empty(functionAppName) ? functionAppName : '${abbrs.functionApp}${resourceToken}'
    location: location
    tags: union(tags, {})
    functionAppIdentityId: managedIdentity.outputs.id
    appServicePlanId: appServicePlan.outputs.id
    appSettings: concat([
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://ghcr.io'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_USERNAME'
          value: ''
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_PASSWORD'
          value: null
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.outputs.connectionString
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.outputs.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccountRef.listKeys().keys[0].value}'
        }
        {
          name: 'KEY_VAULT_URI'
          value: keyVault.outputs.uri
        }
        {
          name: 'BlobSourceStorageAccountName'
          value: storageAccount.outputs.name
        }
        {
          name: 'BlobTargetStorageAccountName'
          value: storageAccount.outputs.name
        }
      ], functionAppSettings.outputs.environmentVariables)
  }
}

output resourceGroupInfo object = {
  id: resourceGroup.id
  name: resourceGroup.name
}

output functionAppInfo object = {
  id: functionApp.outputs.id
  name: functionApp.outputs.name
  host: functionApp.outputs.host
}
