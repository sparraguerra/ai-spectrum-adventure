# Research: Dynamic World, Lore & Procedural Adventures

**Feature**: 002-dynamic-world-lore | **Date**: 2026-09-02

## Decision 1: Extend the modular monolith with a shared World aggregate

- **Decision**: Add a persisted `World` aggregate, with `Region`, `Location`, `Connection`, world events, canonical NPC state, and world-level puzzles as owned entities. Retain `Game` as the player-session aggregate and add `WorldId` plus player knowledge to its snapshot.
- **Rationale**: Feature 001's `Game` owns one adventure's entire state. Feature 002 requires world facts to endure independently from individual player knowledge and to coordinate concurrent exploration. This preserves the existing project boundaries and event-based mutation pattern.
- **Alternatives considered**: Keep all world state in each `Game` snapshot (rejected: duplicates shared reality and makes concurrent expansion unsafe); separate services per world concern (rejected: no independent deployment or scaling need).

## Decision 2: Use versioned deterministic structural generation

- **Decision**: Persist `WorldSeed` and `GenerationVersion` when creating a world. Derive each candidate's local random stream from a stable `GenerationKey` based on the seed, version, source location, direction, and expansion ordinal.
- **Rationale**: A deterministic key makes a repeated expansion request reproducible and testable without global random state. Persisted structure and later world events always take precedence over recomputation.
- **Alternatives considered**: One process-wide random generator (rejected: non-repeatable and replica-dependent); regenerating the whole world from seed on every read (rejected: overwrites evolution and makes migrations fragile).

## Decision 3: Use a rule-object generator pipeline

- **Decision**: `IWorldGenerator` coordinates `IRegionGenerator`, `ILocationGenerator`, `IConnectionGenerator`, `IWorldGenerationRules`, and `IWorldConstraintValidator`. Rule objects live in Domain and accept only deterministic inputs and read-only world projections.
- **Rationale**: It matches existing deterministic Rules modules, makes terrain and uniqueness policies unit-testable, and lets new region types be added without moving policy into an agent or database.
- **Alternatives considered**: Data-only rules (deferred: useful only after a larger authored rule catalog exists); agent-designed topology (rejected: violates the Constitution).

## Decision 4: Persist winning expansions atomically and idempotently

- **Decision**: Use PostgreSQL transactions, optimistic concurrency tokens, and a unique `(WorldId, GenerationKey)` constraint. A request that loses the race loads and returns the existing expansion.
- **Rationale**: This prevents replicas from authoritatively creating two destinations for the same boundary. It follows Feature 001's EF Core concurrency pattern while correcting the scope from player game rows to shared world expansion.
- **Alternatives considered**: In-memory locks (rejected: ineffective across Azure Container Apps replicas); pessimistic locking (rejected: greater contention without benefit for turn-based play).

## Decision 5: Treat lore as structured facts and scoped perspectives

- **Decision**: Store lore entries with scope, subject references, truth classification, discoverability, and optional source/perspective. Immutable facts and historical facts are authoritative; regional facts are constrained by their region; rumours are explicitly non-authoritative. Player discovery is stored separately in `Game`.
- **Rationale**: The classification prevents descriptions or rumours from silently rewriting history and supports credible but uncertain local stories.
- **Alternatives considered**: Free-form lore documents only (rejected: cannot enforce constraints or player discovery); a vector database by default (rejected: relational keyed retrieval is sufficient for the bounded world and authoritative filtering).

## Decision 6: Scope NPC context to authoritative knowledge

- **Decision**: Keep NPC identity, traits, goals, relationships, location, and allowed knowledge references in `World`. Construct agent context from only those references, player-known facts, current interaction, and bounded conversation memory.
- **Rationale**: NPC persistence and knowledge leakage are separately testable. Dialogue remains expressive while the agent cannot receive forbidden facts.
- **Alternatives considered**: Give the NPC agent a complete world history (rejected: violates layered memory and leaks knowledge); persist agent-written character biographies as facts (rejected: needs deterministic validation first).

## Decision 7: Generalize puzzles into deterministic definitions and solutions

- **Decision**: Replace the single-purpose puzzle condition with reusable puzzle definitions containing state, prerequisites, clues, conditions, rewards, and one or more validated solutions. Puzzle outcomes emit events that can unlock locations, items, interactions, events, or follow-on puzzles.
- **Rationale**: Explicit solution conditions preserve creative natural-language input without allowing the interpreter to decide that a puzzle is complete.
- **Alternatives considered**: Prompt-only puzzle adjudication (rejected: not deterministic/testable); a single universal expression language (deferred: premature before condition variety is known).

## Decision 8: Enrich only after persistence and preserve graceful failure

- **Decision**: Add a World Enrichment Agent whose validated, versioned output contains presentation fields only. It runs after structural commit; failed or rejected enrichment produces a factual fallback experience and may be retried later.
- **Rationale**: Meets the mandatory generation pipeline while keeping the world playable and permitting cacheable/observable enrichment.
- **Alternatives considered**: Enrich before persistence (rejected: gives AI influence over state); synchronously block movement for enrichment (rejected: harms responsiveness).

## Decision 9: Reuse the existing visual pipeline

- **Decision**: Send persisted location and scene-version projections to the existing Visual Art Director and asynchronous image queue. Cache assets by `(WorldId, LocationId, SceneVersion)` and request a new image only on first discovery or a material state change.
- **Rationale**: It preserves Feature 001's non-blocking retro visual workflow and prevents narrative variation from causing needless image work.
- **Alternatives considered**: Generate an image for every narration (rejected: high latency/cost and visual inconsistency); separate visual worker Container App (deferred until load requires independent scaling).

## Decision 10: Azure Container Apps remain one stateless application

- **Decision**: Keep a single containerized modular monolith backed by PostgreSQL and Blob Storage, use managed identity in production, externalize configuration, and retain in-process best-effort queues only for non-authoritative enrichment/image work.
- **Rationale**: Existing Azure AI application guidance recommends scoped agent responsibility, structured outputs, observability, external persistent state, and incremental architecture. PostgreSQL transactions make authoritative workflows safe across replicas.
- **Alternatives considered**: Dapr/event infrastructure (rejected: no concrete independent-processing or cross-service requirement); filesystem-backed world state (rejected: incompatible with replicas and restarts).

## Decision 11: Instrument authoritative and optional paths separately

- **Decision**: Extend existing OpenTelemetry sources with generation request/completion, validation failures, generation-key deduplication, world persistence conflicts, lore discovery, NPC state transitions, puzzle results, enrichment validation/failure, and visual scene requests. Do not record raw player or narrative content by default.
- **Rationale**: Supports reconstruction of world changes without exposing unnecessary player data.
- **Alternatives considered**: Logs only (rejected: insufficient cross-component causality); telemetry containing prompts/responses by default (rejected: unnecessary data exposure).

All technical decisions are resolved; no clarification blocks Phase 1 design.