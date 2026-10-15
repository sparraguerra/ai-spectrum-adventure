# Feature Specification: Aspire Local Orchestration

**Feature Branch**: `004-aspire-local-orchestration`

**Created**: 2026-10-15

**Status**: In Progress

**Input**: User description: "Implement Feature 004 and apply Aspire to the solution so Docker Compose is no longer used."

## Clarifications

### Session 2026-10-15

- Q: What should happen to local data when the Aspire environment stops and starts again? -> A: Preserve database, blobs, and keys; reset only through a documented action.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Start the Complete Local Adventure (Priority: P1)

As a developer, I want to start the web application and all of its local dependencies from one solution entry point so that I can develop and debug the adventure without Docker Compose commands or manually coordinating services.

**Why this priority**: A reliable one-step local environment is the primary value of the migration.

**Independent Test**: Start the local orchestrator in a clean development environment and verify that the web application, relational database, and blob-storage emulator become healthy and the web health endpoint responds.

**Acceptance Scenarios**:

1. **Given** a developer has restored the solution and has its local container runtime available, **When** they start the local orchestration entry point, **Then** the web application, database, and blob-storage emulator start together.
2. **Given** the local environment is starting, **When** a dependency is not ready, **Then** the web application waits or reports a clear dependency state instead of failing with an unusable connection error.
3. **Given** all local resources are healthy, **When** the developer opens the application endpoint, **Then** they can start and play an adventure using the same local dependencies.

---

### User Story 2 - Use Local Configuration and AI Settings Safely (Priority: P1)

As a developer, I want local service configuration to be supplied automatically while AI credentials remain external to source control so that the application starts consistently without exposing secrets.

**Why this priority**: The environment must be usable without weakening the existing configuration and credential boundaries.

**Independent Test**: Start the local environment with developer-provided AI settings, confirm the application receives dependency settings without manually entering hostnames or connection strings, and inspect the repository for absent secrets.

**Acceptance Scenarios**:

1. **Given** a developer supplies optional AI settings through their local secret/configuration mechanism, **When** the environment starts, **Then** those settings are available to the web application without being written to tracked files.
2. **Given** the database and blob-storage emulator are started locally, **When** the web application starts, **Then** it receives the connection information required to use them without developer-maintained Compose environment mappings.
3. **Given** AI settings are absent or an AI provider is unavailable, **When** the developer starts the local environment, **Then** the application reports a clear configuration or feature-level failure and continues to honor its existing non-blocking fallback behavior where applicable.

---

### User Story 3 - Diagnose the Local System (Priority: P2)

As a developer, I want one local view of resource health, endpoints, logs, and telemetry so that I can diagnose the web application and its dependencies without searching through separate container output.

**Why this priority**: Fast diagnosis protects the productivity gains of one-command startup.

**Independent Test**: Start the environment, open its local diagnostic view, and verify that each resource's health, endpoint, and recent logs can be inspected without revealing credentials or player data.

**Acceptance Scenarios**:

1. **Given** the local environment is running, **When** a developer opens its diagnostic view, **Then** they can identify the web application, database, and blob-storage emulator and their current health state.
2. **Given** a resource fails to start or becomes unavailable, **When** a developer inspects the diagnostic view, **Then** they can identify the affected resource and access relevant recent diagnostic output.
3. **Given** diagnostics contain configuration or request data, **When** the developer inspects them, **Then** secrets and protected game content are not exposed beyond the existing telemetry policy.

---

### User Story 4 - Retire Docker Compose from Local Development (Priority: P2)

As a developer, I want the solution and its documentation to use the new local orchestration workflow exclusively so that I do not need to choose between two competing startup procedures.

**Why this priority**: Keeping Compose as an active path would create configuration drift and undermine the migration.

**Independent Test**: Follow the documented local setup from a fresh checkout and confirm it starts the complete environment without any Docker Compose command, project, or configuration file.

**Acceptance Scenarios**:

1. **Given** a fresh checkout, **When** a developer follows the local-development documentation, **Then** they can restore, start, stop, and troubleshoot the environment without Docker Compose.
2. **Given** the solution is opened in a supported development environment, **When** a developer inspects its projects and startup guidance, **Then** the local orchestration entry point is available and Docker Compose is not presented as an active project.
3. **Given** the migration is complete, **When** a developer searches tracked local-development documentation and configuration, **Then** no Docker Compose workflow remains.

### Edge Cases

- **Unavailable container runtime**: Startup must identify the missing prerequisite and provide an actionable diagnostic rather than silently starting only part of the environment.
- **Port collision**: A locally occupied default port must not prevent the orchestrator from exposing a discoverable usable endpoint, or it must clearly report the conflict.
- **Dependency startup failure**: Failure of the database or blob-storage emulator must leave the web application diagnosable and must not corrupt local persisted data.
- **Existing local data**: Restarting the local environment must preserve database, blob-storage, and data-protection state; the documented reset action is the only supported way to erase them.
- **Missing AI configuration**: A missing optional AI setting must be identifiable without adding secrets to the repository or blocking deterministic gameplay paths that already support fallback behavior.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The solution MUST provide one local orchestration entry point that starts the web application, relational database, and blob-storage emulator together.
- **FR-002**: The local orchestration workflow MUST make service endpoints and dependency connection information available to the web application without manually maintained Docker Compose mappings.
- **FR-003**: The web application MUST begin serving playable requests only after required local dependencies are ready, or clearly expose an unavailable dependency state.
- **FR-004**: The local workflow MUST expose a developer-facing diagnostic view with resource identity, health, reachable endpoints, and recent diagnostic output.
- **FR-005**: The diagnostic view and startup output MUST not expose API keys, connection secrets, or protected player/adventure content.
- **FR-006**: The local workflow MUST allow AI endpoint, model, and credential settings to remain developer-supplied and untracked.
- **FR-007**: The local workflow MUST preserve the current application behavior for database migration and blob-storage-backed visual assets.
- **FR-008**: The repository MUST no longer require or present Docker Compose files, projects, commands, or configuration as a supported local-development workflow.
- **FR-009**: The solution file and developer documentation MUST identify the local orchestration entry point and describe how to start, stop, configure, and troubleshoot it.
- **FR-010**: Existing automated tests and production container deployment artifacts MUST remain usable; this feature changes local orchestration only.
- **FR-011**: The local workflow MUST retain database, blob-storage, and data-protection state across an environment restart and provide one documented explicit action to reset all local persisted state.

### Key Entities

- **Local Application Topology**: The developer-visible grouping of the web application and the local dependencies it requires to run.
- **Local Resource**: A startable dependency with an identity, health state, endpoint information, configuration inputs, and diagnostics.
- **Developer-Supplied Configuration**: Local, untracked values such as AI credentials and endpoint settings that can be applied at startup.
- **Local Persisted State**: Database, blob-storage, and data-protection information retained or reset according to the documented development workflow.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: From a clean development checkout with prerequisites installed and no existing named Aspire volumes or local key ring, a developer can start the complete local environment using one documented command or launch action in under 3 minutes. Measure from command invocation until all managed resources are `Ready` and the web `/health` endpoint returns HTTP 200.
- **SC-002**: In startup validation, 100% of required resources report a visible ready or failed state, and the web health endpoint is reachable when all required resources are ready.
- **SC-003**: In an end-to-end local smoke test, a developer can start an adventure, persist one game action, and retrieve a ready visual-asset path produced through the application's configured image-generation path and stored in the orchestrated Azurite resource. The validation records the game identifier, action, and returned path; it may use developer-supplied AI settings, which remain untracked.
- **SC-004**: The local diagnostic view shows health and recent output for 100% of managed resources while automated checks confirm that no configured secret value is emitted.
- **SC-005**: A fresh-checkout documentation test completes the local setup and shutdown workflow without a Docker Compose command, file, project, or manual dependency connection string.
- **SC-006**: The existing solution build and automated test suite complete successfully after the migration.
- **SC-007**: In a restart validation, a recorded SC-003 game and visual-asset path remain available after a normal AppHost stop/start. In a separate destructive validation after explicit `RESET` confirmation, the documented reset action removes the named PostgreSQL and Azurite state plus the local data-protection key ring.

## Assumptions

- The local orchestration technology for this feature is .NET Aspire, as requested; it may still use a locally installed container runtime to run PostgreSQL and Azurite images, but it replaces Docker Compose as the developer workflow.
- PostgreSQL and Azurite remain the authoritative local substitutes for the production database and blob-storage dependencies.
- The current production container image, infrastructure templates, and Azure Container Apps deployment path are out of scope unless a local-orchestration compatibility change is strictly required.
- AI credentials remain optional developer-managed configuration; no shared development key will be stored in the repository.
- Local database, blob-storage, and data-protection state persist across normal restarts; one documented explicit reset action removes all three categories of local state.