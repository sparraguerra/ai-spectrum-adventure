# Intent Recognition Contract

## Purpose

Convert the player's submitted text into one existing supported game intent, using deterministic command patterns first and AI classification for natural-language input those patterns do not resolve.

## Input

- Original player text.
- The current adventure target vocabulary, including aliases and canonical identifiers.
- Existing supported `IntentAction` values and their synonym guidance.

The active game context is limited to the information needed to identify an action and resolve its references. Classification does not receive authority to mutate game state.

## Output

An existing `ParsedIntent`:

- `Action`: a supported action or `Unknown`.
- `Target`: a canonical target or direction when identified.
- `Parameters`: supported structured details only.
- `Confidence`: a value in the existing supported range.
- `RawInput`: the original player text.

## Behavior

1. Preserve the existing deterministic interpretation of classic commands.
2. Use a shared synonym vocabulary for common phrases, including:
   - `take`, `grab`, `pick`, and `pick up` as collection wording when the context supports collecting a target.
   - `back` and `return` as return-navigation wording.
3. When deterministic patterns do not resolve the input, ask the AI classifier to map the text to an existing action and known target, or to return `Unknown`.
4. Do not infer an action from a mere mention, question, quotation, hypothetical, or negation.
5. If one action is clear but a required target is not, return the known action without a target so the gameplay flow can ask a focused clarification.
6. If no supported action matches, or the interpretation is materially ambiguous, return `Unknown`; respond clearly and do not apply an action.
7. Validate the structured output and target identifiers before passing the intent to the rules engine.
8. The rules engine remains responsible for deciding whether a valid intent is possible in the current game state. Interpreting an intent never implies that its action succeeded.

## Failure Behavior

- If model invocation or structured-output validation fails, use the existing safe fallback and produce no successful action.
- Cancellation is propagated to the model runner.
- The deterministic command path remains available independently of AI availability.

