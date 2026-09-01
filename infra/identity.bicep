targetScope = 'resourceGroup'

@description('Azure region for the identity resource.')
param location string = resourceGroup().location

@description('Managed identity name.')
param managedIdentityName string

@description('Resource ID of the storage account. Kept for callers that pass it; the role assignment is scoped via storageAccountName.')
param storageAccountResourceId string = ''

@description('Storage account name used to resolve the resource for the role assignment.')
param storageAccountName string

@description('Azure AI Foundry account name used to resolve the resource for the AI role assignments.')
param aiFoundryAccountName string = ''

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource storageBlobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, managedIdentity.id, 'Storage Blob Data Contributor')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource aiFoundryAccount 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = if (!empty(aiFoundryAccountName)) {
  name: aiFoundryAccountName
}

resource cognitiveServicesOpenAiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(aiFoundryAccountName)) {
  name: guid(resourceGroup().id, aiFoundryAccountName, managedIdentity.id, 'Cognitive Services OpenAI User')
  scope: aiFoundryAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource azureAiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(aiFoundryAccountName)) {
  name: guid(resourceGroup().id, aiFoundryAccountName, managedIdentity.id, 'Azure AI User')
  scope: aiFoundryAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

output resourceId string = managedIdentity.id
output clientId string = managedIdentity.properties.clientId
output principalId string = managedIdentity.properties.principalId
