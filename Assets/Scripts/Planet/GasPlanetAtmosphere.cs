using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Otrovna atmosfera plinskih planeta (pandan VolcanicHazardZone, ali za cijeli
    // planet): dok igrač stoji na Gaseous planetu bez gas maske u hotbaru, prima
    // štetu u tickovima od sekunde. Samoinicijalizira se pri pokretanju umjesto
    // scene objekta — editor drži scenu u memoriji pa disk izmjene scene ne prežive.
    public class GasPlanetAtmosphere : MonoBehaviour
    {
        private const float TickInterval = 1f;
        private const float DamagePerSecond = 5f;
        // Nakon dolaska na planet šteta kreće tek nakon grace perioda — igrač koji
        // je samo u prolazu stigne otići bez ozljede.
        private const float GraceSeconds = 3f;

        private PlayerController _player;
        private PlayerHealth _health;
        private Transform _lastPlanetTransform;
        private Planet _lastPlanet;
        private float _nextTickTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<GasPlanetAtmosphere>() != null) return;
            new GameObject("GasPlanetAtmosphere").AddComponent<GasPlanetAtmosphere>();
        }

        void Update()
        {
            if (!GameManager.IsPlaying) return;

            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerController>();
                if (_player == null) return;
                _health = _player.GetComponent<PlayerHealth>();
            }
            if (_health == null || _health.IsDead) return;

            Transform planetTransform = _player.currentPlanet;
            if (planetTransform != _lastPlanetTransform)
            {
                _lastPlanetTransform = planetTransform;
                _lastPlanet = planetTransform != null ? planetTransform.GetComponent<Planet>() : null;
                _nextTickTime = Time.time + GraceSeconds;
            }

            bool toxic = _lastPlanet != null && !_lastPlanet.IsHub && _lastPlanet.Type == PlanetType.Gaseous;
            if (!toxic || GasMaskData.IsWorn())
            {
                // Zaštićen ili izvan atmosfere: tick timer se drži barem interval
                // ispred, da skidanje zaštite ne izazove trenutačni burst štete.
                _nextTickTime = Mathf.Max(_nextTickTime, Time.time + TickInterval);
                return;
            }

            if (Time.time < _nextTickTime) return;
            _nextTickTime = Time.time + TickInterval;

            _health.TakeDamage(DamagePerSecond * TickInterval);
        }
    }
}
