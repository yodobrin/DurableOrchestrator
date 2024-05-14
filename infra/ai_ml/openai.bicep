@description('Name of the resource.')
param name string
@description('Location to deploy the resource. Defaults to the location of the resource group.')
param location string = resourceGroup().location
@description('Tags for the resource.')
param tags object = {}

type roleAssignmentInfo = {
  roleDefinitionId: string
  principalId: string
}

@description('Information about a model deployment for an OpenAI resource.')
type modelDeploymentInfo = {
  @description('Name for the model deployment. Must be unique within the OpenAI resource.')
  name: string
  @description('Information about the model to deploy.')
  model: {
    @description('Format of the model. Expects "OpenAI".')
    format: string
    @description('ID of the model, e.g., gpt-35-turbo. For more information on model IDs: https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models')
    name: string
    @description('Version of the model, e.g., 0125. For more information on model versions: https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models')
    version: string
  }?
  @description('Name of the content filter policy to apply to the model deployment.')
  raiPolicyName: string?
  @description('Sizing for the model deployment.')
  sku: {
    @description('Name of the SKU. Expects "Standard".')
    name: string
    @description('TPM quota allocation for the model deployment. For more information on model quota limits per region: https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/models')
    capacity: int
  }?
}

@description('List of model deployments.')
param deployments modelDeploymentInfo[] = []
@description('Whether to enable public network access. Defaults to Enabled.')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'
@description('Whether to disable local (key-based) authentication. Defaults to true.')
param disableLocalAuth bool = true
@description('Role assignments to create for the OpenAI instance.')
param roleAssignments roleAssignmentInfo[] = []

resource openAIService 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'OpenAI'
  properties: {
    customSubDomainName: toLower(name)
    disableLocalAuth: disableLocalAuth
    publicNetworkAccess: publicNetworkAccess
    networkAcls: {
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: []
    }
  }
  sku: {
    name: 'S0'
  }
}

@batchSize(1)
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = [
  for deployment in deployments: {
    parent: openAIService
    name: deployment.name
    properties: {
      model: contains(deployment, 'model') ? deployment.model : null
      raiPolicyName: contains(deployment, 'raiPolicyName') ? deployment.raiPolicyName : null
    }
    sku: contains(deployment, 'sku')
      ? deployment.sku
      : {
          name: 'Standard'
          capacity: 20
        }
  }
]

resource assignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for roleAssignment in roleAssignments: {
    name: guid(openAIService.id, roleAssignment.principalId, roleAssignment.roleDefinitionId)
    scope: openAIService
    properties: {
      principalId: roleAssignment.principalId
      roleDefinitionId: roleAssignment.roleDefinitionId
      principalType: 'ServicePrincipal'
    }
  }
]

@description('ID for the deployed Azure OpenAI Service.')
output id string = openAIService.id
@description('Name for the deployed Azure OpenAI Service.')
output name string = openAIService.name
@description('Endpoint for the deployed Azure OpenAI Service.')
output endpoint string = openAIService.properties.endpoint
@description('Host for the deployed Azure OpenAI Service.')
output host string = split(openAIService.properties.endpoint, '/')[2]
