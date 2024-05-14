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

@description('Document Intelligence SKU. Defaults to S0.')
param sku object = {
  name: 'S0'
}
@description('Whether to enable public network access. Defaults to Enabled.')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'
@description('Whether to disable local (key-based) authentication. Defaults to true.')
param disableLocalAuth bool = true
@description('Role assignments to create for the Document Intelligence instance.')
param roleAssignments roleAssignmentInfo[] = []

resource documentIntelligenceService 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: name
  location: location
  tags: tags
  kind: 'FormRecognizer'
  identity: {
    type: 'SystemAssigned'
  }
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
  sku: sku
}

resource assignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for roleAssignment in roleAssignments: {
    name: guid(documentIntelligenceService.id, roleAssignment.principalId, roleAssignment.roleDefinitionId)
    scope: documentIntelligenceService
    properties: {
      principalId: roleAssignment.principalId
      roleDefinitionId: roleAssignment.roleDefinitionId
      principalType: 'ServicePrincipal'
    }
  }
]

@description('ID for the deployed Document Intelligence resource.')
output id string = documentIntelligenceService.id
@description('Name for the deployed Document Intelligence resource.')
output name string = documentIntelligenceService.name
@description('Endpoint for the deployed Document Intelligence resource.')
output endpoint string = documentIntelligenceService.properties.endpoint
@description('Host for the deployed Document Intelligence resource.')
output host string = split(documentIntelligenceService.properties.endpoint, '/')[2]
@description('Identity principal ID for the deployed Document Intelligence resource.')
output systemIdentityPrincipalId string = documentIntelligenceService.identity.principalId
