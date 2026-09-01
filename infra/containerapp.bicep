param location string
param containerAppName string
param environmentName string
param imageName string
param registryServer string
param registryUsername string
@secure()
param registryPassword string
param managedIdentityResourceId string
@description('Client id of the user-assigned identity, used by DefaultAzureCredential inside the container.')
param managedIdentityClientId string = ''
param postgresConnectionString string
param azureAiEndpoint string
param azureAiChatDeployment string
param azureAiImageDeployment string
param storageConnectionString string
param appInsightsConnectionString string
param logAnalyticsCustomerId string = ''
@secure()
param logAnalyticsSharedKey string = ''

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        transport: 'auto'
      }
      registries: [
        {
          server: registryServer
          username: registryUsername
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: registryPassword
        }
        {
          name: 'postgres-connection'
          value: postgresConnectionString
        }
        {
          name: 'storage-connection'
          value: storageConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: containerAppName
          image: imageName
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__AdventureDb'
              secretRef: 'postgres-connection'
            }
            {
              name: 'AzureAI__Endpoint'
              value: azureAiEndpoint
            }
            {
              name: 'AzureAI__ChatDeployment'
              value: azureAiChatDeployment
            }
            {
              name: 'AzureAI__ImageDeployment'
              value: azureAiImageDeployment
            }
            {
              name: 'AzureStorage__ConnectionString'
              secretRef: 'storage-connection'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              value: appInsightsConnectionString
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

output url string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
