# Implementation Plan: Conversational Adventure MVP

**Branch**: `001-conversational-adventure-mvp` | **Date**: 2026-08-31 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-conversational-adventure-mvp/spec.md`

## Summary

Deliver a single-player, single-session vertical slice of "The Forgotten Tower" adventure: a Blazor Server web application backed by a deterministic .NET domain/rules engine that owns all authoritative game state (locations, inventory, objects, NPC, puzzle, world flags), with AI (via Microsoft Agent Framework) used strictly for narration, NPC dialogue, and visual-scene specification — never for state mutation. Player input (classic commands or free natural language) is interpreted into a structured intent, validated by a deterministic Rules Engine, applied as Game Events to the authoritative `GameState`, and only then narrated and (asynchronously, non-blockingly) illustrated with a retro 8-bit image. The MVP is a modular monolith (single deployable ASP.NET Core/Blazor app: `AI.SpectrumAdventure.Web`) composed of clearly separated projects (`Domain`, `Application`, `Infrastructure`, `Agents`, `Contracts`), deployed as one Azure Container App, with EF Core-backed relational persistence for game snapshots and adventure definitions, Azure Blob Storage for generated images, OpenTelemetry/Application Insights observability, and Managed Identity for Azure service access — matching the project constitution's "AI creates possibilities, the game engine preserves reality" principle.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (LTS-track SDK), nullable reference types enabled, implicit usings enabled.

**Primary Dependencies**: ASP.NET Core 10, Blazor (Interactive Server render mode), Microsoft Agent Framework (agent/workflow orchestration + structured output), Entity Framework Core 10, OpenTelemetry (+ Azure Monitor exporter), Azure.Identity (`DefaultAzureCredential`), Azure.Storage.Blobs.

**Storage**: Relational database via EF Core for authoritative `GameState`, persisted `AdventureId`, adventure definition JSON, and visual asset metadata (Azure Database for PostgreSQL Flexible Server in production; local PostgreSQL container for local development) + Azure Blob Storage (Azurite emulator locally) for generated/retro-processed visual scene image blobs. Packaged `Adventures/*.json` files seed the PostgreSQL adventure catalog when empty. No in-memory-only game state — a container replica MUST be able to restart without losing an active game (within the session lifetime defined by the spec's single-continuous-session assumption).

**Testing**: xUnit + FluentAssertions for Domain/Application unit tests; xUnit + `Microsoft.AspNetCore.Mvc.Testing`/`WebApplicationFactory` for integration tests; Agent Framework test doubles / mocked chat clients for Agent-layer tests (no live LLM calls in CI).

**Target Platform**: Linux containers on Azure Container Apps (single environment, single app for the MVP); local dev on any OS supporting .NET 10 + Docker.

**Project Type**: Web application (server-rendered Blazor UI + in-process application/domain layers) — modular monolith, not a distributed system.

**Performance Goals**: Player action → narrative response within a few seconds (target: p95 < 3s excluding first-call LLM cold start) on the synchronous gameplay path; image generation completes asynchronously and MUST NOT add latency to the narrative response.

**Constraints**: Domain layer MUST have zero dependency on Microsoft Agent Framework, LLM providers, Azure SDKs, EF Core, ASP.NET Core, or UI frameworks. AI agents MUST NOT directly mutate `GameState`. Image generation failures MUST NOT block or corrupt gameplay. No Dapr, no per-agent Container Apps, no microservices in the MVP.

**Scale/Scope**: The shipped catalog includes the MVP scenario ("The Forgotten Tower": 4 locations, ~5-8 objects/items, 1 NPC, 1 primary puzzle) and supports additional JSON-defined adventures selected before game start. Each active game persists its originating `AdventureId`; application processes remain stateless/horizontally-scalable (no in-memory session affinity requirement) per constitution Principle XVII.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design (see "Post-Design Re-check" below).*

| # | Principle | Initial Gate Status | Notes |
|---|-----------|----------------------|-------|
| I | Deterministic Game State First | PASS | `GameState` is EF Core-persisted, owned by Domain/Application; agents only read projections and return structured proposals. |
| II | Agent Responsibilities Must Be Explicit | PASS | Three agents scoped: Narrator, NPC Agent, Visual Art Director — each with a single responsibility (§ Agent Architecture). |
| III | Structured Output Over Free-Form State Mutation | PASS | All agent outputs defined as typed contracts (`NarrationResult`, `NpcResponse`, `VisualSceneSpec`) validated before use; see `contracts/`. |
| IV | Rules Before Narrative | PASS | Player Action Processing pipeline enforces Intent → Rules Validation → Game Events → State Update → Narrative, in that order. |
| V | Retro 8-Bit Identity | PASS | Visual Art Director + Retro Processing stage enforce ZX Spectrum-inspired constrained palette/pixelation; see `research.md` Decision 5. |
| VI | Player Must Always Have Agency | PASS | Intent Interpretation accepts both classic commands and free natural language (spec FR-004/FR-005); Rules Engine never hard-rejects on syntax alone. |
| VII | Memory Must Be Layered | PASS | Authoritative memory = `GameState` (EF Core); Narrative memory = recent turn/event window passed to agents; Lore memory deferred (not needed for single-scenario MVP, noted as future work). |
| VIII | Testable AI Behavior | PASS | Domain/Application/Rules Engine are pure and unit-testable; Agents tested via mocked chat clients; contract schemas enable golden-output tests. |
| IX | Observability Is a First-Class Feature | PASS | OpenTelemetry spans planned for action processing, rules validation, agent invocation, image pipeline (see `research.md` Decision 8). |
| X | Cost and Latency Must Be Managed | PASS | Intent interpretation may use a small/cheap classification path before falling back to a full LLM call; image generation is async and cached by scene-state key. |
| XI | Cloud-Native Architecture First | PASS | Single containerized ASP.NET Core app; config externalized via environment variables / Azure App Configuration-compatible settings. |
| XII | Azure Container Apps as Primary Hosting Platform | PASS | One Container App (`spectrum-adventure-web`) for the MVP; no per-agent Container Apps. |
| XIII | Progressive Architecture Evolution | PASS | Modular monolith now (Phase 1 topology); extraction points documented but not built. |
| XIV | Asynchronous Processing and Events | PASS | Image generation runs via an in-process background queue (`Channel<T>` + `BackgroundService`), not Dapr, per research Decision 9. |
| XV | Azure Identity and Secret Management | PASS | `DefaultAzureCredential` for Blob Storage/AI services in production; local dev uses user-secrets/Azurite. |
| XVI | Persistence and Cloud Storage | PASS | Game data + adventure definitions → managed relational DB; generated image blobs → Azure Blob Storage; Domain stays infrastructure-agnostic. |
| XVII | Scalability and Stateless Compute | PASS | No in-memory session ownership; `GameId` resolves state from the database on every request; optimistic concurrency via EF Core row version. |
| XVIII | Infrastructure as Code | DEFERRED (scheduled) | IaC (Bicep/azd) authoring is scheduled as task T179 in `tasks.md` (Phase 20); not a design blocker for this plan. |
| XIX | Continuous Delivery | DEFERRED (scheduled) | CI/CD pipeline definition is scheduled as task T184 in `tasks.md` (Phase 20), not required to unblock architecture. |
| XX | Microsoft Agent Framework and Cloud Boundaries | PASS | Agent Framework used only for Narrator/NPC/Visual Director; Rules Engine and persistence remain plain .NET. |
| XXI | Visual Generation Pipeline | PASS | Visual pipeline is a separate, non-authoritative, cache-aware, asynchronous flow (Decision 5/9). |
| XXII | User Experience and Responsiveness | PASS | Narrative response returned before image is ready; UI progressively updates the image via a lightweight polling/streaming update (Decision 10). |

No unjustified violations. Two items (XVIII IaC, XIX CI/CD) are intentionally deferred to `tasks.md` as concrete implementation tasks (T179, T184) rather than architectural blockers — recorded, not skipped. No entry required in Complexity Tracking.

## Agent Architecture

Per constitution Principle II ("Agent Responsibilities Must Be Explicit"), the MVP defines exactly three AI agent responsibilities plus one AI-assisted interpretation boundary — no agent owns or persists `GameState`, and every agent output is validated against a structured contract (`contracts/`) before the orchestrator acts on it:

| Agent | Responsibility | Input | Output contract | Must NOT |
|---|---|---|---|---|
| Intent Interpreter | Classify free natural-language input into a `ParsedIntent` when the deterministic command-pattern matcher finds no match (research.md Decision 3) | Raw player utterance | `ParsedIntent` | Mutate `GameState`; decide feasibility (that's the Rules Engine's job) |
| Narrator (Game Director) | Turn a validated `ActionResult` into in-tone narrative text | Validated action result + relevant Location/Item/WorldFlag facts | `NarrationResult` | Invent facts, override rules, persist state |
| NPC Agent | Generate in-character NPC dialogue bounded by an explicit knowledge whitelist | NPC personality, `KnowledgeBoundary`, `ConversationMemory`, player utterance | `NpcResponse` | Reveal information outside `KnowledgeBoundary`; mutate `GameState` directly |
| Visual Art Director | Turn validated scene state into a retro-styled visual specification | Current Location, visible items, mood | `VisualSceneSpec` | Mutate `GameState`; become the authoritative scene representation |

The `AdventureOrchestrator` (Application layer) is the only component that invokes these agents and the only component authorized to translate agent output into `GameEvent`s applied to `Game` — agents themselves never see or construct `GameEvent` instances (data-model.md).

## Project Structure

### Documentation (this feature)

```text
specs/001-conversational-adventure-mvp/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── README.md
│   ├── player-action-request.schema.json
│   ├── action-result.schema.json
│   ├── game-event.schema.json
│   ├── narration-result.schema.json
│   ├── npc-response.schema.json
│   └── visual-scene-spec.schema.json
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
AISpectrumAdventure.sln

src/
├── AI.SpectrumAdventure.Web/                  # Blazor Server host + composition root
│   ├── Program.cs                            # DI wiring, OpenTelemetry, EF Core, Agent Framework registration
│   ├── Components/                           # Blazor pages/components (Adventure, Inventory, SceneImage, CommandInput)
│   └── appsettings*.json
│
├── AI.SpectrumAdventure.Application/          # Use cases / orchestration (no infra, no UI)
│   ├── Games/                                 # StartGameUseCase, ProcessPlayerActionUseCase
│   ├── Orchestration/                         # AdventureOrchestrator (Intent -> Rules -> Events -> State -> Narration/Visual)
│   ├── Abstractions/                          # IGameRepository, IIntentInterpreter, INarratorAgent, INpcAgent, IVisualArtDirector, IImagePipeline
│   └── Rules/                                 # RulesEngine (deterministic validation), calls into Domain
│
├── AI.SpectrumAdventure.Domain/                # Pure C#, zero external dependencies
│   ├── Games/ (Game, GameState, WorldFlag, EventHistory)
│   ├── Players/ (Player, Inventory)
│   ├── Locations/ (Location, Exit, Connection)
│   ├── Items/ (GameItem, ItemState)
│   ├── Npcs/ (Npc, KnowledgeEntry, ConversationMemory)
│   ├── Puzzles/ (Puzzle, PuzzleSolutionCondition)
│   ├── Events/ (PlayerMoved, ItemTaken, DoorOpened, NpcRelationshipChanged, PuzzleSolved, LocationDiscovered)
│   └── Adventures/ (packaged JSON adventure definitions used to seed the PostgreSQL catalog)
│
├── AI.SpectrumAdventure.Infrastructure/         # EF Core, Blob Storage, Azure Identity, Telemetry wiring
│   ├── Persistence/ (AdventureDbContext, Game/VisualAsset/Adventure repositories and catalogs, EF configurations, Migrations)
│   ├── Storage/ (BlobVisualAssetStore)
│   └── Telemetry/ (OpenTelemetry setup helpers)
│
├── AI.SpectrumAdventure.Agents/                 # Microsoft Agent Framework agents (Narrator, NPC, Visual Art Director)
│   ├── Narrator/
│   ├── Npc/
│   ├── VisualArtDirector/
│   ├── Intent/ (IntentInterpreterAgent)
│   └── ImagePipeline/ (image generation + retro post-processing + background queue)
│
└── AI.SpectrumAdventure.Contracts/              # Shared DTOs/schemas referenced by Application, Agents, Web
    ├── PlayerActionRequest.cs
    ├── ParsedIntent.cs
    ├── ActionResult.cs
    ├── GameEvent.cs
    ├── NarrationResult.cs
    ├── NpcResponse.cs
    └── VisualSceneSpec.cs

tests/
├── AI.SpectrumAdventure.Domain.Tests/           # Movement/inventory/puzzle rules, state transitions, events
├── AI.SpectrumAdventure.Application.Tests/       # Orchestrator workflows, rules coordination, agent-boundary contract tests
├── AI.SpectrumAdventure.Agents.Tests/            # Structured-output parsing, invalid-response handling, NPC knowledge limits
└── AI.SpectrumAdventure.IntegrationTests/        # End-to-end player actions via WebApplicationFactory, persistence, image pipeline fallback

infra/                                            # Infrastructure as Code for the Azure deployment target (Phase 20 / tasks.md T178-T182)
├── main.bicep
├── containerapp.bicep
└── identity.bicep
```

**Structure Decision**: Single-solution modular monolith ("Option 1" style, adapted for a web application) with six projects (`Web`, `Application`, `Domain`, `Infrastructure`, `Agents`, `Contracts`) as mandated by the feature description, deployed as one Azure Container App (`spectrum-adventure-web`), plus a top-level `infra/` folder holding the Bicep IaC definitions used only at deployment time (Phase 20). `Domain` has no outbound project references; `Application` depends only on `Domain` + `Contracts` abstractions (interfaces), never directly on `Infrastructure` or `Agents` implementations (wired via DI in `Web`). Adventure content is JSON-defined and loaded through an `IAdventureCatalog` abstraction; PostgreSQL is the persistent catalog store, while packaged JSON files provide deterministic seed content. This preserves the constitution's dependency-direction requirements and keeps future extraction of `Agents`/image pipeline into a separate worker (Phase 2/3 evolution) a matter of moving a project and swapping a DI registration, not a rewrite.

## Complexity Tracking

*No entries — the Constitution Check above reports full compliance (with two documented, non-blocking deferrals to `tasks.md`: IaC authoring and CI/CD pipeline definition). No architectural deviations require justification.*

## Post-Design Re-check

*Performed after Phase 1 (`data-model.md`, `contracts/`, `quickstart.md`) were produced.*

All gates from the Constitution Check table above remain **PASS** after design:

- The data model (`data-model.md`) keeps every entity in `AI.SpectrumAdventure.Domain` free of infrastructure/agent types (Principle I, XX).
- All six contracts in `contracts/` are explicit JSON Schemas with `additionalProperties: false` and enumerated fields, enforced at the Application boundary before any state mutation (Principle III, VIII).
- `quickstart.md`'s validation scenarios map 1:1 to the spec's acceptance scenarios and the constitution's required acceptance tests (Start Game, Movement, Invalid Movement, Take Item, Use Missing Item, Solve Puzzle, Image Generation Failure) (Principle VIII, XXII).
- No new dependency was introduced that violates Domain purity, and no additional Azure resource beyond {relational DB, Blob Storage, AI/Foundry endpoint, Application Insights} was required (Principle XI, XVI).

**Result**: Constitution Check remains **PASS**. Proceed to `/speckit-tasks`.

### Live deployment re-check (Phase 20 + Phase 21)

- Azure deployment was validated against the live Container App endpoint with a successful `/health` smoke test on the deployed URL.
- The deployment was corrected to use the built ACR image rather than the default sample container image, and the app continued to serve gameplay routes successfully.
- The manual quickstart checklist was completed against the live Azure environment with no contradictions to previously established facts and no constitution principle regressions.
- Conclusion: all 22 constitution principles remain satisfied in the shipped implementation.

### Dynamic adventure catalog re-check (Phase 22)

- The hardcoded Forgotten Tower content was extracted into packaged JSON and `AdventureWorldFactory` now builds a `Game` from validated adventure definitions while preserving deterministic Domain behavior.
- The Blazor start screen lists adventures through `IAdventureCatalog`, and `StartGameUseCase` creates the selected adventure instead of always starting a single hardcoded scenario.
- Each `Game` snapshot persists `AdventureId` with backward-compatible loading for earlier snapshots; no database schema change is required for existing game rows because game state is serialized as JSON.
- PostgreSQL now owns the persistent adventure catalog via the `adventures` table and `DatabaseAdventureCatalog`; packaged JSON files seed the table when empty. Blob Storage remains scoped to generated visual image blobs.
- Validation: `az bicep build --file infra/main.bicep --outfile infra/main.json` and `dotnet test --nologo --verbosity minimal` completed successfully (`144` tests, `0` failed).

**Result**: Constitution Check remains **PASS** after dynamic adventure catalog work. The Domain remains infrastructure-free, rules remain deterministic, and cloud storage responsibilities are split cleanly between PostgreSQL adventure/game data and Blob image blobs.

