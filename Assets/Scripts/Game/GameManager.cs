using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    public enum GameState { Playing, Paused, GameOver, Victory }

    // Game over tok: PlayerDiedEvent -> zamrzni simulaciju i ugasi player input;
    // R oživljava igrača na aktivnom respawn totemu (default: glavni totem na Hubu).
    // Respawn umjesto reload-a scene — proceduralno generirane planete i izgrađena
    // mreža bi se reloadom izgubile.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlanetCreator planetCreator;
        [Tooltip("Data asset za glavni respawn totem na Hubu (vizual/prefab + ime). Prazno = fallback kocka.")]
        [SerializeField] private RespawnTotemMachineData hubTotemData;

        [Header("Testing")]
        [Tooltip("Testing mod: sve što inače košta resurse je besplatno — crafting (sastojci + pragovi), veze, teleport, otključavanje hub pragova, održavanje strojeva. Dok je uključen, na ekranu stoji watermark.")]
        [SerializeField] private bool testingMode;

        public GameState State { get; private set; } = GameState.Playing;

        // Gate za gameplay input skripte (Interactor, MachinePlacer...);
        // bez GameManagera u sceni ponašanje ostaje kao prije.
        public static bool IsPlaying => Instance == null || Instance.State == GameState.Playing;

        // Centralni prekidač za besplatne troškove (zamjena za stari CraftingUI.freeCrafting,
        // koji je pokrivao samo crafting). Čitaju ga sva mjesta koja troše resurse.
        public static bool TestingMode => Instance != null && Instance.testingMode;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            GameEventBus.OnMilestoneReached += HandleMilestone;
            GameEventBus.OnPlayerDied += HandlePlayerDied;
        }

        void OnDisable()
        {
            GameEventBus.OnMilestoneReached -= HandleMilestone;
            GameEventBus.OnPlayerDied -= HandlePlayerDied;
        }

        IEnumerator Start()
        {
            yield return null; // pričekaj PlanetCreator.Start (isti obrazac kao ResourceSpawnManager)
            SpawnHubTotem();
        }

        // Glavni respawn totem na Hubu postoji od starta, smješten uz računalo i
        // skladište (HubBase); oko spawn pointa se gradi dekoracija baze. Craftani
        // totemi idu kroz MachinePlacer.
        void SpawnHubTotem()
        {
            if (RespawnTotem.HubTotem != null) return;

            Transform hub = FindHubPlanet();
            if (hub == null) return;

            // Fallback kad scene referenca nije postavljena (asset je u Resources folderu).
            if (hubTotemData == null)
                hubTotemData = Resources.Load<RespawnTotemMachineData>("RespawnTotem");

            bool hasBase = HubBase.TryGetArea(hub, out Vector3 baseCenter, out float baseRadius);

            Vector3 pos;
            if (hasBase)
            {
                pos = HubBase.FindTotemSpot(hub, baseCenter, baseRadius);
            }
            else
            {
                // Bez računala/skladišta u sceni: bilo koje čisto mjesto na površini.
                pos = hub.position + hub.up * SurfacePlacement.GetPlanetRadius(hub);
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    Vector3 dir = attempt == 0 ? hub.up : Random.onUnitSphere;
                    SurfacePlacement.GetSurfacePoint(hub, dir, out pos, out _);
                    if (MachinePlacer.IsSpotClear(pos, hub)) break;
                }
            }

            Quaternion rot = Quaternion.FromToRotation(Vector3.up, (pos - hub.position).normalized);
            RespawnTotem.Spawn(hubTotemData, hub, pos, rot, isHubTotem: true);

            if (hasBase)
                HubBase.BuildDecor(hub, baseCenter, baseRadius, pos);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Testing: K ubija igrača na mjestu — za brzo testiranje smrti/respawna.
            if (State == GameState.Playing && TestingMode && keyboard.kKey.wasPressedThisFrame)
            {
                ResolveReferences();
                if (playerHealth != null) playerHealth.Kill();
                return;
            }

            if (State != GameState.GameOver) return;

            if (keyboard.rKey.wasPressedThisFrame)
                Respawn();
        }

        public void Pause()    => SetState(GameState.Paused);
        public void Resume()   => SetState(GameState.Playing);
        public void GameOver() => SetState(GameState.GameOver);
        // Pobjeda (svi hub pragovi otključani) — ekran i input rješava VictoryUI.
        public void Win()      => SetState(GameState.Victory);

        void SetState(GameState newState)
        {
            State = newState;
            Time.timeScale = newState == GameState.Playing ? 1f : 0f;
        }

        void HandlePlayerDied(PlayerDiedEvent e)
        {
            SetState(GameState.GameOver);
            ResolveReferences();
            if (playerController != null) playerController.SetInputEnabled(false);
        }

        // Respawn na aktivnom totemu (default: glavni na Hubu): puno zdravlje,
        // inventar ostaje (smrt košta samo povratak).
        public void Respawn()
        {
            ResolveReferences();
            SetState(GameState.Playing);

            if (playerHealth != null) playerHealth.Revive();

            RespawnTotem totem = RespawnTotem.Active;
            Transform target;
            Transform marker = null;
            if (totem != null && totem.Planet != null)
            {
                target = totem.Planet;
                marker = totem.transform;
            }
            else
            {
                target = FindHubPlanet();
            }

            if (planetCreator != null && target != null)
                planetCreator.TeleportToPlanet(target, playerController != null ? playerController.currentPlanet : null, marker);

            if (playerController != null) playerController.SetInputEnabled(true);
        }

        // Scene reference se mogu ostaviti prazne u Inspectoru — nađi ih pri prvoj upotrebi.
        void ResolveReferences()
        {
            if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
            if (playerHealth == null)     playerHealth     = FindFirstObjectByType<PlayerHealth>();
            if (planetCreator == null)    planetCreator    = FindFirstObjectByType<PlanetCreator>();
        }

        static Transform FindHubPlanet()
        {
            foreach (var p in FindObjectsByType<Planet>(FindObjectsSortMode.None))
                if (p.IsHub) return p.transform;
            return null;
        }

        void HandleMilestone(MilestoneEvent e)
        {
            if (!string.IsNullOrEmpty(e.StoryFragment))
                GameEventBus.RaiseStoryFragment(e.StoryFragment);
        }
    }
}
