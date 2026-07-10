using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public enum GameState { Playing, Paused, GameOver }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Playing;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
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

        public void Pause()   => SetState(GameState.Paused);
        public void Resume()  => SetState(GameState.Playing);
        public void GameOver() => SetState(GameState.GameOver);

        void SetState(GameState newState)
        {
            State = newState;
            Time.timeScale = newState == GameState.Paused ? 0f : 1f;
        }

        void HandleMilestone(MilestoneEvent e)
        {
            if (!string.IsNullOrEmpty(e.StoryFragment))
                GameEventBus.RaiseStoryFragment(e.StoryFragment);
        }

        void HandlePlayerDied(PlayerDiedEvent e) => GameOver();
    }
}
