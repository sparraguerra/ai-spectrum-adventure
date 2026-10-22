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
        document: { getElementById: () => null },
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
});

function audioIsReleased(audio) {
    return audio.paused && audio.loaded && audio.src === "";
}
