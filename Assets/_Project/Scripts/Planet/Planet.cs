using UnityEngine;

namespace WebOfPlanets
{
    [RequireComponent(typeof(Rigidbody))]
    public class Planet : MonoBehaviour
    {
        public PlanetType Type;
        public bool IsHub;
        public float Gravity = 20f;

        // Nestabilni planeti (GDD 4.2): ubrzavaju degradaciju veza, strojevi se češće kvare (tjedan 3).
        public bool IsUnstable => Type == PlanetType.Volcanic || Type == PlanetType.Gaseous;

        [SerializeField] private Material surfaceMaterial;

        void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;

            // Convex MeshCollider (scena ga tako drži na Hubu) je hull od ≤255 poligona:
            // premošćuje udoline i siječe kroz brda visokopoligonskog planeta, pa igrač
            // i sve što se postavlja raycastom lebdi/tone u odnosu na vidljivu površinu.
            // Kinematic rigidbody smije nositi non-convex MeshCollider, pa fizičku
            // površinu izjednačavamo sa stvarnim mesheom. Runtime umjesto scene edita —
            // editor drži scenu u memoriji pa disk izmjene scene ne prežive.
            if (TryGetComponent(out MeshCollider meshCollider) && meshCollider.convex)
                meshCollider.convex = false;

            if (surfaceMaterial != null)
            {
                Renderer renderer = GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    // Kameni planeti (uklj. Hub) dobivaju jupiter fotku iz
                    // Planet_Mining.mat (equirectangular, horizontalno seamless) —
                    // isto što PlanetCreator radi za spawnane Mining planete.
                    // Autorski UV otoci FBX mesha bi teksturu rezali na granicama,
                    // pa se UV-ovi prvo preračunaju sferno.
                    if (Type == PlanetType.Mining)
                    {
                        SphericalUV.Apply(renderer);
                        renderer.material = surfaceMaterial;
                    }
                    else
                    {
                        renderer.material = surfaceMaterial;
                    }
                }
            }
        }

        void Start()
        {
            GameEventBus.RaisePlanetDiscovered(transform);
        }
    }

    // Premješteno iz GasPlanetAtmosphere.cs (konsolidacija malih datoteka, 31. 7. 2026.).
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
