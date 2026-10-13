targetScope = 'resourceGroup'

@description('Existing Storage account that hosts generated scene images.')
param storageAccountName string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' existing = {
  parent: storageAccount
  name: 'default'
}

resource visualAssets 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'visual-assets'
  properties: {
    publicAccess: 'Blob'
  }
}
