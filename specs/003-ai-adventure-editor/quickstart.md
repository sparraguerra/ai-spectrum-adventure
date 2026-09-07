# Quickstart: AI Adventure Editor

**Feature**: 003-ai-adventure-editor | **Date**: 2026-09-02

## Prerequisites

- .NET 10 and Docker for local PostgreSQL.
- Existing Web development connection settings.
- AI settings are optional; automated proposal tests use mocks.

## Validation

Run `dotnet build`, `dotnet test`, and `dotnet run --project src/AI.SpectrumAdventure.Web`.

For the complete Feature 003 validation gate, run from the repository root:

```powershell
dotnet test
az bicep build --file infra/main.bicep
```

The integration tests use EF Core InMemory and deterministic fakes, so they do not require PostgreSQL, Azure AI, or image generation. They cover concurrent draft saves, concurrent publication requests, optional retro preview failure with factual fallback, and the small-draft save-and-validate p95 target of less than 2 seconds. The editor emits authoring proposal, validation, acceptance, publication, restore, and playtest telemetry through the existing OpenTelemetry setup without recording raw authoring content.

## Manual scenarios

1. Create, save, reopen, and edit a draft with a unique identifier and start location.
2. Add three locations, connections, an item, an NPC, lore, and two linked puzzles; observe precise validation failures for invalid references.
3. Request a proposal, accept an edited valid proposal, and reject another; only accepted content changes the draft.
4. Block publication for invalid content, correct it, publish, and start a game from the resulting version.
5. Publish another version, confirm an existing game retains its source version, then restore the older version to a new draft.
6. Start a playtest from a valid draft and verify rules/puzzles match player gameplay.
7. Disable optional AI/preview behavior and verify editing, saving, validation, publishing, and playtesting still work.
8. Follow `docs/adventure-authoring-guide.md` to complete the workflow without database changes.

## Failure and concurrency checks

- Triggering a preview failure must leave the factual location name, mood, and visible objects available in the editor.
- Saving the same draft revision twice must produce one accepted write and one clear conflict; neither write may silently overwrite the other.
- Publishing the same validated draft revision twice must create at most one published version.
- The performance test measures 100 small-draft save-and-validate operations and checks the calculated p95 is below 2 seconds.