targetScope = 'subscription'

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

@description('Primary location for the Document Intelligence service. Default is westeurope for latest preview support.')
param documentIntelligenceLocation string = 'westeurope'
@description('Primary location for the OpenAI service. Default is francecentral for latest preview support.')
param openAILocation string = 'francecentral'

var abbrs = loadJsonContent('./abbreviations.json')
var roles = loadJsonContent('./roles.json')
var resourceToken = toLower(uniqueString(subscription().id, workloadName, location))
var documentIntelligenceResourceToken = toLower(uniqueString(
  subscription().id,
  workloadName,
  documentIntelligenceLocation
))
var openAIResourceToken = toLower(uniqueString(subscription().id, workloadName, openAILocation))

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourceGroup}${workloadName}'
  location: location
  tags: union(tags, {})
}

module managedIdentity './security/managed-identity.bicep' = {
  name: '${abbrs.managedIdentity}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.managedIdentity}${resourceToken}'
    location: location
    tags: union(tags, {})
  }
}

resource keyVaultSecretsOfficer 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.keyVaultSecretsOfficer
}

module keyVault './security/key-vault.bicep' = {
  name: '${abbrs.keyVault}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.keyVault}${resourceToken}'
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

module eventHubNamespace './analytics/event-hub-namespace.bicep' = {
  name: '${abbrs.eventHubsNamespace}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.eventHubsNamespace}${resourceToken}'
    location: location
    tags: union(tags, {})
    sku: {
      name: 'Basic'
    }
  }
}

resource containerRegistryPull 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.acrPull
}

module containerRegistry 'containers/container-registry.bicep' = {
  name: '${abbrs.containerRegistry}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.containerRegistry}${resourceToken}'
    location: location
    tags: union(tags, {})
    sku: {
      name: 'Basic'
    }
    adminUserEnabled: true
    roleAssignments: [
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: containerRegistryPull.id
      }
    ]
  }
}

resource cognitiveServicesLanguageOwner 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.cognitiveServicesLanguageOwner
}

module textAnalytics './ai_ml/text-analytics.bicep' = {
  name: '${abbrs.languageService}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.languageService}${resourceToken}'
    location: location
    tags: union(tags, {})
    roleAssignments: [
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: cognitiveServicesLanguageOwner.id
      }
    ]
  }
}

resource cognitiveServicesUser 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.cognitiveServicesUser
}

module documentIntelligence './ai_ml/document-intelligence.bicep' = {
  name: '${abbrs.documentIntelligence}${documentIntelligenceResourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.documentIntelligence}${documentIntelligenceResourceToken}'
    location: documentIntelligenceLocation
    tags: union(tags, {})
    roleAssignments: [
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: cognitiveServicesUser.id
      }
    ]
  }
}

resource cognitiveServicesOpenAIUser 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: resourceGroup
  name: roles.cognitiveServicesOpenAIUser
}

var completionModelDeploymentName = 'gpt-35-turbo'
var embeddingModelDeploymentName = 'text-embedding-ada-002'

module openAI './ai_ml/openai.bicep' = {
  name: '${abbrs.openAIService}${openAIResourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.openAIService}${openAIResourceToken}'
    location: openAILocation
    tags: union(tags, {})
    deployments: [
      {
        name: completionModelDeploymentName
        model: {
          format: 'OpenAI'
          name: 'gpt-35-turbo'
          version: '1106'
        }
        sku: {
          name: 'Standard'
          capacity: 30
        }
      }
      {
        name: embeddingModelDeploymentName
        model: {
          format: 'OpenAI'
          name: 'text-embedding-ada-002'
          version: '2'
        }
        sku: {
          name: 'Standard'
          capacity: 30
        }
      }
    ]
    roleAssignments: [
      {
        principalId: managedIdentity.outputs.principalId
        roleDefinitionId: cognitiveServicesOpenAIUser.id
      }
    ]
  }
}

module logAnalyticsWorkspace './management_governance/log-analytics-workspace.bicep' = {
  name: '${abbrs.logAnalyticsWorkspace}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.logAnalyticsWorkspace}${resourceToken}'
    location: location
    tags: union(tags, {})
  }
}

module applicationInsights './management_governance/application-insights.bicep' = {
  name: '${abbrs.applicationInsights}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.applicationInsights}${resourceToken}'
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
  name: '${abbrs.storageAccount}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.storageAccount}${resourceToken}'
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

module containerAppsEnvironment 'containers/container-apps-environment.bicep' = {
  name: '${abbrs.containerAppsEnvironment}${resourceToken}'
  scope: resourceGroup
  params: {
    name: '${abbrs.containerAppsEnvironment}${resourceToken}'
    location: location
    tags: union(tags, {})
    logAnalyticsWorkspaceName: logAnalyticsWorkspace.outputs.name
  }
}

output resourceGroupInfo object = {
  id: resourceGroup.id
  name: resourceGroup.name
  location: resourceGroup.location
  workloadName: workloadName
}

output managedIdentityInfo object = {
  id: managedIdentity.outputs.id
  name: managedIdentity.outputs.name
  principalId: managedIdentity.outputs.principalId
  clientId: managedIdentity.outputs.clientId
}

output keyVaultInfo object = {
  id: keyVault.outputs.id
  name: keyVault.outputs.name
  uri: keyVault.outputs.uri
}

output eventHubNamespaceInfo object = {
  id: eventHubNamespace.outputs.id
  name: eventHubNamespace.outputs.name
}

output containerRegistryInfo object = {
  id: containerRegistry.outputs.id
  name: containerRegistry.outputs.name
  loginServer: containerRegistry.outputs.loginServer
}

output textAnalyticsInfo object = {
  id: textAnalytics.outputs.id
  name: textAnalytics.outputs.name
  endpoint: textAnalytics.outputs.endpoint
  host: textAnalytics.outputs.host
}

output documentIntelligenceInfo object = {
  id: documentIntelligence.outputs.id
  name: documentIntelligence.outputs.name
  endpoint: documentIntelligence.outputs.endpoint
  host: documentIntelligence.outputs.host
}

output openAIInfo object = {
  id: openAI.outputs.id
  name: openAI.outputs.name
  endpoint: openAI.outputs.endpoint
  host: openAI.outputs.host
  completionModelDeploymentName: completionModelDeploymentName
  embeddingModelDeploymentName: embeddingModelDeploymentName
}

output logAnalyticsWorkspaceInfo object = {
  id: logAnalyticsWorkspace.outputs.id
  name: logAnalyticsWorkspace.outputs.name
  customerId: logAnalyticsWorkspace.outputs.customerId
}

output applicationInsightsInfo object = {
  id: applicationInsights.outputs.id
  name: applicationInsights.outputs.name
}

output storageAccountInfo object = {
  id: storageAccount.outputs.id
  name: storageAccount.outputs.name
  primaryKeySecretUri: storageAccount.outputs.primaryKeySecretUri
  connectionStringSecretUri: storageAccount.outputs.connectionStringSecretUri
}

output containerAppsEnvironmentInfo object = {
  id: containerAppsEnvironment.outputs.id
  name: containerAppsEnvironment.outputs.name
  defaultDomain: containerAppsEnvironment.outputs.defaultDomain
  staticIp: containerAppsEnvironment.outputs.staticIp
}
