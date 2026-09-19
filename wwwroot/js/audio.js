// Tiny sound engine for Wiggly Snake using the Web Audio API.
// No audio files needed — every sound is synthesized on the fly.
window.snakeAudio = (function () {
    let ctx = null;
    let muted = false;

    function ensureCtx() {
        if (!ctx) {
            const AC = window.AudioContext || window.webkitAudioContext;
            if (AC) ctx = new AC();
        }
        // Browsers start the context suspended until a user gesture.
        if (ctx && ctx.state === "suspended") ctx.resume();
        return ctx;
    }

    // Play a single tone.
    function tone(freq, start, duration, type, peak) {
        const c = ensureCtx();
        if (!c) return;
        const t0 = c.currentTime + start;
        const osc = c.createOscillator();
        const gain = c.createGain();
        osc.type = type || "sine";
        osc.frequency.setValueAtTime(freq, t0);
        gain.gain.setValueAtTime(0.0001, t0);
        gain.gain.exponentialRampToValueAtTime(peak || 0.2, t0 + 0.015);
        gain.gain.exponentialRampToValueAtTime(0.0001, t0 + duration);
        osc.connect(gain).connect(c.destination);
        osc.start(t0);
        osc.stop(t0 + duration + 0.02);
    }

    // Slide a tone from one frequency to another.
    function slide(f1, f2, start, duration, type, peak) {
        const c = ensureCtx();
        if (!c) return;
        const t0 = c.currentTime + start;
        const osc = c.createOscillator();
        const gain = c.createGain();
        osc.type = type || "sine";
        osc.frequency.setValueAtTime(f1, t0);
        osc.frequency.exponentialRampToValueAtTime(Math.max(f2, 1), t0 + duration);
        gain.gain.setValueAtTime(0.0001, t0);
        gain.gain.exponentialRampToValueAtTime(peak || 0.2, t0 + 0.02);
        gain.gain.exponentialRampToValueAtTime(0.0001, t0 + duration);
        osc.connect(gain).connect(c.destination);
        osc.start(t0);
        osc.stop(t0 + duration + 0.02);
    }

    return {
        unlock: function () { ensureCtx(); },
        setMuted: function (m) { muted = m; },
        // Cheerful two-note "munch" when the snake eats fruit.
        eat: function () {
            if (muted) return;
            tone(660, 0, 0.09, "square", 0.18);
            tone(990, 0.08, 0.12, "square", 0.16);
        },
        // Sad descending slide when the snake crashes.
        die: function () {
            if (muted) return;
            slide(440, 90, 0, 0.6, "sawtooth", 0.22);
            tone(160, 0.18, 0.4, "triangle", 0.14);
        },
        // Sparkle when a new game starts.
        start: function () {
            if (muted) return;
            tone(523, 0, 0.1, "triangle", 0.16);
            tone(659, 0.1, 0.1, "triangle", 0.16);
            tone(784, 0.2, 0.16, "triangle", 0.16);
        }
    };
})();
