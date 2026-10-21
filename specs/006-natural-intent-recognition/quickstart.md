# Quickstart: Natural-Language Intent Recognition

## Prerequisites

- .NET 10 SDK installed.
- Repository dependencies restored.
- A configured AI endpoint is only required for an optional manual live-model check; the automated tests use scripted agent responses.

## Automated Validation

Run focused intent-agent tests:

```powershell
dotnet test tests/AI.SpectrumAdventure.Agents.Tests/AI.SpectrumAdventure.Agents.Tests.csproj
```

Run application rule and ambiguity regressions:

```powershell
dotnet test tests/AI.SpectrumAdventure.Application.Tests/AI.SpectrumAdventure.Application.Tests.csproj
```

Run the gameplay input integration tests:

```powershell
dotnet test tests/AI.SpectrumAdventure.IntegrationTests/AI.SpectrumAdventure.IntegrationTests.csproj
```

## Scenarios to Verify

1. With a key target present in the current adventure vocabulary, `take the key`, `grab the key`, `pick the key`, and `take key` resolve to `Take` with the same canonical target.
2. `back` and `return` resolve to the same `Back` intent.
3. A natural paraphrase not explicitly present in the synonym vocabulary is classified as the matching supported intent when its meaning and target are clear.
4. A message that does not request a supported action resolves to `Unknown` and does not change game state.
5. A quoted, negated, or hypothetical action is not executed as a command.
6. An ambiguous action or missing required target produces a clarification response without changing game state.
7. A recognized but currently invalid action is rejected by existing deterministic rules.
8. Existing classic commands continue to resolve identically.

## Acceptance Evaluation

Use a labeled set of at least 100 supported utterances and a separate set of at least 100 non-actionable mentions, questions, quotations, hypotheticals, or negations. Record resolved action and target for supported samples and state-change occurrence for non-actionable samples. Confirm at least 95% correct intent-and-target resolution, fewer than 2% unintended actions, and no state changes in ambiguity cases.

