@description('Defines the output model for an environment variable for an application.')
type appEnvironmentVariableInfo = {
  @description('Name of the environment variable for the application.')
  name: string
  @description('Value of the environment variable represented as a Key Vault Secret URI.')
  value: string
}

@description('Defines the input model for an environment variable and the Key Vault Secret URI containing the value for the environment variable.')
type keyVaultEnvironmentVariableInfo = {
  @description('Name of the environment variable for the application.')
  name: string
  @description('URI of the Key Vault Secret containing the value for the environment variable.')
  keyVaultSecretUri: string
}

@description('Names of the environment variables to retrieve from Key Vault Secrets.')
param keyVaultVariables keyVaultEnvironmentVariableInfo[]

var appVariables = [
  for variable in keyVaultVariables: {
    name: variable.name
    value: '@Microsoft.KeyVault(SecretUri=${variable.keyVaultSecretUri})'
  }
]

@description('Environment variables containing the name and a value represented as a Key Vault Secret URI.')
output environmentVariables appEnvironmentVariableInfo[] = appVariables
