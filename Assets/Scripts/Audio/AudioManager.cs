using System.Threading.Tasks;
using UnityEngine;

namespace WebOfPlanets
{
    // Sav zvuk je proceduralno sintetiziran u kodu — projekt nema audio asseta,
    // a runtime sinteza ne traži ni datoteke ni izmjene scene (isti samopokretajući
    // Bootstrap obrazac kao MainMenuUI). Sama DSP sinteza živi u AudioSynth
    // (čiste statičke funkcije); ovdje je orkestracija — izvori, bus eventi,
    // throttle pravila i recepture pojedinih zvukova.
    public class AudioManager : MonoBehaviour
    {
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
            _musicTask = Task.Run(() => AudioSynth.BuildMusicSamples(MusicLoopSeconds, MusicCrossfadeSeconds));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEventBus.OnMiningProgress += HandleMiningProgress;
            GameEventBus.OnQuickSlotsChanged += HandleQuickSlotsChanged;
            GameEventBus.OnInventoryItemAdded += HandleInventoryItemAdded;
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
            GameEventBus.OnInventoryItemAdded -= HandleInventoryItemAdded;
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
                    int loopSamples = (int)(MusicLoopSeconds * AudioSynth.SampleRate);
                    var clip = AudioClip.Create("music_space_ambient", loopSamples, 2, AudioSynth.SampleRate, false);
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

        // Resurs ušao u inventar (završeno kopanje, preuzimanje iz stroja/skladišta)
        // — bus event iz InventorySystem.Add (S14: umjesto direktnog poziva odande).
        // Throttle: preuzimanje N komada odjednom zove Add N puta u istom frameu —
        // bez ovoga bi se pop naslagao N puta.
        private void HandleInventoryItemAdded()
        {
            if (Time.unscaledTime < _nextResourcePickupTime) return;
            _nextResourcePickupTime = Time.unscaledTime + 0.08f;
            Play(_resourcePickup, 0.7f);
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

        // ── Recepture SFX zvukova (sinteza u AudioSynth) ──────────────────────

        private void BuildSfxClips()
        {
            // Kopanje: tup udarac — niski sinus + kratki prasak šuma
            var b = AudioSynth.NewBuffer(0.12f);
            AudioSynth.AddTone(b, 95f, 55f, 0.9f, 3f);
            AudioSynth.AddNoise(b, 0.35f, 4f, 0.25f, seed: 1);
            _miningHit = AudioSynth.MakeClip("sfx_mining", b);

            // Pickup / promjena hotbar slota: kratki tihi klik
            b = AudioSynth.NewBuffer(0.06f);
            AudioSynth.AddTone(b, 1046f, 880f, 0.35f, 2f);
            _pickup = AudioSynth.MakeClip("sfx_pickup", b);

            // Resurs u inventar: veseli "pop" uvis (drukčiji od hotbar klika)
            b = AudioSynth.NewBuffer(0.11f);
            AudioSynth.AddTone(b, 520f, 820f, 0.45f, 2.2f);
            AudioSynth.AddTone(b, 1040f, 1640f, 0.12f, 3f);
            _resourcePickup = AudioSynth.MakeClip("sfx_resource_pickup", b);

            // UI klik (main menu gumbi)
            b = AudioSynth.NewBuffer(0.05f);
            AudioSynth.AddTone(b, 1318f, 1046f, 0.4f, 2f);
            _uiClick = AudioSynth.MakeClip("sfx_ui_click", b);

            // Craft: dva zvona
            b = AudioSynth.NewBuffer(0.5f);
            AudioSynth.AddTone(b, 659f, 659f, 0.5f, 3f, 0f, 0.4f);
            AudioSynth.AddTone(b, 988f, 988f, 0.4f, 3f, 0.12f, 0.35f);
            _craft = AudioSynth.MakeClip("sfx_craft", b);

            // Hub prag otključan: uzlazni arpeggio
            b = AudioSynth.NewBuffer(1.15f);
            AudioSynth.AddTone(b, 523f, 523f, 0.4f, 2.5f, 0.00f, 0.5f);
            AudioSynth.AddTone(b, 659f, 659f, 0.4f, 2.5f, 0.15f, 0.5f);
            AudioSynth.AddTone(b, 784f, 784f, 0.4f, 2.5f, 0.30f, 0.5f);
            AudioSynth.AddTone(b, 1047f, 1047f, 0.45f, 2.5f, 0.45f, 0.7f);
            _tierUnlocked = AudioSynth.MakeClip("sfx_tier_unlocked", b);

            // Veza stvorena: mekani uzlazni sweep
            b = AudioSynth.NewBuffer(0.6f);
            AudioSynth.AddTone(b, 180f, 760f, 0.45f, 1.5f);
            _connectionCreated = AudioSynth.MakeClip("sfx_connection_created", b);

            // Veza uništena: silazni sweep + šum
            b = AudioSynth.NewBuffer(0.7f);
            AudioSynth.AddTone(b, 460f, 60f, 0.5f, 1.5f);
            AudioSynth.AddNoise(b, 0.2f, 2f, 0.15f, seed: 2);
            _connectionDestroyed = AudioSynth.MakeClip("sfx_connection_destroyed", b);

            // Upozorenje (AlertsUI toast): dva bipa
            b = AudioSynth.NewBuffer(0.4f);
            AudioSynth.AddTone(b, 880f, 880f, 0.5f, 1.8f, 0.00f, 0.12f);
            AudioSynth.AddTone(b, 880f, 880f, 0.5f, 1.8f, 0.20f, 0.12f);
            _alert = AudioSynth.MakeClip("sfx_alert", b);

            // Šteta: kratki pad + šum
            b = AudioSynth.NewBuffer(0.18f);
            AudioSynth.AddTone(b, 220f, 90f, 0.7f, 2f);
            AudioSynth.AddNoise(b, 0.3f, 3f, 0.3f, seed: 3);
            _damaged = AudioSynth.MakeClip("sfx_damaged", b);

            // Smrt: dugi pad s laganim vibratom
            b = AudioSynth.NewBuffer(1.5f);
            AudioSynth.AddTone(b, 330f, 42f, 0.7f, 1.2f, 0f, -1f, vibratoHz: 5f, vibratoDepth: 0.02f);
            _died = AudioSynth.MakeClip("sfx_died", b);

            // Teleport: sweep uvis sa shimmerom
            b = AudioSynth.NewBuffer(0.65f);
            AudioSynth.AddTone(b, 240f, 1500f, 0.4f, 1.2f, 0f, -1f, vibratoHz: 9f, vibratoDepth: 0.04f);
            AudioSynth.AddTone(b, 480f, 3000f, 0.15f, 1.5f);
            _teleport = AudioSynth.MakeClip("sfx_teleport", b);

            // Stroj postavljen: thump + klik
            b = AudioSynth.NewBuffer(0.25f);
            AudioSynth.AddTone(b, 75f, 55f, 0.8f, 2.5f);
            AudioSynth.AddTone(b, 1200f, 900f, 0.2f, 4f, 0.02f, 0.05f);
            _machinePlaced = AudioSynth.MakeClip("sfx_machine_placed", b);

            // Planet otkriven: ping s dugim repom
            b = AudioSynth.NewBuffer(0.9f);
            AudioSynth.AddTone(b, 988f, 988f, 0.35f, 3.5f);
            AudioSynth.AddTone(b, 1480f, 1480f, 0.18f, 4f, 0.05f);
            _planetDiscovered = AudioSynth.MakeClip("sfx_planet_discovered", b);
        }
    }
}
