# Azure deployment notes

## Required Azure resources

The MVP deployment uses a single Azure Container App with a small managed PostgreSQL server, Azure Blob Storage for generated images, Azure Monitor/Application Insights, and an Azure AI Foundry / Azure OpenAI endpoint.

1. Azure Container Apps Environment
2. Azure Container App (`spectrum-adventure-web`)
3. Azure Database for PostgreSQL Flexible Server (`adventure` database) storing game snapshots, visual asset metadata, and adventure definitions
4. Storage Account for generated retro scene image blobs
5. Azure AI Foundry account (`Microsoft.CognitiveServices/accounts`, kind `AIServices`, `allowProjectManagement: true`) with an AI Foundry project and the chat + image model deployments, provisioned by `infra/aifoundry.bicep`
6. Application Insights + Log Analytics workspace for telemetry
7. Azure Container Registry for the application image
8. User-assigned managed identity for Azure service access

## Infrastructure-as-Code layout

| File | Contents |
|---|---|
| `infra/main.bicep` | Entry point: Log Analytics, Application Insights, Storage, ACR, PostgreSQL flexible server + database, and the three modules below |
| `infra/aifoundry.bicep` | AI Foundry account, project, chat deployment and (optional) image deployment |
| `infra/identity.bicep` | User-assigned managed identity and its role assignments |
| `infra/containerapp.bicep` | Container Apps Environment, Container App, secrets and environment variables |

## AI Foundry parameters

| Parameter | Default | Purpose |
|---|---|---|
| `deployAiFoundry` | `true` | Provision a new AI Foundry account; set `false` to reuse an existing endpoint |
| `aiFoundryLocation` | `location` | Region for the AI Foundry account — model availability is region specific |
| `deployAiFoundryImageModel` | `true` | Set `false` when the image model is unavailable in `aiFoundryLocation` |
| `azureAiEndpoint` | `''` | Overrides the endpoint; when empty the deployed AI Foundry endpoint is used |
| `azureAiChatDeployment` / `azureAiChatModelName` / `azureAiChatModelVersion` | `gpt-4.1-mini` / `gpt-4.1-mini` / `2025-04-14` | Chat model deployment |
| `azureAiImageDeployment` / `azureAiImageModelName` / `azureAiImageModelVersion` | `gpt-image-1-mini` / `gpt-image-1-mini` / `2025-10-06` | Image model deployment |

`main.bicep` outputs `azureAiEndpoint`, `aiFoundryAccountName`, `aiFoundryProjectName` and `managedIdentityClientId` for downstream automation.

## Deployment topology

The application is a modular monolith. It runs as one Linux container on Azure Container Apps and persists game state and adventure definitions in PostgreSQL. Generated scenes are uploaded to Blob Storage. Packaged adventure JSON files seed the `adventures` table when it is empty; after that, PostgreSQL is the persistent adventure catalog. AI calls require the Azure AI endpoint and a valid managed identity or secret-based credentials. The health endpoint remains available even when optional Azure services are not yet fully provisioned, which keeps smoke tests deterministic during initial rollout.

## Environment variables used by the app

The container reads the following values from environment variables:

- `ConnectionStrings__AdventureDb`
- `AzureAI__Endpoint`
- `AzureAI__ChatDeployment`
- `AzureAI__ImageDeployment`
- `AzureStorage__ConnectionString`
- `ApplicationInsights__ConnectionString` (also exposed as `APPLICATIONINSIGHTS_CONNECTION_STRING`)
- `AZURE_CLIENT_ID` — client id of the user-assigned managed identity, required so `DefaultAzureCredential` selects the correct identity inside the container

`AzureAI__Endpoint` is populated from the AI Foundry account's Azure OpenAI endpoint (`https://<account>.openai.azure.com/`) unless `azureAiEndpoint` is supplied explicitly.

## Identity and permissions

The user-assigned managed identity is granted:

- `Storage Blob Data Contributor` on the storage account (scene image uploads)
- `Cognitive Services OpenAI User` on the AI Foundry account (chat and image inference)
- `Azure AI User` on the AI Foundry account (Foundry project access)

For the database, the app currently relies on a PostgreSQL connection string. A future hardening step can add AAD-based PostgreSQL authentication and database-level grants for the managed identity once the database is configured for Azure AD integration.
## Deployment sequence

1. Build and push the app image to Azure Container Registry.
2. Provision the Azure resources via the Bicep definitions in `infra/`.
3. Deploy the Container App referencing the Container Apps Environment and registry image.
4. Confirm the health endpoint returns HTTP 200 from the public app URL.
5. Run the quickstart validation scenarios against the live environment.

## Current verification status

The repository includes the infrastructure definition and GitHub deployment workflow needed for automated rollout. The Container App health check is used as the smoke-test gate before the app is considered ready for full gameplay validation.
