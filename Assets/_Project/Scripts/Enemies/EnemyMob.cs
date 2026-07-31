using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    // Neprijatelj vezan uz jedan planet: stoji na mjestu dok mu se igrač ne
    // približi unutar detekcijskog radijusa, tada ga prati KONSTANTNOM brzinom
    // (malo manjom od igračeve) po površini planeta. Šteta ide pri dodiru kroz
    // PlayerHealth, čiji invulnerability prozor određuje ritam udaraca.
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyMob : MonoBehaviour
    {
        [Tooltip("Brzina kretanja (m/s). Konstantna — bez ubrzanja i usporavanja; namjerno malo manja od igračeve (3).")]
        [SerializeField] private float moveSpeed = 2.5f;
        [Tooltip("Udaljenost na kojoj mob primijeti igrača i krene u potjeru.")]
        [SerializeField] private float detectionRadius = 12f;
        [Tooltip("Udaljenost na kojoj mob odustane od potjere — veća od detekcije da potjera ne treperi na rubu radijusa.")]
        [SerializeField] private float loseRadius = 18f;
        [Tooltip("Šteta igraču po dodiru; učestalost ograničava PlayerHealth invulnerability.")]
        [SerializeField] private float contactDamage = 10f;
        [Tooltip("Koliko brzo se mob okreće prema smjeru kretanja.")]
        [SerializeField] private float turnSpeed = 8f;

        private Transform _planet;
        private Planet _planetComponent;
        private Rigidbody _rig;
        private PlayerController _player;
        private PlayerHealth _playerHealth;
        private bool _chasing;

        // Spawner prosljeđuje igrača da svaki mob ne radi vlastiti
        // FindFirstObjectByType u Startu (N6); Start ostaje fallback.
        public void Init(Transform planet, PlayerController player = null)
        {
            _planet = planet;
            _planetComponent = planet != null ? planet.GetComponent<Planet>() : null;
            if (player != null)
            {
                _player = player;
                _playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        void Awake()
        {
            _rig = GetComponent<Rigidbody>();
            _rig.useGravity = false;
            _rig.interpolation = RigidbodyInterpolation.Interpolate;
            // Isti obrazac kao Attractor: fizika ne smije rušiti moba, orijentaciju
            // vodimo sami kroz MoveRotation.
            _rig.constraints = RigidbodyConstraints.FreezeRotation;
        }

        void Start()
        {
            if (_player == null)
                _player = FindFirstObjectByType<PlayerController>();
            if (_player != null && _playerHealth == null)
                _playerHealth = _player.GetComponent<PlayerHealth>();
        }

        void FixedUpdate()
        {
            if (_planet == null) return;

            Vector3 up = (transform.position - _planet.position).normalized;

            float gravity = _planetComponent != null ? _planetComponent.Gravity : 20f;
            _rig.AddForce(-up * gravity, ForceMode.Acceleration);

            UpdateChaseState();

            // Kao i igrač: horizontala se postavlja direktno svaki korak (konstantna
            // brzina, bez ubrzanja), vertikala (pad) se ne dira.
            Vector3 vertical = Vector3.Project(_rig.linearVelocity, up);
            Vector3 moveDir = Vector3.zero;
            if (_chasing)
                moveDir = Vector3.ProjectOnPlane(_player.transform.position - transform.position, up).normalized;

            _rig.linearVelocity = moveDir * moveSpeed + vertical;

            Orient(up, moveDir);
        }

        // Potjera kreće unutar detectionRadius, a prekida se tek na loseRadius,
        // smrću igrača ili kad igrač ode s ovog planeta.
        private void UpdateChaseState()
        {
            if (_player == null || (_playerHealth != null && _playerHealth.IsDead) || _player.currentPlanet != _planet)
            {
                _chasing = false;
                return;
            }

            float distance = Vector3.Distance(_player.transform.position, transform.position);
            if (_chasing)
                _chasing = distance <= loseRadius;
            else
                _chasing = distance <= detectionRadius;
        }

        private void Orient(Vector3 up, Vector3 moveDir)
        {
            // Alien model gleda u lokalni +z (za razliku od robota igrača koji
            // koristi -direction) — potvrđeno u igri: s minusom je hodao unatrag.
            Quaternion target = moveDir.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(moveDir, up)
                : Quaternion.FromToRotation(transform.up, up) * transform.rotation;

            _rig.MoveRotation(Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.fixedDeltaTime));
        }

        void OnCollisionStay(Collision collision)
        {
            if (collision.rigidbody == null) return;
            if (!collision.rigidbody.TryGetComponent(out PlayerHealth health)) return;

            health.TakeDamage(contactDamage);
        }
    }

    // Spawna EnemyMob-ove na svakom otkrivenom planetu (osim Huba — sigurna baza).
    // Isti obrazac kao VolcanicHazardSpawner: OnPlanetDiscovered + prolaz kroz već
    // postojeće planete nakon PlanetCreator.Start. Mob se gradi proceduralno iz
    // primitiva pa nije potreban prefab ni referenca u sceni. Konsolidirano iz
    // EnemyMobSpawner.cs (čišćenje malih datoteka, srpanj 2026.) — cijela Enemies
    // domena u jednoj datoteci.
    public class EnemyMobSpawner : MonoBehaviour
    {
        [SerializeField] private int minMobsPerPlanet = 3;
        [SerializeField] private int maxMobsPerPlanet = 5;
        [Tooltip("Skala alien modela — kit modeli su mali (totemi koriste 5, smelter 3).")]
        [SerializeField] private float modelScale = 3f;
        [Tooltip("Boja fallback kapsule kad alien model nije u Resources.")]
        [SerializeField] private Color bodyColor = new Color(0.65f, 0.12f, 0.12f);

        private readonly HashSet<Transform> _processed = new();
        private PlayerController _player;

        // Runtime bootstrap umjesto dodavanja u scenu — editor drži scenu u
        // memoriji pa disk izmjene scene ne prežive (isti razlog kao Planet.Awake).
        // Guard dopušta i ručno postavljen spawner u sceni bez dupliranja.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<EnemyMobSpawner>() != null) return;
            new GameObject("EnemyMobSpawner").AddComponent<EnemyMobSpawner>();
        }

        void OnEnable()  => GameEventBus.OnPlanetDiscovered += OnPlanetDiscovered;
        void OnDisable() => GameEventBus.OnPlanetDiscovered -= OnPlanetDiscovered;

        private IEnumerator Start()
        {
            yield return null; // wait for PlanetCreator.Start() to finish spawning
            foreach (var planet in FindObjectsByType<Planet>(FindObjectsSortMode.None))
                OnPlanetDiscovered(planet.transform);
        }

        private void OnPlanetDiscovered(Transform planetTransform)
        {
            if (_processed.Contains(planetTransform)) return;
            _processed.Add(planetTransform);

            Planet planet = planetTransform.GetComponent<Planet>();
            if (planet == null || planet.IsHub) return;

            int count = Random.Range(minMobsPerPlanet, maxMobsPerPlanet + 1);
            for (int i = 0; i < count; i++)
                SpawnMob(planetTransform);
        }

        private void SpawnMob(Transform planet)
        {
            // Čisto tlo kao kod strojeva/totema: mob spawnan u totemu/resursu bi
            // depenetracijom odletio, a mob na idealnoj točki veze bi smetao
            // markerima. Nakon 8 pokušaja prihvati zadnju točku (mali planeti).
            Vector3 dir = Random.onUnitSphere;
            SurfacePlacement.GetSurfacePoint(planet, dir, out Vector3 hitPoint, out Vector3 hitNormal);
            for (int attempt = 0; attempt < 8 && !MachineFactory.IsSpotClear(hitPoint, planet); attempt++)
            {
                dir = Random.onUnitSphere;
                SurfacePlacement.GetSurfacePoint(planet, dir, out hitPoint, out hitNormal);
            }
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hitNormal);

            // Vizual: alien iz Space kita. Spawner se bootstrapa runtime pa nema
            // Inspector referencu — model se čita iz Resources kopije (isti fallback
            // obrazac kao GameManager za hub totem data). Bez modela: crvena kapsula.
            GameObject model = Resources.Load<GameObject>("EnemyMobAlien");

            GameObject mob;
            if (model != null)
            {
                mob = new GameObject("EnemyMob");
                mob.transform.SetPositionAndRotation(hitPoint, rot);

                GameObject visual = Instantiate(model, mob.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * modelScale;
            }
            else
            {
                mob = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                mob.transform.localScale = Vector3.one * 0.8f;
                mob.transform.SetPositionAndRotation(hitPoint, rot);
                mob.GetComponent<Renderer>().material.color = bodyColor;

                AddEye(mob.transform, new Vector3(0.18f, 0.55f, 0.38f));
                AddEye(mob.transform, new Vector3(-0.18f, 0.55f, 0.38f));
            }
            mob.name = "EnemyMob";

            // Isti put kao strojevi (MachineFactory.SpawnObject): dno stvarne
            // geometrije na površinu, pa jedan BoxCollider po granicama geometrije.
            SurfacePlacement.GroundToSurface(mob, planet, hitPoint, hitNormal);
            SurfacePlacement.FitBoxColliderToGeometry(mob);

            Rigidbody rig = mob.AddComponent<Rigidbody>();
            rig.mass = 1f;

            // Jedan lookup igrača za sve mobove umjesto po-mob Finda u Startu.
            if (_player == null)
                _player = FindFirstObjectByType<PlayerController>();

            mob.AddComponent<EnemyMob>().Init(planet, _player);
        }

        private static void AddEye(Transform body, Vector3 localPos)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Eye";
            Destroy(eye.GetComponent<Collider>());
            eye.transform.SetParent(body, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = Vector3.one * 0.18f;
            eye.GetComponent<Renderer>().material.color = Color.white;
        }
    }
}
