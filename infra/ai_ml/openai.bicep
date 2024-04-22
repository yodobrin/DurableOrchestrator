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

@description('List of model deployments.')
param deployments array = []
@description('Whether to enable public network access. Defaults to Enabled.')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'
@description('Role assignments to create for the OpenAI instance.')
param roleAssignments roleAssignmentInfo[] = []

resource openAIService 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'OpenAI'
  properties: {
    customSubDomainName: toLower(name)
    disableLocalAuth: true
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
