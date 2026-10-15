# Local Orchestration Contract

## Purpose

This contract defines the developer-facing configuration and lifecycle behavior introduced by Feature 004. It is not a public HTTP API and does not change the production Container Apps contract.

## Resource Names and References

| Logical resource | Consumer | Contract |
|------------------|----------|----------|
| `AdventureDb` | Web project | Aspire supplies the PostgreSQL connection through an ASP.NET Core named connection string. The existing database registration consumes this name. |
| `VisualAssets` | Web project | Aspire supplies the Azurite Blob Storage connection through a named connection string. The storage boundary resolves it before the existing production `AzureStorage:ConnectionString` fallback. |
| `web` | Developer browser | Aspire exposes the existing Blazor host endpoint and its `/health` endpoint. |
| `dashboard` | Developer | Aspire exposes resource health, endpoints, logs, traces, and metrics locally. |

## Configuration Rules

1. The AppHost owns local PostgreSQL and Azurite endpoint generation; no tracked file contains their runtime host/port connection values.
2. The web host retains compatibility with production environment variables: `ConnectionStrings__AdventureDb`, `AzureStorage__ConnectionString`, `AzureStorage__PublicBlobEndpoint`, `DataProtection__KeyRingPath`, `AzureAI__Endpoint`, `AzureAI__ChatDeployment`, and `AzureAI__ImageDeployment`.
3. The AppHost supplies a browser-reachable local blob endpoint separately from its internal resource connection when the visual pipeline returns image URLs to the browser.
4. Azure AI and Application Insights values remain developer-supplied user secrets or environment values. The AppHost must not copy secret values into source-controlled settings or diagnostics.

## Lifecycle Contract

| Action | Expected behavior |
|--------|-------------------|
| Start AppHost | Starts web, PostgreSQL, and Azurite; the dashboard exposes their state and endpoints. |
| Stop AppHost | Stops managed resources while retaining database, blob, and key-ring state. |
| Start after stop | Reuses persisted state and applies existing EF migration behavior before serving gameplay. |
| Reset local state | A documented PowerShell script asks for confirmation and removes only the named PostgreSQL/Azurite state and local key ring. |
| Dependency failure | Dashboard and web diagnostics identify the failed dependency; no persisted state is deleted automatically. |

## Non-Disclosure Contract

- Diagnostic output must not contain Azure AI API keys, database passwords, storage account keys, or game/player content.
- The reset script must not inspect, emit, or modify Azure production resources.
- The AppHost must not change the Dockerfile, Bicep templates, Azure Container Apps configuration, managed identity, or production deployment commands.