# Research: Natural-Language Intent Recognition

## Decision 1: Keep deterministic recognition for classic commands and use the model for natural-language fallback

**Decision**: Preserve the current exact command-pattern path as the first stage. If it cannot resolve the player's text, pass the utterance and current adventure target vocabulary to an intent-classification agent.

**Rationale**: `TwoStageIntentInterpreter` already separates deterministic command patterns from a natural-language fallback. This retains predictable classic commands and avoids a model call when local rules already identify the intent, consistent with Constitution Principles VIII and X.

**Alternatives considered**:
- Send every player message to the model: rejected because it adds cost and latency and can make established commands less deterministic.
- Expand exact patterns to cover all natural-language phrasings: rejected because the synonym set cannot anticipate all reasonable paraphrases.

## Decision 2: Share synonym definitions across deterministic recognition and the classifier

**Decision**: Maintain one action-to-synonym vocabulary for supported actions and use it in the deterministic recognizer and classifier instructions. Add the requested item-taking and return-navigation variants while retaining the existing synonyms.

**Rationale**: The current action phrases are distributed between `CommandPatternInterpreter` and `KeywordFallbackClassifier`. A shared source prevents the fast path and AI guidance from drifting while still allowing the model to recognize unlisted paraphrases.

**Alternatives considered**:
- Maintain separate synonym lists: rejected because a phrase could resolve differently depending on which interpreter path handles it.
- Treat the dictionary as the complete language boundary: rejected because the feature requires recognizing natural paraphrases beyond listed phrases.

## Decision 3: Reuse the existing structured agent runner and safe fallback

**Decision**: Implement the intent classifier as a bounded agent using `IAgentRunner` and `StructuredOutputValidator`. Return the existing `ParsedIntent` shape; use `IntentAction.Unknown` when there is no safe supported match or the model output cannot be used.

**Rationale**: The agent library already provides JSON-schema structured output and a retry-once/safe-fallback helper. Reusing them avoids a new provider integration and keeps tests independent of network calls.

**Alternatives considered**:
- Parse an unstructured natural-language model response: rejected because it is fragile and conflicts with Constitution Principle III.
- Add a new model provider: rejected because the application already has a configured AI agent runner.

## Decision 4: Constrain intent classification to existing actions and adventure targets

**Decision**: Give the classifier the set of supported `IntentAction` values and the active adventure's target vocabulary. Validate returned actions and targets before constructing an actionable result. A known action with an unresolved required target remains ambiguous and is clarified by the existing gameplay flow; no supported intent returns `Unknown`.

**Rationale**: The model should identify what the player can ask the game to do, not create new capabilities or entity identifiers. `ParsedIntent` is a proposal and the existing rules engine remains the authority on whether it can be executed.

**Alternatives considered**:
- Allow arbitrary model-generated action names or target identifiers: rejected because they can bypass the known contract and create invalid state references.
- Treat successful classification as successful action execution: rejected because action feasibility belongs to deterministic rules.

## Decision 5: Keep the feature stateless and test model behavior with scripted responses

**Decision**: Do not persist synonym, prompt, or classification state per player. Use fake `IAgentRunner` implementations for unit tests, a labeled set of paraphrase and negative examples for recognition evaluation, and an end-to-end test of the gameplay input path.

**Rationale**: The existing target vocabulary is supplied from the active game and the intent result is consumed immediately. The constitution requires provider-independent AI tests; no new persistence is needed for this behavior.

**Alternatives considered**:
- Persist per-player learned synonyms: rejected because this is outside the feature's scope and would introduce data lifecycle and consistency concerns.
- Depend only on live model calls in tests: rejected because it is nondeterministic, costly, and unsuitable for reliable CI.

