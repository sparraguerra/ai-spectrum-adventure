# Feature Specification: Natural-Language Intent Recognition

**Feature Branch**: `006-natural-intent-recognition`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "Quiero mejorar el entendimiento de las intenciones del usuario para que la conversación sea más natural. Expresiones como take the key, grab the key, pick the key y take key deben entenderse con la misma intención; back debe equivaler a return. Quiero que el modelo de IA pueda determinar mejor la intención cuando se le consulta."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Express an action naturally (Priority: P1)

As an adventure player, I want to describe an action in different natural ways so that I can interact with the world without memorizing exact command patterns.

**Why this priority**: Understanding equivalent phrasings is the core improvement requested and removes friction from the main gameplay loop.

**Independent Test**: In a location where a key is available to collect, enter several equivalent requests and verify that each resolves to the same action and target and produces the same game outcome.

**Acceptance Scenarios**:

1. **Given** a collectible key is available in the current location, **When** the player enters `take the key`, `grab the key`, `pick the key`, or `take key`, **Then** each request is understood as collecting that key and follows the same game rules.
2. **Given** an action and target can be identified from the player's wording, **When** the player expresses that action with a reasonable paraphrase, **Then** the game recognizes the same underlying action and target rather than requiring an exact phrase from a fixed command list.
3. **Given** the player uses different capitalization or harmless punctuation in an otherwise equivalent request, **When** the request is interpreted, **Then** those formatting differences do not change its intended action or target.
4. **Given** the player uses an existing classic command such as `TAKE KEY`, **When** the game interprets it, **Then** it continues to resolve to the same intended action and target.
5. **Given** the intended action or target is disallowed or unavailable in the current game state, **When** the player makes the request, **Then** the existing game rules determine the outcome and no unavailable action is reported as successful.

### User Story 2 - Navigate using familiar alternatives (Priority: P1)

As an adventure player, I want common navigation phrases such as `back` and `return` to have the same meaning so that I can move through the world conversationally.

**Why this priority**: Navigation is a frequent action, and inconsistent recognition of familiar alternatives interrupts play.

**Independent Test**: With a previous location available, issue `back` and `return` in equivalent test runs and verify that both return to the same previous location.

**Acceptance Scenarios**:

1. **Given** the player has a previous location, **When** the player enters `back` or `return`, **Then** each request resolves to the same return-navigation action and follows the existing navigation rules.
2. **Given** a player asks to go back using a natural paraphrase, **When** the intended return action is clear from the wording and context, **Then** the game recognizes it as return navigation.
3. **Given** no previous location is available, **When** the player requests `back` or `return`, **Then** both requests produce the same existing no-previous-location outcome without changing the game state.

### User Story 3 - Have the game clarify uncertain intent (Priority: P2)

As an adventure player, I want the game to ask a concise follow-up when my request could mean materially different actions so that it does not take an unintended action.

**Why this priority**: Broader language understanding is useful only if uncertainty does not cause incorrect or confusing changes to the adventure.

**Independent Test**: Submit an ambiguous request in a location with multiple plausible targets or actions, and verify that the game asks for clarification without changing the world.

**Acceptance Scenarios**:

1. **Given** the player's request has more than one plausible action or target, **When** the game cannot determine the intended one reliably, **Then** it asks a focused clarification question and does not apply an action.
2. **Given** the player is merely discussing, quoting, negating, or asking about an action rather than requesting it, **When** the game interprets the message, **Then** it does not execute that action solely because action-related words appear.
3. **Given** the player's meaning is not understood, **When** the game cannot safely resolve the request, **Then** it explains that it did not understand and invites the player to rephrase, without inventing a target or changing game state.
4. **Given** intent understanding is unavailable or produces no usable interpretation, **When** the player submits an utterance, **Then** existing supported commands remain available where they can be safely recognized, otherwise the game provides a clear response without claiming success.

### Edge Cases

- The player omits an article or uses a harmless grammatical variation, such as `take key`.
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
- **FR-002**: For an understood request, the game MUST identify the intended action and any relevant target or parameters needed to process it.
- **FR-003**: The game MUST treat `take the key`, `grab the key`, `pick the key`, and `take key` as equivalent requests to collect the key when that action and target are available in the current context.
- **FR-004**: The game MUST treat `back` and `return` as equivalent requests for return navigation when the player's meaning is clear from the current context.
- **FR-005**: The game MUST consider the current adventure context when resolving references to locations, exits, items, characters, and other supported targets.
- **FR-006**: The game MUST distinguish an actionable request from a mere mention, question, quotation, hypothetical, or negation of an action, and MUST NOT execute an action based only on matching action words.
- **FR-007**: When multiple materially different actions or targets remain plausible, the game MUST request clarification before applying an action.
- **FR-008**: When a request cannot be understood safely, the game MUST communicate that it was not understood and MUST leave game state unchanged.
- **FR-009**: Intent recognition MUST NOT bypass existing rules that determine whether an action is allowed or what effects it has.
- **FR-010**: The game MUST continue to understand existing supported classic commands while also accepting their natural-language equivalents.
- **FR-011**: When intent understanding is unavailable or unsuccessful, the game MUST provide a clear player-facing response and MUST NOT report an action as successful unless it was recognized and accepted by the existing game rules.

### Key Entities

- **Player utterance**: The text the player submits, including its wording, references, and conversational context.
- **Recognized intent**: The supported action understood from an utterance, including any target or parameters required to attempt it.
- **Adventure context**: The current game situation and available entities that help interpret the player's references and determine whether an action can be attempted.
- **Clarification request**: A player-facing question used when multiple materially different interpretations remain plausible.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 95% of a representative, labeled evaluation set of at least 100 supported player requests are resolved to the intended action and target, including natural paraphrases not limited to the example phrases in this specification.
- **SC-002**: All tested example variants `take the key`, `grab the key`, `pick the key`, and `take key` resolve to the same intended collection action and key target when that key is available.
- **SC-003**: All tested equivalent return-navigation requests, including `back` and `return`, resolve to the same outcome in matching game states.
- **SC-004**: Fewer than 2% of a labeled evaluation set of at least 100 non-actionable mentions, questions, quotations, hypotheticals, or negations cause an unintended game action.
- **SC-005**: In all tested ambiguous requests, the game asks for clarification and makes no game-state change before the player clarifies.
- **SC-006**: Existing supported classic commands continue to resolve to their intended actions in all regression scenarios.

## Assumptions

- The feature improves understanding of actions already supported by the adventure; adding new game actions is out of scope.
- The game may use AI to interpret meaning, but the resulting interpretation remains subject to the existing game rules and authoritative state.
- Example phrases are in English because they illustrate the current command vocabulary; this feature does not add a new supported player language.
- Equivalent wording is judged by meaning in the current adventure context, not by a requirement to maintain an exhaustive list of synonyms.
- Clarification is preferred over taking an uncertain action that could change the world.
