"""Original PHsystem PCM cues. Local deterministic synthesis; no samples/downloads."""
from pathlib import Path
import math
import random
import struct
import wave

ROOT = Path(__file__).resolve().parents[1] / "assets" / "sounds"
CUES = {
    "click": (0.045, 1100), "navigation": (0.065, 900), "panel": (0.10, 650),
    "scan-start": (0.14, 520), "success": (0.19, 780), "confirm": (0.10, 1000),
    "error": (0.14, 230), "notification": (0.15, 1250), "reply": (0.17, 1050),
    "warning": (0.13, 310),
}

def main():
    ROOT.mkdir(parents=True, exist_ok=True)
    for index, (name, (duration, frequency)) in enumerate(CUES.items()):
        noise = random.Random(20261115 + index)
        samples = []
        previous = 0.0
        for n in range(int(44100 * duration)):
            t = n / 44100
            envelope = min(1, t / 0.003) * math.exp(-7 * t / duration) * min(1, (duration - t) / 0.008)
            # Damped resonator + high-pass transient: a dry, low-level tap.
            current = noise.uniform(-1, 1)
            texture = (current - previous) * math.exp(-60 * t)
            previous = current
            partials = math.sin(2 * math.pi * frequency * t) + 0.3 * math.sin(2 * math.pi * frequency * 2.71 * t)
            sample = 0.12 * envelope * (0.65 * partials + 0.35 * texture)
            samples.append(struct.pack("<h", round(max(-1, min(1, sample)) * 32767)))
        with wave.open(str(ROOT / (name + ".wav")), "wb") as output:
            output.setnchannels(1)
            output.setsampwidth(2)
            output.setframerate(44100)
            output.writeframes(b"".join(samples))

if __name__ == "__main__":
    main()
