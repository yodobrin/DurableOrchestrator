@description('Name of the resource.')
param name string
@description('Location to deploy the resource. Defaults to the location of the resource group.')
param location string = resourceGroup().location
@description('Tags for the resource.')
param tags object = {}

type vnetConfigInfo = {
  @description('Resource ID of a subnet for infrastructure components.')
  infrastructureSubnetId: string
  @description('Value indicating whether the environment only has an internal load balancer.')
  internal: bool
}

type logAnalyticsConfigInfo = {
  @description('Name of the Log Analytics workspace.')
  name: string
}

@description('Name of the Log Analytics Workspace to store application logs.')
param logAnalyticsWorkspaceName string
@description('Virtual network configuration for the environment.')
param vnetConfig vnetConfigInfo = {
  infrastructureSubnetId: ''
  internal: true
}
@description('Value indicating whether the environment is zone-redundant. Defaults to false.')
param zoneRedundant bool = false

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  name: logAnalyticsWorkspaceName
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    vnetConfiguration: !empty(vnetConfig.infrastructureSubnetId) ? vnetConfig : {}
    zoneRedundant: zoneRedundant
  }
}

@description('ID for the deployed Container Apps Environment resource.')
output id string = containerAppsEnvironment.id
@description('Name for the deployed Container Apps Environment resource.')
output name string = containerAppsEnvironment.name
@description('Default domain for the deployed Container Apps Environment resource.')
output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
@description('Static IP for the deployed Container Apps Environment resource.')
output staticIp string = containerAppsEnvironment.properties.staticIp
