targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short application name used for resource naming.')
param appName string = 'spectrum-adventure'

@description('Container App name.')
param containerAppName string = 'spectrum-adventure-web'

@description('Container registry name suffix.')
param registryName string = 'spectrumadventure'

@description('PostgreSQL admin username.')
param postgresAdminUsername string = 'pgadmin'

@secure()
@description('PostgreSQL admin password.')
param postgresAdminPassword string

@description('Deployment container image reference, e.g. myregistry.azurecr.io/spectrum-adventure:latest. This must be set explicitly for a real deployment; the sample image is intentionally not the default.')
param containerImage string = ''

@description('Deploy an Azure AI Foundry account with the chat and image model deployments. Set to false to reuse an existing endpoint via azureAiEndpoint.')
param deployAiFoundry bool = true

@description('Region for the Azure AI Foundry account. Model availability is region specific, so it can differ from the app region.')
param aiFoundryLocation string = location

@description('Set to false when the image model is not available in aiFoundryLocation.')
param deployAiFoundryImageModel bool = true

@description('Azure AI Foundry / Azure OpenAI endpoint for the app. Leave empty to use the endpoint of the AI Foundry account deployed by this template.')
param azureAiEndpoint string = ''

@description('Chat deployment name for Azure OpenAI.')
param azureAiChatDeployment string = 'gpt-4.1-mini'

@description('Chat model name in the Azure OpenAI catalogue.')
param azureAiChatModelName string = 'gpt-4.1-mini'

@description('Chat model version.')
param azureAiChatModelVersion string = '2025-04-14'

@description('Image deployment name for Azure OpenAI.')
param azureAiImageDeployment string = 'gpt-image-1-mini'

@description('Image model name in the Azure OpenAI catalogue.')
param azureAiImageModelName string = 'gpt-image-1-mini'

@description('Image model version.')
param azureAiImageModelVersion string = '2025-10-06'

@description('Application Insights connection string used by the app.')
param appInsightsConnectionString string = ''

var uniqueSuffix = uniqueString(resourceGroup().id)
var normalizedAppName = toLower(replace(appName, '_', '-'))
var logAnalyticsWorkspaceName = take('law-${normalizedAppName}-${uniqueSuffix}', 63)
var appInsightsName = take('appi-${normalizedAppName}-${uniqueSuffix}', 255)
var storageAccountName = take(replace(toLower('st${normalizedAppName}${uniqueSuffix}'), '-', ''), 24)
var acrName = take(replace(toLower('${registryName}${uniqueSuffix}'), '-', ''), 50)
var postgresServerName = take('psql-${normalizedAppName}-${uniqueSuffix}', 63)
var databaseName = 'adventure'
var visualAssetsContainerName = 'visual-assets'
var identityName = 'id-${normalizedAppName}-${uniqueSuffix}'
var aiFoundryAccountName = take('aif-${normalizedAppName}-${uniqueSuffix}', 63)
var aiFoundryProjectName = take('proj-${normalizedAppName}', 32)
var resolvedAiEndpoint = !empty(azureAiEndpoint)
  ? azureAiEndpoint
  : (deployAiFoundry ? aiFoundry!.outputs.openAiEndpoint : 'https://placeholder.openai.azure.com')

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    RetentionInDays: 90
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    publicNetworkAccess: 'Enabled'
    allowSharedKeyAccess: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource visualAssetsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: visualAssetsContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: postgresServerName
  location: location
  sku: {
    name: 'Standard_B2s'
    tier: 'Burstable'
  }
  properties: {
    version: '16'
    administratorLogin: postgresAdminUsername
    administratorLoginPassword: postgresAdminPassword
    storage: {
      storageSizeGB: 32
      autoGrow: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
    }
    network: {
      publicNetworkAccess: 'Enabled'
      delegatedSubnetResourceId: null
      privateDnsZoneArmResourceId: null
    }
    highAvailability: {
      mode: 'Disabled'
    }
    availabilityZone: '1'
    createMode: 'Default'
  }
}

resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgresServer
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

module aiFoundry './aifoundry.bicep' = if (deployAiFoundry) {
  name: 'ai-foundry-module'
  params: {
    location: aiFoundryLocation
    aiFoundryAccountName: aiFoundryAccountName
    aiFoundryProjectName: aiFoundryProjectName
    aiFoundryProjectDisplayName: 'AI Spectrum Adventure'
    customSubDomainName: aiFoundryAccountName
    chatDeploymentName: azureAiChatDeployment
    chatModelName: azureAiChatModelName
    chatModelVersion: azureAiChatModelVersion
    deployImageModel: deployAiFoundryImageModel
    imageDeploymentName: azureAiImageDeployment
    imageModelName: azureAiImageModelName
    imageModelVersion: azureAiImageModelVersion
  }
}

module managedIdentity './identity.bicep' = {
  name: 'identity-module'
  params: {
    location: location
    managedIdentityName: identityName
    storageAccountResourceId: storageAccount.id
    storageAccountName: storageAccount.name
    aiFoundryAccountName: deployAiFoundry ? aiFoundry!.outputs.accountName : ''
  }
}

module containerApp './containerapp.bicep' = {
  name: 'container-app-module'
  params: {
    location: location
    containerAppName: containerAppName
    environmentName: 'cae-${normalizedAppName}-${uniqueSuffix}'
    imageName: empty(containerImage) ? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest' : containerImage
    registryServer: containerRegistry.properties.loginServer
    registryUsername: containerRegistry.listCredentials().username
    registryPassword: containerRegistry.listCredentials().passwords[0].value
    managedIdentityResourceId: managedIdentity.outputs.resourceId
    managedIdentityClientId: managedIdentity.outputs.clientId
    postgresConnectionString: 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=${databaseName};Username=${postgresAdminUsername};Password=${postgresAdminPassword};Port=5432;Ssl Mode=Require;'
    azureAiEndpoint: resolvedAiEndpoint
    azureAiChatDeployment: azureAiChatDeployment
    azureAiImageDeployment: deployAiFoundry && !deployAiFoundryImageModel ? '' : azureAiImageDeployment
    storageConnectionString: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
    appInsightsConnectionString: empty(appInsightsConnectionString) ? appInsights.properties.ConnectionString : appInsightsConnectionString
    logAnalyticsCustomerId: logAnalytics.properties.customerId
    logAnalyticsSharedKey: listKeys(logAnalytics.id, logAnalytics.apiVersion).primarySharedKey
  }
}

output containerAppUrl string = containerApp.outputs.url
output acRLoginServer string = containerRegistry.properties.loginServer
output storageAccountName string = storageAccount.name
output postgresServerName string = postgresServer.name
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output azureAiEndpoint string = resolvedAiEndpoint
output aiFoundryAccountName string = deployAiFoundry ? aiFoundry!.outputs.accountName : ''
output aiFoundryProjectName string = deployAiFoundry ? aiFoundry!.outputs.projectName : ''
output managedIdentityClientId string = managedIdentity.outputs.clientId
