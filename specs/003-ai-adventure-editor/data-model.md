# Data Model: AI Adventure Editor

**Feature**: 003-ai-adventure-editor | **Date**: 2026-09-02

| Entity | Key fields | Rules |
|---|---|---|
| `AdventureDraft` | id, adventure identifier, title, definition JSON, revision, status, current version id | Mutable only through validated author actions; identifier is unique. |
| `AdventureVersion` | id, draft id, sequence, definition JSON, published at | Immutable; unique `(draftId, sequence)`. |
| `AuthoringProposal` | id, draft id, request summary, patch JSON, status, validation report | Pending until author accepts/rejects; never applies itself. |
| `AuthoringValidationResult` | draft/proposal id, issues, evaluation revision | Each blocking issue identifies element and reason. |
| `PlaytestSession` | id, draft id, draft revision, game id, started at | Bound to a validated snapshot; does not modify source content. |
| `AuthoringAuditEntry` | id, draft id, action, timestamp, source | Records acceptance, rejection, publication, restoration, and playtest actions. |

```text
AdventureDraft 1 --- * AuthoringProposal
AdventureDraft 1 --- * AdventureVersion
AdventureDraft 1 --- * AuthoringAuditEntry
AdventureDraft 1 --- * PlaytestSession --- 1 Game
AdventureVersion 1 --- * Game (immutable source version identity)
```

Validation reuses runtime JSON validation and adds identifier uniqueness, reachable start, meaningful playable content, protected-lore consistency, and location/NPC/item/puzzle/lore reference checks.