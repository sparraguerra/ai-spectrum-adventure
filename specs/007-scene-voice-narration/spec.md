# Feature Specification: Scene Voice Narration

**Feature Branch**: `007-scene-voice-narration`

**Created**: 2026-10-22

**Status**: Draft

**Input**: User description: "crea una especificación para que el texto de cada escena del juego se narre usando Azure Speech TTS. Quiero que la narración sea con una voz grave, lenta, misteriosa como el narrador de la canción Thriller de Michael Jackson. La narración se tiene que oír más alto que la música del juego, así que haz que la música tenga un volumen bastante más bajo."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Hear each scene narrated (Priority: P1)

As a player, I want the text of each game scene to be spoken aloud so I can experience the story as narration while playing.

**Why this priority**: Narrating scene text is the central value of this feature.

**Independent Test**: Open a scene with visible text and confirm that the corresponding narration plays and matches that text.

**Acceptance Scenarios**:

1. **Given** a scene with text is shown, **When** the scene is presented, **Then** the complete scene text is narrated in the same language as the displayed text.
2. **Given** a player enters another scene, **When** its text is presented, **Then** that scene's narration plays instead of continuing the previous scene's narration.
3. **Given** a player revisits a scene, **When** its text is presented again, **Then** its narration plays again.

### User Story 2 - Experience a clear, atmospheric voice mix (Priority: P1)

As a player, I want a deep, slow, mysterious narration that is clearly louder than the background music so I can follow the story without the music masking the words.

**Why this priority**: The requested voice and sound balance are essential to making narration usable and establishing the intended atmosphere.

**Independent Test**: Play a scene with narration and background music and check that the narration is intelligible, has the specified mood, and remains clearly in the foreground.

**Acceptance Scenarios**:

1. **Given** narration is playing over background music, **When** the player listens at the default mix, **Then** the narration is clearly audible above the music.
2. **Given** the narration is rendered, **When** its voice is evaluated, **Then** it sounds low-pitched, measured, and mysterious without imitating a specific person's voice.
3. **Given** narration pauses or ends, **When** background music continues, **Then** the music remains at the reduced game-volume level.
4. **Given** the game is using its default audio mix, **When** a scene is played, **Then** background music is at 25% of its previous default level and remains at least 10 dB below narration while narration is playing.

### User Story 3 - Continue when narration cannot play (Priority: P2)

As a player, I want scene text to remain available if narration is temporarily unavailable so I can continue the game.

**Why this priority**: A speech service or playback problem should not prevent access to the game's story.

**Independent Test**: Make narration unavailable for a scene and confirm that the scene text remains visible and gameplay can continue.

**Acceptance Scenarios**:

1. **Given** narration cannot be generated or played, **When** a scene is presented, **Then** its text remains visible and the player can continue without a blocking error.
2. **Given** narration for one scene fails, **When** the player enters a later scene, **Then** the game attempts narration for the later scene independently.

### Edge Cases

- If the player leaves a scene while its narration is still playing, the old narration stops and does not overlap the next scene.
- If a scene has no text, no empty or unrelated narration is played.
- If the text is long, the complete text is narrated without being cut off by the scene transition.
- If speech rendering or playback fails, visible scene text and normal game controls remain usable.
- If background music is already quiet or muted, narration remains audible and the music setting is not raised to compensate.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The game MUST provide spoken narration for the text of every scene that contains text.
- **FR-002**: Narration MUST preserve the wording and language of the scene text.
- **FR-003**: Narration MUST use Azure Speech text-to-speech.
- **FR-004**: The narration voice MUST have a low pitch, slow measured delivery, and mysterious tone, with an older male narrator quality. It MUST evoke a classic suspenseful storyteller without reproducing or claiming to be the voice of a specific person.
- **FR-005**: The default game background-music volume MUST be reduced to 25% of its existing default level.
- **FR-006**: While narration is playing, the background music MUST be at least 10 dB quieter than the narration. The game MUST temporarily duck music as needed and restore the 25% default gain when narration stops, ends, or fails.
- **FR-007**: Narration for a scene MUST stop when the player leaves that scene, and narration from consecutive scenes MUST NOT overlap.
- **FR-008**: Scene text MUST remain available and gameplay MUST remain usable when narration is unavailable or fails.
- **FR-009**: A scene without text MUST NOT trigger narration.

### Key Entities

- **Scene text**: The text presented to the player for a scene, including its language and scene association.
- **Narration**: The spoken rendering of a scene's text, with its voice characteristics and playback state.
- **Game audio mix**: The relative output levels of narration and background music during gameplay.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In functional tests, 100% of scenes containing text produce narration in the same language and with the same wording as the displayed text.
- **SC-002**: During narration, background music measures at least 10 dB below the narration, and its default game volume is 25% of its previous default level.
- **SC-003**: At least 9 out of 10 playtesters can understand the narrated scene without asking for repetition because of background music.
- **SC-004**: At least 8 out of 10 playtesters describe the narration as low-pitched, slow, and mysterious.
- **SC-005**: When narration is unavailable, players can still read scene text and continue gameplay in every tested failure case.

## Assumptions

- Narration is enabled by default for all scenes with text.
- The game's existing scene text remains visible; narration supplements rather than replaces it.
- The existing default music level is the reference for the requested reduction; the new default is one quarter of that level.
- "Like the narrator of Thriller" is interpreted as a mood reference (deep, slow, mysterious), not a request to copy or clone a specific person's voice.
- Brief interruptions, such as leaving a scene, stop its narration rather than allowing audio from an inactive scene to continue.
