# Implementation Plan: Dynamic World, Lore & Procedural Adventures

**Branch**: `002-dynamic-world-lore` | **Date**: 2026-09-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-dynamic-world-lore/spec.md`

## Summary

Extend the existing modular monolith with a persistent, deterministic `World` model shared by Feature 002 games. Structural exploration is lazy: an attempted unexplored boundary is deterministically expanded from a versioned world seed, validated against the persisted world graph and lore constraints, atomically persisted, and only then optionally enriched by AI for display. The existing `Game` aggregate retains player state and player knowledge; it references a `WorldId` rather than owning a private copy of the world. The existing intent, rules, event, narration, NPC, visual, EF Core/PostgreSQL, Blob Storage, and OpenTelemetry patterns remain the foundation.

The mandatory flow is enforced throughout: **Structural Generation -> Constraint Validation -> Authoritative Persistence -> Narrative Enrichment -> Player Experience**. AI interprets input and enriches validated facts but never creates topology, changes authoritative NPC state, completes puzzles, or commits consequences.

## Technical Context

**Language/Version**: C# 13 / .NET 10, nullable reference types and implicit usings enabled.

**Primary Dependencies**: Existing ASP.NET Core 10 Blazor Interactive Server host, EF Core 10 with Npgsql, Microsoft Agent Framework, Microsoft.Extensions.AI, Azure.Identity, Azure Storage Blobs, and OpenTelemetry/Azure Monitor exporter.

**Storage**: Existing PostgreSQL database remains the authoritative store. Add normalized world-side records for shared-world coordination and generation uniqueness; retain serialized `GameSnapshot` for player-session state, now including `WorldId` and player knowledge. Azure Blob Storage remains only for generated visual assets.

**Testing**: Existing xUnit and FluentAssertions unit tests; `WebApplicationFactory` integration tests; mocked agent/chat-client outputs. Add deterministic generator/invariant tests and PostgreSQL-backed concurrency integration tests. No live AI calls in CI.

**Target Platform**: Linux container hosted as one Azure Container App; local development via .NET 10 and Docker Compose.

**Project Type**: Server-rendered web application and modular monolith. No microservices, Dapr, or independently deployed agents.

**Performance Goals**: An existing connection resolves promptly; a first valid exploration expansion completes within 3 seconds at p95 excluding optional enrichment. Narrative remains available when enrichment or image work fails. Image work remains asynchronous.

**Constraints**: Domain stays free of EF Core, Azure, Agent Framework, UI, and provider types. Authoritative changes are deterministic and transactional. Container instances are stateless; no local filesystem state or replica affinity. AI output is structured, validated, and non-authoritative.

**Scale/Scope**: Bounded, lazily expanded worlds with a small initial set of region and location types, multiple persistent NPCs and reusable puzzle chains. Shared multiplayer, infinite generation, real-time combat, and large autonomous simulation remain out of scope.

## Constitution Check

*GATE: Passed before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Design response |
|---|---|---|
| I, III, IV - authoritative deterministic state and rules | PASS | `World` and `Game` mutate only through validated domain events and application use cases; agent outputs cannot mutate either aggregate. |
| II, XX - explicit agent responsibilities | PASS | Existing agents remain scoped. Add one World Enrichment Agent that consumes read-only persisted projections and emits a validated enrichment proposal. |
| V, XXI - retro visuals | PASS | Visual specifications derive from persisted scene facts. Image work is cached, versioned, and non-blocking. |
| VI - player agency | PASS | Classic commands and natural language become structured actions; deterministic rules assess creative actions against known facts and configured puzzle solutions. |
| VII - layered memory | PASS | World facts and events are authoritative; player knowledge is game-owned; narrative memory is a bounded turn window; lore retrieval is scoped. |
| VIII - testable AI behavior | PASS | Generation, constraints, puzzles, and events are pure/deterministic tests; agents use mockable contracts and cannot be sole correctness proof. |
| IX, X - observability, cost, latency | PASS | Trace generation key, validation, persistence, deduplication, enrichment, puzzles, NPC/lore updates. Avoid agent calls when a deterministic answer is available. |
| XI-XVII - Azure, persistence, scaling, events | PASS | One stateless Container App with PostgreSQL transactions, unique generation keys, optimistic concurrency, and in-process best-effort enrichment/image queues. No Dapr without a demonstrated need. |
| XVIII-XIX - IaC and delivery | PASS | Existing Bicep/container approach remains applicable; Feature 002 requires only configuration and migration updates, to be scheduled by task generation. |
| XXII - responsiveness | PASS | Persisted structural expansion precedes player movement; nonessential narrative and image enrichment never blocks continued play. |

No constitutional deviation requires complexity tracking.

## Architecture and Ownership

### Aggregate boundaries

- **World aggregate root** owns immutable identity (`WorldId`), `WorldSeed`, `GenerationVersion`, generated base structure, world-level rules/facts, active connections, durable world events, and generated-content metadata. Its mutation methods accept only validated `WorldEvent`s.
- **Region** is owned by `World` for this bounded-world release. It groups locations, terrain constraints, regional lore references, and expansion policy; it is not independently saved or mutated, avoiding cross-aggregate topology coordination.
- **Location** and **Connection** are owned entities inside `World`; a location owns environmental state and references to persistent objects, NPC placement, puzzles, and enrichment versions. A connection is a graph edge with source, direction, destination, visibility, and availability state.
- **Game aggregate root** remains the per-player adventure state: `GameId`, `WorldId`, current location, inventory, player-specific discovered map/lore/clues/NPC knowledge, and personal event history. It cannot change `World` structure directly.
- **NPC state** has two layers: `World` owns canonical identity, traits, goals, relationships, and current location; `Game` owns only what that player has learned and a bounded conversation history. Dialogue is narrative expression, not canonical state.
- **Puzzle state** belongs to `World` when shared world consequences matter. Player discovery/progress belongs to `Game` where appropriate. A puzzle solution produces validated events affecting one or both aggregates.

### Existing project extension points

```text
src/
├── AI.SpectrumAdventure.Domain/
│   ├── Worlds/           # World, Region, Location, Connection, generation metadata, WorldEvent
│   ├── Lore/             # LoreEntry, LoreScope, truth classification, lore references
│   ├── Npcs/             # extend canonical state and player-safe knowledge descriptors
│   ├── Puzzles/          # reusable definitions, conditions, solutions, chains
│   └── Games/            # add WorldId and player knowledge to Game/GameSnapshot
├── AI.SpectrumAdventure.Application/
│   ├── Worlds/           # create/expand world, explore boundary, apply world event use cases
│   ├── Lore/             # discover/retrieve player-safe lore
│   ├── Puzzles/          # deterministic puzzle evaluation orchestration
│   ├── Abstractions/     # world repository, generator, constraint validator, enrichment agent
│   └── Rules/            # extend movement, NPC, and puzzle rules without agent dependencies
├── AI.SpectrumAdventure.Contracts/
│   └──                 # add structured DTOs for world proposal/enrichment and player results
├── AI.SpectrumAdventure.Agents/
│   ├── VisualArtDirector/ # consume persisted scene projections as today
│   ├── Npc/              # consume scoped NPC knowledge projection
│   └── WorldEnrichment/  # names, descriptions, atmosphere, hooks, lore perspective only
├── AI.SpectrumAdventure.Infrastructure/
│   └── Persistence/      # World records/configurations/repository/migrations and uniqueness constraints
└── AI.SpectrumAdventure.Web/
    └── Components/       # exploration/map/known-lore presentation using Application results

tests/
├── AI.SpectrumAdventure.Domain.Tests/          # generation, graph and puzzle invariants
├── AI.SpectrumAdventure.Application.Tests/     # expansion/lore/NPC/puzzle workflows
├── AI.SpectrumAdventure.Agents.Tests/          # enrichment and scoped-context contracts
└── AI.SpectrumAdventure.IntegrationTests/      # persistence, concurrency, full player flow
```

**Structure Decision**: Preserve the six existing projects and dependency direction. Feature modules are folders/namespaces, not services. `Application` defines abstractions and orchestrates aggregate updates; `Infrastructure` implements PostgreSQL persistence; `Web` composes dependencies; `Agents` cannot reference persistence implementations.

## Core Workflows

### Create or resume a world

1. A new dynamic adventure creates a `World` with a cryptographically generated, persisted seed and a fixed `GenerationVersion`.
2. Deterministic rules establish the initial region and starting location from `(WorldSeed, GenerationVersion)`.
3. The initial structure passes the same constraints used for later expansion and is saved before a `Game` referencing its `WorldId` is created.
4. Feature 001 snapshots without a `WorldId` are upgraded once: their current authoritative scenario state is materialized into a dedicated World, the new WorldId is saved with the Game in the same transaction, and existing player progress is preserved.
5. Resuming reads the persisted world and events; the seed is never replayed to overwrite evolved state. New generator versions apply only to newly created worlds unless an explicit migration is designed.

### Explore an unknown direction

1. The intent interpreter returns a structured movement action; existing deterministic parsing remains first choice.
2. Movement rules inspect the persisted source location graph. A direction without an existing connection but allowed by the source location's expansion policy is an **unknown boundary**.
3. `IWorldGenerator` derives a stable `GenerationKey` from `WorldSeed`, `GenerationVersion`, source location, direction, and expansion ordinal. It uses a local deterministic pseudo-random stream seeded from that key; no shared/global randomness is used.
4. Region, location, and connection generators produce a structural candidate only. `IWorldConstraintValidator` checks graph topology, terrain adjacency, unique identities, positions/relationships, lore restrictions, NPC placement, and prior events.
5. On success, the application saves the `World` update and immutable generation metadata in one transaction, guarded by a database unique constraint on `(WorldId, GenerationKey)`. It publishes a domain event after commit.
6. The movement use case reloads/reuses the persisted expansion, applies the player move and player discovery, and returns factual location details. Optional enrichment is requested only after the structure exists.
7. On validation or persistence failure, no candidate is retained; return coherent in-world feedback and leave known play available.

### Discover lore, meet NPCs, and solve puzzles

- A deterministic interaction rule identifies discoverable lore references from the current persisted location, object, NPC, event, or puzzle result. It adds only new entries to `PlayerKnowledge` and treats repeats as previously known.
- NPC conversation context is built from the NPC's canonical traits, current location, allowed personal/regional/event lore, player-known facts, and bounded player-specific history. The NPC agent receives no global world snapshot or private actions it cannot know.
- Intent interpretation returns a proposed action; `PuzzleEvaluator` compares it with explicit solution conditions and current `World`/`Game` facts. Accepted results generate deterministic events, persist both aggregates in one transaction, then request narration. Rejected results never mutate state.

### Enrichment and visuals

- `IWorldEnrichmentAgent` receives a read-only, post-commit projection: location/region types, allowed features, factual neighbours, permitted lore, visual constraints, and forbidden features.
- It returns structured optional values: display name, description, atmosphere, local legend perspective, narrative hooks, and visual characteristics. `WorldEnrichmentValidator` rejects facts/claims that conflict with its input. Approved enrichment is stored as versioned presentation metadata linked to the authoritative entity; it never changes topology or state.
- The current visual pipeline consumes the persisted scene projection and scene version. A new image is requested only for a first discovery or material visual state change. It is asynchronous and cache-keyed; a failure retains the narrative experience.

## Persistence, Concurrency, and Evolution

- Add `IWorldRepository` with load and atomic save/expand operations. The repository owns EF Core record mapping, transaction use, concurrency tokens, and conversion between domain snapshots and records.
- Use a unique `GenerationKey` to make identical boundary requests idempotent across Container App replicas. On a uniqueness conflict, discard the local candidate, load the winning persisted expansion, and continue with that authoritative result.
- Use optimistic concurrency tokens on `World` and existing `Game` rows. A use case retries a fresh read once for a conflict; a remaining conflict returns a retryable player-facing result with no partial change.
- Persist a world event log in the world record model. Base generation metadata remains immutable; applied events and validated player consequences are the current reality. Derived descriptions, map projections, and image cache keys are recomputed or invalidated from current persisted data.
- If updating both `World` and `Game`, save both under the same PostgreSQL transaction. Do not claim success or enqueue enrichment until commit succeeds.

## Incremental Delivery Strategy

1. Add world identifiers, snapshots, authoritative `World`/graph/event entities, and persistence migrations while keeping Feature 001 game creation compatible.
2. Add deterministic generation keys, region/location/connection generators, terrain rules, constraint validation, and pure invariant tests.
3. Add lazy boundary exploration and transaction/idempotency handling; prove two concurrent requests produce one persisted expansion.
4. Add player knowledge, discovered-map projection, structured lore, and scoped lore retrieval.
5. Extend persistent NPC state and safe conversation context construction.
6. Replace the one-off puzzle representation with reusable puzzle definitions, conditions, multiple solutions, and chains while retaining the Forgotten Tower behavior as a migrated definition. Adventure definition JSON accepts a preferred `puzzles` collection; the existing singular `puzzle` property remains a backward-compatible fallback.
7. Add deterministic dynamic events and consequence application.
8. Add structured world enrichment and dynamic visual requests as optional post-commit behavior. The deterministic world is playable before these steps.
9. Add end-to-end regression, failure, and observability coverage; update infrastructure configuration only as required by new migrations/settings.

## Post-Design Re-check

The design preserves the constitution: structure is deterministic and persisted before AI is invoked; all AI inputs are scoped projections and all outputs are optional, structured, validated enrichment. PostgreSQL uniqueness and concurrency tokens remove reliance on in-memory locks, keeping a single Container App horizontally safe. No new infrastructure, microservice, Dapr dependency, or provider-owned domain dependency is introduced.

## Complexity Tracking

No entries. The required world/persistence abstractions extend existing project boundaries and are necessary for externally durable shared world state; they do not add deployable components or violate the Constitution.
