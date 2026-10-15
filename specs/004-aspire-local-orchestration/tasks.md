# Tasks: Aspire Local Orchestration

**Input**: Design documents from `specs/004-aspire-local-orchestration/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/local-orchestration-contract.md](contracts/local-orchestration-contract.md), and [quickstart.md](quickstart.md)

**Tests**: Tests are required by the feature's acceptance scenarios and success criteria. Add focused AppHost/configuration tests before implementation, then run the existing solution test suite and the documented local smoke scenarios.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment after shared prerequisites are complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can be completed in parallel with tasks in different files after its dependencies are met.
- **[Story]**: Maps the task to the corresponding user story in [spec.md](spec.md).
- Every task includes its exact target path.

## Phase 1: Setup (Shared Project Structure)

**Purpose**: Add the two local-development projects and make them visible to the solution.

- [X] T001 Create the .NET Aspire AppHost project and package references in `src/AI.SpectrumAdventure.AppHost/AI.SpectrumAdventure.AppHost.csproj`.
- [X] T002 [P] Create the shared Aspire health and OpenTelemetry defaults project in `src/AI.SpectrumAdventure.ServiceDefaults/AI.SpectrumAdventure.ServiceDefaults.csproj` and `src/AI.SpectrumAdventure.ServiceDefaults/Extensions.cs`.
- [X] T003 Register the AppHost and Service Defaults projects in `AISpectrumAdventure.slnx`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the shared web-host and test integration needed by every local orchestration story.

**CRITICAL**: Complete this phase before starting any user-story phase.

- [X] T004 Add the Service Defaults project reference and .NET Aspire-compatible dependencies to `src/AI.SpectrumAdventure.Web/AI.SpectrumAdventure.Web.csproj`.
- [X] T005 Integrate shared service defaults and readiness/liveness health endpoints while preserving conditional Azure Monitor telemetry in `src/AI.SpectrumAdventure.Web/Program.cs`.
- [X] T006 Add the AppHost testing dependency and project references required for focused local orchestration tests in `tests/AI.SpectrumAdventure.IntegrationTests/AI.SpectrumAdventure.IntegrationTests.csproj`.

**Checkpoint**: AppHost, web host, shared defaults, and test infrastructure compile together; user-story work can begin.

---

## Phase 3: User Story 1 - Start the Complete Local Adventure (Priority: P1) MVP

**Goal**: A developer starts the web application, PostgreSQL, and Azurite from the AppHost and reaches the web health endpoint only after dependencies are ready.

**Independent Test**: Start the AppHost in a clean local environment, observe `web`, PostgreSQL, and blob storage reach ready state, then request `<web-endpoint>/health` successfully.

### Tests for User Story 1

- [X] T007 [P] [US1] Write failing resource-topology and dependency-readiness tests in `tests/AI.SpectrumAdventure.IntegrationTests/LocalOrchestrationStartupTests.cs`.

### Implementation for User Story 1

- [X] T008 [US1] Define PostgreSQL 16, the `AdventureDb` database reference, and a persistent named local volume in `src/AI.SpectrumAdventure.AppHost/Program.cs`.
- [X] T009 [US1] Define the Azurite Blob Storage emulator, `VisualAssets` reference, externally reachable blob endpoint, and persistent named local volume in `src/AI.SpectrumAdventure.AppHost/Program.cs`.
- [X] T010 [US1] Add the existing web project to the AppHost, reference `AdventureDb` and `VisualAssets`, wait for required dependencies, and provide a persisted local Data Protection key path in `src/AI.SpectrumAdventure.AppHost/Program.cs`.

**Checkpoint**: The AppHost starts the complete playable local environment without a Docker Compose command, and T007 passes.

---

## Phase 4: User Story 2 - Use Local Configuration and AI Settings Safely (Priority: P1)

**Goal**: Aspire provides local dependency configuration automatically while Azure AI settings remain developer-supplied and untracked.

**Independent Test**: Start with local AI user secrets, verify web configuration resolves AppHost-provided database/blob connections without tracked host mappings, and verify the same host still supports production configuration fallbacks.

### Tests for User Story 2

- [X] T011 [P] [US2] Write failing configuration-precedence and production-fallback tests in `tests/AI.SpectrumAdventure.IntegrationTests/LocalOrchestrationConfigurationTests.cs`.
- [X] T012 [P] [US2] Write failing tests that assert developer AI secrets and resource credentials are absent from diagnostic configuration/log output in `tests/AI.SpectrumAdventure.IntegrationTests/LocalOrchestrationSecurityTests.cs`.

### Implementation for User Story 2

- [X] T013 [US2] Add a focused local-resource configuration boundary that resolves Aspire `AdventureDb` and `VisualAssets` references before existing production configuration fallbacks in `src/AI.SpectrumAdventure.Web/Program.cs`.
- [X] T014 [US2] Preserve browser-reachable local visual-asset URLs and Data Protection key-ring configuration without introducing secret values in `src/AI.SpectrumAdventure.AppHost/Program.cs` and `src/AI.SpectrumAdventure.Web/Program.cs`.
- [X] T015 [US2] Add ignored local AppHost state/key-ring paths and verify no developer secrets are tracked in `.gitignore`.

**Checkpoint**: T011 and T012 pass; a developer can use AppHost-supplied local connections and optional user-secret AI settings without changing production connection behavior.

---

## Phase 5: User Story 3 - Diagnose the Local System (Priority: P2)

**Goal**: A developer can inspect all local resources, health, endpoints, logs, metrics, and traces through the Aspire dashboard without exposing secrets or game content.

**Independent Test**: Start the AppHost, open its dashboard, identify every managed resource and endpoint, and induce/inspect a dependency failure without seeing credential values.

### Tests for User Story 3

- [X] T016 [P] [US3] Write failing health-check and OpenTelemetry registration tests in `tests/AI.SpectrumAdventure.IntegrationTests/LocalOrchestrationDiagnosticsTests.cs`.

### Implementation for User Story 3

- [X] T017 [US3] Configure shared health checks, OpenTelemetry logging, metrics, tracing, and safe development exporters in `src/AI.SpectrumAdventure.ServiceDefaults/Extensions.cs`.
- [X] T018 [US3] Configure dashboard-visible resource names, health states, and endpoint annotations for the web, PostgreSQL, and blob emulator in `src/AI.SpectrumAdventure.AppHost/Program.cs`.

**Checkpoint**: T016 passes and the dashboard exposes all managed resources and safe diagnostics while the existing Application Insights path remains conditional.

---

## Phase 6: User Story 4 - Retire Docker Compose from Local Development (Priority: P2)

**Goal**: Developers use Aspire documentation, solution startup, and an explicit local-reset workflow without a competing Docker Compose path.

**Independent Test**: From a clean checkout, follow the documentation to start, stop, restart, inspect, and reset local state without a Compose command, project, or configuration file.

### Tests for User Story 4

- [X] T019 [P] [US4] Write failing reset-scope and confirmation tests for local persisted state in `tests/AI.SpectrumAdventure.IntegrationTests/ResetLocalStateScriptTests.cs`.

### Implementation for User Story 4

- [X] T020 [US4] Implement a confirmation-gated reset script that removes only named PostgreSQL/Azurite development state and the ignored local key ring in `scripts/reset-local-state.ps1`.
- [X] T021 [US4] Remove the Docker Compose project from `AISpectrumAdventure.slnx` and delete the retired `docker-compose.dcproj` and `docker-compose.yml` files without deleting developer data implicitly.
- [X] T022 [US4] Replace Compose startup, configuration, shutdown, and troubleshooting guidance with the AppHost workflow in `README.md`.
- [X] T023 [US4] Replace historical active local-setup guidance with the AppHost workflow in `specs/001-conversational-adventure-mvp/quickstart.md` and finalize Feature 004 validation scenarios in `specs/004-aspire-local-orchestration/quickstart.md`.

**Checkpoint**: T019 passes and a documentation-led fresh checkout uses only Aspire for local orchestration, preserves state across restarts, and resets only through T020.

---

## Phase 7: Polish and Cross-Cutting Validation

**Purpose**: Verify the complete feature, protect the production boundary, and ensure the documented developer workflow remains accurate.

- [X] T024 [P] Verify the production container and infrastructure boundary remains unchanged by reviewing `Dockerfile`, `infra/main.bicep`, and `infra/containerapp.bicep`.
- [X] T025 Run the full solution build and automated test suite documented in `specs/004-aspire-local-orchestration/quickstart.md`.
- [X] T026 Measure the clean-state AppHost startup from `dotnet run` until `web`, PostgreSQL, and Azurite are `Ready` and `/health` returns HTTP 200 in under three minutes; inspect the dashboard for safe health and diagnostic output as documented in `specs/004-aspire-local-orchestration/quickstart.md`.
- [X] T027 Search tracked developer guidance and solution configuration for remaining Docker Compose workflows, resolving any active references in `README.md`, `AISpectrumAdventure.slnx`, and `specs/001-conversational-adventure-mvp/quickstart.md`.
- [ ] T028 Run the documented end-to-end visual-asset smoke and restart-persistence scenario: record a game identifier, persisted action, and Azurite-backed ready visual-asset path; after AppHost restart, verify the same state and path in `specs/004-aspire-local-orchestration/quickstart.md`.
- [X] T029 Run the explicit destructive reset validation by confirming `RESET`, then verify only the named PostgreSQL/Azurite state and local data-protection key ring are new while Azure and legacy data folders remain untouched, following `specs/004-aspire-local-orchestration/quickstart.md`.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001-T003 can start immediately; T001 and T002 are parallel, while T003 follows their project creation.
- **Foundational (Phase 2)**: T004-T006 depend on Phase 1 and block all user stories.
- **US1 (Phase 3)**: Depends on Phase 2; delivers the MVP local environment.
- **US2 (Phase 4)**: Depends on T008-T010 because it binds the AppHost references to web configuration.
- **US3 (Phase 5)**: Depends on Phase 2 and may proceed after T008-T010; coordinate changes to `src/AI.SpectrumAdventure.AppHost/Program.cs` with US1/US2.
- **US4 (Phase 6)**: Depends on US1 and US2 because Compose may be removed only after the equivalent AppHost workflow and state lifecycle work.
- **Polish (Phase 7)**: Depends on all required stories.

### User Story Completion Order

1. **US1 (P1)**: Complete AppHost startup topology and health checks.
2. **US2 (P1)**: Complete automatic configuration and safe developer configuration.
3. **US3 (P2)**: Complete dashboard diagnostics and OpenTelemetry visibility.
4. **US4 (P2)**: Retire Compose and document/reset the final workflow.

### Parallel Opportunities

- T001 and T002 can run in parallel; T003 follows both.
- T007 can be authored while T008-T010 are being implemented.
- T011 and T012 can run in parallel, then T013-T015 proceed in order.
- T016 can be authored while diagnostic configuration is prepared; T017 and T018 require coordination because they jointly affect dashboard telemetry.
- T019 can run in parallel with implementation work outside the reset script; T024 can run in parallel with final documentation review.

## Parallel Examples

### User Story 1

```text
Task: "Write startup topology tests in tests/AI.SpectrumAdventure.IntegrationTests/LocalOrchestrationStartupTests.cs"
Task: "Define PostgreSQL resources in src/AI.SpectrumAdventure.AppHost/Program.cs"
```

### User Story 2

```text
Task: "Write configuration tests in tests/AI.SpectrumAdventure.IntegrationTests/LocalOrchestrationConfigurationTests.cs"
Task: "Write secret non-disclosure tests in tests/AI.SpectrumAdventure.IntegrationTests/LocalOrchestrationSecurityTests.cs"
```

### User Story 3

```text
Task: "Write diagnostics tests in tests/AI.SpectrumAdventure.IntegrationTests/LocalOrchestrationDiagnosticsTests.cs"
Task: "Configure service defaults in src/AI.SpectrumAdventure.ServiceDefaults/Extensions.cs"
```

### User Story 4

```text
Task: "Write reset-script tests in tests/AI.SpectrumAdventure.IntegrationTests/ResetLocalStateScriptTests.cs"
Task: "Review the production boundary in Dockerfile and infra/main.bicep"
```

## Implementation Strategy

### MVP First

1. Complete T001-T006 to establish the Aspire projects, web integration, and test support.
2. Complete T007-T010 to start PostgreSQL, Azurite, and the web application from one AppHost.
3. Validate the User Story 1 health scenario before changing configuration or removing Compose.

### Incremental Delivery

1. Add US1 and validate one-command local startup.
2. Add US2 and validate configuration precedence, user secrets, and browser-visible blob URLs.
3. Add US3 and validate dashboard diagnostics.
4. Add US4 only after the replacement workflow is proven, then remove Compose and validate state persistence/reset.

### Format Validation

All 27 tasks use the required checkbox, sequential task ID, optional `[P]` marker, user-story label for story phases, and exact target path format.