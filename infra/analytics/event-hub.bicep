@description('Name of the resource.')
param name string

type keyVaultSecretsInfo = {
  keyVaultName: string
  connectionString: string
}

@description('Name for the Event Hub Namespace associated with the Event Hub.')
param eventHubNamespaceName string
@description('Number of days to retain events in the Event Hub. Default is 1 day.')
param retentionInDays int = 1
@description('Number of partitions in the Event Hub. Default is 1 partition.')
param partitionCount int = 1
@description('Properties to store in a Key Vault.')
param keyVaultConfig keyVaultSecretsInfo = {
  keyVaultName: ''
  connectionString: ''
}

resource eventHubNamespace 'Microsoft.EventHub/namespaces@2023-01-01-preview' existing = {
  name: eventHubNamespaceName
}

resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2023-01-01-preview' = {
  name: name
  parent: eventHubNamespace
  properties: {
    messageRetentionInDays: retentionInDays
    partitionCount: partitionCount
  }

  resource listenSendAuthorization 'authorizationRules' = {
    name: 'ListenSend'
    properties: {
      rights: [
        'Listen'
        'Send'
      ]
    }
  }
}

module connectionString '../security/key-vault-secret.bicep' =
  if (!empty(keyVaultConfig.connectionString)) {
    name: '${keyVaultConfig.connectionString}-secret'
    params: {
      keyVaultName: keyVaultConfig.keyVaultName
      name: keyVaultConfig.connectionString
      value: eventHub::listenSendAuthorization.listKeys().primaryConnectionString
    }
  }

@description('ID for the deployed Event Hub resource.')
output id string = eventHub.id
@description('Name for the deployed Event Hub resource.')
output name string = eventHub.name
@description('Key Vault secret URI to the connection string for the deployed Event Hub resource.')
output connectionStringSecretUri string = !empty(keyVaultConfig.connectionString) ? connectionString.outputs.uri : ''
