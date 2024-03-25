targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the workload which is used to generate a short unique hash used in all resources.')
param workloadName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Tags for all resources.')
param tags object = {}

@description('Name of the container image for the Durable Orchestrator container.')
param durableOrchestratorContainerImage string

@description('Primary location for the deployed Document Intelligence service. Default is westeurope for latest preview support.')
param documentIntelligenceLocation string = 'westeurope'
@description('Primary location for the deployed OpenAI service. Default is francecentral for latest preview support.')
param openAILocation string = 'francecentral'
@description('Name of the GPT model deployment to use for the OpenAI service. Default is gpt-35-turbo.')
param gptModelDeploymentName string = 'gpt-35-turbo'

var abbrs = loadJsonContent('../../abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, workloadName, location))
var documentIntelligenceResourceToken = toLower(uniqueString(
  subscription().id,
  workloadName,
  documentIntelligenceLocation
))
var openAIResourceToken = toLower(uniqueString(subscription().id, workloadName, openAILocation))

resource managedIdentityRef 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' existing = {
  name: '${abbrs.managedIdentity}${resourceToken}'
}

resource containerRegistryRef 'Microsoft.ContainerRegistry/registries@2022-12-01' existing = {
  name: '${abbrs.containerRegistry}${resourceToken}'
}

resource applicationInsightsRef 'Microsoft.Insights/components@2020-02-02' existing = {
  name: '${abbrs.applicationInsights}${resourceToken}'
}

resource keyVaultRef 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: '${abbrs.keyVault}${resourceToken}'
}

resource storageAccountRef 'Microsoft.Storage/storageAccounts@2022-09-01' existing = {
  name: '${abbrs.storageAccount}${resourceToken}'
}

resource textAnalyticsRef 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' existing = {
  name: '${abbrs.languageService}${resourceToken}'
}

resource documentIntelligenceRef 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' existing = {
  name: '${abbrs.documentIntelligence}${documentIntelligenceResourceToken}'
}

resource openAIRef 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' existing = {
  name: '${abbrs.openAIService}${openAIResourceToken}'
}

resource containerAppsEnvironmentRef 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: '${abbrs.containerAppsEnvironment}${resourceToken}'
}

var durableOrchestratorToken = toLower(uniqueString(subscription().id, workloadName, location, 'durable-orchestrator'))

module durableOrchestratorApp '../../containers/container-app.bicep' = {
  name: '${abbrs.containerApp}${durableOrchestratorToken}'
  params: {
    name: '${abbrs.containerApp}${durableOrchestratorToken}'
    location: location
    tags: union(tags, { App: 'durable-orchestrator' })
    containerAppsEnvironmentId: containerAppsEnvironmentRef.id
    containerAppIdentityId: managedIdentityRef.id
    imageInContainerRegistry: true
    containerRegistryName: containerRegistryRef.name
    containerImageName: durableOrchestratorContainerImage
    containerIngress: {
      external: true
      targetPort: 80
      transport: 'auto'
      allowInsecure: false
    }
    containerScale: {
      minReplicas: 1
      maxReplicas: 1
    }
    environmentVariables: [
      {
        name: 'FUNCTIONS_EXTENSION_VERSION'
        value: '~4'
      }
      {
        name: 'FUNCTIONS_WORKER_RUNTIME'
        value: 'dotnet-isolated'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: applicationInsightsRef.properties.ConnectionString
      }
      {
        name: 'AzureWebJobsStorage'
        value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountRef.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccountRef.listKeys().keys[0].value}'
      }
      {
        name: 'MANAGED_IDENTITY_CLIENT_ID'
        value: managedIdentityRef.properties.clientId
      }
      {
        name: 'KEY_VAULT_URL'
        value: keyVaultRef.properties.vaultUri
      }
      {
        name: 'TEXT_ANALYTICS_ENDPOINT'
        value: textAnalyticsRef.properties.endpoint
      }
      {
        name: 'DOCUMENT_INTELLIGENCE_ENDPOINT'
        value: documentIntelligenceRef.properties.endpoint
      }
      {
        name: 'OPENAI_ENDPOINT'
        value: openAIRef.properties.endpoint
      }
      {
        name: 'OPENAI_MODEL_DEPLOYMENT_NAME'
        value: gptModelDeploymentName
      }
      {
        name: 'BlobSourceStorageAccountName'
        value: storageAccountRef.name
      }
      {
        name: 'BlobTargetStorageAccountName'
        value: storageAccountRef.name
      }
      {
        name: 'WEBSITE_HOSTNAME'
        value: 'localhost'
      }
    ]
  }
}

output durableOrchestratorInfo object = {
  id: durableOrchestratorApp.outputs.id
  name: durableOrchestratorApp.outputs.name
  fqdn: durableOrchestratorApp.outputs.fqdn
  url: durableOrchestratorApp.outputs.url
  latestRevisionFqdn: durableOrchestratorApp.outputs.latestRevisionFqdn
  latestRevisionUrl: durableOrchestratorApp.outputs.latestRevisionUrl
}
