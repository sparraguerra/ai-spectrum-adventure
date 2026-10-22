# Implementation Plan: Scene Voice Narration

**Branch**: `007-scene-voice-narration` | **Date**: 2026-10-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/007-scene-voice-narration/spec.md`

## Summary

Narrate the text shown in the gameplay Narrative panel with Azure Speech, using a locale-appropriate neural voice and SSML prosody tuned for a low-pitched, slow, mysterious delivery. Keep synthesis and credentials on the server, play the resulting audio in the existing Blazor page, and reduce background music gain to 25% of the browser's current default. Cancel obsolete narration, keep text and gameplay available on failure, and cache repeated synthesis in bounded process memory to limit service calls.

## Technical Context

**Language/Version**: C# / .NET 10; JavaScript for existing browser audio controls.

**Primary Dependencies**: Existing ASP.NET Core Blazor Interactive Server application; Azure AI Speech SDK for .NET; existing `Azure.Identity` credential patterns; browser audio playback and Blazor JS interop.

**Storage**: No persistent storage. Synthesized audio may be cached with a bounded in-memory cache; no audio or narration state is added to the game database or Blob Storage.

**Testing**: xUnit and FluentAssertions in `AI.SpectrumAdventure.IntegrationTests`, with focused tests for SSML generation, cache keys/eviction behavior, cancellation and service failures using a replaceable speech boundary. Browser audio playback and the requested mix are verified with the quickstart scenarios.

**Target Platform**: Existing .NET 10 ASP.NET Core Blazor Interactive Server application, containerized for Azure Container Apps.

**Project Type**: Multi-project .NET application.

**Performance Goals**: Do not wait for speech synthesis before showing scene text or accepting later game actions. Starting a new narration must stop or cancel the previous narration. Re-entering a scene with the same text and voice settings should reuse cached audio when available.

**Constraints**:

- Use SSML synthesis to control voice pitch and speaking rate; do not use Speech text-streaming mode because it does not support the per-text SSML needed for the requested delivery.
- Keep Azure Speech credentials and synthesis on the server; use Microsoft Entra credentials/managed identity in Azure and local user secrets for development.
- Use the current visible `LastResult.Narrative` paragraph as the scene text. Narrate the initial scene narrative and each subsequent non-empty narrative update; do not narrate UI labels, status messages, or unrelated panels.
- Set the background audio element's effective `volume` property to `0.25` (25% of its current browser-default gain) and narration playback to foreground gain. The configured gain ratio is nominally about 12 dB in favor of narration.
- Speech-service errors, cancellation, or browser playback errors must not block text display or gameplay. Do not log speech keys or full scene text.
- Voice and style availability varies by locale. Select a prebuilt neural voice for the scene language, verify the candidate and any optional style against Azure's supported-voice list, and tune by listening; do not imitate a named person's voice.

**Scale/Scope**: One player-facing narration stream per active game page, for the current visible narrative text. No voice recording, custom voice training, new public API, persistent audio assets, music generation, or user-configurable audio mixer.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Evidence / plan |
|---|---|---|
| I. Deterministic Game State First | PASS | Speech consumes text already produced by the validated gameplay pipeline and cannot change game state. |
| II. Agent Responsibilities Must Be Explicit | PASS | Speech synthesis is a bounded audio service, not a new autonomous agent. |
| III. Structured Output Over Free-Form State Mutation | PASS | The speech service receives plain display text and returns audio bytes; it does not parse or mutate state. |
| IV. Rules Before Narrative | PASS | Narration starts only after the existing deterministic gameplay result has been produced. |
| V. Retro 8-Bit Identity Is a Core Requirement | PASS | The feature changes audio only and retains existing visuals and game identity. |
| VI. The Player Must Always Have Agency | PASS | Audio runs independently of game state, is stopped on new narration or leaving the game, and does not disable later actions. |
| VIII. Testable AI Behavior | PASS | The Azure boundary is replaceable in tests; SSML, failure handling, and playback orchestration have deterministic coverage. |
| IX. Observability Is a First-Class Feature | PASS | Record synthesis duration and failure category without logging secrets or full narration text. |
| X. Cost and Latency Must Be Managed | PASS | Skip empty text, cancel stale work, and reuse exact synthesized output from a bounded cache. |
| XI. Cloud-Native Architecture First | PASS | All settings are external configuration; the existing container remains the only deployable application component. |
| XV. Azure Identity and Secret Management | PASS | Use managed identity/Entra credentials in production and developer user secrets locally; never send credentials to the browser. |
| XVI. Persistence and Cloud Storage | PASS | No persistent state is introduced; speech output is transient and can be regenerated. |
| XVII. Scalability and Stateless Compute | PASS | Playback and cache are per-process transient state; correctness does not depend on a particular replica. |
| XXII. User Experience and Responsiveness | PASS | Text appears independently of synthesis; an audio failure is non-blocking and later game input remains usable. |

**Gate result**: No constitution violations or unresolved policy decisions. The chosen scope does not add a deployable service, persistent schema, or public endpoint.

## Project Structure

### Documentation (this feature)

```text
specs/007-scene-voice-narration/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── tasks.md
```

No `contracts/` directory is needed: the feature adds no externally exposed API. Its service boundary and JavaScript audio calls remain internal to the existing application.

### Source Code (repository root)

```text
src/
├── AI.SpectrumAdventure.Application/
│   └── Abstractions/
│       └── [scene narration service contract]
├── AI.SpectrumAdventure.Infrastructure/
│   └── Speech/
│       └── [Azure Speech synthesis implementation and SSML builder]
└── AI.SpectrumAdventure.Web/
    ├── Program.cs
    ├── Components/
    │   └── AdventurePage.razor
    └── wwwroot/
        └── js/
            └── adventure-audio.js

tests/
└── AI.SpectrumAdventure.IntegrationTests/
    └── [scene narration service and orchestration tests]
```

**Structure Decision**: Extend the existing modular .NET application. Keep Azure SDK details behind an application abstraction, register the service in the Web composition root, and reuse the current gameplay page and JavaScript audio module. Tests use the existing integration test project, which already references the Web and Infrastructure projects.

## Complexity Tracking

No constitution violations, new deployable projects, public endpoints, or persistent stores are required.
