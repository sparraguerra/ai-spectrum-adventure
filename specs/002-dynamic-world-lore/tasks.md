---
description: "Implementation tasks for Dynamic World, Lore & Procedural Adventures"
---

# Tasks: Dynamic World, Lore & Procedural Adventures

**Input**: Design documents from `/specs/002-dynamic-world-lore/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Test tasks are required by the specification, implementation plan, and Constitution Principle VIII. Core correctness tests must be deterministic and use mocked agents; no automated test requires a live AI service.

**Organization**: Tasks are grouped by user story after shared deterministic foundations. The mandatory ordering remains: structural generation, constraint validation, authoritative persistence, optional AI enrichment, then player presentation.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel once its listed dependencies are complete.
- **[USn]**: Maps to User Story n in [spec.md](./spec.md).
- Every task includes its target file path.

## Requirement Traceability

| Requirement / Success Criteria | Implementing tasks | Acceptance evidence |
|---|---|---|
| FR-001, FR-002, FR-003, FR-004; SC-001, SC-002, SC-003 | T007-T040 | T023-T024, T032-T035, T039-T040 |
| FR-005, FR-006, FR-007, FR-008; SC-004 | T041-T048, T069-T074 | T046-T048, T073-T074 |
| FR-009, FR-010, FR-011; SC-005 | T049-T055 | T053-T055 |
| FR-012, FR-013, FR-014, FR-015; SC-006 | T056-T063 | T061-T063 |
| FR-016, FR-017, FR-018; SC-007 | T030-T031, T058-T060 | T033, T061-T063, T086-T087 |
| FR-019, FR-020; SC-008 | T064-T068 | T067-T068, T086-T087 |
| FR-021; SC-009 | T069-T074 | T073-T074 |
| FR-022, FR-023, FR-024; SC-010 | T075-T084 | T082-T084, T087 |
| FR-025 | T001, T089 | Architecture and deployment scope review in T089 |

## Phase 1: Setup and Contract Alignment

**Purpose**: Establish Feature 002 namespaces, contracts, and test fixtures without changing gameplay behavior.

- [X] T001 Create Feature 002 folder and namespace layout under `src/AI.SpectrumAdventure.Domain/Worlds/`, `src/AI.SpectrumAdventure.Domain/Lore/`, and `src/AI.SpectrumAdventure.Application/Worlds/`
- [X] T002 [P] Add typed world workflow DTOs matching `specs/002-dynamic-world-lore/contracts/world-generation-request.schema.json` in `src/AI.SpectrumAdventure.Contracts/WorldGenerationRequest.cs`
- [X] T003 [P] Add typed exploration and lore result DTOs matching their schemas in `src/AI.SpectrumAdventure.Contracts/WorldExplorationResult.cs` and `src/AI.SpectrumAdventure.Contracts/LoreDiscoveryResult.cs`
- [X] T004 [P] Add typed NPC context, puzzle evaluation, and world enrichment DTOs in `src/AI.SpectrumAdventure.Contracts/NpcContext.cs`, `src/AI.SpectrumAdventure.Contracts/PuzzleEvaluationResult.cs`, and `src/AI.SpectrumAdventure.Contracts/WorldEnrichmentProposal.cs`
- [X] T005 [P] Create deterministic test world builders and fixed-time/random fixtures in `tests/AI.SpectrumAdventure.Domain.Tests/TestDoubles/WorldTestBuilder.cs` and `tests/AI.SpectrumAdventure.Application.Tests/TestDoubles/WorldTestBuilder.cs`
- [X] T006 Register Feature 002 source files and test projects in `AISpectrumAdventure.slnx` if required by the solution format

**Checkpoint**: Contract DTOs and deterministic fixtures compile; no new authoritative state is yet produced.

## Phase 2: Foundational Persistent World Model

**Purpose**: Add the shared authoritative World aggregate and persistence boundaries required by every user story.

- [X] T007 Create `WorldId`, `RegionId`, `ConnectionId`, `LoreId`, and `WorldEventId` identifiers in `src/AI.SpectrumAdventure.Domain/Common/Identifiers.cs`
- [X] T008 [P] Create `WorldSeed`, `GenerationVersion`, `GenerationKey`, and `GeneratedContentMetadata` value objects in `src/AI.SpectrumAdventure.Domain/Worlds/GenerationMetadata.cs`
- [X] T009 [P] Create region/location types, environmental properties, and location state value types in `src/AI.SpectrumAdventure.Domain/Worlds/WorldTypes.cs`
- [X] T010 Create world `Location` and directional `Connection` entities with discovered/hidden/blocked state in `src/AI.SpectrumAdventure.Domain/Worlds/WorldLocation.cs` and `src/AI.SpectrumAdventure.Domain/Worlds/WorldConnection.cs`
- [X] T011 Create owned `Region` with location membership, terrain profile, and expansion policy in `src/AI.SpectrumAdventure.Domain/Worlds/Region.cs`
- [X] T012 Create append-only `WorldEvent` kinds for expansion, connection changes, NPC movement, puzzle consequences, and environmental changes in `src/AI.SpectrumAdventure.Domain/Worlds/WorldEvent.cs`
- [X] T013 Create the `World` aggregate with validated event application, graph lookup, and generation-key idempotency in `src/AI.SpectrumAdventure.Domain/Worlds/World.cs`
- [X] T014 Extend `Game`, `GameSnapshot`, and snapshot rehydration with `WorldId` and additive `PlayerKnowledge`, while accepting legacy snapshots without WorldId in `src/AI.SpectrumAdventure.Domain/Games/Game.cs` and `src/AI.SpectrumAdventure.Domain/Games/GameSnapshot.cs`
- [X] T015 [P] Unit-test World aggregate identity, append-only events, unique source-direction connections, and generation-key idempotency in `tests/AI.SpectrumAdventure.Domain.Tests/WorldTests.cs`
- [X] T016 [P] Unit-test new and legacy Game snapshot rehydration preserves WorldId/player knowledge when present and preserves Feature 001 state when WorldId is absent in `tests/AI.SpectrumAdventure.Domain.Tests/GameWorldSnapshotTests.cs`
- [X] T017 Define `IWorldRepository` atomic create-initial-world, load, save, expand, and legacy-world-materialization operations in `src/AI.SpectrumAdventure.Application/Abstractions/IWorldRepository.cs`
- [X] T018 Add world, region, connection, generation metadata, lore, NPC-state, puzzle, and world-event record sets to `src/AI.SpectrumAdventure.Infrastructure/Persistence/AdventureDbContext.cs`
- [X] T019 Configure optimistic concurrency and a unique `(WorldId, GenerationKey)` index in `src/AI.SpectrumAdventure.Infrastructure/Persistence/Configurations/WorldConfiguration.cs`
- [X] T020 Implement `EfWorldRepository` with transaction-aware initial-world creation, atomic expansion persistence, winner reload on duplicate generation, and one-time legacy Game-to-World materialization in `src/AI.SpectrumAdventure.Infrastructure/Persistence/EfWorldRepository.cs`
- [X] T021 Add the Feature 002 world persistence migration in `src/AI.SpectrumAdventure.Infrastructure/Migrations/`
- [X] T022 Update `StartGameUseCase` to create and persist the initial validated World before saving a Game bound to its WorldId, and register the required services in `src/AI.SpectrumAdventure.Application/Games/StartGameUseCase.cs` and `src/AI.SpectrumAdventure.Web/Program.cs`
- [X] T023 [P] Integration-test initial world creation precedes new Game persistence, and legacy game loading materializes one dedicated World without losing recorded progress, in `tests/AI.SpectrumAdventure.IntegrationTests/WorldPersistenceTests.cs`
- [X] T024 [P] Integration-test concurrent identical generation-key requests persist exactly one expansion in `tests/AI.SpectrumAdventure.IntegrationTests/WorldGenerationConcurrencyTests.cs`

**Checkpoint**: A shared World has durable identity, versioned generation metadata, graph/event invariants, and replica-safe persistence before any AI enrichment.

## Phase 3: User Story 1 - Discover Unknown Places (Priority: P1)

**Goal**: Let a player expand an allowed unknown boundary into a coherent persistent region/location/connection.

**Independent Test**: From a known location, explore an allowed unknown direction and verify a unique, connected, persisted location is reached; retrying the same boundary returns the same location.

- [X] T025 [US1] Define `IRegionGenerator`, `ILocationGenerator`, `IConnectionGenerator`, `IWorldGenerationRules`, and `IWorldConstraintValidator` abstractions in `src/AI.SpectrumAdventure.Application/Abstractions/WorldGeneration.cs`
- [X] T026 [US1] Implement deterministic generation-key derivation and local seeded random selection in `src/AI.SpectrumAdventure.Domain/Worlds/DeterministicGenerationKeyFactory.cs`
- [X] T027 [P] [US1] Implement permitted terrain-transition and region-expansion rules in `src/AI.SpectrumAdventure.Domain/Worlds/WorldGenerationRules.cs`
- [X] T028 [US1] Implement deterministic region, location, and connection candidate generators in `src/AI.SpectrumAdventure.Application/Worlds/WorldGenerator.cs`
- [X] T029 [US1] Implement constraint validation for geography, graph shape, identity, location relationships, lore restrictions, NPC placement, and prior events in `src/AI.SpectrumAdventure.Application/Worlds/WorldConstraintValidator.cs`
- [X] T030 [US1] Implement `ExploreUnknownDirectionUseCase` using generate -> validate -> atomically persist -> move/discover in `src/AI.SpectrumAdventure.Application/Worlds/ExploreUnknownDirectionUseCase.cs`
- [X] T031 [US1] Extend movement dispatch to identify an allowed unknown boundary and invoke exploration rather than inventing an exit in `src/AI.SpectrumAdventure.Application/Rules/MovementRules.cs` and `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs`
- [X] T032 [P] [US1] Unit-test identical world seed/version/boundary inputs produce identical structural candidates in `tests/AI.SpectrumAdventure.Domain.Tests/DeterministicWorldGenerationTests.cs`
- [X] T033 [P] [US1] Unit-test invalid terrain, duplicate identity, cyclic-invalid graph, lore, NPC, and event conflict candidates are rejected without mutating a World in `tests/AI.SpectrumAdventure.Application.Tests/WorldConstraintValidatorTests.cs`
- [X] T034 [US1] Integration-test known location -> unknown boundary -> persisted destination -> repeat traversal flow in `tests/AI.SpectrumAdventure.IntegrationTests/UnknownBoundaryExplorationTests.cs`
- [X] T035 [US1] Integration-test generation or persistence failure leaves existing state unchanged and returns playable in-world feedback in `tests/AI.SpectrumAdventure.IntegrationTests/WorldGenerationFailureTests.cs`

**Checkpoint**: Deterministic procedural expansion is playable without AI, and failure cannot create or corrupt world reality.

## Phase 4: User Story 2 - Return to a Persistent World (Priority: P1)

**Goal**: Preserve authoritative locations, objects, paths, NPCs, puzzles, and consequences across later visits and sessions.

**Independent Test**: Alter a discovered location or connection, leave and reload it, and verify its identity and relevant changed state persist.

- [X] T036 [US2] Add deterministic world-event application for material location, object, connection, NPC, and puzzle state changes in `src/AI.SpectrumAdventure.Domain/Worlds/World.cs`
- [X] T037 [US2] Add world-aware state projection and existing-location travel retrieval in `src/AI.SpectrumAdventure.Application/Worlds/GetWorldLocationQuery.cs` and `src/AI.SpectrumAdventure.Application/Worlds/TravelToKnownLocationUseCase.cs`
- [X] T038 [US2] Apply cross-aggregate World and Game changes in one transaction in `src/AI.SpectrumAdventure.Infrastructure/Persistence/EfWorldRepository.cs` and `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs`
- [X] T039 [P] [US2] Unit-test state-changing world events preserve location identity and never reopen changed connections without an explicit event in `tests/AI.SpectrumAdventure.Domain.Tests/WorldEvolutionTests.cs`
- [X] T040 [US2] Integration-test return, application restart, and concurrent-save conflict behavior for a changed generated location in `tests/AI.SpectrumAdventure.IntegrationTests/GeneratedLocationConsistencyTests.cs`

**Checkpoint**: Generated base reality and subsequent world evolution persist independently of process memory.

## Phase 5: User Story 3 - Discover World Lore Naturally (Priority: P1)

**Goal**: Store structured lore and reveal only player-earned entries through gameplay.

**Independent Test**: Discover lore by multiple gameplay mechanisms, review it later, and verify undiscovered content remains unavailable.

- [X] T041 [US3] Create lore categories, scopes, truth classifications, and `LoreEntry` invariants in `src/AI.SpectrumAdventure.Domain/Lore/LoreEntry.cs` and `src/AI.SpectrumAdventure.Domain/Lore/LoreTypes.cs`
- [X] T042 [US3] Add authoritative lore references and immutable/historical/regional/rumour validation to `src/AI.SpectrumAdventure.Domain/Worlds/World.cs`
- [X] T043 [US3] Implement deterministic discovery trigger evaluation from locations, items, NPCs, puzzles, and events in `src/AI.SpectrumAdventure.Application/Lore/LoreDiscoveryRules.cs`
- [X] T044 [US3] Implement player-safe known-lore query and idempotent discovery update in `src/AI.SpectrumAdventure.Application/Lore/DiscoverLoreUseCase.cs` and `src/AI.SpectrumAdventure.Application/Lore/GetKnownLoreQuery.cs`
- [X] T045 [US3] Return player-facing lore discoveries with `LoreDiscoveryResult` in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs`
- [X] T046 [P] [US3] Unit-test immutable/historical facts resist contradictory enrichment and rumours remain explicitly uncertain in `tests/AI.SpectrumAdventure.Domain.Tests/LoreConsistencyTests.cs`
- [X] T047 [P] [US3] Unit-test duplicate lore discovery is idempotent and undiscovered lore is excluded from player projections in `tests/AI.SpectrumAdventure.Application.Tests/LoreDiscoveryTests.cs`
- [X] T048 [US3] Integration-test discovery through NPC, object, and environmental triggers with later recall in `tests/AI.SpectrumAdventure.IntegrationTests/LoreDiscoveryFlowTests.cs`

**Checkpoint**: Structured world knowledge is durable and player knowledge is progressive rather than omniscient.

## Phase 6: User Story 4 - Meet Credible Characters (Priority: P1)

**Goal**: Support multiple persistent NPCs with bounded knowledge and validated state changes.

**Independent Test**: Meet two NPCs, revisit them, ask local and forbidden questions, and verify identity, location, personality, goals, and knowledge remain coherent.

- [X] T049 [US4] Extend canonical NPC state with world location, goals, relationships, allowed lore references, and state version in `src/AI.SpectrumAdventure.Domain/Npcs/Npc.cs`
- [X] T050 [US4] Implement deterministic NPC placement, retrieval, and validated movement events in `src/AI.SpectrumAdventure.Application/Worlds/NpcWorldStateService.cs`
- [X] T051 [US4] Build scoped `NpcContext` from canonical traits, allowed lore, player-known facts, and bounded conversation history in `src/AI.SpectrumAdventure.Application/Orchestration/NpcConversationOrchestrator.cs`
- [X] T052 [US4] Update `NpcAgent` instructions and output validation to reject knowledge outside allowed references in `src/AI.SpectrumAdventure.Agents/Npc/NpcInstructions.cs` and `src/AI.SpectrumAdventure.Agents/Npc/NpcAgent.cs`
- [X] T053 [P] [US4] Unit-test NPC context excludes private player actions, distant unknown facts, and unauthorized lore in `tests/AI.SpectrumAdventure.Application.Tests/NpcContextBuilderTests.cs`
- [X] T054 [P] [US4] Unit-test canonical NPC movement prevents incompatible simultaneous locations in `tests/AI.SpectrumAdventure.Domain.Tests/NpcWorldStateTests.cs`
- [X] T055 [US4] Integration-test repeated multi-NPC conversations and a validated relocation across persistence reload in `tests/AI.SpectrumAdventure.IntegrationTests/PersistentNpcFlowTests.cs`

**Checkpoint**: NPC dialogue is expressive but can only reflect scoped, authoritative character knowledge and state.

## Phase 7: User Story 5 - Solve Linked Puzzles Creatively (Priority: P1)

**Goal**: Replace the single-purpose puzzle with reusable stateful definitions, explicit alternative solutions, and chains.

**Independent Test**: Complete two puzzles using the common model, including one valid natural-language approach that produces a follow-on discovery; invalid attempts leave state unchanged.

- [X] T056 [US5] Create reusable puzzle states, prerequisite references, condition types, solution definitions, outcomes, and chain links in `src/AI.SpectrumAdventure.Domain/Puzzles/`
- [X] T057 [US5] Refactor the Feature 001 Forgotten Tower puzzle into the reusable definition; support JSON-declared `puzzles` collections with explicit conditions, alternatives, outcomes, and chain links while preserving the singular `puzzle` format as a backward-compatible fallback in `src/AI.SpectrumAdventure.Domain/Games/AdventureWorldFactory.cs` and `src/AI.SpectrumAdventure.Domain/Puzzles/Puzzle.cs`
- [X] T058 [US5] Implement pure deterministic puzzle evaluation against structured intent, World, Game, and explicit solution conditions in `src/AI.SpectrumAdventure.Application/Rules/PuzzleRules.cs`
- [X] T059 [US5] Implement validated puzzle outcomes that unlock a clue, item, NPC interaction, location, connection, event, or follow-on puzzle in `src/AI.SpectrumAdventure.Application/Puzzles/ApplyPuzzleOutcomeUseCase.cs`
- [X] T060 [US5] Map accepted/rejected evaluation to `PuzzleEvaluationResult` before narration in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs`
- [X] T061 [P] [US5] Unit-test states, prerequisites, multiple valid solutions, invalid creative attempts, and solved-state idempotency in `tests/AI.SpectrumAdventure.Domain.Tests/ReusablePuzzleTests.cs`
- [X] T062 [P] [US5] Unit-test a puzzle chain produces only declared follow-on discoveries in `tests/AI.SpectrumAdventure.Application.Tests/PuzzleChainTests.cs`
- [X] T063 [US5] Integration-test two-puzzle exploration flow including a valid natural-language solution and durable consequence in `tests/AI.SpectrumAdventure.IntegrationTests/DynamicPuzzleFlowTests.cs`

**Checkpoint**: Intent interpretation proposes actions; deterministic puzzle rules alone decide completion and chains.

## Phase 8: User Story 6 - Experience Lasting Consequences (Priority: P2)

**Goal**: Make validated player actions and dynamic events change later world choices coherently.

**Independent Test**: Cause a path, relationship, location, object, or puzzle change, continue playing, and verify that later interactions reflect it.

- [X] T064 [US6] Define deterministic world-event causes, sequence ordering, and consequence payload validation in `src/AI.SpectrumAdventure.Domain/Worlds/WorldEvent.cs`
- [X] T065 [US6] Implement action-triggered and turn-based dynamic-event selection without agent ownership in `src/AI.SpectrumAdventure.Application/Worlds/WorldEventScheduler.cs`
- [X] T066 [US6] Apply validated consequences to world state and player-facing available choices in `src/AI.SpectrumAdventure.Application/Worlds/ApplyWorldEventUseCase.cs`
- [X] T067 [P] [US6] Unit-test events require valid causes and cannot restore destroyed/blocked state without an explicit permitted transition in `tests/AI.SpectrumAdventure.Domain.Tests/WorldEventInvariantTests.cs`
- [X] T068 [US6] Integration-test persistent consequences for a path, NPC relationship, and puzzle outcome in `tests/AI.SpectrumAdventure.IntegrationTests/PersistentConsequenceTests.cs`

**Checkpoint**: Events and player choices evolve a World through validated, observable state transitions.

## Phase 9: User Story 7 - Reveal a Personal Map (Priority: P2)

**Goal**: Show only player-discovered regions, locations, and connections, including learned changes.

**Independent Test**: Start with the initial map, discover a location and connection, then verify they appear while undiscovered facts remain hidden.

- [X] T069 [US7] Extend `PlayerKnowledge` with discovered region, location, connection, clue, lore, and NPC information references in `src/AI.SpectrumAdventure.Domain/Games/Game.cs` and `src/AI.SpectrumAdventure.Domain/Games/GameSnapshot.cs`
- [X] T070 [US7] Implement a player-safe map projection that filters World graph facts by `PlayerKnowledge` in `src/AI.SpectrumAdventure.Application/Worlds/GetDiscoveredMapQuery.cs`
- [X] T071 [US7] Update exploration/travel/event workflows to record only confirmed map discoveries in `src/AI.SpectrumAdventure.Application/Worlds/ExploreUnknownDirectionUseCase.cs` and `src/AI.SpectrumAdventure.Application/Worlds/ApplyWorldEventUseCase.cs`
- [X] T072 [US7] Add discovered-map display and changed-path presentation in `src/AI.SpectrumAdventure.Web/Components/MapPanel.razor`
- [X] T073 [P] [US7] Unit-test map projection excludes undiscovered nodes/edges and includes newly confirmed discoveries in `tests/AI.SpectrumAdventure.Application.Tests/DiscoveredMapTests.cs`
- [X] T074 [US7] Integration-test map updates after exploration and a learned blocked-path event in `tests/AI.SpectrumAdventure.IntegrationTests/ProgressiveMapDiscoveryTests.cs`

**Checkpoint**: Player knowledge and World knowledge remain visibly and persistently distinct.

## Phase 10: User Story 8 - Preserve the Retro Adventure Identity (Priority: P3)

**Goal**: Provide optional retro 8-bit enrichment and visuals only after valid world persistence.

**Independent Test**: Discover or materially change a location; verify its presentation remains retro and factual, then simulate enrichment/image failure and continue playing.

- [X] T075 [US8] Define `IWorldEnrichmentAgent` and deterministic enrichment validation boundary in `src/AI.SpectrumAdventure.Application/Abstractions/IWorldEnrichmentAgent.cs` and `src/AI.SpectrumAdventure.Application/Worlds/WorldEnrichmentValidator.cs`
- [X] T076 [US8] Implement World Enrichment Agent with scoped persisted location/region/lore inputs and structured proposal output in `src/AI.SpectrumAdventure.Agents/WorldEnrichment/WorldEnrichmentAgent.cs` and `src/AI.SpectrumAdventure.Agents/WorldEnrichment/WorldEnrichmentInstructions.cs`
- [X] T077 [US8] Persist approved presentation metadata by world/location/scene version and retain factual fallback values in `src/AI.SpectrumAdventure.Infrastructure/Persistence/WorldEnrichmentRepository.cs`
- [X] T078 [US8] Invoke enrichment only after committed structural persistence and use fallback presentation on rejection/failure in `src/AI.SpectrumAdventure.Application/Worlds/ExploreUnknownDirectionUseCase.cs`
- [X] T079 [US8] Extend scene-state key construction with WorldId, LocationId, and material scene version in `src/AI.SpectrumAdventure.Agents/VisualArtDirector/SceneStateKeyBuilder.cs`
- [X] T080 [US8] Feed persisted enriched visual characteristics to the existing asynchronous image queue in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` and `src/AI.SpectrumAdventure.Agents/ImagePipeline/ImageGenerationWorker.cs`
- [X] T081 [US8] Display world-aware scene loading, ready, and failure states in `src/AI.SpectrumAdventure.Web/Components/SceneImage.razor`
- [X] T082 [P] [US8] Unit-test enrichment cannot change identity, topology, state, or forbidden lore claims in `tests/AI.SpectrumAdventure.Application.Tests/WorldEnrichmentValidatorTests.cs`
- [X] T083 [P] [US8] Unit-test scene keys change only for material world scene changes in `tests/AI.SpectrumAdventure.Agents.Tests/WorldSceneStateKeyBuilderTests.cs`
- [X] T084 [US8] Integration-test post-commit enrichment/image failure preserves exploration and factual narration in `tests/AI.SpectrumAdventure.IntegrationTests/WorldEnrichmentFailureTests.cs`

**Checkpoint**: Retro visual enrichment remains optional, asynchronous, and incapable of defining world reality.

## Phase 11: Polish and Cross-Cutting Validation

**Purpose**: Complete observability, regression coverage, local documentation, and deployment-compatible configuration.

- [X] T085 [P] Add generation, validation, duplicate-prevention, world persistence, lore, NPC, puzzle, enrichment, and image telemetry in `src/AI.SpectrumAdventure.Application/Worlds/WorldTelemetry.cs` and `src/AI.SpectrumAdventure.Infrastructure/Persistence/EfWorldRepository.cs`
- [X] T086 [P] Add structured regression tests for contradictory candidate/enrichment output, impossible actions, repeated discovery, NPC knowledge leakage, puzzle bypass, and repeated actions in `tests/AI.SpectrumAdventure.Application.Tests/WorldConsistencyRegressionTests.cs`
- [X] T087 Add end-to-end Feature 002 scenarios from `specs/002-dynamic-world-lore/quickstart.md` to `tests/AI.SpectrumAdventure.IntegrationTests/Feature002EndToEndTests.cs`
- [X] T088 Update local dynamic-world setup, database migration, and test instructions in `README.md` and `specs/002-dynamic-world-lore/quickstart.md`
- [X] T089 Validate Feature 002 reuses the existing `AdventureDb` connection and managed-identity configuration without new production secrets or infrastructure resources; run `az bicep build --file infra/main.bicep` and document the expected unchanged configuration in `specs/002-dynamic-world-lore/quickstart.md`
- [X] T090 Run the complete validation suite and resolve Feature 002 regressions with `dotnet test` from the repository root

**Checkpoint**: Feature 002 is observable, regression-protected, locally runnable, and remains compatible with the existing single Azure Container App deployment.

## Dependencies and Execution Order

## Execution Notes (2026-09-02)

- T085-T090 research (2026-09-02): OpenTelemetry already exports the `AI.SpectrumAdventure.Application`, `AI.SpectrumAdventure.Agents`, and `AI.SpectrumAdventure.Infrastructure` activity sources in the Web composition root. `ExploreUnknownDirectionUseCase` controls structural generation, validation, and best-effort enrichment; `EfWorldRepository` controls durable saves, duplicate-generation convergence, and persistence conflicts. The final slice will introduce a shared application world telemetry facade, instrument these ownership points and the existing deterministic lore/NPC/puzzle/image workflows, add focused regression and quickstart E2E coverage using test doubles, document the unchanged AdventureDb/managed-identity deployment contract, validate `infra/main.bicep` without deployment, and run the full solution test suite. The known `System.Security.Cryptography.Xml` dependency warning is pre-existing and will not be changed.
- T036-T040 research confirmed that `World.Apply` supports location and connection mutations, but `EfWorldRepository.ToDomain` currently omits persisted events during rehydration. The US2 slice will replay authoritative events when loading a world, add read-only existing-location projection/travel application services, and verify state survives persistence.
- Later phases will remain deterministic: lore, NPC placement, reusable puzzle evaluation, consequences, and map projection will use canonical World/Game state and will not grant agents authority to mutate it.
- US2 implementation evidence: `EfWorldRepository` now restores persisted world events and saves mutable location/connection material state with newly appended events. `WorldPersistenceTests.SaveAsync_ReloadsChangedConnectionAndItsAuthoritativeEvent` passed on 2026-09-02. T036-T040 remain unchecked because their full acceptance scope also requires object/NPC/puzzle event effects, world-aware travel/projections, atomic World+Game updates, and concurrent restart coverage.
- T049-T055 research confirmed that `World` already applies persisted `NpcMovedWorldEvent` placement while `Npc` owns per-character traits and bounded conversation history. This slice will extend `Npc` with canonical location, goals, relationships, allowed lore, and a monotonic state version; add deterministic placement/retrieval/relocation validation; construct conversation context only from canonical and player-known facts; retain agent output filtering; and cover leakage, incompatible placement, and reload persistence without granting agents mutation authority.
- T049-T055 complete: canonical NPC snapshots retain world location, goals, allowed lore, relationships, and state version; `NpcWorldStateService` validates locations and appends the sole authoritative movement event; context exposes only NPC-approved, player-known facts and a three-turn NPC-specific history; agent filtering and instructions retain the knowledge boundary. Focused domain, application, agent, and integration NPC tests passed on 2026-09-02. The integration build retains eight pre-existing `NU1903` warnings from `System.Security.Cryptography.Xml` 9.0.0 in Infrastructure; no touched unit warnings remain.
- T056-T063 research confirmed the Feature 001 `Game` owns one legacy `Puzzle`, represented by a required item plus clue and serialized via `PuzzleSnapshot`; `PuzzleRules` produces `PuzzleSolvedEvent` and `AdventureOrchestrator` narrates that result. This slice will preserve those accessors and snapshot compatibility while adding reusable explicit states, prerequisite conditions, alternative solutions, outcomes, and declared chain links. It will add deterministic structured-intent evaluation, outcome application, and a `PuzzleEvaluationResult` projection without allowing agent output to mutate state. Focused domain, application, and integration tests will validate the new reusable flow plus Forgotten Tower regression behavior.
- T056-T063 complete: reusable puzzle definitions now support explicit state, prerequisites, alternative solutions, declared outcomes, and chains; `Game` preserves a backwards-compatible primary puzzle while persisting a puzzle registry. Forgotten Tower uses the reusable key-and-clue definition. `PuzzleRules` deterministically evaluates structured intent against Game and optional World state, and the orchestrator exposes the resulting `PuzzleEvaluationResult` before narration. Puzzle outcome application returns only declared outcome IDs. On 2026-09-02, domain puzzle tests passed (14), application puzzle tests passed (10), and integration puzzle tests passed (2). The integration build retains the eight pre-existing Infrastructure `NU1903` warnings noted above; no touched production diagnostics remain.
- T075-T084 research (2026-09-02): `ExploreUnknownDirectionUseCase` persists the generated World and Game before returning factual narration, while `VisualSceneOrchestrator` and `ImageGenerationWorker` already provide a non-blocking, failure-contained image queue. This slice will add a read-only enrichment input/proposal contract, deterministic proposal validation, and a `(WorldId, LocationId, SceneVersion)` presentation repository with a factual fallback. Enrichment will be best-effort after commit; its output will only contribute display and visual characteristics. Scene keys will include WorldId, LocationId, and the material SceneVersion. Focused validator, key, and failure-path tests will use fakes only, with no live AI service.
- T075-T084 complete (2026-09-02): Enrichment is scoped to committed location/region/lore projections, validated against fixed identity/version and allowed lore/visual characteristics, and stored as versioned presentation metadata retaining factual fallback values. Exploration invokes it after successful World-and-Game persistence and swallows presentation failures. World-aware keys and persisted visual characteristics flow through the asynchronous image path; the UI displays pending, ready, and failure states. Focused tests passed: validator (3), scoped agent (1), world scene key (1), post-commit enrichment failure (1), and existing image-worker failure (1). The Web build is clean except for eight pre-existing Infrastructure `NU1903` dependency warnings. No T085+ task was changed.

```text
Setup (T001-T006)
  -> Foundational World Model and Persistence (T007-T024)
    -> US1 Unknown Places (T025-T035)
      -> US2 Persistent World (T036-T040)
      -> US3 Structured Lore (T041-T048)
      -> US4 Persistent NPCs (T049-T055)
      -> US5 Reusable Puzzles (T056-T063)
        -> US6 Consequences (T064-T068)
        -> US7 Personal Map (T069-T074)
        -> US8 Retro Enrichment (T075-T084)
          -> Polish and Validation (T085-T090)
```

US2-US5 can proceed in parallel after US1's persisted structural model and exploration boundary are stable. US6 and US7 can proceed in parallel after the required world events and discovery paths exist. US8 begins only after structural persistence is proven.

## Parallel Execution Examples

- After T013: T015 and T016 can run in parallel; after T020: T023 and T024 can run in parallel.
- In US1: T027 can run beside T026; T032 and T033 can run in parallel after T028-T029.
- In US3: T046 and T047 can run in parallel after T041-T045.
- In US4: T053 and T054 can run in parallel after T049-T052.
- In US5: T061 and T062 can run in parallel after T056-T060.
- In US8: T082 and T083 can run in parallel after T075-T080.
- In polish: T085 and T086 can run in parallel; T087-T090 follow their results.

## Implementation Strategy

**MVP first**: Complete Phases 1-3 to prove a deterministic, persistent, lazily expanding world that remains playable without AI. This is the first independently demonstrable Feature 002 increment.

**Incremental delivery**: Add persistence/evolution, lore, NPCs, puzzles, consequences, and map knowledge as independently testable deterministic slices. Add AI enrichment and dynamic visuals only after world state and player-facing exploration are reliable.

**Completion rule**: No task may let an AI response become authoritative. Every externally visible state change must originate from validated deterministic rules, persist successfully, and be covered by a deterministic test.