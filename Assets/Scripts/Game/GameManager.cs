using UnityEngine;
using UnityEngine.InputSystem;

namespace xyz.germanfica.unity.planet.gravity
{
    public enum GameState { Playing, Paused, GameOver }

    // Game over tok: PlayerDiedEvent -> zamrzni simulaciju i ugasi player input;
    // R oživljava igrača na Hubu (respawn umjesto reload-a scene — proceduralno
    // generirane planete i izgrađena mreža bi se reloadom izgubile).
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlanetCreator planetCreator;

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

        void Update()
        {
            if (State != GameState.GameOver) return;

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                Respawn();
        }

        public void Pause()    => SetState(GameState.Paused);
        public void Resume()   => SetState(GameState.Playing);
        public void GameOver() => SetState(GameState.GameOver);

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

        // Respawn na Hubu: puno zdravlje, inventar ostaje (smrt košta samo povratak).
        public void Respawn()
        {
            ResolveReferences();
            SetState(GameState.Playing);

            if (playerHealth != null) playerHealth.Revive();

            Transform hub = FindHubPlanet();
            if (planetCreator != null && hub != null)
                planetCreator.TeleportToPlanet(hub, playerController != null ? playerController.currentPlanet : null);

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
