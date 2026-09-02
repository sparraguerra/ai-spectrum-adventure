# Adventure Authoring Guide

This guide is for authors using the Adventure Editor at `/authoring/adventure`. It describes the complete workflow for creating a draft, validating it, publishing an immutable version, and playtesting it. The editor stores authoring data in the same modular monolith as the player experience; no developer or database access is required.

## Authoring boundary

The draft definition is the authoritative game content. Locations, exits, item placement, NPC placement, lore classifications, puzzle rules, prerequisites, outcomes, and chain links are facts that the deterministic validator and game engine use.

AI proposals, generated descriptions, enrichment, and visual previews are optional authoring aids. They are never authoritative, never publish automatically, and cannot replace validation. Review and explicitly accept a proposal before it can change a draft. A failed AI or image request leaves manual editing, validation, publication, and playtesting available.

## 1. Create a draft

1. Open **Adventure Editor** and enter a unique, whitespace-free identifier, a title, and the starting location ID.
2. Enter or paste the definition JSON, then choose **Create draft**.
3. Use **Reopen draft** with the identifier to load a saved draft. Subsequent saves use the draft revision and report a conflict instead of silently overwriting a newer edit.

A draft may be incomplete while it is being authored. It must pass blocking validation before publication or playtesting.

## 2. Build the world structure

Add locations with stable IDs, names, descriptions, and exits. Each exit has a direction (`North`, `East`, `South`, `West`, `Up`, or `Down`) and a destination location ID. Add items and place them with `objectIds`. Set the editor's starting location to an existing location. The validator checks duplicate IDs, destinations, directions, references, reachability, and meaningful initial interaction.

## 3. Add NPCs and lore

An NPC should have a stable ID, name, personality, goals, relationships, and a knowledge boundary. Give it one authoritative location through the location's `npcIds` (or the NPC's `locationId` when supported). Lore entries use a classification such as `ImmutableFact`, `HistoricalFact`, `RegionalFact`, `Rumour`, or `PlayerDiscoverableKnowledge`. Protected or immutable facts must not contradict one another.

## 4. Add puzzles and chains

Define reusable puzzles with one or more solutions. Conditions can reference an item, known clue, world flag, or solved puzzle. Outcomes can reveal a clue, change an item or location, trigger an NPC interaction or world event, or follow on to another puzzle. Add the follow-on puzzle to `chainLinks`. The validator reports every missing item, lore key, NPC, location, puzzle, or chain target before publication.

## Complete example

The following example is a small, valid adventure. In the editor, set **Identifier** to `lantern-trail`, **Title** to `The Lantern Trail`, and **Starting location** to `gate`; paste the JSON into **Definition JSON**, and save it as a new draft.

```json
{
  "id": "lantern-trail",
  "title": "The Lantern Trail",
  "startingLocationId": "gate",
  "locations": [
    {
      "id": "gate",
      "name": "Moss Gate",
      "description": "A moonlit gate opens onto a narrow trail.",
      "exits": [{ "direction": "East", "to": "watchpost" }],
      "objectIds": ["brass-key"],
      "npcIds": ["mira"]
    },
    {
      "id": "watchpost",
      "name": "Old Watchpost",
      "description": "A quiet tower watches the forest.",
      "exits": [{ "direction": "East", "to": "lantern-room" }, { "direction": "West", "to": "gate" }]
    },
    {
      "id": "lantern-room",
      "name": "Lantern Room",
      "description": "A sealed lantern waits beside a stone door.",
      "exits": [{ "direction": "West", "to": "watchpost" }]
    }
  ],
  "items": [{ "id": "brass-key", "name": "Brass Key", "description": "A small key marked with a sun." }],
  "npcs": [{
    "id": "mira",
    "name": "Mira the Keeper",
    "personality": "Patient and observant",
    "goals": ["Keep the lantern safe"],
    "knowledgeBoundary": ["lantern-oath"]
  }],
  "lore": [{
    "id": "oath",
    "classification": "ImmutableFact",
    "contentKey": "lantern-oath",
    "content": "The keeper's oath protects the first lantern."
  }],
  "puzzles": [
    {
      "id": "gate-lock",
      "solutions": [
        { "id": "key-solution", "conditions": [{ "type": "ItemPossessed", "referenceId": "brass-key" }] },
        { "id": "oath-solution", "conditions": [{ "type": "ClueKnown", "referenceId": "lantern-oath" }] }
      ],
      "outcomes": [{ "id": "open-lantern", "type": "FollowOnPuzzle", "referenceId": "lantern-seal" }],
      "chainLinks": ["lantern-seal"]
    },
    {
      "id": "lantern-seal",
      "prerequisites": [{ "type": "PuzzleSolved", "referenceId": "gate-lock" }],
      "solutions": [{ "id": "seal-solution", "conditions": [{ "type": "ItemPossessed", "referenceId": "brass-key" }] }]
    }
  ]
}
```

## AI proposals

Use **Proposal** to request a focused suggestion, such as an NPC description or a location patch. The proposal is scoped to the selected draft context and appears as pending. Edit its structured patch if needed, then choose **Accept** only after reviewing it. Acceptance runs deterministic validation before changing the draft. Choose **Reject** to record the decision without changing the draft. Invalid, unavailable, or failed AI output is a safe proposal failure and does not block manual authoring.

## Validate and publish

Choose **Validate and publish** after saving. Read every blocking issue; each issue names the affected element and explains the correction. Fix the draft and save it again. Publication succeeds only when structural, reference, topology, lore, and meaningful-interaction checks pass. A successful publication creates the next immutable version. Editing the draft later does not change that version.

## Versions and restore

Use **Version history** to inspect published sequences and their snapshots. To return to an earlier release, enter a new unique identifier and choose **Restore**. Restore copies the selected JSON into a new editable draft; it never edits the original version or changes games already started from it. New player games use the latest published version, while existing games remain pinned to their source version.

## Playtest

Start **Playtest** from the saved draft after validation succeeds. The editor creates a detached snapshot and runs it through the same deterministic world factory, rules, puzzle evaluation, and action engine used by player games. Try movement, item actions, NPC interactions, clues, and each puzzle solution. A playtest start failure reports validation issues and leaves the draft and version history unchanged.

## Failure recovery

- **Duplicate ID or identifier**: choose a unique value; no existing adventure is overwritten.
- **Invalid exit or missing reference**: correct the destination, direction, or referenced element and validate again.
- **Conflicting edit**: reopen the draft, review the newer revision, and reapply your intended change.
- **Failed AI proposal or preview**: reject or discard it and continue manually; optional AI never controls the authoritative definition.
- **Failed publication or playtest**: fix the reported blocking issue and retry. Failed operations do not create a partial published version or mutate a published snapshot.

Keep this guide aligned with the editor whenever author-facing controls or validation rules change.