# Feature Specification: Natural-Language Intent Recognition

**Feature Branch**: `006-natural-intent-recognition`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "Ampliar el diccionario de sinónimos para que, según el texto que inserte el jugador, el modelo de IA identifique la intención que mejor se ajuste, si existe, a las acciones que puede realizar el usuario."

## User Scenarios & Testing *(mandatory)*

### User Story 3 - Match player text to a supported intent (Priority: P1)

As an adventure player, I want the game to interpret what I write against an expanded dictionary of synonyms and the actions I can perform, so that it recognizes the matching intent when one exists and does not invent an action when none applies.

**Why this priority**: Matching the player's actual wording to a supported action is the central improvement needed to make interaction more natural and reliable.

**Independent Test**: Submit synonymous commands, natural-language requests, ambiguous messages, and messages with no supported action; verify that each is mapped to the correct supported intent or to no intent, without bypassing game rules.

**Acceptance Scenarios**:

1. **Given** collecting a key is a supported action and the key is an identifiable target, **When** the player enters `take the key`, `grab the key`, `pick the key`, or `take key`, **Then** the synonym dictionary and intent understanding resolve each phrase to the same collection intent and key target.
2. **Given** returning to a previous location is a supported action, **When** the player enters `back` or `return`, **Then** both phrases resolve to the same return-navigation intent.
3. **Given** the player expresses a supported action using wording beyond an exact command phrase, **When** the intended action and target can be identified from the text and adventure context, **Then** the model selects the corresponding supported intent.
4. **Given** the player's text does not match any supported action, **When** the model evaluates the text, **Then** it selects no intent and the game does not perform an action.
5. **Given** the player's text could correspond to more than one materially different intent or target, **When** the intended one cannot be determined reliably, **Then** the game asks for clarification without applying an action.
6. **Given** the player is mentioning, quoting, questioning, hypothesizing, or negating an action rather than requesting it, **When** the text is interpreted, **Then** the game does not execute that action solely because its words appear.
7. **Given** the player uses an existing classic command such as `TAKE KEY`, **When** the text is interpreted, **Then** it continues to resolve to the same supported intent and target.
8. **Given** an identified action or target is disallowed or unavailable in the current game state, **When** the player makes the request, **Then** the existing game rules determine the outcome and no unavailable action is reported as successful.

### Edge Cases

- The player omits an article or uses a harmless grammatical variation, such as `take key`.
- The synonym dictionary does not yet contain the exact wording used, but the player's meaning may still match a supported intent.
- Several visible objects have similar names, or the request names an object that is not present or otherwise available.
- An utterance contains more than one action, or a target mention could refer to multiple entities.
- The player uses a command word in a question, hypothetical statement, quotation, or negation.
- The player combines familiar command words with additional natural-language context.
- A request is outside the actions supported by the adventure.
- A recognized action is invalid under the current world rules; intent recognition must not make it valid.
- The language-understanding service is unavailable or returns an unusable result; the player must still receive a clear response and game state must remain unchanged unless an existing safe interpretation is available.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The game MUST recognize semantically equivalent natural-language requests as the same underlying supported action, even when they do not exactly match a predefined command phrase.
- **FR-002**: The synonym dictionary MUST include natural wording alternatives for supported player actions, and each listed synonym MUST map to its corresponding supported intent.
- **FR-003**: Based on the player's submitted text and the current adventure context, the AI model MUST select the supported intent that best matches the player's meaning, including any relevant target or parameters.
- **FR-004**: The AI model MUST select no intent when the player's text does not match a supported player action, rather than inventing an unsupported action.
- **FR-005**: The game MUST treat `take the key`, `grab the key`, `pick the key`, and `take key` as equivalent requests to collect the key when that intent and target can be identified.
- **FR-006**: The game MUST treat `back` and `return` as equivalent requests for return navigation when that intent can be identified from the current context.
- **FR-007**: The game MUST consider the current adventure context when resolving references to locations, exits, items, characters, and other supported targets.
- **FR-008**: The game MUST distinguish an actionable request from a mere mention, question, quotation, hypothetical, or negation of an action, and MUST NOT execute an action based only on matching action words.
- **FR-009**: When multiple materially different actions or targets remain plausible, the game MUST request clarification before applying an action.
- **FR-010**: When no supported intent can be identified safely, the game MUST communicate that it did not understand and MUST leave game state unchanged.
- **FR-011**: Intent recognition MUST NOT bypass existing rules that determine whether an action is allowed or what effects it has.
- **FR-012**: The game MUST continue to understand existing supported classic commands while also accepting their natural-language equivalents.
- **FR-013**: When intent understanding is unavailable or unsuccessful, the game MUST provide a clear player-facing response and MUST NOT report an action as successful unless it was recognized and accepted by the existing game rules.

### Key Entities

- **Player utterance**: The text the player submits, including its wording, references, and conversational context.
- **Recognized intent**: The supported action understood from an utterance, including any target or parameters required to attempt it.
- **Synonym dictionary**: The set of natural wording alternatives that map to supported player actions.
- **Adventure context**: The current game situation and available entities that help interpret the player's references and determine whether an action can be attempted.
- **Clarification request**: A player-facing question used when multiple materially different interpretations remain plausible.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 95% of a representative, labeled evaluation set of at least 100 supported player requests are resolved to the intended supported action and target, including natural paraphrases not limited to the example phrases in this specification.
- **SC-002**: All tested example variants `take the key`, `grab the key`, `pick the key`, and `take key` resolve to the same collection intent and key target when that intent and target can be identified.
- **SC-003**: All tested equivalent return-navigation requests, including `back` and `return`, resolve to the same intent in matching game contexts.
- **SC-004**: Fewer than 2% of a labeled evaluation set of at least 100 non-actionable mentions, questions, quotations, hypotheticals, or negations cause an unintended game action.
- **SC-005**: In all tested ambiguous requests, the game asks for clarification and makes no game-state change before the player clarifies.
- **SC-006**: Existing supported classic commands continue to resolve to their intended actions in all regression scenarios.

## Assumptions

- The synonym dictionary covers wording for actions already supported by the adventure; adding new game actions is out of scope.
- The AI model selects from supported player intents or selects no intent; the resulting interpretation remains subject to existing game rules and authoritative state.
- Example phrases are in English because they illustrate the current command vocabulary; this feature does not add a new supported player language.
- The dictionary is expanded to cover common wording alternatives, while the model can also identify a supported intent from wording not explicitly listed as a synonym.
- Clarification is preferred over taking an uncertain action that could change the world.
