# Quickstart: Scene Voice Narration

## Prerequisites

- .NET 10 SDK.
- The existing local Aspire prerequisites from the project README.
- An Azure Speech resource with a neural voice available for the language of the game's scene text.
- Azure Speech credentials configured securely for the Web application. For local development, use user secrets; in Azure, use the app's managed identity and an authorized Speech role.

## Configure local development

Set the Speech resource's region and key in user secrets for the Web project. Do not add credentials to tracked settings or browser code:

```powershell
dotnet user-secrets set "AzureSpeech:Region" "<speech-resource-region>" --project src/AI.SpectrumAdventure.Web
dotnet user-secrets set "AzureSpeech:Key" "<speech-resource-key>" --project src/AI.SpectrumAdventure.Web
```

## Run automated tests

From the repository root:

```powershell
dotnet test tests/AI.SpectrumAdventure.IntegrationTests/AI.SpectrumAdventure.IntegrationTests.csproj
```

The focused tests must not call the live Azure Speech resource. They verify SSML escaping/configuration, cache identity and bounds, cancellation, and non-blocking failure behavior through the replaceable speech boundary.

## Run the gameplay validation

Start the existing Aspire AppHost:

```powershell
dotnet run --project src/AI.SpectrumAdventure.AppHost
```

Open the Web endpoint shown by Aspire and use the following scenarios:

1. Start an adventure. Confirm the first visible narrative paragraph is spoken in the matching language with a low-pitched, measured, mysterious delivery.
2. Confirm the background music is clearly subordinate to speech. Inspect the audio element gains: music is `0.25`, narration is at foreground gain; confirm the measured mix meets the 10 dB target.
3. Enter a new scene while speech is playing. Confirm the old narration stops and does not overlap the new narrative.
4. Revisit a scene with the same narrative. Confirm narration plays again and a cache hit avoids another synthesis request when the item is still cached.
5. Mute music and confirm narration remains audible. Confirm music remains at its reduced gain when unmuted.
6. Temporarily make Speech credentials unavailable. Confirm the visible narrative and game controls remain usable, the failure is reported without exposing credentials or full text, and later scene narration can be attempted independently.
7. Start a scene with no narrative text. Confirm no speech plays.

## Expected outcome

The narrative remains readable and gameplay responsive; each non-empty narrative update is spoken once at a time, the music stays at the reduced default level, and a speech-service or browser-audio failure does not stop the game.
