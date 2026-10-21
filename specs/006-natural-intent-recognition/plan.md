# Implementation Plan: Natural-Language Intent Recognition

**Branch**: `006-natural-intent-recognition` | **Date**: 2026-10-21 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-natural-intent-recognition/spec.md`

## Summary

Expand and centralize the synonyms for existing player actions, retain deterministic recognition for classic commands, and use the existing AI agent runner to classify unmatched natural-language input into a supported structured intent or `Unknown`. Pass the adventure's target vocabulary to the classifier, validate its output before use, and preserve the existing deterministic rules engine as the sole authority for whether an action can change game state.

## Technical Context

**Language/Version**: C# / .NET 10

**Primary Dependencies**: Existing Microsoft Agent Framework integration through `IAgentRunner`; existing `StructuredOutputValidator`; existing application intent contracts and rules engine. No new package is required.

**Storage**: None for intent classification. Synonyms and intent definitions are code/configuration data; game persistence is unchanged.

**Testing**: xUnit and FluentAssertions in `AI.SpectrumAdventure.Agents.Tests`, `AI.SpectrumAdventure.Application.Tests`, and `AI.SpectrumAdventure.IntegrationTests`. Use fake `IAgentRunner` responses for deterministic model-boundary tests.

**Target Platform**: Existing .NET 10 ASP.NET Core adventure application and its agent library.

**Project Type**: Multi-project .NET application.

**Performance Goals**: Keep exact classic commands on the deterministic path; invoke the model only when the deterministic interpreter cannot resolve the player's input. The specification sets no additional latency target.

**Constraints**:

- Only existing `IntentAction` values may be returned as actionable intents.
- Model output must not mutate the game; existing rule validation and state transitions remain authoritative.
- Targets returned by the model must resolve against the adventure's supplied target vocabulary.
- Model errors, invalid output, and no-match cases must result in a safe unknown/clarification response, not a successful action.
- Preserve cancellation behavior and existing classic-command behavior.

**Scale/Scope**: Existing supported player actions, target names, and their synonyms. No new actions, persistent data, model provider, or infrastructure.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Evidence / plan |
|---|---|---|
| I. Deterministic Game State First | PASS | The interpreter returns a proposal only; deterministic rules continue to validate and apply actions. |
| II. Agent Responsibilities Must Be Explicit | PASS | Intent recognition is a bounded classifier responsibility; it does not narrate or mutate the world. |
| III. Structured Output Over Free-Form State Mutation | PASS | The classifier returns the existing structured `ParsedIntent` contract and output is validated before use. |
| IV. Rules Before Narrative | PASS | The resulting intent continues through `AdventureOrchestrator` and `RulesEngine`. |
| VIII. Testable AI Behavior | PASS | The model runner is injectable and tests use scripted responses; representative paraphrases and no-match inputs are evaluated. |
| IX. Observability Is a First-Class Feature | PASS | Reuse existing agent telemetry patterns and retain enough structured outcome information to distinguish a match from fallback. |
| X. Cost and Latency Must Be Managed | PASS | Deterministic command patterns are attempted first; natural-language fallback makes one bounded classifier invocation with the existing safe retry/fallback policy. |

**Gate result**: No constitution violations or unresolved policy decisions.

## Project Structure

### Documentation (this feature)

```text
specs/006-natural-intent-recognition/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── intent-recognition.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── AI.SpectrumAdventure.Agents/
│   └── Intent/
│       ├── CommandPatternInterpreter.cs
│       ├── TwoStageIntentInterpreter.cs
│       ├── WorldVocabulary.cs
│       ├── [intent classifier]
│       └── [shared action-synonym definitions]
├── AI.SpectrumAdventure.Application/
│   └── Abstractions/
│       ├── IIntentInterpreter.cs
│       └── IIntentInterpreterAgent.cs
├── AI.SpectrumAdventure.Contracts/
│   └── ParsedIntent.cs
└── AI.SpectrumAdventure.Web/
    ├── Program.cs
    └── Components/
        └── AdventurePage.razor

tests/
├── AI.SpectrumAdventure.Agents.Tests/
├── AI.SpectrumAdventure.Application.Tests/
└── AI.SpectrumAdventure.IntegrationTests/
```

**Structure Decision**: Keep the feature in the existing intent agent, application abstraction, contracts, and web gameplay input path. The web page already supplies the active game's target vocabulary to `IIntentInterpreter`; its returned `ParsedIntent` is then processed by the existing deterministic orchestrator. Keep unit tests beside the agent and add integration coverage for the complete player-input-to-action path.

## Complexity Tracking

No constitution violations or additional projects, services, or persistent stores are required.
