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

@description('Whether to enable public network access. Defaults to Enabled.')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'
@description('Whether to disable local (key-based) authentication. Defaults to true.')
param disableLocalAuth bool = true
@description('Role assignments to create for the Cognitive Service instance.')
param roleAssignments roleAssignmentInfo[] = []

resource textAnalytics 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'TextAnalytics'
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
    name: 'S'
  }
}

resource assignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for roleAssignment in roleAssignments: {
    name: guid(textAnalytics.id, roleAssignment.principalId, roleAssignment.roleDefinitionId)
    scope: textAnalytics
    properties: {
      principalId: roleAssignment.principalId
      roleDefinitionId: roleAssignment.roleDefinitionId
      principalType: 'ServicePrincipal'
    }
  }
]

@description('ID for the deployed Text Analytics resource.')
output id string = textAnalytics.id
@description('Name for the deployed Text Analytics resource.')
output name string = textAnalytics.name
@description('Endpoint for the deployed Text Analytics resource.')
output endpoint string = textAnalytics.properties.endpoint
@description('Host for the deployed Text Analytics resource.')
output host string = split(textAnalytics.properties.endpoint, '/')[2]
