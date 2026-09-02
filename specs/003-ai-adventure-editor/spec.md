# Feature Specification: AI Adventure Editor

**Feature Branch**: `003-ai-adventure-editor`

**Created**: 2026-09-02

**Status**: Draft

**Input**: User description: "Create an in-application AI-assisted editor for authors to create, validate, save, version, publish, playtest, and document new adventures."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Save a Draft (Priority: P1)

As an adventure author, I want to create, save, reopen, and edit a draft adventure so that I can build playable content over time.

**Why this priority**: A durable draft is the foundation for every authoring activity and prevents author work from being lost.

**Independent Test**: Create a draft with an identifier, title, and starting location; save it; reopen it; and verify the same editable content is restored.

**Acceptance Scenarios**:

1. **Given** no draft exists, **When** an author creates an adventure with a unique identifier, title, and starting location, **Then** a persistent draft is created.
2. **Given** a draft exists, **When** the author saves changes to it and reopens it, **Then** all accepted author content is restored in editable form.
3. **Given** an identifier is already used by an adventure, **When** the author attempts to create another adventure with it, **Then** the editor explains the conflict and does not overwrite existing content.

---

### User Story 2 - Author the World Structure (Priority: P1)

As an author, I want to manage locations, connections, items, and starting conditions so that I can define an explorable adventure world.

**Why this priority**: A coherent world graph is the minimum playable structure for an adventure.

**Independent Test**: Create at least three locations, connect them, add an item, select a starting location, save, and verify every relationship is retained.

**Acceptance Scenarios**:

1. **Given** an editable draft, **When** an author adds or edits a location, **Then** the location supports identity, name, description, type, environmental characteristics, objects, NPCs, and exits.
2. **Given** two locations exist, **When** the author creates a connection, **Then** the connection identifies its source, direction, destination, and any availability condition.
3. **Given** an author enters a duplicate identity, missing destination, or invalid connection, **When** the draft is saved or validated, **Then** the editor identifies the affected element and does not treat the structure as valid.
4. **Given** a selected starting location, **When** it cannot reach a meaningful interaction or another accessible location, **Then** validation reports that the initial world is not traversable.

---

### User Story 3 - Author NPCs, Lore, and Puzzles (Priority: P1)

As an author, I want to define characters, lore, puzzles, alternative solutions, and puzzle chains so that the adventure supports coherent discovery and player agency.

**Why this priority**: These elements turn an explorable map into an adventure with meaning and multiple possible approaches.

**Independent Test**: Add one NPC, one lore entry, and two linked puzzles with alternative solutions; validate the draft and verify valid references and chain behavior are retained.

**Acceptance Scenarios**:

1. **Given** a draft, **When** an author defines an NPC, **Then** the author can set its identity, personality, knowledge boundaries, goals, relationships, and authoritative location.
2. **Given** a draft, **When** an author defines lore, **Then** it is classified as an immutable fact, historical fact, regional fact, rumour, or player-discoverable knowledge.
3. **Given** an author defines a puzzle, **When** it includes prerequisites, clues, one or more solutions, outcomes, or chain links, **Then** every referenced item, location, NPC, lore entry, and puzzle must exist.
4. **Given** a puzzle has multiple valid solutions, **When** the draft is playtested, **Then** each configured solution may complete the puzzle while unconfigured proposals do not become valid merely because they are narrated.

---

### User Story 4 - Request and Review AI Proposals (Priority: P2)

As an author, I want to describe an adventure idea in natural language and review structured AI proposals so that I can author content faster without losing control.

**Why this priority**: AI assistance is valuable once a reliable authoring and validation workflow exists.

**Independent Test**: Request proposals for a location, NPC, or puzzle; edit one, accept one, and reject one; verify only the accepted proposal changes the draft.

**Acceptance Scenarios**:

1. **Given** an editable draft, **When** an author submits a natural-language authoring request, **Then** the editor returns a structured proposal for the requested content type.
2. **Given** an AI proposal is available, **When** the author edits, accepts, or rejects it, **Then** the final action is recorded and only accepted, validated content changes the draft.
3. **Given** a proposed change would create an invalid structure or contradict protected lore, **When** the author attempts to accept it, **Then** deterministic validation prevents the change and explains why.
4. **Given** AI proposal generation fails, **When** the author continues editing manually, **Then** the draft remains available and no accepted content is lost.

---

### User Story 5 - Validate and Publish a Version (Priority: P1)

As an author, I want to validate and publish an adventure so that players can start only coherent, stable versions.

**Why this priority**: Publication is the safety boundary between author experimentation and player-accessible content.

**Independent Test**: Attempt to publish an invalid draft and verify publication is blocked; fix the reported issues, publish, and verify a stable published version is available.

**Acceptance Scenarios**:

1. **Given** a draft has structural errors, **When** the author validates or publishes it, **Then** publication is prevented and each error identifies its affected element and reason.
2. **Given** a draft has a valid starting location, traversable initial world, valid references, unique identities, consistent protected lore, valid topology, and a meaningful interaction, discovery, objective, or puzzle, **When** the author publishes it, **Then** a persistent published version is created.
3. **Given** an author has not explicitly requested publication, **When** AI creates or changes a proposal, **Then** the adventure remains a draft.

---

### User Story 6 - Manage Published History (Priority: P2)

As an author, I want to review published versions and restore one as a new draft so that I can evolve adventures safely.

**Why this priority**: Version history protects players and authors when an adventure changes over time.

**Independent Test**: Publish an adventure, revise and publish it again, restore the first version into a draft, and verify the original published versions remain unchanged.

**Acceptance Scenarios**:

1. **Given** an adventure has published versions, **When** the author reviews its history, **Then** each version is identifiable and its content can be inspected.
2. **Given** the author edits a draft derived from a published version, **When** the author saves or republishes it, **Then** existing published versions remain unchanged.
3. **Given** a game began from a published version, **When** a later version is published, **Then** the active game continues using its original version.
4. **Given** an author selects a previous published version, **When** restoration is requested, **Then** the system creates a new editable draft rather than modifying that version.

---

### User Story 7 - Playtest an Adventure (Priority: P1)

As an author, I want to launch a playtest from the editor so that I can verify the player experience before publishing.

**Why this priority**: Playtesting proves that author content works with the same rules that players will experience.

**Independent Test**: Start a playtest for an unpublished valid draft, complete a configured puzzle path, and verify the deterministic game engine evaluates it identically to a player game.

**Acceptance Scenarios**:

1. **Given** a valid draft, **When** the author starts a playtest, **Then** the game uses that draft's accepted content through the existing deterministic game engine.
2. **Given** a draft is invalid, **When** the author starts a playtest, **Then** the editor reports blocking validation issues rather than starting an incoherent game.
3. **Given** a playtest is active, **When** the author takes classic or natural-language actions, **Then** rules, puzzle evaluation, world generation, and AI safety boundaries match player gameplay.

---

### User Story 8 - Follow the Adventure Authoring Manual (Priority: P3)

As an adventure author, I want a Markdown manual explaining the complete workflow so that I can create and publish an adventure without developer assistance.

**Why this priority**: The manual makes the editor usable beyond its initial authors and documents the author-facing contract.

**Independent Test**: A new author follows the manual to create, validate, publish, and playtest a simple adventure without manual database changes or developer help.

**Acceptance Scenarios**:

1. **Given** an author needs guidance, **When** they open the adventure-authoring manual, **Then** it explains drafts, structure, NPCs, lore, puzzles, AI proposals, validation, publication, version restoration, and playtesting.
2. **Given** an author follows the manual's example, **When** they complete its steps, **Then** they create a valid simple adventure containing locations, an NPC, lore, and linked puzzles.
3. **Given** author-facing editor workflows change, **When** the change is released, **Then** the maintained Markdown manual reflects the new workflow.

### Edge Cases

- **Invalid structure**: Invalid connections, duplicate identifiers, missing references, unreachable starts, and contradictory protected lore must be reported without corrupting the saved draft.
- **Concurrent editing**: When two edits target the same draft, the author must receive a clear conflict result and no change may silently overwrite another accepted change.
- **Failed AI proposal**: A failed, invalid, or unavailable proposal must not modify the draft or prevent manual authoring.
- **Publish race**: Concurrent publication requests must create at most one next version for the same validated draft state.
- **Version isolation**: Editing or restoring content must never alter a published version or a game already started from it.
- **Playtest failure**: A failure to start a playtest must leave the draft and published version history unchanged.
- **Visual failure**: A missing or failed preview must not prevent editing, validation, publishing, or playtesting.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide an in-application editor for authors to create and edit adventure drafts.
- **FR-002**: The system MUST persist an adventure's draft content, status, accepted changes, and version history.
- **FR-003**: The system MUST allow authors to manage locations, connections, items, NPCs, lore, and puzzles in a draft.
- **FR-004**: The system MUST preserve a unique identifier for each adventure and prevent a draft from overwriting another adventure.
- **FR-005**: The system MUST validate location identities, destinations, connection directions, starting-location reachability, and initial-world traversability.
- **FR-006**: The system MUST allow authors to define NPC identity, personality, knowledge boundaries, goals, relationships, and authoritative location.
- **FR-007**: The system MUST support lore classified as immutable fact, historical fact, regional fact, rumour, or player-discoverable knowledge.
- **FR-008**: The system MUST support puzzles with prerequisites, clues, multiple valid solutions, outcomes, and chain links.
- **FR-009**: The system MUST validate all puzzle references and prevent a puzzle, outcome, or chain link from referencing a missing element.
- **FR-010**: The system MUST accept natural-language authoring requests and return structured, reviewable AI proposals.
- **FR-011**: The system MUST require an author to explicitly accept a proposal before it changes draft content.
- **FR-012**: The system MUST validate an accepted proposal deterministically before persisting it and MUST reject content that contradicts protected facts or structural rules.
- **FR-013**: The system MUST prevent AI from directly publishing, overwriting, or otherwise authoritatively mutating an adventure.
- **FR-014**: The system MUST block publication until the draft passes all structural, reference, topology, lore, and meaningful-interaction validation rules.
- **FR-015**: The system MUST provide validation results that identify every blocking issue's affected element and reason.
- **FR-016**: The system MUST create an immutable published version from a validated draft only after an explicit author publication action.
- **FR-017**: The system MUST preserve published versions used by active games when later versions are edited or published.
- **FR-018**: The system MUST allow authors to restore a previous published version as a new editable draft without modifying the original version.
- **FR-019**: The system MUST allow authors to start a playtest from a valid draft using the same deterministic game engine, rules, puzzle evaluation, world generation, and AI safety boundaries used by player games.
- **FR-020**: The system MUST prevent a playtest from starting when the draft has blocking validation errors.
- **FR-021**: The system MUST preserve the retro 8-bit identity for author-created locations and optional visual previews.
- **FR-022**: The system MUST allow editing, saving, validation, publishing, and playtesting to continue when AI proposal, enrichment, image-generation, or preview behavior fails.
- **FR-023**: The system MUST maintain a Markdown adventure-authoring manual at `docs/adventure-authoring-guide.md` that describes the complete author workflow and is updated when that workflow changes.
- **FR-024**: The system MUST remain compatible with the existing modular-monolith deployment and must not require independently deployed editor or AI services.
- **FR-025**: The system MUST exclude collaborative real-time editing, public marketplaces, author monetization, mod marketplaces, arbitrary player asset uploads, multiplayer playtests, freehand visual map editing, and autonomous AI publication from this feature.

### Key Entities

- **Adventure Draft**: An editable, persistent collection of accepted author content that is not player-publishable until validation succeeds.
- **Adventure Version**: An immutable published snapshot used to start player games and retain historical releases.
- **Authoring Proposal**: A structured, non-authoritative AI suggestion with its requested purpose, proposed changes, review status, and validation result.
- **Authoring Validation Result**: A report of blocking or informational issues, each connected to the affected authoring element.
- **Playtest Session**: A game session bound to a valid draft snapshot and evaluated through the normal deterministic game engine.
- **Authoring Manual**: The maintained Markdown guide describing how authors use the editor and its review, validation, publication, and playtest workflows.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An author can create, save, reopen, and edit a draft containing a title, identifier, and starting location in one authoring session.
- **SC-002**: An author can create and validate an adventure with at least three locations, one NPC, one lore entry, and two linked puzzles with no unresolved blocking issue.
- **SC-003**: During validation tests, 100% of invalid identifiers, missing references, invalid connections, unreachable starts, and contradictory protected lore are reported before publication.
- **SC-004**: An author can request, edit, accept, and reject AI proposals, with 100% of rejected or unaccepted proposals leaving draft content unchanged.
- **SC-005**: An author can publish a valid adventure and start a playable game from the resulting version in one continuous workflow.
- **SC-006**: In version-isolation tests, 100% of games started from an earlier version retain that version's content after a later version is published.
- **SC-007**: A playtest of a valid draft applies the same deterministic outcomes for configured movement and puzzle actions as a game started from a published version.
- **SC-008**: In failure tests for AI proposals, enrichment, and visual previews, 100% of persisted drafts remain editable and no authoring action is blocked solely by the failed optional behavior.
- **SC-009**: At least 90% of guided authors can create, validate, publish, and playtest a simple adventure without manual database changes.
- **SC-010**: An author can follow `docs/adventure-authoring-guide.md` to create, validate, publish, and playtest a simple adventure without developer assistance.
- **SC-011**: For a small adventure draft, at least 95% of save-and-validate operations complete within 2 seconds during a representative authoring test.

## Assumptions

- Feature 003 builds on Feature 002's validated adventure definition, reusable puzzle, world, lore, NPC, and deterministic game-engine models.
- A single authenticated authoring role is sufficient for this feature; collaboration and granular editor permissions are out of scope.
- A draft may be saved while incomplete, but a playtest or publication requires it to pass blocking validation.
- Published versions are immutable snapshots, while drafts remain editable copies.
- AI suggestions are optional accelerators; authors can complete every workflow manually.
- The existing retro visual pipeline may provide optional previews, but presentation is not a source of authoritative adventure facts.