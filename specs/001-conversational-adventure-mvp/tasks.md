# Tasks: Conversational Adventure MVP

**Input**: Design documents from `/specs/001-conversational-adventure-mvp/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Test tasks are included — the constitution (Principle VIII, "Testable AI Behavior") and the user's explicit testing requirements make automated tests a first-class deliverable, especially for the deterministic Domain/Rules layers.

**Organization**: Per the user's explicit ordering requirement, phases follow the architectural dependency hierarchy `Deterministic Game Foundation → Game Rules → Application Workflow → AI Agents → Visual Experience → User Interface → Cloud Deployment` rather than a pure user-story-first grouping. Tasks that clearly serve a specific spec user story are still labeled `[USn]` per the standard checklist format so story-level traceability and independent testability are preserved.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[USn]**: Maps to User Story n in [spec.md](./spec.md) (US1 Start Adventure, US2 Understand Location, US3 Classic Commands, US4 Natural Language, US5 Explore World, US6 Interact With Objects, US7 Manage Inventory, US8 Interact With NPC, US9 Solve Puzzle, US10 Retro Visual)
- File paths are exact and match the project structure defined in [plan.md](./plan.md)

---

## Phase 1: Setup (Solution & Project Structure)

**Purpose**: Establish the architectural boundaries from plan.md; no gameplay logic yet (per user instruction).

- [X] T001 Create `AISpectrumAdventure.sln` at the repository root
- [X] T002 Create `AI.SpectrumAdventure.Domain` class library project in `src/AI.SpectrumAdventure.Domain/`
- [X] T003 [P] Create `AI.SpectrumAdventure.Contracts` class library project in `src/AI.SpectrumAdventure.Contracts/`
- [X] T004 [P] Create `AI.SpectrumAdventure.Application` class library project in `src/AI.SpectrumAdventure.Application/`
- [X] T005 [P] Create `AI.SpectrumAdventure.Infrastructure` class library project in `src/AI.SpectrumAdventure.Infrastructure/`
- [X] T006 [P] Create `AI.SpectrumAdventure.Agents` class library project in `src/AI.SpectrumAdventure.Agents/`
- [X] T007 Create `AI.SpectrumAdventure.Web` Blazor Server project (Interactive Server render mode, per research.md Decision 2) in `src/AI.SpectrumAdventure.Web/`
- [X] T008 Create `AI.SpectrumAdventure.Domain.Tests` xUnit project in `tests/AI.SpectrumAdventure.Domain.Tests/`
- [X] T009 [P] Create `AI.SpectrumAdventure.Application.Tests` xUnit project in `tests/AI.SpectrumAdventure.Application.Tests/`
- [X] T010 [P] Create `AI.SpectrumAdventure.Agents.Tests` xUnit project in `tests/AI.SpectrumAdventure.Agents.Tests/`
- [X] T011 [P] Create `AI.SpectrumAdventure.IntegrationTests` project in `tests/AI.SpectrumAdventure.IntegrationTests/`
- [X] T012 Configure project references per plan.md's dependency rules: `Domain` has none; `Application` → `Domain` + `Contracts`; `Infrastructure` → `Domain` + `Contracts`; `Agents` → `Contracts`; `Web` → `Application` + `Infrastructure` + `Agents` + `Contracts`
- [X] T013 Add all six `src/` projects and four `tests/` projects to `AISpectrumAdventure.sln` and verify `dotnet build` succeeds
- [X] T014 [P] Add `xunit`, `xunit.runner.visualstudio`, and `FluentAssertions` package references to all four test projects
- [X] T015 [P] Add a root `Directory.Build.props` enabling nullable reference types and implicit usings across all projects
- [X] T016 Scaffold local development configuration (`appsettings.Development.json` placeholders, `dotnet user-secrets init`) in `src/AI.SpectrumAdventure.Web/`
- [X] T017 Add repository root `README.md` summarizing the solution layout and linking to [quickstart.md](./quickstart.md) for run instructions

**Checkpoint**: Clean, compilable solution with the six architectural projects and four test projects wired up; no gameplay logic yet.

---

## Phase 2: Domain Foundation (Deterministic Game Model)

**Purpose**: Implement the authoritative game model before any AI is introduced (constitution Principle I). Must complete before Phase 3.

- [X] T018 [P] Create `GameId`, `LocationId`, `ItemId`, `NpcId`, `PuzzleId` identifier value objects in `src/AI.SpectrumAdventure.Domain/Common/Identifiers.cs`
- [X] T019 [P] Create `WorldFlag` value object and a `WorldFlags` well-known-key constants class (`sign-read`, `tower-unlocked`, `npc-gave-clue`, etc.) in `src/AI.SpectrumAdventure.Domain/Games/WorldFlag.cs`
- [X] T020 [P] Create the `ItemState` flags enum (`Visible`, `Hidden`, `Collectible`, `Locked`, `Movable`, `Interactive`) in `src/AI.SpectrumAdventure.Domain/Items/ItemState.cs`
- [X] T021 Create `GameItem` entity (Id, Name, Description, State, LocationId, RevealCondition) in `src/AI.SpectrumAdventure.Domain/Items/GameItem.cs` (depends on T018, T020)
- [X] T022 [P] Create `Exit` and `WorldFlagCondition` value objects in `src/AI.SpectrumAdventure.Domain/Locations/Exit.cs`
- [X] T023 Create `Location` entity (Id, Name, BaseDescription, Exits, ObjectIds, NpcIds, Discovered) in `src/AI.SpectrumAdventure.Domain/Locations/Location.cs` (depends on T018, T022)
- [X] T024 [P] Create `Inventory` value object with `Add`/`Remove`/`Contains` in `src/AI.SpectrumAdventure.Domain/Players/Inventory.cs`
- [X] T025 Create `Player` entity (CurrentLocationId, Inventory, KnownClues) in `src/AI.SpectrumAdventure.Domain/Players/Player.cs` (depends on T024)
- [X] T026 [P] Create `ConversationTurn` value object in `src/AI.SpectrumAdventure.Domain/Npcs/ConversationTurn.cs`
- [X] T027 Create `Npc` entity (Id, Name, PersonalityProfile, KnowledgeBoundary, ConversationMemory, RelationshipFlags) in `src/AI.SpectrumAdventure.Domain/Npcs/Npc.cs` (depends on T026)
- [X] T028 [P] Create `PuzzleSolutionCondition` value object in `src/AI.SpectrumAdventure.Domain/Puzzles/PuzzleSolutionCondition.cs`
- [X] T029 Create `Puzzle` entity with a pure `CanSolve(Inventory, KnownClues)` domain method in `src/AI.SpectrumAdventure.Domain/Puzzles/Puzzle.cs` (depends on T028)
- [X] T030 Define the `GameEvent` base type and all 8 concrete event kinds (`PlayerMoved`, `ItemTaken`, `ItemUsed`, `ObjectStateChanged`, `DoorOpened`, `NpcRelationshipChanged`, `PuzzleSolved`, `LocationDiscovered`) in `src/AI.SpectrumAdventure.Domain/Events/`
- [X] T031 Create the `Game` aggregate root (Player, Locations, Npcs, Puzzle, WorldFlags, EventHistory, `Apply(GameEvent)`) in `src/AI.SpectrumAdventure.Domain/Games/Game.cs` (depends on T021, T023, T025, T027, T029, T030)
- [X] T032 Create an `AdventureWorldFactory` seeding the 4 MVP locations (Forest Entrance, Dark Forest, Old Bridge, Forgotten Tower) and their connections per spec's world layout, in `src/AI.SpectrumAdventure.Domain/Games/AdventureWorldFactory.cs` (depends on T023, T031)
- [X] T033 [P] Unit tests: `Location`/`Exit` connection and `WorldFlagCondition` rules in `tests/AI.SpectrumAdventure.Domain.Tests/LocationTests.cs`
- [X] T034 [P] Unit tests: `Inventory` add/remove/contains behavior in `tests/AI.SpectrumAdventure.Domain.Tests/InventoryTests.cs`
- [X] T035 [P] Unit tests: `Puzzle.CanSolve` dual-condition (item + clue) logic in `tests/AI.SpectrumAdventure.Domain.Tests/PuzzleTests.cs`
- [X] T036 [P] Unit tests: `Npc.KnowledgeBoundary` enforcement helpers in `tests/AI.SpectrumAdventure.Domain.Tests/NpcTests.cs`
- [X] T037 Unit tests: `Game.Apply` for each of the 8 `GameEvent` kinds plus the append-only `EventHistory` invariant, in `tests/AI.SpectrumAdventure.Domain.Tests/GameEventApplicationTests.cs` (depends on T031)
- [X] T038 Unit tests: `AdventureWorldFactory` produces the 4-location layout matching the spec's world diagram (exits, initial NPC placement, initial item placement) in `tests/AI.SpectrumAdventure.Domain.Tests/AdventureWorldFactoryTests.cs` (depends on T032)

**Checkpoint**: Pure, dependency-free Domain model exists and is fully unit-tested; zero AI, zero infrastructure references.

---

## Phase 3: Deterministic Rules Engine

**Purpose**: Determine what is possible in the game world, independent of AI (constitution Principle IV). Must complete before Phase 4/5.

- [X] T039 [P] Define the `ParsedIntent` contract (Action enum, Target, Parameters, Confidence, RawInput) in `src/AI.SpectrumAdventure.Contracts/ParsedIntent.cs`
- [X] T040 [P] Define `RulesValidationResult` (Success, Reason enum, ProposedEvents) in `src/AI.SpectrumAdventure.Application/Rules/RulesValidationResult.cs`
- [X] T041 Create the `RulesEngine` class with a `Validate(ParsedIntent, Game)` entry point in `src/AI.SpectrumAdventure.Application/Rules/RulesEngine.cs` (depends on T039, T040, T031)
- [X] T042 [US5] Implement movement validation (exit exists, `WorldFlagCondition` satisfied) producing `PlayerMoved`/`LocationDiscovered` proposals in `src/AI.SpectrumAdventure.Application/Rules/MovementRules.cs` (depends on T041)
- [X] T043 [US6] Implement object visibility/examine validation (hidden vs. visible, reveal conditions) in `src/AI.SpectrumAdventure.Application/Rules/ObjectRules.cs` (depends on T041)
- [X] T044 [US7] Implement item possession/take validation producing `ItemTaken` proposals in `src/AI.SpectrumAdventure.Application/Rules/InventoryRules.cs` (depends on T041)
- [X] T045 [US7] Implement "use item" validation (`ItemNotInInventory` failure, item-specific effect) producing `ItemUsed` proposals in `src/AI.SpectrumAdventure.Application/Rules/ItemUsageRules.cs` (depends on T044)
- [X] T046 [US9] Implement Forgotten Tower entrance/puzzle validation gated by `Puzzle.CanSolve` producing `PuzzleSolved` proposals in `src/AI.SpectrumAdventure.Application/Rules/PuzzleRules.cs` (depends on T041, T029)
- [X] T047 [US8] Implement NPC-presence validation (talk-to target exists at the current location) in `src/AI.SpectrumAdventure.Application/Rules/NpcPresenceRules.cs` (depends on T041)
- [X] T048 Wire all rule modules into `RulesEngine.Validate` dispatch by `ParsedIntent.Action` in `src/AI.SpectrumAdventure.Application/Rules/RulesEngine.cs` (depends on T042-T047)
- [X] T049 [P] Unit tests: movement validation incl. blocked tower entrance in `tests/AI.SpectrumAdventure.Application.Tests/MovementRulesTests.cs`
- [X] T050 [P] Unit tests: object visibility/hidden-reveal rules in `tests/AI.SpectrumAdventure.Application.Tests/ObjectRulesTests.cs`
- [X] T051 [P] Unit tests: inventory take/use rules incl. `ItemNotInInventory` in `tests/AI.SpectrumAdventure.Application.Tests/InventoryRulesTests.cs`
- [X] T052 [P] Unit tests: puzzle rules incl. rejection of creative bypass attempts in `tests/AI.SpectrumAdventure.Application.Tests/PuzzleRulesTests.cs`
- [X] T053 Unit tests: full `RulesEngine` dispatch covering all structured outcomes (`Success`, `ItemNotInInventory`, `NoSuchExit`, `ExitConditionNotMet`, `TargetNotPresent`, `AlreadyInState`) in `tests/AI.SpectrumAdventure.Application.Tests/RulesEngineTests.cs` (depends on T048)

**Checkpoint**: Deterministic engine can process structured `ParsedIntent` actions with no AI involvement and full test coverage.

---

## Phase 4: Game Events and State Transitions

**Purpose**: Guarantee controlled, safe world-state changes before wiring the Application workflow.

- [X] T054 Implement `Game.ApplyRange(IEnumerable<GameEvent>)` ensuring all-or-nothing application (no partial state on failure) in `src/AI.SpectrumAdventure.Domain/Games/Game.cs` (depends on T031)
- [X] T055 Implement an idempotency guard so `PuzzleSolved` cannot be applied twice in `src/AI.SpectrumAdventure.Domain/Puzzles/Puzzle.cs` (depends on T029)
- [X] T056 [P] Unit tests: a failed `RulesValidationResult` never produces `GameEvent`s and leaves `Game` state unchanged in `tests/AI.SpectrumAdventure.Domain.Tests/InvalidActionSafetyTests.cs`
- [X] T057 [P] Unit tests: repeating the same action under the same state returns a result consistent with the prior attempt (Repeated Action edge case) in `tests/AI.SpectrumAdventure.Domain.Tests/RepeatedActionConsistencyTests.cs`
- [X] T058 Unit tests: `EventHistory` ordering and append-only guarantee across a scripted 15+ turn sequence (SC-008) in `tests/AI.SpectrumAdventure.Domain.Tests/EventHistoryConsistencyTests.cs` (depends on T054)

**Checkpoint**: State transitions are controlled, validated, testable, and provably cannot corrupt `Game` on invalid input.

---

## Phase 5: Application Gameplay Workflow

**Purpose**: Coordinate the deterministic pipeline end-to-end with temporary/templated narration (no AI dependency yet).

- [X] T059 [P] Define the `IGameRepository` abstraction in `src/AI.SpectrumAdventure.Application/Abstractions/IGameRepository.cs`
- [X] T060 Implement `StartGameUseCase` (creates a `Game` via `AdventureWorldFactory`, persists it, returns the initial state projection) in `src/AI.SpectrumAdventure.Application/Games/StartGameUseCase.cs` (depends on T032, T059)
- [X] T061 Implement `LoadGameUseCase` (fetch by `GameId`) in `src/AI.SpectrumAdventure.Application/Games/LoadGameUseCase.cs` (depends on T059)
- [X] T062 Implement the `AdventureOrchestrator.ProcessAction` skeleton (`ParsedIntent` → `RulesEngine` → `Game.Apply` → temporary templated narrative, no AI) in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T048, T054, T061)
- [X] T063 [US2] Implement `GetLocationDescriptionQuery` (deterministic templated description: atmosphere, exits, objects, characters) in `src/AI.SpectrumAdventure.Application/Games/GetLocationDescriptionQuery.cs` (depends on T062)
- [X] T064 [US7] Implement `GetInventoryQuery` in `src/AI.SpectrumAdventure.Application/Games/GetInventoryQuery.cs` (depends on T059)
- [X] T065 Define the `ActionResult` DTO (per `contracts/action-result.schema.json`) in `src/AI.SpectrumAdventure.Contracts/ActionResult.cs`
- [X] T066 Map `AdventureOrchestrator` output to the `ActionResult` DTO in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T062, T065)
- [X] T067 [P] Integration test: start game → look → move → take → use-missing-item deterministic loop against an in-memory repository test double, in `tests/AI.SpectrumAdventure.Application.Tests/AdventureOrchestratorTests.cs`
- [X] T068 [P] [US2] Integration test: repeated `look` returns identical factual content (US2 acceptance #4) in `tests/AI.SpectrumAdventure.Application.Tests/LocationDescriptionConsistencyTests.cs`

**Checkpoint**: The deterministic gameplay loop is orchestrated end-to-end (still with templated text, no AI, no real persistence yet).

---

## Phase 6: Initial Scenario Completion ("The Forgotten Tower")

**Purpose**: Populate the 4 MVP locations with real scenario content so the adventure is fully playable deterministically.

- [X] T069 [US1] Author Forest Entrance content: base description, an examinable sign (environmental clue), collectible/interactive starter object, and the north path exit, in `src/AI.SpectrumAdventure.Domain/Games/AdventureWorldFactory.cs` (depends on T032)
- [X] T070 [US2] [US6] Author Dark Forest content: ancient-trees description, exits to Old Bridge (east), Forgotten Tower (west), and Forest Entrance (south), and NPC placement, in `src/AI.SpectrumAdventure.Domain/Games/AdventureWorldFactory.cs` (depends on T032, T027)
- [X] T071 [US9] Author Forgotten Tower content: a locked entrance `Exit` gated by a `tower-unlocked` `WorldFlagCondition`, wired to the `Puzzle` entity, in `src/AI.SpectrumAdventure.Domain/Games/AdventureWorldFactory.cs` (depends on T029, T032)
- [X] T072 [US7] [US9] Author Old Bridge content: the `BridgeKey` collectible `GameItem` (the puzzle's required item) plus a narrative clue/event, in `src/AI.SpectrumAdventure.Domain/Games/AdventureWorldFactory.cs` (depends on T032, T020)
- [X] T073 [US8] Author the NPC profile: personality traits, `KnowledgeBoundary` including the `tower-clue` topic, empty initial `ConversationMemory`, in `src/AI.SpectrumAdventure.Domain/Games/AdventureWorldFactory.cs` (depends on T027)
- [X] T074 [US9] Wire `Puzzle.RequiredItemId = BridgeKey` and `Puzzle.RequiredClueKey = "tower-clue"` per Clarification Q1's dual-condition solution, in `src/AI.SpectrumAdventure.Domain/Games/AdventureWorldFactory.cs` (depends on T029, T072, T073)
- [X] T075 [P] [US9] Acceptance test: full deterministic playthrough — explore all 4 locations, obtain `BridgeKey`, learn `tower-clue` (deterministic stand-in for the NPC reveal), solve the puzzle, confirm the tower entrance opens, in `tests/AI.SpectrumAdventure.Application.Tests/ForgottenTowerScenarioTests.cs` (depends on T069-T074)
- [X] T076 [P] [US9] Acceptance test: entering the Forgotten Tower before satisfying both conditions is consistently blocked across repeated attempts, in `tests/AI.SpectrumAdventure.Application.Tests/PuzzleBlockedEntryTests.cs`
- [X] T077 [P] [US9] Acceptance test: creative/incorrect puzzle-bypass attempts get a meaningful response without solving the puzzle, in `tests/AI.SpectrumAdventure.Application.Tests/PuzzleBypassAttemptTests.cs`

**Checkpoint**: The complete MVP scenario is playable end-to-end through deterministic/programmatic actions, with zero AI involvement.

---

## Phase 7: Persistence

**Purpose**: Ensure `GameState` survives application restarts and containers own no authoritative in-memory state (constitution Principles XVI/XVII).

- [X] T078 [P] Add EF Core + Npgsql package references to `AI.SpectrumAdventure.Infrastructure` per research.md Decision 1
- [X] T079 Create `AdventureDbContext` with entity/owned-type configurations mapping the `Game` aggregate (Player, Inventory, Puzzle, WorldFlags, EventHistory) in `src/AI.SpectrumAdventure.Infrastructure/Persistence/AdventureDbContext.cs` (depends on T031, T078)
- [X] T080 Configure the `RowVersion` optimistic-concurrency token for `Game` per research.md Decision 7 in `src/AI.SpectrumAdventure.Infrastructure/Persistence/Configurations/GameConfiguration.cs` (depends on T079)
- [X] T081 Implement `EfGameRepository : IGameRepository` with load/save and a single automatic retry on a concurrency conflict, in `src/AI.SpectrumAdventure.Infrastructure/Persistence/EfGameRepository.cs` (depends on T059, T080)
- [X] T082 Add the initial EF Core migration (`InitialCreate`) in `src/AI.SpectrumAdventure.Infrastructure/Persistence/Migrations/` (depends on T079)
- [X] T083 Register `AdventureDbContext` and `EfGameRepository` in the DI composition root in `src/AI.SpectrumAdventure.Web/Program.cs` (depends on T081)
- [X] T084 [P] Integration test: saving then loading a game reproduces an identical `Game` state, in `tests/AI.SpectrumAdventure.IntegrationTests/GamePersistenceTests.cs`
- [X] T085 [P] Integration test: a concurrent action on the same `GameId` triggers the retry-once path then a friendly conflict response, in `tests/AI.SpectrumAdventure.IntegrationTests/ConcurrencyConflictTests.cs`
- [X] T086 [P] Integration test: an unavailable database surfaces a controlled failure without any partial state commit, in `tests/AI.SpectrumAdventure.IntegrationTests/PersistenceFailureTests.cs`
- [X] T087 Document local PostgreSQL/Azurite startup (docker compose) and EF Core migration commands in `README.md` (depends on T082)

**Checkpoint**: `Game` state is durable across process restarts; the Domain layer remains persistence-ignorant.

---

## Phase 8: Player Input Interpretation

**Purpose**: Transform player text (classic commands and free natural language) into a structured `ParsedIntent`, without touching `GameState` directly (constitution Principle VI).

- [X] T088 Implement a deterministic `CommandPatternInterpreter` recognizing classic-command shapes and common synonyms (`LOOK`, `EXAMINE <target>`, `GO <direction>`, `TAKE <item>`, `OPEN <target>`, `TALK TO <npc>`, `USE <item> [ON <target>]`) with zero LLM cost, per research.md Decision 3, in `src/AI.SpectrumAdventure.Agents/Intent/CommandPatternInterpreter.cs`
- [X] T089 [US4] Define the `IIntentInterpreter` abstraction in `src/AI.SpectrumAdventure.Application/Abstractions/IIntentInterpreter.cs` (depends on T039)
- [X] T090 [US3] [US4] Implement `TwoStageIntentInterpreter` (pattern matcher first, LLM-backed fallback only on no-match) in `src/AI.SpectrumAdventure.Agents/Intent/TwoStageIntentInterpreter.cs` (depends on T088, T089)
- [X] T091 [US4] Handle the ambiguous-intent outcome (low confidence) by asking a natural in-world clarifying question instead of guessing wrong (Edge Case: Ambiguous Action) in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T062, T090)
- [X] T092 [US3] [US4] Handle unrecognized/unsupported actions with a meaningful in-world message (FR-006) instead of a technical error, in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T091)
- [X] T093 Wire `TwoStageIntentInterpreter` into `AdventureOrchestrator.ProcessAction`, replacing Phase 5's temporary stub, in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T090, T062)
- [X] T094 [P] [US3] Unit tests: classic commands and reasonable wording variations (`look`/`look around`/`l`, `take`/`grab`/`pick up`) in `tests/AI.SpectrumAdventure.Agents.Tests/CommandPatternInterpreterTests.cs`
- [X] T095 [P] [US4] Unit tests: natural-language phrases map to the correct `ParsedIntent` via a mocked LLM fallback in `tests/AI.SpectrumAdventure.Agents.Tests/TwoStageIntentInterpreterTests.cs`
- [X] T096 [P] [US4] Unit tests: ambiguous input triggers the clarifying-question path in `tests/AI.SpectrumAdventure.Application.Tests/AmbiguousActionTests.cs`
- [X] T097 [P] Unit tests: invalid/unparseable input returns an in-world message, never an unhandled exception, in `tests/AI.SpectrumAdventure.Agents.Tests/InvalidInputHandlingTests.cs`

**Checkpoint**: Both classic commands and free natural language resolve to structured intents; the Rules Engine (not the interpreter) still decides feasibility.

---

## Phase 9: Microsoft Agent Framework Integration

**Purpose**: Introduce the Agent Framework only now that the deterministic loop is proven, per constitution Principle XX.

- [X] T098 Add the Microsoft Agent Framework NuGet package(s) to `AI.SpectrumAdventure.Agents` per research.md Decision 4
- [X] T099 Configure the Agent Framework chat-client/model-provider registration (Azure AI Foundry endpoint, `DefaultAzureCredential`) in `src/AI.SpectrumAdventure.Web/Program.cs` (depends on T098)
- [X] T100 Define `INarratorAgent`, `INpcAgent`, `IVisualArtDirector`, `IIntentInterpreterAgent` abstractions in `src/AI.SpectrumAdventure.Application/Abstractions/` (depends on T098)
- [X] T101 Configure structured-output/response-format enforcement per agent against the `contracts/*.schema.json` files in `src/AI.SpectrumAdventure.Agents/` (depends on T100)
- [X] T102 Implement schema-invalid-output handling: validate once, retry once, then fall back to a safe default response, in `src/AI.SpectrumAdventure.Agents/StructuredOutputValidator.cs` (depends on T101)
- [X] T103 [P] Create mocked `IChatClient`-based test doubles for all agents per research.md Decision 6 in `tests/AI.SpectrumAdventure.Agents.Tests/TestDoubles/`
- [X] T104 Add an `AgentTelemetry` activity-source instrumentation hook around every agent invocation (agent name, duration, success/failure, schema-valid flag) in `src/AI.SpectrumAdventure.Agents/AgentTelemetry.cs` (depends on T100)
- [X] T105 [P] Unit tests: schema-invalid agent output triggers retry-then-fallback without throwing to the player, in `tests/AI.SpectrumAdventure.Agents.Tests/StructuredOutputValidatorTests.cs`

**Checkpoint**: Agent Framework is wired in with enforced structured contracts and full test-double coverage; no agent can yet mutate `GameState` (no agent implementations exist beyond the shared plumbing).

---

## Phase 10: Narrator Agent

**Purpose**: Implement the Game Director / Narrator (constitution's Agent Architecture § Game Director / Narrator).

- [X] T106 Define the `NarrationResult` DTO (per `contracts/narration-result.schema.json`) in `src/AI.SpectrumAdventure.Contracts/NarrationResult.cs`
- [X] T107 Define the Narrator input context projection (validated `ActionResult` + relevant Location/Item/WorldFlag facts, not a full `GameState` dump) in `src/AI.SpectrumAdventure.Contracts/NarrationResult.cs` (co-located with `NarrationResult` to avoid an Agents→Application circular reference; depends on T065)
- [X] T108 Author Narrator system instructions enforcing tone and "describe validated outcomes only, never invent facts" per constitution Principle IV, in `src/AI.SpectrumAdventure.Agents/Narrator/NarratorInstructions.cs`
- [X] T109 Implement `NarratorAgent : INarratorAgent` generating a `NarrationResult` from `NarratorContext`, in `src/AI.SpectrumAdventure.Agents/Narrator/NarratorAgent.cs` (depends on T106, T107, T108, T101)
- [X] T110 Wire `NarratorAgent` into `AdventureOrchestrator` via the new `ProcessActionWithNarrationAsync` entry point (kept alongside the original synchronous `ProcessAction` for backward compatibility with existing call sites), in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T109, T093)
- [X] T111 Handle Narrator agent failure by falling back to a minimal deterministic templated description so gameplay is never blocked, in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T110, T102)
- [X] T112 [P] Unit tests: Narrator output for successful movement/examine/take actions (mocked agent) in `tests/AI.SpectrumAdventure.Agents.Tests/NarratorAgentTests.cs`
- [X] T113 [P] Unit tests: Narrator output for failed actions (blocked movement, missing item, impossible action) stays consistent with `RulesValidationResult.Reason`, in `tests/AI.SpectrumAdventure.Agents.Tests/NarratorFailureNarrationTests.cs`
- [X] T114 [P] Integration test: a Narrator agent failure does not interrupt the gameplay loop, in `tests/AI.SpectrumAdventure.IntegrationTests/NarratorFailureFallbackTests.cs`

**Checkpoint**: All player actions now receive AI-generated narrative text, with a safe deterministic fallback on agent failure.

---

## Phase 11: NPC Agent

**Purpose**: Implement NPC dialogue (spec US8), strictly bounded by authoritative knowledge.

- [X] T115 [US8] Define the `NpcResponse` DTO (per `contracts/npc-response.schema.json`) in `src/AI.SpectrumAdventure.Contracts/NpcResponse.cs`
- [X] T116 [US8] Define the NPC Agent input context (`PersonalityProfile`, `KnowledgeBoundary` restricted to currently-unlocked topics, recent `ConversationMemory`, current player utterance) in `src/AI.SpectrumAdventure.Contracts/NpcResponse.cs` (co-located `NpcContext`; depends on T027, T115)
- [X] T117 [US8] Implement `NpcAgent : INpcAgent` generating an `NpcResponse` constrained to `NpcContext.KnowledgeBoundary`, in `src/AI.SpectrumAdventure.Agents/Npc/NpcAgent.cs` (depends on T116, T101)
- [X] T118 [US8] Implement orchestrator-side validation that rejects/regenerates any `NpcResponse.revealedKnowledgeKeys` not present in `Npc.KnowledgeBoundary` (enforces FR-017), in `src/AI.SpectrumAdventure.Application/Orchestration/NpcConversationOrchestrator.cs` (depends on T117, T047)
- [X] T119 [US8] Append a validated `NpcResponse` to `Npc.ConversationMemory` via an `NpcRelationshipChanged` event when a clue/fact is revealed (e.g., `tower-clue`), in `src/AI.SpectrumAdventure.Application/Orchestration/NpcConversationOrchestrator.cs` (depends on T118, T074)
- [X] T120 [US8] Implement proactive topic suggestion when player input is generic/unsure (Clarification Q3), in `src/AI.SpectrumAdventure.Agents/Npc/NpcAgent.cs` (via NpcInstructions system prompt; depends on T117)
- [X] T121 [P] [US8] Unit tests: NPC personality/knowledge consistency across a 3+ turn conversation (mocked agent + real `ConversationMemory`), in `tests/AI.SpectrumAdventure.Agents.Tests/NpcConsistencyTests.cs`
- [X] T122 [P] [US8] Unit tests: the NPC refuses to reveal knowledge outside its boundary even under repeated/creative questioning, in `tests/AI.SpectrumAdventure.Agents.Tests/NpcKnowledgeBoundaryTests.cs`
- [X] T123 [P] [US8] [US9] Unit tests: `tower-clue` is only revealed via a relevant question/action and persists in `Player.KnownClues` once revealed, in `tests/AI.SpectrumAdventure.Application.Tests/NpcClueRevealTests.cs`

**Checkpoint**: The NPC is conversational, consistent, and cannot leak information outside its authoritative knowledge boundary.

---

## Phase 12: Visual Art Director

**Purpose**: Transform validated scene information into a structured, retro-styled visual specification (constitution's Agent Architecture § Visual Art Director).

- [X] T124 Define the `VisualSceneSpec` DTO (per `contracts/visual-scene-spec.schema.json`) in `src/AI.SpectrumAdventure.Contracts/VisualSceneSpec.cs`
- [X] T125 Define the Visual Art Director input context (current `Location`, visible `GameItem`s, mood/narrative context — never raw `GameState`) in `src/AI.SpectrumAdventure.Contracts/VisualSceneSpec.cs` (co-located `VisualContext`; depends on T124)
- [X] T126 [US10] Author retro 8-bit/ZX-Spectrum style constraints (fixed `style` field, bounded object list) in `src/AI.SpectrumAdventure.Agents/VisualArtDirector/RetroStyleConstraints.cs`
- [X] T127 [US10] Implement `VisualArtDirectorAgent` generating a `VisualSceneSpec` from `VisualContext`, in `src/AI.SpectrumAdventure.Agents/VisualArtDirector/VisualArtDirectorAgent.cs` (depends on T125, T126, T101)
- [X] T128 Compute a deterministic `sceneStateKey` (locationId + relevant `WorldFlag`s) alongside the agent output, in `src/AI.SpectrumAdventure.Agents/VisualArtDirector/SceneStateKeyBuilder.cs` (depends on T124)
- [X] T129 [P] Unit tests: `VisualSceneSpec` always uses the fixed retro style tag and cannot mutate `GameState`, in `tests/AI.SpectrumAdventure.Agents.Tests/VisualArtDirectorAgentTests.cs`
- [X] T130 [P] Unit tests: `sceneStateKey` changes only when a materially relevant `WorldFlag` changes, not on unrelated actions, in `tests/AI.SpectrumAdventure.Agents.Tests/SceneStateKeyBuilderTests.cs`

**Checkpoint**: Visual scene specifications are structured, retro-constrained, and never touch authoritative state.

---

## Phase 13: Image Generation Pipeline

**Purpose**: Generate and store retro visuals asynchronously, without ever blocking gameplay (FR-024/FR-025, constitution Principle XXI).

- [X] T131 Define the `VisualAsset` read model (`SceneStateKey`, `BlobUri`, `Status`, `GeneratedAt`) in `src/AI.SpectrumAdventure.Domain/Games/VisualAsset.cs`
- [X] T132 Implement a `Channel<T>`-based `ImageGenerationQueue` and an `ImageGenerationWorker` `BackgroundService` per research.md Decision 9, in `src/AI.SpectrumAdventure.Agents/ImagePipeline/ImageGenerationWorker.cs` (depends on T131)
- [X] T133 Implement the image-generation call (Azure AI Foundry image deployment) from a `VisualSceneSpec`, in `src/AI.SpectrumAdventure.Agents/ImagePipeline/ImageGenerator.cs` (depends on T124)
- [X] T134 Implement deterministic retro post-processing (palette quantization, downscale, optional dithering) per research.md Decision 5, in `src/AI.SpectrumAdventure.Agents/ImagePipeline/RetroImageProcessor.cs` (depends on T133)
- [X] T135 Implement `BlobVisualAssetStore` (upload processed image, return a blob URI) using `DefaultAzureCredential`, in `src/AI.SpectrumAdventure.Infrastructure/Storage/BlobVisualAssetStore.cs` (depends on T131)
- [X] T136 Wire the orchestrator to enqueue an `ImageGenerationRequested` work item on scene change instead of blocking the narrative response (FR-024/FR-025), in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T132, T110)
- [X] T137 Handle image generation/storage failures: mark `VisualAsset.Status = Failed`, trace the failure, leave gameplay unaffected (Edge Case: Visual Failure), in `src/AI.SpectrumAdventure.Agents/ImagePipeline/ImageGenerationWorker.cs` (depends on T132)
- [X] T138 [P] Integration test: successful end-to-end image generation updates `VisualAsset` to `Ready` with a blob URI, in `tests/AI.SpectrumAdventure.IntegrationTests/ImageGenerationPipelineTests.cs`
- [X] T139 [P] [US10] Integration test: an image-generation failure leaves gameplay uninterrupted and the narrative response unaffected, in `tests/AI.SpectrumAdventure.IntegrationTests/ImageGenerationFailureTests.cs`
- [X] T140 [P] [US10] Integration test: the narrative response completes before image generation finishes (non-blocking behavior), in `tests/AI.SpectrumAdventure.IntegrationTests/NonBlockingImageGenerationTests.cs`

**Checkpoint**: Retro visuals generate asynchronously; failures degrade gracefully without affecting gameplay.

---

## Phase 14: Image Cache and Scene Versioning

**Purpose**: Avoid unnecessary regeneration of unchanged scenes (constitution Principle X).

- [X] T141 Implement a `VisualAssetCache` lookup by `sceneStateKey` before enqueuing generation (reuse an existing `Ready` asset), in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T128, T136)
- [X] T142 Persist `VisualAsset` records keyed by `sceneStateKey` in `AdventureDbContext`, in `src/AI.SpectrumAdventure.Infrastructure/Persistence/Configurations/VisualAssetConfiguration.cs` (depends on T131, T079)
- [X] T143 [US10] Skip visual regeneration for actions that don't materially change the scene (FR-024), in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T141)
- [X] T144 [P] Unit tests: an identical `sceneStateKey` reuses the cached `VisualAsset` without a new generation request, in `tests/AI.SpectrumAdventure.Application.Tests/VisualAssetCacheTests.cs`
- [X] T145 [P] Unit tests: a materially changed scene (e.g., tower unlocked) produces a new `sceneStateKey` and triggers regeneration, in `tests/AI.SpectrumAdventure.Application.Tests/SceneChangeDetectionTests.cs`

**Checkpoint**: Visual generation only happens when the scene has meaningfully changed.

---

## Phase 15: Web API and Blazor UI

**Purpose**: Make the complete gameplay loop playable through a web interface, with no authoritative rules in the UI.

- [X] T146 [US1] Create the `AdventurePage` Blazor component (game start, narrative display, command input, submit) in `src/AI.SpectrumAdventure.Web/Components/AdventurePage.razor` (depends on T060, T093)
- [X] T147 [P] [US7] Create the `InventoryPanel` component in `src/AI.SpectrumAdventure.Web/Components/InventoryPanel.razor` (depends on T064)
- [X] T148 [P] [US2] Create the `LocationHeader` component (current location name/exits) in `src/AI.SpectrumAdventure.Web/Components/LocationHeader.razor` (depends on T063)
- [X] T149 [P] [US10] Create the `SceneImage` component (renders the current `VisualAsset` or a placeholder) in `src/AI.SpectrumAdventure.Web/Components/SceneImage.razor` (depends on T131)
- [X] T150 Create the `CommandInput` component (submits a `PlayerActionRequest`, disables while processing) in `src/AI.SpectrumAdventure.Web/Components/CommandInput.razor` (depends on T146)
- [X] T151 Implement a processing-feedback UI state (spinner/"the world responds..." indicator) while an action is in flight, in `src/AI.SpectrumAdventure.Web/Components/AdventurePage.razor` (depends on T150)
- [X] T152 Implement in-world error/edge-case presentation (invalid movement, missing item, impossible action) reusing `ActionResult.reason`, in `src/AI.SpectrumAdventure.Web/Components/AdventurePage.razor` (depends on T146, T065)
- [X] T153 [US8] Create the `NpcDialoguePanel` component for conversation presentation, in `src/AI.SpectrumAdventure.Web/Components/NpcDialoguePanel.razor` (depends on T115)
- [X] T154 [P] Integration test: `WebApplicationFactory`-based smoke test of the start-game → submit-action round trip through the UI composition root, in `tests/AI.SpectrumAdventure.IntegrationTests/WebSmokeTests.cs`

**Checkpoint**: The full MVP gameplay loop (US1-US9) is playable through the web UI, with visuals still pending Phase 16/17 polish.

---

## Phase 16: Retro UI Experience

**Purpose**: Reinforce the project's ZX-Spectrum-inspired identity (US10) without harming usability.

- [X] T155 [US10] Apply a ZX-Spectrum-inspired colour palette and retro typography as a shared theme in `src/AI.SpectrumAdventure.Web/wwwroot/css/retro-theme.css`
- [X] T156 [US10] Apply pixel-art-consistent rendering (`image-rendering: pixelated`, fixed aspect-ratio frame) to `SceneImage`, in `src/AI.SpectrumAdventure.Web/Components/SceneImage.razor` (depends on T149, T155)
- [X] T157 [US10] Add an optional, toggle-able CRT-inspired effect (scanlines/vignette) in `src/AI.SpectrumAdventure.Web/wwwroot/css/retro-theme.css` (depends on T155)
- [X] T158 [US10] Implement a retro-styled loading/placeholder state for `SceneImage` while an image is `Pending`, in `src/AI.SpectrumAdventure.Web/Components/SceneImage.razor` (depends on T149)
- [X] T159 Accessibility pass verifying retro styling preserves readable contrast and keyboard operability for `CommandInput`/`AdventurePage`, in `src/AI.SpectrumAdventure.Web/Components/` (depends on T155-T158)

**Checkpoint**: The UI visually reflects the retro 8-bit identity while remaining fully usable.

---

## Phase 17: Asynchronous UI Updates

**Purpose**: Let the player keep playing while an image generates in the background (US10 acceptance #4, constitution Principle XXII).

- [X] T160 Implement an in-process per-`GameId` `SceneUpdateNotifier` service per research.md Decision 10, in `src/AI.SpectrumAdventure.Web/Services/SceneUpdateNotifier.cs` (depends on T132)
- [X] T161 Subscribe `SceneImage` to `SceneUpdateNotifier` and re-render when the worker finishes (depends on T160, T149)
- [X] T162 Implement a client-side polling fallback (documented alternative per research.md Decision 10) in `src/AI.SpectrumAdventure.Web/Components/SceneImage.razor`
- [X] T163 [P] [US10] Integration test: the player can submit a new action while a previous scene's image is still generating, without being blocked, in `tests/AI.SpectrumAdventure.IntegrationTests/AsyncUiUpdateTests.cs`

**Checkpoint**: Image updates arrive progressively without ever blocking player input.

---

## Phase 18: Observability

**Purpose**: Make agent/rules/persistence/image-pipeline behavior traceable before production deployment (constitution Principle IX).

- [X] T164 Add OpenTelemetry SDK + Azure Monitor exporter packages to `AI.SpectrumAdventure.Web` and `AI.SpectrumAdventure.Infrastructure` per research.md Decision 8
- [X] T165 Configure OpenTelemetry tracing/metrics registration (console exporter for local dev, Azure Monitor exporter for production) in `src/AI.SpectrumAdventure.Web/Program.cs` (depends on T164)
- [X] T166 Instrument `AdventureOrchestrator.ProcessAction` with one span per player action (action type, outcome), in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T165)
- [X] T167 Instrument `RulesEngine.Validate` with validation result/reason tagging, in `src/AI.SpectrumAdventure.Application/Rules/RulesEngine.cs` (depends on T165)
- [X] T168 Instrument agent invocations (name, duration, success/failure, schema-valid flag) using `AgentTelemetry` from Phase 9, in `src/AI.SpectrumAdventure.Agents/AgentTelemetry.cs` (depends on T104, T165)
- [X] T169 Instrument the image pipeline (generation attempt, duration, success/failure, cache hit/miss), in `src/AI.SpectrumAdventure.Agents/ImagePipeline/ImageGenerationWorker.cs` (depends on T165, T132)
- [X] T170 Instrument persistence errors (`EfGameRepository` save/load failures), in `src/AI.SpectrumAdventure.Infrastructure/Persistence/EfGameRepository.cs` (depends on T165, T081)
- [X] T171 Add a redaction/allow-list policy excluding raw player utterances/agent prompt content from default telemetry (metadata only), in `src/AI.SpectrumAdventure.Agents/AgentTelemetry.cs` (depends on T168)
- [X] T172 [P] Integration test: a simulated agent failure and a simulated persistence failure both produce an observable trace/log entry, in `tests/AI.SpectrumAdventure.IntegrationTests/ObservabilityTests.cs`

**Checkpoint**: Every significant execution path is observable without leaking sensitive content.

---

## Phase 19: Containerization

**Purpose**: Package the modular monolith as a single container image with no local-filesystem dependency (constitution Principle XI).

- [X] T173 Create a multi-stage `Dockerfile` for `AI.SpectrumAdventure.Web` at the repository root
- [X] T174 Configure environment-based configuration (connection strings, AI endpoint, storage account) via environment variables, with no secrets baked into the image, in `src/AI.SpectrumAdventure.Web/appsettings.json`
- [X] T175 Add a `.dockerignore` excluding build artifacts/secrets from the image context at the repository root
- [X] T176 Verify local container build/run against local PostgreSQL/Azurite (docker compose) and document the steps in `README.md` (depends on T173, T174)
- [X] T177 [P] Integration/smoke test: the containerized app responds to a health-check endpoint, in `tests/AI.SpectrumAdventure.IntegrationTests/ContainerHealthCheckTests.cs`

**Checkpoint**: The application runs correctly as a stateless container.

---

## Phase 20: Azure Container Apps Deployment

**Purpose**: Ship the MVP to the production target with a minimal, justified topology (constitution Principles XI/XII/XVIII/XIX).

- [X] T178 Identify and document required Azure resources (Container Apps Environment, Container App, managed PostgreSQL, Storage Account, Azure AI Foundry endpoint, Application Insights, Container Registry) in `specs/001-conversational-adventure-mvp/deployment-notes.md`
- [X] T179 Author Infrastructure-as-Code (Bicep) for the resources above, parameterized per environment, in `infra/main.bicep`
- [X] T180 Configure Azure Container Registry build/push for the Phase 19 Dockerfile, in `infra/` (depends on T173, T179)
- [X] T181 Configure the Container App's environment variables/secret references, in `infra/containerapp.bicep` (depends on T179)
- [X] T182 Configure a user-assigned Managed Identity with role assignments for Blob Storage, Azure AI Foundry, and the database per research.md Decision 11, in `infra/identity.bicep` (depends on T179)
- [X] T183 Configure Application Insights and wire the OpenTelemetry Azure Monitor exporter's connection string from configuration (depends on T165, T179)
- [X] T184 Add a build → test → container-build → deploy CI/CD pipeline per constitution Principle XIX, in `.github/workflows/deploy.yml` (depends on T176)
- [X] T185 Deploy to Azure Container Apps and record a smoke-test health verification (a deployment is not complete until this succeeds) (depends on T180, T181, T182, T183)

**Checkpoint**: The MVP runs in Azure Container Apps as a single, observable, Managed-Identity-secured Container App.

---

## Phase 21: End-to-End Validation

**Purpose**: Prove the full MVP against every spec user story and constitution acceptance scenario.

- [X] T186 [US1] End-to-end test: Start Game — a new adventure begins at Forest Entrance with narrative + visual and no extra input required, in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_StartGameTests.cs`
- [X] T187 [US5] End-to-end test: Movement — Forest Entrance → Dark Forest updates state and narrative correctly, in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_MovementTests.cs`
- [X] T188 [US5] End-to-end test: Invalid Movement — a blocked attempt leaves state unchanged with meaningful feedback, in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_InvalidMovementTests.cs`
- [X] T189 [US6] End-to-end test: Object Interaction — examine/interact responses stay consistent with world state, in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_ObjectInteractionTests.cs`
- [X] T190 [US7] End-to-end test: Inventory — obtain `BridgeKey`, view inventory, later use it successfully, in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_InventoryTests.cs`
- [X] T191 [US8] End-to-end test: NPC — a multi-turn conversation stays consistent with personality/knowledge boundaries, in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_NpcInteractionTests.cs`
- [X] T192 [US9] End-to-end test: Puzzle — discover the clue and item, satisfy both conditions, tower entrance unlocks and persists, in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_PuzzleSolvedTests.cs`
- [X] T193 End-to-end test: AI Failure — a simulated Narrator/NPC agent failure keeps `Game` state valid and the failure observable, in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_AgentFailureTests.cs`
- [X] T194 [US10] End-to-end test: Image Failure — a simulated image-pipeline failure does not interrupt gameplay, in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_ImageFailureTests.cs`
- [X] T195 End-to-end test: multi-turn consistency — a scripted 15+ turn session asserts no previously established fact is contradicted (SC-008), in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_MultiTurnConsistencyTests.cs`
- [X] T198 [P] [US3] [US4] Statistical acceptance test: run a fixed corpus of classic-command inputs and natural-language inputs and assert ≥90% correct-result rate for classic commands (SC-003) and ≥80% coherent-response rate for natural-language actions (SC-004), in `tests/AI.SpectrumAdventure.IntegrationTests/E2E_SuccessRateThresholdTests.cs`
- [X] T196 Execute the full [quickstart.md](./quickstart.md) manual validation checklist against the deployed Azure Container Apps environment and record results (depends on T185)
- [X] T197 Final Constitution Compliance re-check confirming all 22 principles remain satisfied in the shipped implementation, updating plan.md's Post-Design Re-check note if anything shifted (depends on T196, T198)

**Checkpoint**: The MVP satisfies every spec Success Criterion (SC-001–SC-010) and every constitution acceptance scenario.

---

## Dependencies & Execution Order

### Phase Dependencies (strict, per user's required ordering)

```text
Phase 1 (Setup)
  → Phase 2 (Domain Foundation)
    → Phase 3 (Rules Engine)
      → Phase 4 (Game Events & State Transitions)
        → Phase 5 (Application Gameplay Workflow)
          → Phase 6 (Initial Scenario Completion) — deterministic MVP playable here
            → Phase 7 (Persistence)
              → Phase 8 (Player Input Interpretation)
                → Phase 9 (Microsoft Agent Framework Integration)
                  → Phase 10 (Narrator Agent)
                    → Phase 11 (NPC Agent)
                      → Phase 12 (Visual Art Director)
                        → Phase 13 (Image Generation Pipeline)
                          → Phase 14 (Image Cache & Scene Versioning)
                            → Phase 15 (Web API & Blazor UI)
                              → Phase 16 (Retro UI Experience)
                                → Phase 17 (Asynchronous UI Updates)
                                  → Phase 18 (Observability)
                                    → Phase 19 (Containerization)
                                      → Phase 20 (Azure Container Apps Deployment)
                                        → Phase 21 (End-to-End Validation)
```

Rule enforced throughout: **the deterministic playable game (end of Phase 6) exists before AI becomes a dependency for gameplay (Phase 9+).** AI is never on the critical path for movement, inventory, or puzzle validation — only for narration, dialogue, and visuals.

### User Story Coverage Map

| Story | Primary phases | Independently testable at |
|---|---|---|
| US1 Start Adventure | 5, 6, 15, 21 | End of Phase 6 (deterministic), confirmed end-to-end in Phase 21 (T186) |
| US2 Understand Location | 5, 15 | End of Phase 5 (T063, T068) |
| US3 Classic Commands | 8 | End of Phase 8 (T094) |
| US4 Natural Language | 8 | End of Phase 8 (T095, T096) |
| US5 Explore World | 3, 6, 21 | End of Phase 6, confirmed in Phase 21 (T187, T188) |
| US6 Interact With Objects | 3, 6, 21 | End of Phase 6, confirmed in Phase 21 (T189) |
| US7 Manage Inventory | 3, 5, 6, 15, 21 | End of Phase 6, confirmed in Phase 21 (T190) |
| US8 Interact With NPC | 11, 15, 21 | End of Phase 11 (T121-T123), confirmed in Phase 21 (T191) |
| US9 Solve Puzzle | 3, 6, 11, 21 | End of Phase 6 (T075-T077), confirmed in Phase 21 (T192) |
| US10 Retro Visual | 12, 13, 16, 17, 21 | End of Phase 17, confirmed in Phase 21 (T194) |

### Within Each Phase

- Domain/Contract models before the logic that consumes them
- Rules before events before orchestration before agents (never the reverse)
- Tests for a component follow immediately after that component's implementation tasks within the same phase
- A phase's `**Checkpoint**` marks the point at which its capability is independently verifiable

### Parallel Opportunities

- All `[P]`-marked Setup tasks (T003-T006, T009-T011, T014-T015) can run in parallel once their prerequisite exists
- Independent Domain value objects/entities in Phase 2 marked `[P]` (T018-T020, T022, T024, T026, T028) can be built in parallel before the aggregate root (T031) that composes them
- Independent Rules modules in Phase 3 (T042-T047) can be built in parallel once T041 exists, since each targets a different file and a different `ParsedIntent.Action`
- Test tasks marked `[P]` within any phase can run in parallel with each other (different test files)
- Phase 6's four location-content tasks (T069-T072) can run in parallel; T073/T074 depend on the NPC/Puzzle domain types but not on each other's files
- Phase 15's UI component tasks marked `[P]` (T147-T149) can run in parallel once their respective query/contract dependency exists

## Parallel Example: Phase 2 (Domain Foundation)

```text
Launch together (different files, no shared dependency yet):
  T018 Identifiers.cs
  T019 WorldFlag.cs
  T020 ItemState.cs
  T022 Exit.cs
  T024 Inventory.cs
  T026 ConversationTurn.cs
  T028 PuzzleSolutionCondition.cs
Then sequentially: T021 → T023 → T025 → T027 → T029 → T030 → T031 → T032
Then in parallel: T033, T034, T035, T036 (each a distinct test file)
```

## Parallel Example: Phase 3 (Rules Engine)

```text
After T041 exists, launch together (distinct rule files, distinct ParsedIntent.Action values):
  T042 MovementRules.cs
  T043 ObjectRules.cs
  T044 InventoryRules.cs (T045 depends on T044, run after)
  T046 PuzzleRules.cs
  T047 NpcPresenceRules.cs
Then: T048 (dispatch wiring)
Then in parallel: T049, T050, T051, T052 (distinct test files); T053 after T048.
```

## Implementation Strategy

1. **Deterministic-first MVP (Phases 1-7)**: Deliver a fully playable, persisted, AI-free version of "The Forgotten Tower" first. This is the largest risk-reduction milestone — it proves the domain model, rules, and world content are correct before any AI cost/latency/nondeterminism is introduced.
2. **Add player expressiveness (Phase 8)**: Layer in classic-command + natural-language interpretation on top of the already-correct deterministic engine.
3. **Introduce AI incrementally (Phases 9-14)**: Agent Framework plumbing → Narrator → NPC → Visual Art Director → Image Pipeline → Cache, each with its own safe-fallback behavior so a failure at any AI stage never blocks or corrupts gameplay.
4. **User-facing polish (Phases 15-17)**: Build the Blazor UI once the underlying contracts (ActionResult, VisualAsset) are stable, then layer retro styling and async image updates.
5. **Operational readiness (Phases 18-20)**: Observability, containerization, and Azure Container Apps deployment — deliberately sequenced *after* the gameplay experience is proven locally, per the user's explicit instruction not to let infrastructure work obscure gameplay validation.
6. **Final validation (Phase 21)**: Full end-to-end proof against every spec user story, edge case, and constitution acceptance scenario, closing with a Constitution Compliance re-check.

**Suggested MVP demo checkpoint**: End of Phase 6 — a fully playable, deterministic, test-covered "Forgotten Tower" adventure (no AI, no web UI yet) is the earliest point at which the core gameplay loop and puzzle can be demonstrated and validated against the spec's Success Criteria (minus AI narration and visuals).

## Format Validation

All 198 tasks above follow the required checklist format: `- [ ] T### [P?] [USn?] Description with exact file path`. Setup (Phase 1), Foundational-equivalent phases (2-7, 9, 13, 14, 17-20) carry no story label where the task serves the architecture as a whole; every task that clearly serves one or more spec user stories carries the corresponding `[USn]` label(s); every task references at least one concrete file path under `src/` or `tests/`, or a named infrastructure/documentation artifact (`infra/`, `README.md`, `.github/workflows/`).
