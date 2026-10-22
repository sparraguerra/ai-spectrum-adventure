# Research: Scene Voice Narration

## Decision 1: Synthesize speech on the server with Azure Speech SDK

**Decision**: Add a server-side synthesis boundary behind an application abstraction. The implementation uses Azure Speech SDK for .NET and returns transient audio for the current visible narrative text. The Blazor page hands the audio stream to the existing browser audio module through JS interop.

**Rationale**: Server-side synthesis keeps service credentials out of browser code and follows the existing application composition pattern. The current app already uses server-side Azure integrations and has an `Azure.Identity` credential pattern. The app remains one deployable service.

**Alternatives considered**:
- Call Azure Speech directly from browser JavaScript with a long-lived key: rejected because it would expose a service credential.
- Add a public synthesis API or separate speech microservice: rejected because this is an internal feature of the existing web app and does not justify another deployment boundary.

## Decision 2: Use SSML synthesis for the voice delivery

**Decision**: Use standard text-to-speech synthesis with SSML prosody controls for pitch and speaking rate. The current English narrator profile uses `en-US-RogerNeural`, pitch `-15%`, and rate `-25%` as an older, measured male approximation. The Azure standard `en-US` voices do not expose older-adult role support, so do not add an unsupported age role to SSML; tune the selected prebuilt voice by listening instead. Only apply a named speaking style after confirming that the selected voice supports it.

**Rationale**: Azure SSML supports voice selection, pitch, speaking rate, volume, and other prosody controls. Azure describes these values as suggestions interpreted relative to the selected voice, so the exact voice and values need a listening test. This captures the requested suspenseful mood without cloning a specific person's voice.

**Alternatives considered**:
- Pick a hard-coded style name without checking voice support: rejected because available styles vary by voice and locale.
- Use Speech text streaming: rejected because the documented text-streaming mode does not support SSML, which is needed for the requested pitch and rate.
- Use custom voice training: rejected because it is unnecessary and outside the requested scope.

## Decision 3: Narrate the visible gameplay narrative text

**Decision**: Use the non-empty `LastResult.Narrative` text rendered in the Adventure page's Narrative panel. Request narration for the initial scene result and each later narrative update. Do not include headings, UI controls, status/error text, or other panels.

**Rationale**: `AdventurePage.razor` already renders this text as the player's narrative for the current game result. The domain location description is not currently rendered as scene text, so using hidden description data would not match what the player sees.

**Alternatives considered**:
- Synthesize `Location.BaseDescription` directly: rejected because it is not the text currently shown to the player.
- Narrate every visible UI string or lore item: rejected because the feature is specifically for scene narrative and would create extra, potentially overlapping speech.

## Decision 4: Keep playback in the existing browser audio module

**Decision**: Extend `adventure-audio.js` to play a transient audio stream, stop the previous narration, and release browser resources when playback ends or is cancelled. Keep background music at `0.25` outside narration, duck it to `0.03` while speech plays, and keep narration at the foreground level.

**Rationale**: The gameplay page already controls background music through a hidden HTML audio element and JavaScript interop. Reusing that module keeps audio lifecycle and mixing next to the existing browser-specific behavior. A browser Web Audio RMS measurement showed that `0.25` music gain alone did not meet the 10 dB requirement, so music is temporarily ducked to `0.03` during speech and restored to `0.25` afterward.

**Alternatives considered**:
- Store generated audio as a permanent Blob asset: rejected because it adds persistence and lifecycle work for text that can be synthesized again.
- Add a new audio player framework: rejected because the existing browser audio element is sufficient.

## Decision 5: Make errors non-blocking and bound repeat synthesis

**Decision**: Keep narrative text visible regardless of synthesis or playback outcome. Cancel synthesis for superseded text; report audio failures through the existing page status/logging patterns without logging the text or credentials. Cache successful output in bounded process memory using the text, locale, voice, and prosody settings as the cache identity.

**Rationale**: Speech synthesis is an external network call and must not block gameplay. A bounded cache reduces repeated service calls when a player revisits the same scene while keeping the app stateless and avoiding persistent audio storage.

**Alternatives considered**:
- Retry every failure indefinitely: rejected because it can delay play and increase cost during an outage.
- Persist all generated audio: rejected because this feature has no durable audio-asset requirement.
- Use an unbounded process cache: rejected because it can grow with arbitrary generated narration.

## Decision 6: Use external configuration and Azure identity

**Decision**: Keep Azure Speech resource details outside source and container images. Use Microsoft Entra credentials/managed identity in Azure and local user secrets for development. Do not add Speech-resource provisioning to this feature's infrastructure.

**Rationale**: The constitution prefers managed identity, forbids committed secrets, and externalizes cloud configuration. The feature can use an already-provisioned Speech resource; deployment of new Azure resources is out of scope.

**Alternatives considered**:
- Commit a resource key or include it in browser configuration: rejected because it would expose a secret.
- Add Azure Speech resource deployment in this feature: rejected because no infrastructure provisioning was requested and it is not needed to define application behavior.

## Sources

- [Customize voice and sound with SSML](https://learn.microsoft.com/azure/ai-services/speech-service/speech-synthesis-markup-voice)
- [How to synthesize speech from text in C#](https://learn.microsoft.com/azure/ai-services/speech-service/how-to-speech-synthesis?pivots=programming-language-csharp)
- [Microsoft Entra authentication with the Speech SDK](https://learn.microsoft.com/azure/ai-services/speech-service/how-to-configure-azure-ad-auth)
- [Speech SDK synthesis latency guidance](https://learn.microsoft.com/azure/ai-services/speech-service/how-to-lower-speech-synthesis-latency)
- [Azure Speech supported languages and voices](https://learn.microsoft.com/azure/ai-services/speech-service/language-support?tabs=tts)
