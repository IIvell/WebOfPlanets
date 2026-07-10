using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Sekunde nakon primljene štete tijekom kojih igrač ne prima novu štetu.")]
        [SerializeField] private float damageInvulnerability = 0.5f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        private float _invulnerableUntil;

        void Awake()
        {
            CurrentHealth = maxHealth;
        }

        void Start()
        {
            GameEventBus.Raise(new PlayerHealthChangedEvent { Current = CurrentHealth, Max = maxHealth });
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            if (Time.time < _invulnerableUntil) return;

            _invulnerableUntil = Time.time + damageInvulnerability;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

            GameEventBus.Raise(new PlayerDamagedEvent { Amount = amount, Current = CurrentHealth, Position = transform.position });
            GameEventBus.Raise(new PlayerHealthChangedEvent { Current = CurrentHealth, Max = maxHealth });

            if (CurrentHealth <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            GameEventBus.Raise(new PlayerHealthChangedEvent { Current = CurrentHealth, Max = maxHealth });
        }

        public void Revive()
        {
            IsDead = false;
            CurrentHealth = maxHealth;
            GameEventBus.Raise(new PlayerHealthChangedEvent { Current = CurrentHealth, Max = maxHealth });
        }

        private void Die()
        {
            IsDead = true;
            GameEventBus.Raise(new PlayerDiedEvent { Position = transform.position });
        }
    }
}
