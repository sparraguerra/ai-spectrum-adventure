# Tasks: Scene Voice Narration

**Input**: Design documents from `specs/007-scene-voice-narration/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`

**Tests**: Included because acceptance criteria require exact text-to-speech behavior, safe external-service failure handling, audio cancellation, and a measurable foreground audio mix.

**Organization**: Tasks are grouped by the independently testable user journeys in `spec.md`. User Story 2 builds on the playback delivered by User Story 1; User Story 3 verifies that service and browser failures do not block the game.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel with other marked tasks because they change separate files and have no unfinished dependencies.
- **[Story]**: `US1`, `US2`, or `US3` maps to the user stories in `spec.md`.
- Every task names its target file or files.

## Path Conventions

Source and test paths are repository-relative under `src/` and `tests/`, as detailed in `plan.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the Azure Speech SDK dependency to the existing Infrastructure project.

- [ ] T001 [P] Add the Azure AI Speech SDK package reference to `src/AI.SpectrumAdventure.Infrastructure/AI.SpectrumAdventure.Infrastructure.csproj`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define the application boundary that all narration stories use.

- [ ] T002 Define `ISceneNarrationService` and its transient audio result contract in `src/AI.SpectrumAdventure.Application/Abstractions/ISceneNarrationService.cs`.

**Checkpoint**: The application can request audio without depending on Azure Speech SDK types.

---

## Phase 3: User Story 1 - Hear each scene narrated (Priority: P1) 🎯 MVP

**Goal**: Synthesize and play the visible narrative paragraph for the initial scene and each later non-empty narrative update.

**Independent Test**: Start an adventure with a configured Speech resource and confirm the initial narrative is spoken with the same text and language. Trigger another narrative update and confirm it plays without overlapping the previous audio.

### Tests for User Story 1

- [ ] T003 [US1] Add deterministic tests for successful synthesis, cache reuse, and cancellation using a fake speech client in `tests/AI.SpectrumAdventure.IntegrationTests/AzureSpeechNarrationServiceTests.cs`.

### Implementation for User Story 1

- [ ] T004 [US1] Implement Azure Speech options, a replaceable SDK client boundary, the Azure Speech SDK adapter, and the bounded in-memory `ISceneNarrationService` implementation in `src/AI.SpectrumAdventure.Infrastructure/Speech/AzureSpeechOptions.cs`, `src/AI.SpectrumAdventure.Infrastructure/Speech/ISpeechSynthesisClient.cs`, `src/AI.SpectrumAdventure.Infrastructure/Speech/AzureSpeechSdkClient.cs`, and `src/AI.SpectrumAdventure.Infrastructure/Speech/AzureSpeechNarrationService.cs` (depends on T002).
- [ ] T005 [US1] Register the Azure Speech options and `ISceneNarrationService` implementation in `src/AI.SpectrumAdventure.Web/Program.cs` using server-side credentials and external configuration (depends on T004).
- [ ] T006 [US1] Request narration for the initial and updated non-empty `LastResult.Narrative`, cancel obsolete requests, stop playback when returning to the start, and stream generated audio to JavaScript in `src/AI.SpectrumAdventure.Web/Components/AdventurePage.razor` (depends on T005).
- [ ] T007 [US1] Add browser playback for the narration stream, stop/replacement behavior, and audio URL/resource cleanup to `src/AI.SpectrumAdventure.Web/wwwroot/js/adventure-audio.js` (depends on T006).

**Checkpoint**: A player can hear the current visible narrative text, and successive narrations do not overlap.

---

## Phase 4: User Story 2 - Experience a clear, atmospheric voice mix (Priority: P1)

**Goal**: Give narration a low-pitched, slow, mysterious delivery and keep it clearly above the reduced background music.

**Independent Test**: Play a narrated scene with background music. Confirm the selected locale-matched voice meets the requested mood, music gain is `0.25`, and the mix is at least 10 dB in favor of narration.

### Tests for User Story 2

- [ ] T008 [US2] Add tests for locale selection, XML-safe SSML text, and configured pitch/rate prosody in `tests/AI.SpectrumAdventure.IntegrationTests/NarrationSsmlBuilderTests.cs`.

### Implementation for User Story 2

- [ ] T009 [US2] Build locale-aware SSML using a verified neural voice and configurable lower-pitch, slower-rate prosody in `src/AI.SpectrumAdventure.Infrastructure/Speech/NarrationSsmlBuilder.cs` and `src/AI.SpectrumAdventure.Infrastructure/Speech/AzureSpeechOptions.cs` (depends on T004 and T008).
- [ ] T010 [P] [US2] Set the effective background music gain to `0.25` of its browser default and keep narration at foreground gain in `src/AI.SpectrumAdventure.Web/wwwroot/js/adventure-audio.js` (depends on T007).
- [ ] T011 [US2] Record the selected locale/voice and the measured mix verification steps in `specs/007-scene-voice-narration/quickstart.md` (depends on T009 and T010).

**Checkpoint**: Narration has the requested delivery, and the music remains clearly subordinate during speech and pauses.

---

## Phase 5: User Story 3 - Continue when narration cannot play (Priority: P2)

**Goal**: Preserve readable text and usable controls when Azure Speech synthesis or browser playback fails.

**Independent Test**: Make synthesis unavailable, confirm the narrative remains visible and controls remain usable, then restore service availability and confirm a later scene can be narrated.

### Tests for User Story 3

- [ ] T012 [US3] Add tests for synthesis failure, playback cancellation, and a successful later synthesis after a prior failure using fakes in `tests/AI.SpectrumAdventure.IntegrationTests/SceneNarrationFailureTests.cs`.

### Implementation for User Story 3

- [ ] T013 [P] [US3] Handle typed synthesis failures without hiding the narrative or blocking game input, and report a non-blocking audio status in `src/AI.SpectrumAdventure.Web/Components/AdventurePage.razor`.
- [ ] T014 [P] [US3] Report browser playback failures to the page, stop failed audio, and release audio resources in `src/AI.SpectrumAdventure.Web/wwwroot/js/adventure-audio.js`.

**Checkpoint**: A failed narration does not prevent reading the scene, playing the game, or trying narration for a later scene.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Document configuration and validate the complete requested experience.

- [ ] T015 Update `README.md` with the Azure Speech resource setup and secure local/production configuration requirements without including credentials.
- [ ] T016 Run the focused integration tests and complete every manual scenario in `specs/007-scene-voice-narration/quickstart.md`; record test and audio-mix results there.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 adds the SDK package and can run independently of T002.
- **Foundational (Phase 2)**: T002 defines the app-level speech boundary; it must finish before service implementation.
- **User Story 1 (Phase 3)**: T003 defines expected deterministic service behavior; T004 implements the service; T005 registers it; T006 and T007 connect page lifecycle to browser playback.
- **User Story 2 (Phase 4)**: Depends on the User Story 1 playback path. T008 and T009 establish the voice/prosody behavior; T010 changes the independent music gain in the same JS module after T007; T011 records the final manual validation.
- **User Story 3 (Phase 5)**: Depends on the existing service and playback. T012 verifies failure behavior; T013 and T014 handle the separate server and browser failure surfaces.
- **Polish (Phase 6)**: Depends on all user stories.

### User Story Dependencies

- **US1 (P1)**: Depends on T001 and T002; delivers the base narration flow.
- **US2 (P1)**: Depends on US1 because the requested voice mix cannot be evaluated without narration playback.
- **US3 (P2)**: Depends on US1 so it can preserve the actual visible narration flow when synthesis or playback fails.

### Parallel Execution Examples

```text
# Initial setup can proceed in parallel:
T001 Add the SDK package reference
T002 Define the application narration boundary

# After US1 playback exists, voice shaping and music gain are separate changes:
T008 Add SSML builder tests
T010 Reduce music gain in the browser audio module
```

The two User Story 2 tasks above can be prepared in parallel, but T010 must wait for T007 because both update `adventure-audio.js`. T009 must wait for T004 because it extends the Azure Speech options and synthesis path.

### Implementation Strategy

1. Build the complete P1 experience by finishing US1 and US2: visible scene text is narrated, voice delivery is tuned, and music is reduced.
2. Add the P2 resilience behavior in US3 so service and browser failures remain non-blocking.
3. Run automated tests and the manual audio, transition, and failure scenarios in the quickstart guide.

**Suggested MVP**: US1 and US2 together. US1 alone would not meet the requested voice style or audio balance.
