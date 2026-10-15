# Research: Aspire Local Orchestration

## Decision 1: Use Aspire AppHost and Service Defaults as local-only projects

- **Decision**: Add an AppHost that models the existing web project and its local dependencies, plus a Service Defaults project consumed by the web host.
- **Rationale**: The AppHost is the single developer entry point and dashboard owner. Service Defaults centralizes health checks and OpenTelemetry instrumentation without creating an independent production service.
- **Alternatives considered**: Retain `docker-compose.yml` (rejected because it leaves competing local workflows); make the AppHost a production deployment mechanism (rejected because existing Bicep and Container Apps already own production deployment).

## Decision 2: Model PostgreSQL 16 as a referenced persistent local resource

- **Decision**: The AppHost will start PostgreSQL 16, expose a logical database named `AdventureDb`, retain its data in a named local volume, and reference it from the web project.
- **Rationale**: `AdventureDb` is the existing ASP.NET Core connection-string contract. A reference lets Aspire deliver it without manually maintaining a host, port, or Compose environment mapping. Existing EF migrations remain the schema authority.
- **Alternatives considered**: Continue injecting a fixed `localhost` connection string (rejected because it duplicates dependency wiring); replace PostgreSQL with an in-memory database locally (rejected because it would not validate PostgreSQL behavior or persistence).

## Decision 3: Use the Aspire Azure Storage emulator integration for visual assets

- **Decision**: The AppHost will use the Aspire Azure Storage resource in emulator mode for Azurite and expose a named blob connection to the web host, with persistent local storage.
- **Rationale**: This retains Blob Storage semantics for the existing visual pipeline while making resource lifecycle and health visible in the dashboard.
- **Alternatives considered**: Use a live Azure Storage account for normal local development (rejected because it requires credentials and incurs shared-resource risk); retain a hand-authored Azurite Compose service (rejected because Compose is retired by this feature).

## Decision 4: Preserve production configuration while adopting local reference configuration

- **Decision**: Adapt the web host's database and blob-storage configuration boundary to prefer Aspire-injected named connection strings locally, while retaining its current environment-variable and managed-identity production paths.
- **Rationale**: The current host reads `ConnectionStrings:AdventureDb`, `AzureStorage:ConnectionString`, `AzureStorage:PublicBlobEndpoint`, and `DataProtection:KeyRingPath`. Resolving these at one boundary prevents AppHost-specific values from spreading through infrastructure code.
- **Alternatives considered**: Mirror every Aspire value into a new source-controlled configuration file (rejected because it creates secret/configuration drift); rewrite storage and AI clients for Aspire-only abstractions (rejected because it expands the feature beyond local orchestration).

## Decision 5: Keep Azure AI settings developer-supplied

- **Decision**: The AppHost does not provision, store, or synthesize Azure AI credentials. The web host continues to read its existing user-secret or environment-based `AzureAI` values.
- **Rationale**: This follows the constitution's secret-management requirement and permits local startup with existing optional-feature behavior when AI configuration is absent.
- **Alternatives considered**: Commit shared development credentials (rejected for security); make an Azure AI endpoint a mandatory AppHost resource (rejected because this feature does not provision cloud resources and would make local startup dependent on a shared account).

## Decision 6: Use Service Defaults and the Aspire dashboard for local diagnostics

- **Decision**: Add the standard Service Defaults health and OpenTelemetry pipeline to the web project and use the Aspire dashboard as the local diagnostic view. Keep Azure Monitor export conditional on its existing production configuration.
- **Rationale**: The dashboard provides resource status, endpoints, logs, metrics, and traces without changing the application's production observability destination.
- **Alternatives considered**: Depend on container stdout alone (rejected because resource health and traces are fragmented); always export local telemetry to Application Insights (rejected because it can leak local diagnostic traffic and requires cloud configuration).

## Decision 7: Make persisted local state explicit and resettable

- **Decision**: Preserve PostgreSQL, Azurite, and Data Protection state through normal AppHost restarts. Provide a PowerShell reset script that removes only documented local state after confirmation.
- **Rationale**: This implements the clarified lifecycle and lets developers deliberately reset a reproducible local environment without losing state on a normal stop/start.
- **Alternatives considered**: Reset state on every launch (rejected because it breaks persistence testing); retain Compose-era bind-mount folders as the reset mechanism (rejected because it leaves the retired workflow active and can confuse state ownership).

## Decision 8: Retire Compose artifacts without changing production containers

- **Decision**: Remove the Compose project/configuration from the solution and update local developer documentation to use the AppHost. Retain the production Dockerfile, container build process, and Bicep templates.
- **Rationale**: Local orchestration and production packaging have distinct responsibilities. Keeping production artifacts avoids an unrelated deployment migration.
- **Alternatives considered**: Keep Compose as a fallback (rejected because it creates drift); remove Dockerfile/Bicep with Compose (rejected because it breaks the established Azure Container Apps delivery path).

## Resolved Technical Context

All technical-context decisions are resolved. Package versions will be selected as the current stable .NET 10-compatible Aspire release during implementation and pinned consistently across the new projects; this is a dependency-maintenance choice, not an open functional requirement.