window.adventureAudio = (() => {
    const getAudio = () => document.getElementById("adventure-background-audio");

    return {
        async start(muted) {
            const audio = getAudio();
            if (!audio) {
                console.error("Background audio element was not found.");
                return false;
            }

            audio.muted = muted;
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
        }
    };
})();
