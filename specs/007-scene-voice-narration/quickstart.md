# Quickstart: Scene Voice Narration

## Prerequisites

- .NET 10 SDK.
- The existing local Aspire prerequisites from the project README.
- An Azure Speech resource with a neural voice available for the language of the game's scene text.
- Azure Speech credentials configured securely for the Web application. For local development, use user secrets; in Azure, use the app's managed identity and an authorized Speech role.

## Configure local development

The Web application binds `AzureSpeechTTS` settings. The current default voice configuration is `en-US` / `en-US-RogerNeural`, listed in [Azure Speech language and voice support](https://learn.microsoft.com/azure/ai-services/speech-service/language-support?tabs=tts#voice-styles-and-roles), with pitch `-15%` and speaking rate `-25%`. This is an approximation of an older, measured narrator; Azure's standard `en-US` voices do not support an older-adult role. Tune the settings by listening with the intended scene text. The voice name, locale, pitch, and rate can be overridden in user secrets.

For local development, authenticate with Microsoft Entra (for example, `az login`) and grant the signed-in identity access to synthesize speech. Set the Speech resource endpoint in user secrets if it is not already supplied by local settings:

```powershell
dotnet user-secrets set "AzureSpeechTTS:Endpoint" "<speech-resource-endpoint>" --project src/AI.SpectrumAdventure.Web
dotnet user-secrets set "AzureSpeechTTS:Locale" "en-US" --project src/AI.SpectrumAdventure.Web
dotnet user-secrets set "AzureSpeechTTS:VoiceName" "en-US-RogerNeural" --project src/AI.SpectrumAdventure.Web
dotnet user-secrets set "AzureSpeechTTS:Pitch" "-15%" --project src/AI.SpectrumAdventure.Web
dotnet user-secrets set "AzureSpeechTTS:Rate" "-25%" --project src/AI.SpectrumAdventure.Web
```

In Azure, use the app's managed identity with the corresponding Speech resource permissions. Do not put resource keys in tracked settings or send credentials to the browser.

## Run automated tests

From the repository root:

```powershell
dotnet test tests/AI.SpectrumAdventure.IntegrationTests/AI.SpectrumAdventure.IntegrationTests.csproj
node --test tests\AI.SpectrumAdventure.IntegrationTests\narration-audio.test.js
```

The focused tests must not call the live Azure Speech resource. The .NET tests verify SSML escaping/configuration, cache identity, cancellation, retries, and non-blocking failure behavior through the replaceable speech boundary. The Node test verifies browser playback cancellation, failure reporting, and audio-resource cleanup.

## Run the gameplay validation

Start the existing Aspire AppHost:

```powershell
dotnet run --project src/AI.SpectrumAdventure.AppHost
```

Open the Web endpoint shown by Aspire and use the following scenarios:

1. Start an adventure. Confirm the first visible narrative paragraph is spoken in the matching language with a low-pitched, measured, mysterious delivery.
2. Confirm the background music is clearly subordinate to speech. Inspect the music element's `volume` (`0.25` outside narration, `0.03` while narration plays) and the narration element's `volume` (`1.0`). For the measurable check, capture the combined output with a loopback recording or separate Web Audio analyzers, compare music and narration RMS over representative quiet and loud music passages, and verify that music RMS is at least 10 dB below narration RMS while speech is active.
3. Enter a new scene while speech is playing. Confirm the old narration stops and does not overlap the new narrative.
4. Revisit a scene with the same narrative. Confirm narration plays again and a cache hit avoids another synthesis request when the item is still cached.
5. Mute music and confirm narration remains audible. Confirm music remains at its ducked gain if unmuted during narration and returns to `0.25` after narration ends.
6. Temporarily make Speech credentials unavailable. Confirm the visible narrative and game controls remain usable, the failure is reported without exposing credentials or full text, and later scene narration can be attempted independently.
7. Start a scene with no narrative text. Confirm no speech plays.

## Expected outcome

The narrative remains readable and gameplay responsive; each non-empty narrative update is spoken once at a time, music stays at `0.25` outside speech and is ducked during narration, and a speech-service or browser-audio failure does not stop the game.

## Phase 6 validation record

### Automated checks

- .NET narration tests: 9 passed across `AzureSpeechNarrationServiceTests`, `NarrationSsmlBuilderTests`, and `SceneNarrationFailureTests`.
- Browser audio tests: 6 passed with `node --test tests\AI.SpectrumAdventure.IntegrationTests\narration-audio.test.js`.
- JavaScript syntax check: `node --check src\AI.SpectrumAdventure.Web\wwwroot\js\adventure-audio.js` passed.

### Manual checks observed in the local browser

- A visible Forest Entrance narrative triggered a browser audio `play` event; narration playback used volume `1.0`.
- Background music used `0.25` outside narration and ducked to `0.03` during speech. Muting during narration preserved the ducked gain and did not stop narration; the baseline was restored after narration ended.
- During a transition from Forest Entrance to Dark Forest, the previous narration was paused and its source released; the new narration started at volume `1.0` while music was ducked.
- Separate Web Audio analyzers measured a 21.4 dB narration-over-music margin in a two-second overlapping interval. Five additional music-loop positions measured margins from 17.4 to 22.3 dB while narration was active and music volume was `0.03`.
- A one-shot browser playback rejection displayed the non-blocking failure status while leaving the narrative and command field usable. After restoring normal playback, a later scene narrated successfully.

### Deterministic equivalents for non-interactive scenarios

- Repeated exact text reuses cached output: the service test verifies two identical synthesis requests produce one speech-client call.
- Speech-service failure and later recovery are covered by fake-client tests; browser playback failure and later narration were also exercised in the running game without changing or exposing credentials.
- The current playable adventure does not expose an empty-narrative scene. The page's whitespace guard was verified to return before calling the synthesis service.
- The default voice profile is now `en-US-RogerNeural`, pitch `-15%`, and rate `-25%`, targeting an older, measured male narrator. Azure's standard `en-US` voices do not list older-adult role support; the updated profile is covered by SSML tests but still needs subjective listening validation.

These checks use deterministic or browser-level equivalents where the current adventure does not expose a corresponding live scenario. The measured local mix exceeded the specified 10 dB threshold in all sampled speech-active intervals.
