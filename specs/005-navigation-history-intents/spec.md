# Feature Specification: Navigation History and Vertical Movement

**Feature Branch**: `005-navigation-history-intents`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "crea una especificación para que la intención "back" vuelva a la localización anterior y que la intención "Down" que aparece en una de las aventuras funcione porque actualmente no la reconoce"

## User Scenarios & Testing

### User Story 1 - Return to the previous location (Priority: P1)

As an adventure player, I want to use the `back` intent to return to the location I visited immediately before my current one, so I can retrace my route without having to remember or specify the reverse exit.

**Why this priority**: Returning through recent travel is a basic navigation action and directly addresses the requested `back` behavior.

**Independent Test**: Visit two connected locations, enter `back`, and verify that the player returns to the location they just left.

**Acceptance Scenarios**:

1. **Given** the player has successfully traveled from location A to location B, **When** the player enters `back`, **Then** the player returns to location A.
2. **Given** the player has successfully traveled through locations A, B, and C, **When** the player enters `back` twice, **Then** the player returns first to B and then to A.
3. **Given** the player is at the start of an adventure and has no previous location, **When** the player enters `back`, **Then** the player remains at the current location and is told that there is no previous location to return to.

---

### User Story 2 - Travel through a downward exit (Priority: P1)

As an adventure player, I want the `Down` direction authored for an exit to be understood as a movement intent, so I can follow routes such as descending from the leaning lighthouse into the sluice chamber.

**Why this priority**: An authored exit must be reachable through the matching player command; otherwise part of the adventure cannot be played as intended.

**Independent Test**: In *The Tide Beneath the Harbor*, reach the Leaning Lighthouse, enter `Down`, and verify that the player arrives at the Sluice Chamber.

**Acceptance Scenarios**:

1. **Given** the player is at the Leaning Lighthouse, which has a `Down` exit to the Sluice Chamber, **When** the player enters `Down`, **Then** the player travels to the Sluice Chamber.
2. **Given** the player is at the Leaning Lighthouse, **When** the player enters a natural-language downward movement command such as `go down`, **Then** the player travels through the same `Down` exit.
3. **Given** the player is at the Sluice Chamber, which has an `Up` exit to the Leaning Lighthouse, **When** the player enters a natural-language upward movement command such as `go up`, **Then** the player travels through the same `Up` exit.
4. **Given** the current location has no exit in the requested vertical direction, **When** the player requests that direction, **Then** the player remains in place and receives the existing no-exit feedback.
5. **Given** the current location has a vertical exit whose travel condition is unmet, **When** the player requests that direction, **Then** the player remains in place and receives feedback that the exit cannot yet be used.

### Edge Cases

- A failed or blocked movement does not create a previous-location entry.
- Using `back` after multiple successful movements returns through those movements in reverse chronological order.
- Once no earlier successful movement remains, another `back` leaves the player in place and gives the no-previous-location feedback.
- Direction words are recognized without case sensitivity, including `Down`, `down`, `Up`, and `up`.
- A vertical direction with no matching exit must not trigger movement to an unrelated location or an unknown-boundary exploration in place of normal no-exit behavior.

## Requirements

### Functional Requirements

- **FR-001**: The game MUST recognize `back` as a player request to return to the location most recently left through a successful movement.
- **FR-002**: The game MUST retain the sequence of locations reached through successful movement so that repeated `back` requests return through that sequence in reverse order.
- **FR-003**: The game MUST NOT add failed or blocked movement attempts to the previous-location sequence.
- **FR-004**: If no previous location is available, the game MUST keep the player at the current location and communicate that there is nowhere to go back to.
- **FR-005**: The game MUST recognize `Down` and `Up` as valid movement directions, including when those directions are authored on adventure exits.
- **FR-006**: The game MUST recognize vertical direction commands without regard to capitalization and MUST treat a natural-language command to go down or up as the corresponding direction.
- **FR-007**: The game MUST apply the same exit availability and movement conditions to vertical travel as it does to other directions.
- **FR-008**: When the requested vertical exit is unavailable or its conditions are unmet, the game MUST leave the player in place and provide the applicable movement feedback.

### Key Entities

- **Location history**: The ordered record of locations visited through successful player movement, used to determine the destination of a `back` request.
- **Adventure exit**: A connection from a location to another location, identified by its direction and any conditions required for travel.
- **Movement intent**: A player request to move, either by direction or by returning to a previous location.

## Success Criteria

### Measurable Outcomes

- **SC-001**: In all acceptance tests involving an available previous location, `back` returns the player to the immediately preceding location.
- **SC-002**: In a route containing at least three locations, successive `back` requests restore each preceding location in reverse order, with no skipped or duplicated locations.
- **SC-003**: In *The Tide Beneath the Harbor*, a player can travel from the Leaning Lighthouse to the Sluice Chamber using `Down` and return using `Up`.
- **SC-004**: All tested capitalization variants of `Up` and `Down` resolve to the intended direction.
- **SC-005**: In every tested case with no previous location or no available vertical exit, the player remains at the current location and receives clear feedback.

## Assumptions

- `back` refers to the most recent successful travel history, rather than an exit that happens to lead to the previously visited location.
- Each successful movement adds the departed location to the history; using `back` consumes that entry, so repeated requests continue to earlier locations.
- The existing movement rules, including exit conditions and failure feedback, remain in force for vertical travel.
- Adventure authors may continue to define vertical exits using the existing `Up` and `Down` direction names.
- Forward navigation after using `back` is not included in this feature.
