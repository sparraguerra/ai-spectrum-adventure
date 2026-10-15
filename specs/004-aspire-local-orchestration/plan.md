# Implementation Plan: Aspire Local Orchestration

**Branch**: `004-aspire-local-orchestration` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from [spec.md](spec.md)

## Summary

Replace Docker Compose as the local developer workflow with a .NET Aspire AppHost that starts the existing Blazor web host, PostgreSQL 16, and an Azurite blob emulator. Preserve local state across normal restarts, surface health and diagnostics through the Aspire dashboard, and retain the existing Azure Container Apps, Dockerfile, and Bicep production path.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core 10 with Npgsql, Azure Storage Blobs, Microsoft Agent Framework, OpenTelemetry, .NET Aspire AppHost and Service Defaults packages compatible with .NET 10

**Storage**: PostgreSQL 16 for games, authoring, worlds, and visual-asset metadata; Azurite Blob Storage emulator for local visual assets; a local persisted Data Protection key ring

**Testing**: xUnit, FluentAssertions, `dotnet test`, AppHost startup/health smoke validation, and manual Aspire dashboard validation

**Target Platform**: Windows developer workstations with .NET 10 and a local container runtime; Linux container in Azure Container Apps for production

**Project Type**: Modular-monolith web application with a local distributed-application orchestrator

**Performance Goals**: A clean local checkout reaches ready resources and a reachable web health endpoint in under 3 minutes

**Constraints**: No secrets in tracked files or dashboard output; one local startup workflow; state persists across normal restarts; production Dockerfile, Bicep, and Container Apps deployment remain unchanged

**Scale/Scope**: One web project plus two local supporting resources; no new deployable production services and no production resource provisioning changes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Evidence |
|------|--------|----------|
| Preserve deterministic authoritative state | PASS | PostgreSQL remains the authoritative store; Aspire only orchestrates local dependencies. |
| Avoid premature distributed production architecture | PASS | The existing web modular monolith remains the only application process; AppHost is a local developer tool. |
| Keep persistent state outside production containers | PASS | The production Container Apps path remains backed by managed PostgreSQL and Blob Storage; local persistence uses dedicated development resources only. |
| Externalize configuration and protect secrets | PASS | References supply local dependency configuration; AI settings remain user secrets/environment settings; no secret values enter source control. |
| Maintain observability without sensitive data | PASS | Service Defaults and the Aspire dashboard expose health, logs, metrics, and traces while existing telemetry redaction policy remains applicable. |
| Preserve cloud-native Azure deployment | PASS | Dockerfile, Bicep, managed identity, and Azure Container Apps artifacts are explicitly out of scope. |

**Post-design re-check**: PASS. The design adds local orchestration projects only, does not add a production microservice, and preserves all constitution boundaries above.

## Project Structure

### Documentation (this feature)

```text
specs/004-aspire-local-orchestration/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── AI.SpectrumAdventure.AppHost/          # New local Aspire resource orchestration
├── AI.SpectrumAdventure.ServiceDefaults/  # New shared health and OpenTelemetry defaults
├── AI.SpectrumAdventure.Web/              # Existing Blazor host; consumes Aspire references
├── AI.SpectrumAdventure.Infrastructure/   # Existing EF Core and Blob Storage implementations
├── AI.SpectrumAdventure.Application/
├── AI.SpectrumAdventure.Agents/
├── AI.SpectrumAdventure.Contracts/
└── AI.SpectrumAdventure.Domain/

tests/
├── AI.SpectrumAdventure.IntegrationTests/ # Existing health/persistence coverage; new local smoke coverage
├── AI.SpectrumAdventure.Application.Tests/
├── AI.SpectrumAdventure.Agents.Tests/
└── AI.SpectrumAdventure.Domain.Tests/

scripts/
└── reset-local-state.ps1                  # New explicit local state reset workflow

infra/                                     # Existing production Bicep; unchanged by this feature
Dockerfile                                 # Existing production image; unchanged by this feature
```

**Structure Decision**: Keep the current modular monolith and add the standard Aspire local-development pair: an AppHost to model resources and a Service Defaults project to centralize health and OpenTelemetry wiring. Add only the reset script necessary to fulfill the persisted-state lifecycle. No API, agent, domain, or production-infrastructure project is introduced.

