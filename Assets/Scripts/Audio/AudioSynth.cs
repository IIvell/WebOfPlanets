using System;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Čista DSP sinteza (bez Unity scene ovisnosti osim AudioClip/Mathf):
    // toneovi, šum, pad slojevi i ambient glazba. Izdvojeno iz AudioManagera —
    // on ostaje orkestrator (izvori, eventi, throttle), a sve funkcije ovdje su
    // statičke i čiste pa se glazba smije računati u pozadinskoj niti.
    internal static class AudioSynth
    {
        public const int SampleRate = 44100;

        public static float[] NewBuffer(float seconds) => new float[(int)(seconds * SampleRate)];

        // Sinus sweep od startFreq do endFreq s pow decay envelopeom i 4 ms attackom
        // (bez attacka početak tona klikne). duration < 0 = do kraja buffera.
        public static void AddTone(float[] buf, float startFreq, float endFreq, float amp,
            float decayPow, float startTime = 0f, float duration = -1f,
            float vibratoHz = 0f, float vibratoDepth = 0f)
        {
            int start = (int)(startTime * SampleRate);
            int len = duration < 0f
                ? buf.Length - start
                : Mathf.Min((int)(duration * SampleRate), buf.Length - start);
            if (len <= 0) return;

            float attackSamples = 0.004f * SampleRate;
            double phase = 0.0;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                if (vibratoHz > 0f)
                    freq *= 1f + vibratoDepth * Mathf.Sin(2f * Mathf.PI * vibratoHz * i / SampleRate);
                phase += 2.0 * Math.PI * freq / SampleRate;

                float env = Mathf.Pow(1f - t, decayPow) * Mathf.Min(1f, i / attackSamples);
                buf[start + i] += amp * env * (float)Math.Sin(phase);
            }
        }

        // Bijeli šum kroz one-pole lowpass (k = koliko "otvoren", 0–1) s pow decayem.
        public static void AddNoise(float[] buf, float amp, float decayPow, float k, int seed,
            float startTime = 0f, float duration = -1f)
        {
            int start = (int)(startTime * SampleRate);
            int len = duration < 0f
                ? buf.Length - start
                : Mathf.Min((int)(duration * SampleRate), buf.Length - start);
            if (len <= 0) return;

            var rng = new System.Random(seed);
            float attackSamples = 0.004f * SampleRate;
            float y = 0f;
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / len;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                y += k * (white - y);

                float env = Mathf.Pow(1f - t, decayPow) * Mathf.Min(1f, i / attackSamples);
                buf[start + i] += amp * env * y;
            }
        }

        public static AudioClip MakeClip(string name, float[] samples)
        {
            float peak = 0f;
            foreach (var s in samples) peak = Mathf.Max(peak, Mathf.Abs(s));
            if (peak > 1f)
                for (int i = 0; i < samples.Length; i++) samples[i] /= peak;

            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        // ── Glazba ────────────────────────────────────────────────────────────

        // Vraća interleaved stereo (L,R,L,R...) duljine točno loopSeconds.
        // Generira se loop + crossfade viška, pa se rep umiješa u početak da
        // loop točka bude nečujna. Pozadinska nit: samo čista matematika.
        public static float[] BuildMusicSamples(float loopSeconds, float crossfadeSeconds)
        {
            int loopSamples = (int)(loopSeconds * SampleRate);
            int fadeSamples = (int)(crossfadeSeconds * SampleRate);
            int genSamples = loopSamples + fadeSamples;

            // Dm (D2 A2 D3 F3 A3) i Bb (Bb1 F2 D3 F3 Bb3) — izmjena jednom po loopu.
            float[] chordA = { 73.42f, 110.00f, 146.83f, 174.61f, 220.00f };
            float[] chordB = { 58.27f, 87.31f, 146.83f, 174.61f, 233.08f };
            float[] noteAmps = { 0.20f, 0.16f, 0.13f, 0.11f, 0.10f };
            float chordLfoHz = 1f / loopSeconds;

            var left = new float[genSamples];
            var right = new float[genSamples];
            var rng = new System.Random(1234);

            for (int channel = 0; channel < 2; channel++)
            {
                float[] buf = channel == 0 ? left : right;

                for (int n = 0; n < chordA.Length; n++)
                {
                    AddPad(buf, chordA[n], noteAmps[n], NextRange(rng, 0.03f, 0.09f),
                        NextRange(rng, 0f, 6.28f), NextRange(rng, 0.001f, 0.002f), +1, chordLfoHz);
                    AddPad(buf, chordB[n], noteAmps[n], NextRange(rng, 0.03f, 0.09f),
                        NextRange(rng, 0f, 6.28f), NextRange(rng, 0.001f, 0.002f), -1, chordLfoHz);
                }

                // Visoki shimmer, vrlo tih, neovisan o akordu
                AddPad(buf, 587.33f, 0.030f, 0.05f, NextRange(rng, 0f, 6.28f), 0.0015f, 0, chordLfoHz);
                AddPad(buf, 880.00f, 0.025f, 0.04f, NextRange(rng, 0f, 6.28f), 0.0015f, 0, chordLfoHz);

                AddWind(buf, rng.Next());
            }

            CrossfadeLoop(left, loopSamples, fadeSamples);
            CrossfadeLoop(right, loopSamples, fadeSamples);

            // Zajednička normalizacija na 0.8 da omjer kanala ostane
            float peak = 0f;
            for (int i = 0; i < loopSamples; i++)
                peak = Mathf.Max(peak, Mathf.Max(Mathf.Abs(left[i]), Mathf.Abs(right[i])));
            float gain = peak > 0f ? 0.8f / peak : 1f;

            var interleaved = new float[loopSamples * 2];
            for (int i = 0; i < loopSamples; i++)
            {
                interleaved[2 * i] = left[i] * gain;
                interleaved[2 * i + 1] = right[i] * gain;
            }
            return interleaved;
        }

        // Par blago razdešenih sinusa sa sporim amp LFO-om; chordSign bira kojem
        // akordu nota pripada (+1/-1 = protufazne polovice chord LFO-a, 0 = uvijek).
        private static void AddPad(float[] buf, float freq, float amp, float lfoHz,
            float lfoPhase, float detune, int chordSign, float chordLfoHz)
        {
            double p1 = 0.0, p2 = 0.0;
            double step1 = 2.0 * Math.PI * freq * (1f + detune) / SampleRate;
            double step2 = 2.0 * Math.PI * freq * (1f - detune) / SampleRate;

            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float lfo = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * lfoHz * t + lfoPhase);
                float chord = chordSign == 0
                    ? 1f
                    : 0.5f + 0.5f * chordSign * Mathf.Sin(2f * Mathf.PI * chordLfoHz * t);

                p1 += step1;
                p2 += step2;
                buf[i] += amp * lfo * chord * 0.5f * ((float)Math.Sin(p1) + (float)Math.Sin(p2));
            }
        }

        // "Svemirski vjetar": bijeli šum kroz one-pole lowpass čiji se cutoff i
        // glasnoća sporo njišu.
        private static void AddWind(float[] buf, int seed)
        {
            var rng = new System.Random(seed);
            float y = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float t = (float)i / SampleRate;
                float k = 0.02f + 0.015f * Mathf.Sin(2f * Mathf.PI * 0.05f * t);
                float swell = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.03f * t + 1.7f);

                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                y += k * (white - y);
                buf[i] += 0.12f * swell * y;
            }
        }

        // Umiješa rep (iza loop točke) u početak: buf[0..fade] postaje blend
        // početka i onoga što slijedi iza kraja loopa, pa je prijelaz kraj→početak
        // kontinuiran.
        private static void CrossfadeLoop(float[] buf, int loopSamples, int fadeSamples)
        {
            for (int i = 0; i < fadeSamples; i++)
            {
                float mix = (float)i / fadeSamples;
                buf[i] = buf[i] * mix + buf[loopSamples + i] * (1f - mix);
            }
        }

        private static float NextRange(System.Random rng, float min, float max)
            => min + (float)rng.NextDouble() * (max - min);
    }
}
