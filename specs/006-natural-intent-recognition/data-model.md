# Data Model: Natural-Language Intent Recognition

The feature adds no persisted entities. It enriches the existing intent interpretation data flow.

## Supported Intent

Represents an action the player can request and the rules engine already understands.

| Field | Type | Description | Validation |
|---|---|---|---|
| Action | `IntentAction` | One supported gameplay action, or `Unknown` when no safe match exists. | Must be an existing enum value; the classifier cannot add actions. |
| Target | Optional string | Canonical identifier of the target entity, direction, or location as required by the action. | When present, must resolve against the active adventure's vocabulary or valid direction set. |
| Parameters | String-keyed values | Additional supported details, such as an NPC utterance or a `Use` target. | Keys and values must conform to existing action rules and known targets. |
| Confidence | Number | Confidence associated with the interpretation. | Must be within the existing 0-to-1 range; low-confidence output must not be treated as a safe match. |
| RawInput | String | Original player wording for diagnostics and result context. | Preserve the submitted text; do not replace it with a model paraphrase. |

This maps to the existing `ParsedIntent` contract.

## Action Synonym

An in-memory vocabulary entry that maps one or more natural-language phrases to one supported action.

| Field | Type | Description | Validation |
|---|---|---|---|
| Phrase | String | A normalized phrase or verb variation recognized for an action. | Non-empty; normalization must be case-insensitive. |
| Action | `IntentAction` | The canonical action represented by the phrase. | Must reference a supported non-`Unknown` action. |

The synonym vocabulary is shared by deterministic command matching and model guidance. It is not a new user-managed or persisted dictionary.

## Adventure Target Vocabulary

The per-game mapping already passed by `AdventurePage` to the interpreter.

| Field | Type | Description | Validation |
|---|---|---|---|
| Alias | String | Player-facing item, character, location, exit, or other target phrase. | Non-empty and scoped to the current game. |
| TargetId | String | Canonical game identifier represented by the alias. | Must identify an entity known to the active game. |

The model may use this vocabulary to resolve references but may not create target identifiers.

## Interpretation Outcomes

- **Resolved**: A supported action and all required target information were identified. Continue to deterministic rules validation.
- **Action recognized, target unclear**: Return the supported action without a target; the existing gameplay flow asks which target the player means and makes no state change.
- **Unknown or unsupported**: Return `IntentAction.Unknown`; the player receives a clear response inviting a rephrased request and no action is applied.
- **Invalid model result**: Reject an unsupported action, invalid target, malformed structured result, or unsafe confidence; use the existing safe unknown fallback.

Interpretation itself never changes game state. Successful rules validation remains the only path to state mutation.

