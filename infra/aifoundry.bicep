targetScope = 'resourceGroup'

@description('Azure region for the AI Foundry account. Model availability is region specific.')
param location string = resourceGroup().location

@description('Azure AI Foundry (Cognitive Services AIServices) account name.')
param aiFoundryAccountName string

@description('Azure AI Foundry project name created under the account.')
param aiFoundryProjectName string

@description('Display name for the AI Foundry project.')
param aiFoundryProjectDisplayName string = aiFoundryProjectName

@description('Custom subdomain used to build the account endpoints. Must be globally unique.')
param customSubDomainName string = toLower(aiFoundryAccountName)

@description('Chat model deployment name consumed by the app as AzureAI__ChatDeployment.')
param chatDeploymentName string = 'gpt-4.1-mini'

@description('Chat model name published in the Azure OpenAI catalogue.')
param chatModelName string = 'gpt-4.1-mini'

@description('Chat model version.')
param chatModelVersion string = '2025-04-14'

@description('Chat deployment SKU name.')
param chatDeploymentSkuName string = 'GlobalStandard'

@description('Chat deployment capacity in thousands of tokens per minute.')
param chatDeploymentCapacity int = 20

@description('Set to false when the image model is not available in the target region.')
param deployImageModel bool = true

@description('Image model deployment name consumed by the app as AzureAI__ImageDeployment.')
param imageDeploymentName string = 'gpt-image-1-mini'

@description('Image model name published in the Azure OpenAI catalogue.')
param imageModelName string = 'gpt-image-1-mini'

@description('Image model version.')
param imageModelVersion string = '2025-10-06'

@description('Image deployment SKU name.')
param imageDeploymentSkuName string = 'GlobalStandard'

@description('Image deployment capacity.')
param imageDeploymentCapacity int = 1

resource aiFoundry 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: aiFoundryAccountName
  location: location
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    // Required for the account to host Foundry projects rather than being a plain Azure OpenAI resource.
    allowProjectManagement: true
    customSubDomainName: customSubDomainName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

resource aiFoundryProject 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: aiFoundry
  name: aiFoundryProjectName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    displayName: aiFoundryProjectDisplayName
    description: 'AI Spectrum Adventure narration, NPC dialogue and retro scene generation.'
  }
}

// Project creation and model deployments all mutate the same account, so they must be serialized.
resource chatDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: aiFoundry
  name: chatDeploymentName
  dependsOn: [
    aiFoundryProject
  ]
  sku: {
    name: chatDeploymentSkuName
    capacity: chatDeploymentCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: chatModelName
      version: chatModelVersion
    }
    versionUpgradeOption: 'OnceNewDefaultVersionAvailable'
  }
}

// Model deployments on the same account must be created one at a time.
resource imageDeployment 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = if (deployImageModel) {  parent: aiFoundry
  name: imageDeploymentName
  dependsOn: [
    chatDeployment
  ]
  sku: {
    name: imageDeploymentSkuName
    capacity: imageDeploymentCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: imageModelName
      version: imageModelVersion
    }
  }
}

@description('Azure OpenAI compatible endpoint consumed by AzureOpenAIClient (AzureAI__Endpoint).')
output openAiEndpoint string = 'https://${customSubDomainName}.openai.azure.com/'

@description('Generic AI Services endpoint for the account.')
output aiServicesEndpoint string = aiFoundry.properties.endpoint

output accountName string = aiFoundry.name
output accountResourceId string = aiFoundry.id
output projectName string = aiFoundryProject.name
output chatDeployment string = chatDeploymentName
output imageDeployment string = deployImageModel ? imageDeploymentName : ''
