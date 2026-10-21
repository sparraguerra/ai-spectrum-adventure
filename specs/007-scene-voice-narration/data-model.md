# Data Model: Scene Voice Narration

This feature adds no persistent entities or database schema. All speech data is transient and tied to the currently displayed game result.

## Narration Input

Represents the text requested for playback.

| Field | Description | Validation |
|---|---|---|
| `Text` | Non-empty `LastResult.Narrative` content displayed to the player. | Whitespace-only content is skipped. Text is XML-escaped when placed in SSML. |
| `Locale` | Locale matching the language of the scene text. | Must match a configured, supported voice. |
| `Voice` | Prebuilt neural voice selected for the locale and desired delivery. | Must be present in the Azure Speech supported-voice list for the configured resource. |
| `Prosody` | The configured pitch and speaking-rate adjustments. | Values stay within Azure Speech's supported range and are tuned by listening tests. |

## Synthesized Narration

Represents generated audio before it is played in the browser.

| Field | Description | Lifecycle |
|---|---|---|
| `AudioBytes` | Encoded audio returned by Azure Speech. | Held only long enough to transfer to the browser or serve a cache hit. |
| `AudioFormat` | Browser-playable format chosen for narration. | Shared by cached entries and playback code. |
| `CacheIdentity` | Identity derived from exact text, locale, voice, and prosody settings. | Used only for bounded in-memory reuse; never persisted. |

## Narration Playback

Represents client-side playback associated with the latest narrative update.

| State | Meaning | Transition |
|---|---|---|
| `Idle` | No current narration is being synthesized or played. | A non-empty narrative update starts synthesis. |
| `Synthesizing` | Audio is being generated or loaded from cache. | Success starts playback; failure returns to `Idle` while text remains available. |
| `Playing` | Narration audio is playing in the browser. | Completion, cancellation, game reset, or a newer narrative returns to `Idle`. |

Only one narration is active per game page. Starting a later narrative cancels pending synthesis and stops current playback before playing the new audio.

## Game Audio Mix

| Source | Default gain | Behavior |
|---|---:|---|
| Background music | `0.25` of the browser audio element's default gain | Remains reduced while music is playing, including narration pauses. |
| Narration | Foreground gain (`1.0`) | Plays above music; the configured gain difference is nominally about 12 dB. |

The mix is runtime audio state only. This feature does not persist audio preferences.
