# Tasks: AI Adventure Editor

**Input**: [spec.md](./spec.md), [plan.md](./plan.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), and [quickstart.md](./quickstart.md).

**Tests**: Required for deterministic authoring, validation, versioning, playtesting, and AI contract boundaries. No live model calls in CI.

## Requirement Traceability

| Requirement / Success Criteria | Tasks |
|---|---|
| FR-001, FR-002, FR-004; SC-001 | T001-T014 |
| FR-003, FR-005; SC-002, SC-003 | T015-T024 |
| FR-006, FR-007, FR-008, FR-009 | T020-T024 |
| FR-010, FR-011, FR-012, FR-013; SC-004 | T036-T042 |
| FR-014, FR-015, FR-016; SC-005 | T025-T030 |
| FR-017, FR-018; SC-006 | T003, T006-T010, T027, T043-T046 |
| FR-019, FR-020; SC-007 | T031-T035 |
| FR-021, FR-022; SC-008 | T041-T042, T051-T054 |
| FR-023; SC-009, SC-010 | T047-T049, T053-T054 |
| FR-024, FR-025 | T001-T010, T050-T054 |
| Plan performance goal; SC-011 | T055 |

## Phase 1: Setup and Shared Authoring Foundation

### Execution research for T001-T010

- Verified affected projects are `AI.SpectrumAdventure.Domain`, `Contracts`, `Application`, `Infrastructure`, `Web`, `Domain.Tests`, and `IntegrationTests`; no existing authoring implementation or identifiers were found.
- Existing persistence stores rich aggregates as JSON snapshots with EF configuration classes applied from the Infrastructure assembly. `AdventureDbContext` uses PostgreSQL in production and the integration test factory uses EF InMemory.
- Existing `Game`/`GameSnapshot` already preserve `AdventureId`; T003 must add nullable `AdventureVersionId` with a default-preserving constructor path so Feature 001/002 callers remain compatible.
- Existing tests use xUnit and FluentAssertions; focused validation will run the Domain and Integration test projects plus a solution build without restore.
- No scenario-specific execution skill root or breakdown-hints file was forwarded. This batch is one coherent foundation unit with independent additive model, contract, persistence, DI, builder, and focused test changes; decomposition verdict: `atomic`.

- [X] T001 Create authoring aggregate identifiers, statuses, and audit action types in `src/AI.SpectrumAdventure.Domain/Authoring/AuthoringTypes.cs`
- [X] T002 [P] Create typed draft, validation, proposal, publish, and playtest contracts in `src/AI.SpectrumAdventure.Contracts/AuthoringContracts.cs`
- [X] T003 Create `AdventureDraft`, `AdventureVersion`, `AuthoringProposal`, validation issue, audit entities, and immutable Game source-version identity in `src/AI.SpectrumAdventure.Domain/Authoring/`, `src/AI.SpectrumAdventure.Domain/Games/Game.cs`, and `src/AI.SpectrumAdventure.Domain/Games/GameSnapshot.cs`
- [X] T004 Define `IAdventureAuthoringRepository`, `IAdventureValidator`, and `IAuthoringProposalAgent` in `src/AI.SpectrumAdventure.Application/Abstractions/`
- [X] T005 [P] Add deterministic authoring test builders in `tests/AI.SpectrumAdventure.Domain.Tests/TestDoubles/AdventureDraftBuilder.cs`
- [X] T006 Add EF records, DbSets, configurations, concurrency tokens, and immutable version indexes in `src/AI.SpectrumAdventure.Infrastructure/Persistence/`
- [X] T007 Implement `EfAdventureAuthoringRepository` and add the authoring persistence migration in `src/AI.SpectrumAdventure.Infrastructure/Persistence/`
- [X] T008 Register authoring services and repositories in `src/AI.SpectrumAdventure.Web/Program.cs`
- [X] T009 [P] Unit-test aggregate mutability, immutable version snapshots, proposal statuses, and audit entries in `tests/AI.SpectrumAdventure.Domain.Tests/AuthoringAggregateTests.cs`
- [X] T010 [P] Integration-test draft/version persistence, immutable Game source-version identity, and optimistic concurrency in `tests/AI.SpectrumAdventure.IntegrationTests/AdventureAuthoringPersistenceTests.cs`

## Phase 2: User Story 1 - Create and Save a Draft (P1)

### Execution research for T011-T014

- Verified the foundation exposes `AdventureDraft`, `IAdventureAuthoringRepository.FindDraftAsync`, `FindDraftByIdentifierAsync`, and `SaveDraftAsync(draft, expectedRevision)`; `AdventureDraft.Update` increments revisions and rejects blocking validation results.
- Verified no `AdventureAuthoringValidator` exists yet; T015 owns structural validation, so T011-T014 use an injected validator boundary and a deterministic no-issue validator for the editor flow without implementing T015 rules.
- Verified the Web project uses server-interactive Razor components with route discovery through `Routes.razor`; no existing authoring page or shared editor model exists. The new page will be a thin form adapter and will not implement world/NPC/lore/puzzle panels from T015+.
- Verified application tests use xUnit and FluentAssertions with project references to Application, Domain, and Contracts; integration tests use EF InMemory and the existing authoring repository factory.
- No scenario-specific execution skill root or breakdown-hints file was forwarded. T011-T014 are one coherent CRUD/editor vertical slice with focused unit and integration checks; decomposition verdict: `atomic`.

**Independent Test**: Create a draft, save it, reopen it, edit it, and verify duplicate identifiers are rejected.

- [X] T011 [US1] Implement create, load, and save draft use cases with revision conflict handling in `src/AI.SpectrumAdventure.Application/Authoring/DraftUseCases.cs`
- [X] T012 [US1] Add draft creation, save, reopen, and conflict UI in `src/AI.SpectrumAdventure.Web/Components/Authoring/AdventureEditor.razor`
- [X] T013 [P] [US1] Unit-test draft save/reopen and duplicate-identifier rejection in `tests/AI.SpectrumAdventure.Application.Tests/DraftUseCaseTests.cs`
- [X] T014 [US1] Integration-test persisted draft editing and concurrent edit rejection in `tests/AI.SpectrumAdventure.IntegrationTests/DraftEditingFlowTests.cs`

## Phase 3: User Story 2 - Author the World Structure (P1)

### Execution research for T015-T019

- Verified the authoritative definition JSON shape in `AdventureWorldFactory`: top-level `id`, `title`, `startingLocationId`, `locations`, `items`; locations use `id`, `name`, `description`, `exits`, `objectIds`, and `npcIds`; exits use `direction` and `to`.
- Verified runtime directions are the `ConnectionDirection` values `North`, `East`, `South`, `West`, `Up`, and `Down`; item states are serialized as strings and must remain compatible with the existing JSON serializer.
- Verified `DraftUseCases` accepts an injected `IAdventureValidator`, while `AdventureDraft.Update` rejects blocking validation results; T015 therefore supplies structural validation and T016 supplies deterministic JSON patching without changing persistence contracts.
- Verified the Web editor is currently a single JSON form and can host focused child panels; no existing world-structure panel or patch abstraction exists. T020+ NPC/lore/puzzle validation and editing remain out of scope.
- Verified focused tests use xUnit and FluentAssertions, and integration persistence uses EF InMemory via `TestDbContextFactory.Create()`.
- No scenario-specific execution skill root or breakdown-hints file was forwarded. T015-T019 are one coherent world-structure vertical slice with validator, patcher, panels, and focused tests; decomposition verdict: `atomic`.

**Independent Test**: Add three locations, valid connections, an item, and a start location; save and validate the draft.

- [X] T015 [US2] Implement deterministic authoring validation for identifiers, exits, directions, reachability, and meaningful initial traversal in `src/AI.SpectrumAdventure.Application/Authoring/AdventureAuthoringValidator.cs`
- [X] T016 [US2] Implement draft definition patching for locations, connections, items, and starting location in `src/AI.SpectrumAdventure.Application/Authoring/DraftDefinitionEditor.cs`
- [X] T017 [US2] Add location, connection, item, and start-location editor panels in `src/AI.SpectrumAdventure.Web/Components/Authoring/`
- [X] T018 [P] [US2] Unit-test invalid destinations, duplicate IDs, invalid directions, and unreachable starts in `tests/AI.SpectrumAdventure.Application.Tests/AdventureAuthoringValidatorTests.cs`
- [X] T019 [US2] Integration-test save and reload of a traversable three-location draft in `tests/AI.SpectrumAdventure.IntegrationTests/WorldStructureAuthoringTests.cs`

## Phase 4: User Story 3 - Author NPCs, Lore, and Puzzles (P1)

**Independent Test**: Add NPC, lore, and two linked puzzles with alternative solutions; validate all references.

### Execution research for T020-T024

- Verified `AdventureWorldFactory` consumes top-level `npcs` and either the legacy `puzzle` object or reusable `puzzles` array; NPC placement is expressed by location `npcIds`, knowledge by `knowledgeBoundary`, and puzzle references by `prerequisites`, solution `conditions`, `outcomes`, and `chainLinks`.
- Verified puzzle condition types are `ItemPossessed`, `ClueKnown`, `WorldFlagSet`, and `PuzzleSolved`; follow-on puzzle outcomes use `FollowOnPuzzle`. No existing lore JSON contract exists, so editor/validator support will preserve a compatible additive `lore` array with classification and protected fields.
- Verified the existing JSON patcher preserves unrelated properties and emits optional arrays, while the validator aggregates blocking issues without changing draft persistence APIs. T020-T024 are one coherent US3 vertical slice across the patcher, validator, editor panels, unit tests, and persistence integration test; decomposition verdict: `atomic`.
- No scenario-specific execution skill root or breakdown-hints file was forwarded.

- [X] T020 [US3] Extend draft definition editing for NPCs, lore classifications, reusable puzzles, outcomes, and chains in `src/AI.SpectrumAdventure.Application/Authoring/DraftDefinitionEditor.cs`
- [X] T021 [US3] Extend deterministic validation for NPC location/knowledge, protected lore, puzzle references, alternatives, outcomes, and chains in `src/AI.SpectrumAdventure.Application/Authoring/AdventureAuthoringValidator.cs`
- [X] T022 [US3] Add NPC, lore, puzzle, solution, and chain editor panels in `src/AI.SpectrumAdventure.Web/Components/Authoring/`
- [X] T023 [P] [US3] Unit-test lore classification and puzzle/NPC reference validation in `tests/AI.SpectrumAdventure.Application.Tests/AuthoringLoreAndPuzzleValidationTests.cs`
- [X] T024 [US3] Integration-test persisted NPC, lore, and linked multi-solution puzzle definitions in `tests/AI.SpectrumAdventure.IntegrationTests/AdvancedAuthoringFlowTests.cs`

## Phase 5: User Story 5 - Validate and Publish a Version (P1)

**Independent Test**: Block invalid publication, correct the draft, publish one immutable version, and load it through the player catalog.

### Execution research for T025-T030

- Verified T020-T024 already provide the shared deterministic validator, draft repository, immutable `AdventureVersion` domain type, and EF authoring records; no publish use case, published-version catalog resolution, or publish UI exists yet.
- Verified `AdventureDraft.Update` rejects blocking validation results and increments revisions, while `MarkPublished` stores the current immutable version identity. `IAdventureAuthoringRepository` currently exposes separate draft/version saves, so atomic publication requires an additive repository operation that validates and commits the version plus draft metadata in one persistence transaction.
- Verified `IAdventureCatalog` and `StartGameUseCase` currently load mutable `AdventureRecord` JSON and do not carry `AdventureVersionId`; T027 must add latest-by-adventure resolution for new games and explicit version resolution without changing legacy catalog fallback behavior.
- Verified existing xUnit/FluentAssertions focused tests and EF InMemory integration factory are the cheapest discriminating checks for blocking issue attribution, sequence/version immutability, invalid-to-valid publication, catalog latest selection, and explicit version isolation.
- No Feature 003 execution-stage skill root or Breakdown Hints file was forwarded or found. T025-T030 form one coherent publication vertical slice with a validation gate between draft state and catalog exposure; decomposition verdict: `atomic`.

- [X] T025 [US5] Implement full validation result aggregation with element-specific blocking reasons in `src/AI.SpectrumAdventure.Application/Authoring/AdventureAuthoringValidator.cs`
- [X] T026 [US5] Implement atomic publish use case that creates the next immutable version only after validation in `src/AI.SpectrumAdventure.Application/Authoring/PublishAdventureUseCase.cs`
- [X] T027 [US5] Extend the published adventure catalog so new games select the latest published version by adventure identifier and existing games/playtests resolve their persisted immutable version or draft-snapshot identity in `src/AI.SpectrumAdventure.Infrastructure/Persistence/DatabaseAdventureCatalog.cs` and `src/AI.SpectrumAdventure.Application/Games/StartGameUseCase.cs`
- [X] T028 [US5] Add validation results and explicit publish command UI in `src/AI.SpectrumAdventure.Web/Components/Authoring/AdventureEditor.razor`
- [X] T029 [P] [US5] Unit-test publication blocking, issue attribution, and immutable version creation in `tests/AI.SpectrumAdventure.Application.Tests/PublishAdventureUseCaseTests.cs`
- [X] T030 [US5] Integration-test invalid-to-valid publication and catalog availability in `tests/AI.SpectrumAdventure.IntegrationTests/AdventurePublicationFlowTests.cs`

## Phase 6: User Story 7 - Playtest an Adventure (P1)

### Execution research for T031-T035

- Verified `StartGameUseCase` already creates games through `AdventureWorldFactory`, applies the initial discovery event, and persists through `IGameRepository`; the playtest flow can reuse the same JSON factory path without loading or mutating a published catalog version.
- Verified `AdventureDraft` exposes immutable-at-call snapshot values (`Id`, `Revision`, `AdventureIdentifier`, `DefinitionJson`) and validation is a pure `IAdventureValidator.Validate` operation unless explicitly recorded, so playtest validation must avoid `RecordValidation`/`Update`.
- Verified `IAdventureAuthoringRepository.AddPlaytestSessionAsync` and EF `PlaytestSessionRecord` exist, but persistence currently lacks a snapshot identity field; T032 will add a deterministic snapshot hash and optional source version identity while retaining draft ID/revision.
- Verified the web editor is server-interactive and has no playtest component; T033 will add a focused launch/result panel wired to the existing authoring editor state. Existing orchestration remains the action engine for games after launch.
- Focused checks will be new application unit tests and EF InMemory integration coverage comparing movement/puzzle behavior from an identical draft snapshot and published version. No Feature 003 execution-stage skill root or Breakdown Hints file was forwarded; this is one coherent playtest vertical slice, decomposition verdict: `atomic`.

**Independent Test**: Start a playtest from a valid draft and verify configured actions have the same deterministic results as a published game.

- [X] T031 [US7] Implement playtest creation from a validated immutable draft snapshot using existing game creation in `src/AI.SpectrumAdventure.Application/Authoring/StartPlaytestUseCase.cs`
- [X] T032 [US7] Persist playtest metadata and source revision/version identity in `src/AI.SpectrumAdventure.Infrastructure/Persistence/EfAdventureAuthoringRepository.cs`
- [X] T033 [US7] Add playtest launch and result UI in `src/AI.SpectrumAdventure.Web/Components/Authoring/PlaytestPanel.razor`
- [X] T034 [P] [US7] Unit-test invalid drafts cannot start playtests and valid draft snapshots are isolated in `tests/AI.SpectrumAdventure.Application.Tests/StartPlaytestUseCaseTests.cs`
- [X] T035 [US7] Integration-test draft playtest versus published-game movement and puzzle equivalence in `tests/AI.SpectrumAdventure.IntegrationTests/AdventurePlaytestFlowTests.cs`

## Phase 7: User Story 4 - Request and Review AI Proposals (P2)

### Execution research for T036-T042

- Verified the existing contract exposes only a minimal `AuthoringProposalContract`, while the agent boundary is `IAuthoringProposalAgent.CreateProposalAsync`; `IAgentRunner` already enforces Microsoft Agent Framework JSON-schema output through `AIAgentRunner`.
- Verified `StructuredOutputValidator` retries once and falls back safely, and existing agent wrappers build prompts from narrowly scoped context without persistence dependencies.
- Verified `AuthoringProposal`, `AdventureDraft.Update`, `DraftDefinitionEditor`, and `IAdventureAuthoringRepository` provide the domain/application seams needed for pending proposals, deterministic JSON patch application, and audit persistence; proposal retrieval/update APIs are not yet present and must be additive.
- Verified the editor is an Interactive Server Razor component with existing authoring child panels; proposal review can be added as a focused panel without changing publish/playtest behavior.
- Verified xUnit/FluentAssertions agent, application, and EF InMemory integration test projects are already wired. Focused checks will cover scoped prompt context, retry/fallback, invalid output, rejected/unaccepted immutability, accepted deterministic validation, and audit entries.
- No scenario-specific execution skill root or Breakdown Hints file was forwarded. T036-T042 are one coherent proposal-review vertical slice with explicit validation and author-approval boundaries; decomposition verdict: `atomic`.

**Independent Test**: Request a proposal, accept an edited valid proposal, reject another, and verify only the accepted proposal changes the draft.

- [X] T036 [US4] Define structured proposal prompt/context and output validation in `src/AI.SpectrumAdventure.Contracts/AuthoringContracts.cs` and `src/AI.SpectrumAdventure.Agents/Authoring/AuthoringProposalValidator.cs`
- [X] T037 [US4] Implement `AuthoringProposalAgent` with scoped draft context and no persistence access in `src/AI.SpectrumAdventure.Agents/Authoring/AuthoringProposalAgent.cs`
- [X] T038 [US4] Implement proposal request, edit, accept, reject, deterministic patch validation, and audit use cases in `src/AI.SpectrumAdventure.Application/Authoring/ProposalUseCases.cs`
- [X] T039 [US4] Add proposal review and accept/reject UI in `src/AI.SpectrumAdventure.Web/Components/Authoring/ProposalPanel.razor`
- [X] T040 [P] [US4] Unit-test invalid/unaccepted proposals never mutate drafts in `tests/AI.SpectrumAdventure.Application.Tests/ProposalUseCaseTests.cs`
- [X] T041 [P] [US4] Unit-test schema-invalid or failed agent output returns a safe proposal failure in `tests/AI.SpectrumAdventure.Agents.Tests/AuthoringProposalAgentTests.cs`
- [X] T042 [US4] Integration-test accept/reject/audit behavior and AI failure fallback in `tests/AI.SpectrumAdventure.IntegrationTests/AuthoringProposalFlowTests.cs`

## Phase 8: User Story 6 - Manage Published History (P2)

**Independent Test**: Publish twice, inspect both versions, restore the first into a new draft, and verify games preserve their original version.

### Execution research for T043-T046

- Verified `IAdventureAuthoringRepository.GetVersionsAsync` and `EfAdventureAuthoringRepository` already provide ordered immutable version snapshots, while `AdventureDraft` requires a unique adventure identifier and `SaveDraftAsync(..., -1)` supports creating a new draft.
- Verified `DatabaseAdventureCatalog` resolves latest versions for new games and explicit `AdventureVersionId` values for existing games; `Game` persistence retains that source identity, so isolation tests can prove later publication does not change an active game's version.
- Restore will copy the selected version's JSON and starting location into a fresh `AdventureDraftId`; the use case accepts a new adventure identifier so it cannot overwrite the source draft or violate the unique identifier index. The original `AdventureVersion` is never mutated.
- A version history panel will be a thin Interactive Server component using the existing authoring services and explicit restore inputs. Focused application tests cover query, immutable copy, and identifier validation; EF InMemory integration tests cover two publications, explicit catalog pinning, and restored draft persistence.
- No scenario-specific execution-stage skill root or breakdown-hints file was forwarded. T043-T046 are one coherent version-history vertical slice with application, UI, and focused persistence checks; decomposition verdict: `atomic`.

- [X] T043 [US6] Implement version history query and restore-as-new-draft use case in `src/AI.SpectrumAdventure.Application/Authoring/VersionHistoryUseCases.cs`
- [X] T044 [US6] Add version history, inspection, and restore UI in `src/AI.SpectrumAdventure.Web/Components/Authoring/VersionHistoryPanel.razor`
- [X] T045 [P] [US6] Unit-test immutable version isolation and restore-copy behavior in `tests/AI.SpectrumAdventure.Application.Tests/VersionHistoryUseCaseTests.cs`
- [X] T046 [US6] Integration-test two publications, active-game isolation, and restoration in `tests/AI.SpectrumAdventure.IntegrationTests/AdventureVersionIsolationTests.cs`

## Phase 9: User Story 8 - Follow the Adventure Authoring Manual (P3)

### Execution research for T047-T049

- Verified `AdventureEditor.razor` is the author-facing route at `/authoring/adventure` and already renders draft save/reopen, world/NPC/lore/puzzle/solution/chain panels, proposal review, validation/publish, version history/restore, and playtest controls. T048 can remain a documentation link addition without changing editor behavior.
- Verified `AdventureAuthoringValidator`, `PublishAdventureUseCase`, and `StartPlaytestUseCase` provide deterministic validation, immutable publication, and draft-snapshot playtesting. T049 can extract the guide's fenced JSON example and exercise those existing use cases against EF InMemory, with no live model or manual database changes.
- Verified the manual must explicitly cover draft, structure, NPC/lore/puzzles/chains, AI proposal boundaries, validation, publication, versions/restore, playtesting, failures, and authoritative-vs-enrichment boundaries. README and editor are the two required links; no T050+ implementation is needed.
- No Feature 003 execution-stage skill root or Breakdown Hints file was forwarded. T047-T049 are one coherent documentation-and-verification unit; decomposition verdict: `atomic`.

**Independent Test**: A new author follows the manual's example to create, validate, publish, and playtest an adventure.

- [X] T047 [US8] Create the author-facing workflow manual with a complete example in `docs/adventure-authoring-guide.md`
- [X] T048 [US8] Link the manual from the editor and repository documentation in `src/AI.SpectrumAdventure.Web/Components/Authoring/AdventureEditor.razor` and `README.md`
- [X] T049 [US8] Verify the manual contains all required workflow sections, its complete example is structurally valid, and the documented create-validate-publish-playtest sequence succeeds in `tests/AI.SpectrumAdventure.IntegrationTests/AdventureAuthoringManualTests.cs`

## Phase 10: Polish and Cross-Cutting Concerns

### Execution research for T050-T055

- Confirmed T050's owning operations are `ProposalUseCases`, `DraftUseCases`, `PublishAdventureUseCase`, `VersionHistoryUseCases`, and `StartPlaytestUseCase`; no application-owned authoring telemetry helper exists. Web already exports the Application activity source through OpenTelemetry, so telemetry can remain additive and must not record raw prompts, definitions, or preview bytes.
- Confirmed T051 can use the existing non-authoritative `IVisualArtDirector`, `IImageGenerator`, `VisualContext`, and `VisualSceneSpec` seams. The preview service must catch optional visual failures and return a factual text fallback; the Razor panel should be independently usable and must not affect authoring mutations.
- Confirmed the integration test project targets .NET 10, xUnit/FluentAssertions, EF InMemory, and references all application layers. Existing repository concurrency behavior and publish revision checks provide the direct seams for T052; the performance check can exercise deterministic validator plus in-memory draft save without live AI or PostgreSQL.
- Confirmed `README.md` and this feature's `quickstart.md` already document basic setup but omit telemetry/preview fallback, concurrency/publish-race, performance, and Bicep validation commands. `az` availability and the full test result are runtime checks for T054.
- No Feature 003 execution-stage skill root or Breakdown Hints file was forwarded. T050-T055 are a bounded polish batch, but T052/T055 are independent test additions and T054 is validation/documentation; decomposition verdict: `atomic` for this executor scope.

- [X] T050 [P] Add authoring proposal, validation, acceptance, publication, restore, and playtest telemetry in `src/AI.SpectrumAdventure.Application/Authoring/AuthoringTelemetry.cs`
- [X] T051 [P] Add optional retro preview integration with factual fallback in `src/AI.SpectrumAdventure.Application/Authoring/AuthoringPreviewService.cs` and `src/AI.SpectrumAdventure.Web/Components/Authoring/AdventurePreview.razor`
- [X] T052 Add concurrency and publish-race integration coverage in `tests/AI.SpectrumAdventure.IntegrationTests/AuthoringConcurrencyTests.cs`
- [X] T053 Update local setup and Feature 003 validation documentation in `README.md` and `specs/003-ai-adventure-editor/quickstart.md`
- [X] T054 Run `dotnet test` and `az bicep build --file infra/main.bicep` from the repository root, resolving Feature 003 regressions.
- [X] T055 Measure save-and-validate latency for a small draft and verify the p95 completes within 2 seconds in `tests/AI.SpectrumAdventure.IntegrationTests/AuthoringPerformanceTests.cs`
- [X] T056 [US7] Fix authored item command resolution so simple names such as `Take Screwdriver` map to stable item IDs instead of returning `AmbiguousIntent` in `src/AI.SpectrumAdventure.Agents/Intent/WorldVocabulary.cs`; add regression coverage in `tests/AI.SpectrumAdventure.Agents.Tests/CommandPatternInterpreterTests.cs`.

## Dependencies

```text
Foundation (T001-T010)
  -> US1 Drafts (T011-T014)
    -> US2 World Structure (T015-T019)
    -> US3 NPCs, Lore, Puzzles (T020-T024)
      -> US5 Publish (T025-T030)
        -> US7 Playtest (T031-T035)
        -> US4 AI Proposals (T036-T042)
        -> US6 Version History (T043-T046)
          -> US8 Manual (T047-T049)
            -> Polish (T050-T055)
```

## Parallel Opportunities

- T002 and T005 can run after T001; T009 and T010 can run after T003-T008.
- US2 validation/UI tests can proceed in parallel after its editor model is stable.
- US4 agent contract tests and proposal use-case tests can run in parallel.
- US6 history tests and US8 manual drafting can proceed after publication contracts stabilize.

## Implementation Strategy

The first MVP is T001-T019: authors can create persistent drafts and a valid world manually without AI. Add advanced content, publication/versioning, and playtesting before AI proposals. Finish with optional previews and the user manual. Every accepted change and published version must remain deterministic and auditable. Command targets from authored definitions must resolve through stable IDs, including simple natural-language forms such as `Take Screwdriver`.