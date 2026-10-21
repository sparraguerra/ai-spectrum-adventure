# Tasks: Natural-Language Intent Recognition

**Input**: Design documents from `specs/006-natural-intent-recognition/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`

**Tests**: Included because the acceptance criteria require measurable intent accuracy, safe no-match behavior, and preservation of game-state rules.

**Organization**: Tasks are grouped under the only in-scope user journey, User Story 3.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks because they change separate files and have no unfinished dependencies.
- **[Story]**: `US3` maps to User Story 3 in `spec.md`.
- All tasks name the file or files to change.

## Path Conventions

Source and test paths are repository-relative under `src/` and `tests/`, as detailed in `plan.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Reuse the existing .NET solution, Microsoft Agent Framework integration, and test projects. No project initialization, package addition, persistence, or new service is required.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Provide the shared vocabulary and agent contract required by the intent-recognition story.

- [X] T001 [P] Define the shared supported-action synonym catalog and normalization rules in `src/AI.SpectrumAdventure.Agents/Intent/IntentActionVocabulary.cs`.
- [X] T002 [P] Extend `IIntentInterpreterAgent` to accept the current adventure target vocabulary alongside player text in `src/AI.SpectrumAdventure.Application/Abstractions/IIntentInterpreterAgent.cs`.

**Checkpoint**: The shared action vocabulary and AI-agent boundary are defined; user-story work can proceed.

---

## Phase 3: User Story 3 - Match player text to a supported intent (Priority: P1) 🎯 MVP

**Goal**: Expand action synonyms and use AI classification for natural-language input that deterministic command patterns do not resolve, returning only a validated supported intent or `Unknown`.

**Independent Test**: Run the interpreter with controlled player text and scripted model responses. Verify the requested synonym examples resolve consistently, paraphrases map to supported actions and known targets, unsupported or ambiguous input does not apply an action, and the existing rules engine still controls state changes.

### Tests for User Story 3

> Write the focused tests first and confirm they fail for the missing behavior before implementing it.

- [X] T003 [P] [US3] Add model-classifier unit tests for supported intent, `Unknown`, invalid action/target output, and safe model-failure fallback in `tests/AI.SpectrumAdventure.Agents.Tests/IntentInterpreterAgentTests.cs`.
- [X] T004 [P] [US3] Update interpreter tests for synonym variants, deterministic-command precedence, natural-language fallback, cancellation, and no-match behavior in `tests/AI.SpectrumAdventure.Agents.Tests/TwoStageIntentInterpreterTests.cs`.
- [X] T005 [P] [US3] Add application tests proving unknown and ambiguous intents return clear clarification feedback without changing game state in `tests/AI.SpectrumAdventure.Application.Tests/AmbiguousActionTests.cs`.

### Implementation for User Story 3

- [X] T006 [P] [US3] Implement the structured intent-classification agent, including supported-action guidance, adventure target context, output validation, telemetry, and `Unknown` fallback, in `src/AI.SpectrumAdventure.Agents/Intent/IntentInterpreterAgent.cs`.
- [X] T007 [P] [US3] Expand deterministic command patterns to use the shared action-synonym catalog while preserving existing classic command behavior in `src/AI.SpectrumAdventure.Agents/Intent/CommandPatternInterpreter.cs`.
- [X] T008 [US3] Replace keyword-only fallback classification with the injected AI classifier, pass the target vocabulary and cancellation token through, and remove the obsolete fallback implementation in `src/AI.SpectrumAdventure.Agents/Intent/TwoStageIntentInterpreter.cs` and `src/AI.SpectrumAdventure.Agents/Intent/KeywordFallbackClassifier.cs` (depends on T006 and T007).
- [X] T009 [US3] Register `IIntentInterpreterAgent` with the existing `IAgentRunner` in `src/AI.SpectrumAdventure.Web/Program.cs` (depends on T006).
- [X] T010 [US3] Update unknown-intent feedback to invite the player to clarify or rephrase without applying an action in `src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs` (depends on T005).
- [X] T011 [US3] Add a labeled intent-evaluation corpus with at least 100 supported utterances and 100 non-actionable examples in `tests/AI.SpectrumAdventure.Agents.Tests/TestData/intent-evaluation.json`.
- [X] T012 [US3] Add an integration test that composes the intent interpreter with the deterministic gameplay orchestrator, verifies only rule-accepted intents change state, and updates direct interpreter fixtures in `tests/AI.SpectrumAdventure.IntegrationTests/IntentRecognitionFlowTests.cs`, `tests/AI.SpectrumAdventure.IntegrationTests/E2E_SuccessRateThresholdTests.cs`, and `tests/AI.SpectrumAdventure.IntegrationTests/VerticalMovementFlowTests.cs` (depends on T008 and T010).

**Checkpoint**: The story is independently testable through the interpreter and gameplay pipeline; classic commands remain deterministic, natural-language input is classified with bounded model output, and unsafe/no-match input does not mutate state.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Verify the feature against the runnable validation guide and its measurable acceptance criteria.

- [ ] T013 Run the focused agent, application, and integration test projects from `specs/006-natural-intent-recognition/quickstart.md` and record the measured corpus accuracy and false-action rate.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No code setup required; the existing solution and test infrastructure are reused.
- **Foundational (Phase 2)**: T001 and T002 can proceed in parallel; both must finish before the story implementation begins.
- **User Story 3 (Phase 3)**: Depends on the foundational vocabulary and agent contract. Tests T003–T005 can proceed in parallel; implementation proceeds after the relevant tests.
- **Polish (Phase 4)**: Depends on all User Story 3 tasks.

### User Story Dependencies

- **User Story 3 (P1)**: Independently delivers the complete intent-recognition behavior; there are no dependencies on other user stories.

### Within User Story 3

- T003, T004, and T005 are independent tests and may be written in parallel after Phase 2.
- T006, T007, and T010 work in separate files and may be implemented in parallel after their related tests and prerequisites.
- T008 depends on the classifier and synonym behavior from T006 and T007.
- T009 depends on the classifier implementation in T006.
- T011 can be prepared independently after the supported-action vocabulary is known.
- T012 depends on the completed interpreter wiring and application feedback behavior.
- T013 is the final validation gate.

### Parallel Opportunities

```text
Phase 2: T001 || T002
Story tests: T003 || T004 || T005
Story implementation: T006 || T007 || T010
After classifier: T008 || T009 || T011 (subject to their listed prerequisites)
```

---

## Parallel Example: User Story 3

```text
# After foundational tasks complete:
Task: "Add classifier contract and fallback tests in tests/AI.SpectrumAdventure.Agents.Tests/IntentInterpreterAgentTests.cs"
Task: "Add synonym and two-stage routing tests in tests/AI.SpectrumAdventure.Agents.Tests/TwoStageIntentInterpreterTests.cs"
Task: "Add no-state-change clarification tests in tests/AI.SpectrumAdventure.Application.Tests/AmbiguousActionTests.cs"

# After the tests are in place:
Task: "Implement structured classification in src/AI.SpectrumAdventure.Agents/Intent/IntentInterpreterAgent.cs"
Task: "Centralize command aliases in src/AI.SpectrumAdventure.Agents/Intent/CommandPatternInterpreter.cs"
Task: "Clarify unknown-intent feedback in src/AI.SpectrumAdventure.Application/Orchestration/AdventureOrchestrator.cs"
```

---

## Implementation Strategy

### MVP First (User Story 3)

1. Complete the shared action vocabulary and agent contract.
2. Add the interpreter, classifier, and application tests.
3. Implement synonym handling and structured AI fallback, then register it in the gameplay application.
4. Verify the end-to-end rule-validation boundary and no-action/clarification behavior.
5. Run the representative evaluation corpus and the quickstart test commands.

### Incremental Delivery

Deliver User Story 3 as one complete increment: deterministic synonym coverage first, then AI fallback and output validation, then runtime wiring and corpus-based acceptance verification.

### Parallel Team Strategy

After Phase 2, separate work can proceed on the classifier, deterministic synonym path, and application clarification behavior. Integrate these through `TwoStageIntentInterpreter` and validate the complete flow before release.

## Notes

- Total tasks: 13.
- MVP scope: the single User Story 3, including synonym expansion, AI classification, safe no-match behavior, and end-to-end rule validation.
- No model provider, database, or infrastructure changes are planned.
- Model tests use scripted responses; the labeled evaluation corpus is used to verify measured recognition and false-action thresholds.
