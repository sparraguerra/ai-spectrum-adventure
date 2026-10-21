# Tasks: Navigation History and Vertical Movement

**Input**: Design documents from `/specs/005-navigation-history-intents/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Included because the specification defines independent test criteria and acceptance scenarios, and the quickstart identifies the required automated validation.

**Organization**: Tasks are grouped by the two P1 user stories to preserve independently testable increments.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks because the tasks touch different files and have no unfinished prerequisites.
- **[Story]**: Maps the task to the corresponding user story in `spec.md`.
- Each task names the exact source, test, or design file to change.

---

## Phase 1: Setup

**Purpose**: Use the existing .NET projects and test harness; no new project, package, database, or infrastructure setup is required.

No setup tasks are needed for this feature.

---

## Phase 2: Foundational

**Purpose**: Establish shared prerequisites before story implementation.

No shared foundational code blocks either story; both use the existing deterministic intent, rules, and event pipelines.

---

## Phase 3: User Story 1 - Return to the previous location (Priority: P1) 🎯 MVP

**Goal**: Recognize `back`, return to the most recently departed location, support repeated backtracking, and preserve the history across save/reload.

**Independent Test**: Move through locations A, B, and C, enter `back` twice, and verify the player returns to B and then A. Verify an empty history leaves the player in place and communicates that no previous location exists.

### Tests for User Story 1

- [X] T001 [P] [US1] Add interpreter coverage for targetless `back` recognition in `tests/AI.SpectrumAdventure.Agents.Tests/TwoStageIntentInterpreterTests.cs`.
- [X] T002 [P] [US1] Add domain tests for pushing origins on ordinary movement, popping one location on backtracking, and preserving state on an empty stack in `tests/AI.SpectrumAdventure.Domain.Tests/NavigationHistoryTests.cs`.
- [X] T003 [P] [US1] Add snapshot round-trip and legacy-snapshot history reconstruction cases in `tests/AI.SpectrumAdventure.Domain.Tests/GameSnapshotRoundTripTests.cs`.
- [X] T004 [P] [US1] Add orchestration tests for a single return, repeated returns, empty history feedback, and unchanged state on failure in `tests/AI.SpectrumAdventure.Application.Tests/BackNavigationTests.cs`.
- [X] T005 [P] [US1] Add save/reload coverage proving the player can continue using `back` after persistence in `tests/AI.SpectrumAdventure.IntegrationTests/BackNavigationPersistenceTests.cs`.

### Implementation for User Story 1

- [X] T006 [P] [US1] Add a targetless `Back` action and recognize the exact `back` command in `src/AI.SpectrumAdventure.Contracts/ParsedIntent.cs` and `src/AI.SpectrumAdventure.Agents/Intent/CommandPatternInterpreter.cs`.
- [X] T007 [P] [US1] Add an ordered previous-location stack to `src/AI.SpectrumAdventure.Domain/Players/Player.cs` and update ordinary movement and backtracking event application in `src/AI.SpectrumAdventure.Domain/Games/Game.cs` and `src/AI.SpectrumAdventure.Domain/Events/GameEvent.cs`.
- [X] T008 [US1] Persist the previous-location stack and backtracking event in `src/AI.SpectrumAdventure.Domain/Games/GameSnapshot.cs` and `src/AI.SpectrumAdventure.Domain/Events/GameEventSnapshotMapper.cs`; reconstruct history from legacy movement events when loading snapshots without the new field in `src/AI.SpectrumAdventure.Domain/Games/Game.cs`.
- [X] T009 [US1] Validate `Back`, add the no-previous-location failure reason and user feedback, and apply validated backtracking events in `src/AI.SpectrumAdventure.Application/Rules/RulesValidationResult.cs`, `src/AI.SpectrumAdventure.Application/Rules/RulesEngine.cs`, `src/AI.SpectrumAdventure.Application/Rules/BackNavigationRules.cs`, and `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs`.
- [X] T010 [US1] Run the focused story tests in `tests/AI.SpectrumAdventure.Agents.Tests/AI.SpectrumAdventure.Agents.Tests.csproj`, `tests/AI.SpectrumAdventure.Domain.Tests/AI.SpectrumAdventure.Domain.Tests.csproj`, `tests/AI.SpectrumAdventure.Application.Tests/AI.SpectrumAdventure.Application.Tests.csproj`, and `tests/AI.SpectrumAdventure.IntegrationTests/AI.SpectrumAdventure.IntegrationTests.csproj`.

**Checkpoint**: User Story 1 works independently, including repeated backtracking, empty-history feedback, and persistence compatibility.

---

## Phase 4: User Story 2 - Travel through a downward exit (Priority: P1)

**Goal**: Recognize vertical directions and use authored `Up`/`Down` exits, preserving exit conditions and normal no-exit behavior.

**Independent Test**: In *The Tide Beneath the Harbor*, reach the Leaning Lighthouse and enter `Down` to reach the Sluice Chamber; enter `Up` to return. Verify a missing or blocked vertical exit does not move the player or explore an unknown boundary.

### Tests for User Story 2

- [X] T011 [P] [US2] Add bare, prefixed, and mixed-case `Up`/`Down` intent cases in `tests/AI.SpectrumAdventure.Agents.Tests/VerticalDirectionIntentTests.cs`.
- [X] T012 [P] [US2] Add application tests for successful vertical movement, missing exits, unmet exit conditions, and unchanged history on failure in `tests/AI.SpectrumAdventure.Application.Tests/VerticalMovementTests.cs`.
- [X] T013 [P] [US2] Add an end-to-end intent-to-movement test for the authored lighthouse `Down` exit and chamber `Up` exit using `src/AI.SpectrumAdventure.Domain/Adventures/tide-beneath-the-harbor.json` in `tests/AI.SpectrumAdventure.IntegrationTests/VerticalMovementFlowTests.cs`.

### Implementation for User Story 2

- [X] T014 [US2] Add `Up` and `Down` to the shared case-insensitive direction vocabulary and classic bare/prefixed command patterns in `src/AI.SpectrumAdventure.Agents/Intent/WorldVocabulary.cs` and `src/AI.SpectrumAdventure.Agents/Intent/CommandPatternInterpreter.cs`.
- [X] T015 [US2] Prevent missing vertical exits from invoking unknown-boundary exploration while retaining existing validation for authored exits in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs`.
- [X] T016 [US2] Run the focused vertical-direction and movement tests in `tests/AI.SpectrumAdventure.Agents.Tests/AI.SpectrumAdventure.Agents.Tests.csproj`, `tests/AI.SpectrumAdventure.Application.Tests/AI.SpectrumAdventure.Application.Tests.csproj`, and `tests/AI.SpectrumAdventure.IntegrationTests/AI.SpectrumAdventure.IntegrationTests.csproj`.

**Checkpoint**: User Story 2 works independently; authored vertical exits are reachable, case-insensitive, and subject to their existing conditions.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Verify the completed feature and its documented validation flow.

- [ ] T017 Run the complete feature validation commands and manual adventure scenarios in `specs/005-navigation-history-intents/quickstart.md` and confirm the combined solution with `AISpectrumAdventure.slnx`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No changes are needed; existing projects and tooling are used.
- **Foundational (Phase 2)**: No shared code prerequisite is needed.
- **User Story 1 (Phase 3)**: Can start immediately.
- **User Story 2 (Phase 4)**: Behavior is independent of User Story 1, but the shared parser file `CommandPatternInterpreter.cs` and orchestration file `AdventureOrchestrator.cs` must not be edited concurrently by both stories.
- **Polish (Phase 5)**: Depends on both user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: No dependency on another story; it is the suggested MVP increment.
- **User Story 2 (P1)**: No behavior dependency on User Story 1. Its tests can be prepared independently, but its shared-file implementation tasks should follow T006 and T009 to avoid edit conflicts.

### Within Each User Story

- Add the story's regression tests before implementation and confirm they fail for the missing behavior.
- T006 and T007 can proceed in parallel after the US1 tests; T008 depends on T007; T009 depends on T006 and T008; T010 depends on T009.
- T011, T012, and T013 use separate test files and can proceed in parallel.
- T014 depends on the parser changes in T006; T015 depends on the orchestration changes in T009; T016 depends on T014 and T015.
- T017 depends on the completion of both story checkpoints.

### Parallel Opportunities

- **US1 tests**: T001–T005 touch separate test files and can be written in parallel.
- **US1 implementation**: T006 (contracts/parser) and T007 (domain movement state) can proceed in parallel after their tests; snapshot work and orchestration follow their prerequisites.
- **US2 tests**: T011–T013 touch separate test files and can be written in parallel, including while US1 implementation is underway.
- **Cross-story caution**: T014 and T015 modify files also used by US1, so serialize them after T006 and T009 respectively.

### Dependency Graph

```text
US1:
T001 -> T006 -> T009 -> T010
T002 -> T007 -> T008 -> T009
T003 ----------> T008
T004 -----------------> T009
T005 ----------> T008

US2:
T006, T011 -> T014
T009, T012, T013 -> T015
T014, T015 -> T016

Final:
T010, T016 -> T017
```

## Parallel Example: User Story 1

```text
Start together:
- T001 interpreter tests in tests/AI.SpectrumAdventure.Agents.Tests/TwoStageIntentInterpreterTests.cs
- T002 domain history tests in tests/AI.SpectrumAdventure.Domain.Tests/NavigationHistoryTests.cs
- T003 snapshot tests in tests/AI.SpectrumAdventure.Domain.Tests/GameSnapshotRoundTripTests.cs
- T004 orchestration tests in tests/AI.SpectrumAdventure.Application.Tests/BackNavigationTests.cs
- T005 persistence tests in tests/AI.SpectrumAdventure.IntegrationTests/BackNavigationPersistenceTests.cs

After the tests are in place:
- T006 intent contract and parser implementation
- T007 player history and domain event implementation
```

## Parallel Example: User Story 2

```text
Start together:
- T011 parser tests in tests/AI.SpectrumAdventure.Agents.Tests/VerticalDirectionIntentTests.cs
- T012 movement tests in tests/AI.SpectrumAdventure.Application.Tests/VerticalMovementTests.cs
- T013 adventure flow tests in tests/AI.SpectrumAdventure.IntegrationTests/VerticalMovementFlowTests.cs

After User Story 1 has finished editing shared parser/orchestration files:
- T014 vertical direction parser implementation
- T015 vertical unknown-boundary handling
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Add the US1 tests and implement targetless `back` recognition.
2. Add authoritative history, backtracking events, snapshot compatibility, validation, and feedback.
3. Run T010 and stop to validate User Story 1 independently.
4. Demonstrate backtracking before starting vertical-movement work.

### Incremental Delivery

1. Complete User Story 1 and validate it independently.
2. Complete User Story 2 and validate the authored `Up`/`Down` route independently.
3. Run T017 for full feature verification.

## Notes

- Tests are included because the specification and quickstart explicitly define independent and automated test scenarios.
- `[P]` tasks use different files and have no unfinished prerequisites; shared-file changes are deliberately serialized.
- No packages, database migrations, Azure resources, or API response changes are required.
