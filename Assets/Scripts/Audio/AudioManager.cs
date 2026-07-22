using System;
using System.Threading.Tasks;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Sav zvuk je proceduralno sintetiziran u kodu — projekt nema audio asseta,
    // a runtime sinteza ne traži ni datoteke ni izmjene scene (isti samopokretajući
    // Bootstrap obrazac kao MainMenuUI). Pozadina: svemirski ambient pad (Dm/Bb
    // izmjena s detune sinusima + "vjetar" od filtriranog šuma), generiran u
    // pozadinskoj niti pa zalijepljen u seamless loop crossfadeom repa u početak.
    // SFX: kratki sintetizirani zvukovi spojeni na postojeće GameEventBus evente.
    public class AudioManager : MonoBehaviour
    {
        private const int SampleRate = 44100;
        private const float MusicVolume = 0.32f;
        private const float MusicFadeInSeconds = 2.5f;
        private const float MusicLoopSeconds = 26f;
        private const float MusicCrossfadeSeconds = 3f;
        private const float MiningTickInterval = 0.18f;

        public static AudioManager Instance { get; private set; }

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _miningSource; // odvojen zbog pitch varijacije po udarcu

        private AudioClip _miningHit, _pickup, _resourcePickup, _craft, _tierUnlocked,
            _connectionCreated, _connectionDestroyed, _alert, _damaged, _died, _teleport,
            _machinePlaced, _planetDiscovered, _uiClick;

        private Task<float[]> _musicTask;
        private float _nextMiningTick;
        private float _lastCraftTime = -10f;
        private float _nextResourcePickupTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.volume = 0f; // fade-in u Updateu
            _musicSource.spatialBlend = 0f;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.spatialBlend = 0f;

            _miningSource = gameObject.AddComponent<AudioSource>();
            _miningSource.spatialBlend = 0f;

            BuildSfxClips();

            // Glazba je par sekundi računanja — u pozadinskoj niti da ne koči start;
            // Update je pokupi kad je gotova.
            _musicTask = Task.Run(BuildMusicSamples);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEventBus.OnMiningProgress += HandleMiningProgress;
            GameEventBus.OnQuickSlotsChanged += HandleQuickSlotsChanged;
            GameEventBus.OnConnectionCreated += HandleConnectionCreated;
            GameEventBus.OnConnectionDestroyed += HandleConnectionDestroyed;
            GameEventBus.OnMachinePlaced += HandleMachinePlaced;
            GameEventBus.OnPlanetDiscovered += HandlePlanetDiscovered;
            GameEventBus.OnRecipeTierUnlocked += HandleTierUnlocked;
            GameEventBus.OnPlayerTeleported += HandleTeleported;
            GameEventBus.OnPlayerDamaged += HandleDamaged;
            GameEventBus.OnPlayerDied += HandleDied;
        }

        void OnDisable()
        {
            GameEventBus.OnMiningProgress -= HandleMiningProgress;
            GameEventBus.OnQuickSlotsChanged -= HandleQuickSlotsChanged;
            GameEventBus.OnConnectionCreated -= HandleConnectionCreated;
            GameEventBus.OnConnectionDestroyed -= HandleConnectionDestroyed;
            GameEventBus.OnMachinePlaced -= HandleMachinePlaced;
            GameEventBus.OnPlanetDiscovered -= HandlePlanetDiscovered;
            GameEventBus.OnRecipeTierUnlocked -= HandleTierUnlocked;
            GameEventBus.OnPlayerTeleported -= HandleTeleported;
            GameEventBus.OnPlayerDamaged -= HandleDamaged;
            GameEventBus.OnPlayerDied -= HandleDied;
        }

        void Update()
        {
            if (_musicTask != null && _musicTask.IsCompleted)
            {
                if (_musicTask.Status == TaskStatus.RanToCompletion)
                {
                    int loopSamples = (int)(MusicLoopSeconds * SampleRate);
                    var clip = AudioClip.Create("music_space_ambient", loopSamples, 2, SampleRate, false);
                    clip.SetData(_musicTask.Result, 0);
                    _musicSource.clip = clip;
                    _musicSource.Play();
                }
                else
                {
                    Debug.LogWarning($"[AudioManager] Sinteza glazbe nije uspjela: {_musicTask.Exception?.GetBaseException().Message}");
                }
                _musicTask = null;
            }

            if (_musicSource.isPlaying && _musicSource.volume < MusicVolume)
                _musicSource.volume = Mathf.Min(MusicVolume,
                    _musicSource.volume + MusicVolume * Time.unscaledDeltaTime / MusicFadeInSeconds);
        }

        // ── Javni pozivi za UI (nemaju event na busu) ─────────────────────────

        public static void PlayUiClick() => Instance?.Play(Instance._uiClick, 0.7f);
        public static void PlayAlert() => Instance?.Play(Instance._alert, 0.8f);

        public static void PlayCraft()
        {
            if (Instance == null) return;
            Instance._lastCraftTime = Time.unscaledTime;
            Instance.Play(Instance._craft);
        }

        // Resurs ušao u inventar (završeno kopanje, preuzimanje iz stroja/skladišta).
        // Throttle: preuzimanje N komada odjednom zove Add N puta u istom frameu —
        // bez ovoga bi se pop naslagao N puta.
        public static void PlayResourcePickup()
        {
            if (Instance == null) return;
            if (Time.unscaledTime < Instance._nextResourcePickupTime) return;
            Instance._nextResourcePickupTime = Time.unscaledTime + 0.08f;
            Instance.Play(Instance._resourcePickup, 0.7f);
        }

        // ── Event handleri ────────────────────────────────────────────────────

        private void HandleMiningProgress(MiningProgressEvent e)
        {
            if (!e.IsMining) return;
            if (Time.unscaledTime < _nextMiningTick) return;
            _nextMiningTick = Time.unscaledTime + MiningTickInterval;

            _miningSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            _miningSource.PlayOneShot(_miningHit, 0.8f);
        }

        // Pokriva pickup, craft rezultat i promjenu odabranog slota — tihi klik.
        // Nakon crafta preskačemo: craft zvon je već feedback, klik bi se preklopio.
        private void HandleQuickSlotsChanged()
        {
            if (Time.unscaledTime - _lastCraftTime < 0.15f) return;
            Play(_pickup, 0.5f);
        }

        private void HandleConnectionCreated(ConnectionEvent e)   => Play(_connectionCreated);
        private void HandleConnectionDestroyed(ConnectionEvent e) => Play(_connectionDestroyed);
        private void HandleMachinePlaced(MachineEvent e)          => Play(_machinePlaced);
        private void HandlePlanetDiscovered(Transform planet)     => Play(_planetDiscovered, 0.8f);
        private void HandleTierUnlocked(int tier)                 => Play(_tierUnlocked);
        private void HandleTeleported(PlayerTeleportEvent e)      => Play(_teleport, 0.8f);
        private void HandleDamaged(PlayerDamagedEvent e)          => Play(_damaged, 0.9f);
        private void HandleDied(PlayerDiedEvent e)                => Play(_died);

        private void Play(AudioClip clip, float volume = 1f)
        {
            if (clip != null) _sfxSource.PlayOneShot(clip, volume);
        }

        // ── SFX sinteza ───────────────────────────────────────────────────────

        private void BuildSfxClips()
        {
            // Kopanje: tup udarac — niski sinus + kratki prasak šuma
            var b = NewBuffer(0.12f);
            AddTone(b, 95f, 55f, 0.9f, 3f);
            AddNoise(b, 0.35f, 4f, 0.25f, seed: 1);
            _miningHit = MakeClip("sfx_mining", b);

            // Pickup / promjena hotbar slota: kratki tihi klik
            b = NewBuffer(0.06f);
            AddTone(b, 1046f, 880f, 0.35f, 2f);
            _pickup = MakeClip("sfx_pickup", b);

            // Resurs u inventar: veseli "pop" uvis (drukčiji od hotbar klika)
            b = NewBuffer(0.11f);
            AddTone(b, 520f, 820f, 0.45f, 2.2f);
            AddTone(b, 1040f, 1640f, 0.12f, 3f);
            _resourcePickup = MakeClip("sfx_resource_pickup", b);

            // UI klik (main menu gumbi)
            b = NewBuffer(0.05f);
            AddTone(b, 1318f, 1046f, 0.4f, 2f);
            _uiClick = MakeClip("sfx_ui_click", b);

            // Craft: dva zvona
            b = NewBuffer(0.5f);
            AddTone(b, 659f, 659f, 0.5f, 3f, 0f, 0.4f);
            AddTone(b, 988f, 988f, 0.4f, 3f, 0.12f, 0.35f);
            _craft = MakeClip("sfx_craft", b);

            // Hub prag otključan: uzlazni arpeggio
            b = NewBuffer(1.15f);
            AddTone(b, 523f, 523f, 0.4f, 2.5f, 0.00f, 0.5f);
            AddTone(b, 659f, 659f, 0.4f, 2.5f, 0.15f, 0.5f);
            AddTone(b, 784f, 784f, 0.4f, 2.5f, 0.30f, 0.5f);
            AddTone(b, 1047f, 1047f, 0.45f, 2.5f, 0.45f, 0.7f);
            _tierUnlocked = MakeClip("sfx_tier_unlocked", b);

            // Veza stvorena: mekani uzlazni sweep
            b = NewBuffer(0.6f);
            AddTone(b, 180f, 760f, 0.45f, 1.5f);
            _connectionCreated = MakeClip("sfx_connection_created", b);

            // Veza uništena: silazni sweep + šum
            b = NewBuffer(0.7f);
            AddTone(b, 460f, 60f, 0.5f, 1.5f);
            AddNoise(b, 0.2f, 2f, 0.15f, seed: 2);
            _connectionDestroyed = MakeClip("sfx_connection_destroyed", b);

            // Upozorenje (AlertsUI toast): dva bipa
            b = NewBuffer(0.4f);
            AddTone(b, 880f, 880f, 0.5f, 1.8f, 0.00f, 0.12f);
            AddTone(b, 880f, 880f, 0.5f, 1.8f, 0.20f, 0.12f);
            _alert = MakeClip("sfx_alert", b);

            // Šteta: kratki pad + šum
            b = NewBuffer(0.18f);
            AddTone(b, 220f, 90f, 0.7f, 2f);
            AddNoise(b, 0.3f, 3f, 0.3f, seed: 3);
            _damaged = MakeClip("sfx_damaged", b);

            // Smrt: dugi pad s laganim vibratom
            b = NewBuffer(1.5f);
            AddTone(b, 330f, 42f, 0.7f, 1.2f, 0f, -1f, vibratoHz: 5f, vibratoDepth: 0.02f);
            _died = MakeClip("sfx_died", b);

            // Teleport: sweep uvis sa shimmerom
            b = NewBuffer(0.65f);
            AddTone(b, 240f, 1500f, 0.4f, 1.2f, 0f, -1f, vibratoHz: 9f, vibratoDepth: 0.04f);
            AddTone(b, 480f, 3000f, 0.15f, 1.5f);
            _teleport = MakeClip("sfx_teleport", b);

            // Stroj postavljen: thump + klik
            b = NewBuffer(0.25f);
            AddTone(b, 75f, 55f, 0.8f, 2.5f);
            AddTone(b, 1200f, 900f, 0.2f, 4f, 0.02f, 0.05f);
            _machinePlaced = MakeClip("sfx_machine_placed", b);

            // Planet otkriven: ping s dugim repom
            b = NewBuffer(0.9f);
            AddTone(b, 988f, 988f, 0.35f, 3.5f);
            AddTone(b, 1480f, 1480f, 0.18f, 4f, 0.05f);
            _planetDiscovered = MakeClip("sfx_planet_discovered", b);
        }

        private static float[] NewBuffer(float seconds) => new float[(int)(seconds * SampleRate)];

        // Sinus sweep od startFreq do endFreq s pow decay envelopeom i 4 ms attackom
        // (bez attacka početak tona klikne). duration < 0 = do kraja buffera.
        private static void AddTone(float[] buf, float startFreq, float endFreq, float amp,
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
        private static void AddNoise(float[] buf, float amp, float decayPow, float k, int seed,
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

        private static AudioClip MakeClip(string name, float[] samples)
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

        // Vraća interleaved stereo (L,R,L,R...) duljine točno MusicLoopSeconds.
        // Generira se loop + crossfade viška, pa se rep umiješa u početak da
        // loop točka bude nečujna. Pozadinska nit: samo čista matematika.
        private static float[] BuildMusicSamples()
        {
            int loopSamples = (int)(MusicLoopSeconds * SampleRate);
            int fadeSamples = (int)(MusicCrossfadeSeconds * SampleRate);
            int genSamples = loopSamples + fadeSamples;

            // Dm (D2 A2 D3 F3 A3) i Bb (Bb1 F2 D3 F3 Bb3) — izmjena jednom po loopu.
            float[] chordA = { 73.42f, 110.00f, 146.83f, 174.61f, 220.00f };
            float[] chordB = { 58.27f, 87.31f, 146.83f, 174.61f, 233.08f };
            float[] noteAmps = { 0.20f, 0.16f, 0.13f, 0.11f, 0.10f };
            float chordLfoHz = 1f / MusicLoopSeconds;

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
