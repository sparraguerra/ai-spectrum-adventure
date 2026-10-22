const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const test = require("node:test");
const vm = require("node:vm");

const audioScriptPath = path.resolve(
    __dirname,
    "../../src/AI.SpectrumAdventure.Web/wwwroot/js/adventure-audio.js"
);
const audioScript = fs.readFileSync(audioScriptPath, "utf8");

function createAudioModule(playImplementation = async () => {}) {
    const state = {
        audio: null,
        musicAudio: {
            volume: 1,
            muted: false,
            paused: true,
            currentTime: 0,
            async play() {
                this.paused = false;
            },
            pause() {
                this.paused = true;
            }
        },
        createdUrls: 0,
        revokedUrls: [],
        callbackArguments: null
    };

    class FakeAudio {
        constructor(source) {
            this.src = source;
            this.volume = 1;
            this.listeners = new Map();
            state.audio = this;
        }

        addEventListener(name, listener) {
            this.listeners.set(name, listener);
        }

        async play() {
            return playImplementation();
        }

        pause() {
            this.paused = true;
        }

        removeAttribute(name) {
            if (name === "src") {
                this.src = "";
            }
        }

        load() {
            this.loaded = true;
        }

        emit(name) {
            this.listeners.get(name)?.();
        }
    }

    const context = {
        window: {},
        document: {
            getElementById: (id) => id === "adventure-background-audio" ? state.musicAudio : null
        },
        Audio: FakeAudio,
        Blob,
        URL: {
            createObjectURL: () => {
                state.createdUrls++;
                return "blob:narration";
            },
            revokeObjectURL: (url) => state.revokedUrls.push(url)
        },
        console: {
            warn() {},
            error() {}
        }
    };

    vm.runInNewContext(audioScript, context);
    return { audio: context.window.adventureAudio, state };
}

test("music stays at its reduced baseline and ducks while narration plays", async () => {
    const { audio, state } = createAudioModule();

    assert.equal(await audio.start(false), true);
    assert.equal(state.musicAudio.volume, 0.25);

    const started = await audio.playNarration(
        { arrayBuffer: async () => new ArrayBuffer(4) },
        "audio/mpeg",
        {},
        0
    );

    assert.equal(started, true);
    assert.equal(state.musicAudio.volume, 0.03);
    state.audio.emit("ended");
    assert.equal(state.musicAudio.volume, 0.25);
});

test("unmuting music during narration preserves the ducked gain", async () => {
    const { audio, state } = createAudioModule();
    await audio.start(true);
    await audio.playNarration(
        { arrayBuffer: async () => new ArrayBuffer(4) },
        "audio/mpeg",
        {},
        0
    );

    assert.equal(await audio.setMuted(false), true);
    assert.equal(state.musicAudio.muted, false);
    assert.equal(state.musicAudio.volume, 0.03);

    audio.stopNarration();
    assert.equal(state.musicAudio.volume, 0.25);
});

test("stopNarration cancels a pending audio stream before creating a player", async () => {
    const { audio, state } = createAudioModule();
    let resolveStream;
    const streamData = new Promise((resolve) => {
        resolveStream = resolve;
    });

    const pending = audio.playNarration({ arrayBuffer: () => streamData }, "audio/mpeg", {}, 1);
    audio.stopNarration();
    resolveStream(new ArrayBuffer(4));

    assert.equal(await pending, false);
    assert.equal(state.createdUrls, 0);
    assert.equal(state.audio, null);
});

test("stopNarration prevents a pending player start from reporting success", async () => {
    let resolvePlay;
    const pendingPlay = new Promise((resolve) => {
        resolvePlay = resolve;
    });
    const { audio, state } = createAudioModule(() => pendingPlay);

    const pending = audio.playNarration(
        { arrayBuffer: async () => new ArrayBuffer(4) },
        "audio/mpeg",
        {},
        2
    );
    await new Promise((resolve) => setImmediate(resolve));
    assert.notEqual(state.audio, null);
    audio.stopNarration();
    resolvePlay();

    assert.equal(await pending, false);
});

test("failed browser playback releases its resources", async () => {
    const { audio, state } = createAudioModule(() => Promise.reject(new Error("blocked")));

    const started = await audio.playNarration(
        { arrayBuffer: async () => new ArrayBuffer(4) },
        "audio/mpeg",
        {},
        3
    );

    assert.equal(started, false);
    assert.equal(state.audio.paused, true);
    assert.equal(state.audio.loaded, true);
    assert.deepEqual(state.revokedUrls, ["blob:narration"]);
    assert.equal(state.musicAudio.volume, 0.25);
});

test("media errors report playback failure and release audio resources", async () => {
    const callback = {
        invokeMethodAsync: async (...args) => {
            state.callbackArguments = args;
        }
    };
    const { audio, state } = createAudioModule();

    const started = await audio.playNarration(
        { arrayBuffer: async () => new ArrayBuffer(4) },
        "audio/mpeg",
        callback,
        42
    );
    state.audio.emit("error");
    await new Promise((resolve) => setImmediate(resolve));

    assert.equal(started, true);
    assert.deepEqual(state.callbackArguments, ["ReportNarrationPlaybackFailureAsync", 42]);
    assert.equal(audioIsReleased(state.audio), true);
    assert.deepEqual(state.revokedUrls, ["blob:narration"]);
    assert.equal(state.musicAudio.volume, 0.25);
});

function audioIsReleased(audio) {
    return audio.paused && audio.loaded && audio.src === "";
}
