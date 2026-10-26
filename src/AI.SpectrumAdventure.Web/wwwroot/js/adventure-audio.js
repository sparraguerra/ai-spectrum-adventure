window.adventureAudio = (() => {
    const musicVolume = 0.12;
    const narrationMusicVolume = 0.03;
    const getAudio = () => document.getElementById("adventure-background-audio");
    let narrationAudio = null;
    let narrationObjectUrl = null;
    let narrationGeneration = 0;

    const updateMusicVolume = () => {
        const audio = getAudio();
        if (audio) {
            audio.volume = narrationAudio ? narrationMusicVolume : musicVolume;
        }
    };

    const releaseNarration = (audio, objectUrl) => {
        if (narrationAudio === audio) {
            narrationAudio = null;
            narrationObjectUrl = null;
            updateMusicVolume();
        }

        audio.pause();
        audio.removeAttribute("src");
        audio.load();
        URL.revokeObjectURL(objectUrl);
    };

    const stopNarration = () => {
        narrationGeneration++;
        const audio = narrationAudio;
        const objectUrl = narrationObjectUrl;
        narrationAudio = null;
        narrationObjectUrl = null;
        if (audio) {
            audio.pause();
            audio.removeAttribute("src");
            audio.load();
        }
        if (objectUrl) {
            URL.revokeObjectURL(objectUrl);
        }
        updateMusicVolume();
    };

    return {
        async start(muted) {
            const audio = getAudio();
            if (!audio) {
                console.error("Background audio element was not found.");
                return false;
            }

            audio.muted = muted;
            updateMusicVolume();
            try {
                await audio.play();
                return true;
            } catch (error) {
                console.warn("Background music could not be started.", error);
                return false;
            }
        },

        async setMuted(muted) {
            const audio = getAudio();
            if (!audio) {
                console.error("Background audio element was not found.");
                return false;
            }

            audio.muted = muted;
            updateMusicVolume();
            if (muted) {
                return true;
            }

            try {
                await audio.play();
                return true;
            } catch (error) {
                console.warn("Background music could not be started.", error);
                return false;
            }
        },

        stop() {
            const audio = getAudio();
            if (!audio) {
                console.error("Background audio element was not found.");
                return;
            }

            audio.pause();
            audio.currentTime = 0;
            updateMusicVolume();
        },

        async playNarration(streamReference, contentType, componentReference, playbackGeneration) {
            stopNarration();
            const generation = narrationGeneration;
            const audioBytes = await streamReference.arrayBuffer();
            if (generation !== narrationGeneration) {
                return false;
            }

            const objectUrl = URL.createObjectURL(new Blob([audioBytes], {
                type: contentType || "audio/mpeg"
            }));
            const audio = new Audio(objectUrl);
            narrationAudio = audio;
            narrationObjectUrl = objectUrl;
            audio.volume = 1;
            updateMusicVolume();

            audio.addEventListener("ended", () => releaseNarration(audio, objectUrl), { once: true });
            audio.addEventListener("error", () => {
                if (narrationAudio !== audio) {
                    return;
                }
                console.warn("Scene narration audio could not be decoded or played.");
                releaseNarration(audio, objectUrl);
                componentReference.invokeMethodAsync(
                    "ReportNarrationPlaybackFailureAsync",
                    playbackGeneration
                ).catch(error => console.warn("Scene narration failure could not be reported to the page.", error));
            }, { once: true });

            try {
                await audio.play();
                return generation === narrationGeneration;
            } catch (error) {
                if (generation !== narrationGeneration) {
                    return false;
                }
                console.warn("Scene narration playback could not be started.", error);
                releaseNarration(audio, objectUrl);
                return false;
            }
        },

        stopNarration
    };
})();
