# Quickstart: Aspire Local Orchestration

## Prerequisites

- .NET 10 SDK.
- A running local container runtime supported by Aspire.
- Azure AI user secrets only when exercising AI-backed narration or image generation.

## Configure developer-only AI settings

From the repository root, configure values on the existing web project; do not place them in tracked files.

```powershell
dotnet user-secrets set "AzureAI:Endpoint" "https://<resource>.openai.azure.com" --project src/AI.SpectrumAdventure.Web
dotnet user-secrets set "AzureAI:ApiKey" "<local-development-key>" --project src/AI.SpectrumAdventure.Web
dotnet user-secrets set "AzureAI:ChatDeployment" "<chat-deployment>" --project src/AI.SpectrumAdventure.Web
dotnet user-secrets set "AzureAI:ImageDeployment" "<image-deployment>" --project src/AI.SpectrumAdventure.Web
```

AI settings may be omitted to validate deterministic game flows and the dependency topology.

## Start the environment

```powershell
dotnet restore
dotnet run --project src/AI.SpectrumAdventure.AppHost
```

Expected outcome:

1. The Aspire dashboard opens or prints its local URL.
2. `web`, PostgreSQL, and the Azurite blob resource progress to `Ready`.
3. Opening the web resource endpoint shows the adventure application.
4. Opening `<web-endpoint>/health` returns a successful health response.

For the SC-001 timing evidence, start a timer immediately before the `dotnet run` command. On a clean local state, record the elapsed time only when all three managed resources show `Ready` and `<web-endpoint>/health` returns HTTP 200; it must be under three minutes.

## Validate persistence and browser-visible visual assets

1. Configure the developer-only AI settings above, start a game, and record its identifier.
2. Submit one action that persists game state, then trigger a visual update and wait for its ready visual-asset path. Record the action and path; the path must resolve through the local Azurite-backed visual-asset store.
3. Stop the AppHost and start it again with the same command.
4. Verify the recorded game state and exact visual-asset path are still available after the restart.
5. In the dashboard, inspect each resource's health and recent diagnostics; no secret value should be visible.

## Reset local state deliberately

Stop the AppHost, then run:

```powershell
pwsh ./scripts/reset-local-state.ps1
```

Type `RESET` at the prompt, then start the AppHost again. The named database volume, named Azurite volume, and Data Protection key ring must be empty/new. This is a separate destructive validation from restart persistence. The script must not target Azure resources, production infrastructure, or existing `postgres-data` and `azurite-data` folders.

## Automated regression validation

```powershell
dotnet build
dotnet test
```

Both commands must pass. Local development no longer uses `docker compose` commands or a Compose project.