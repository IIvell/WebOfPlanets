using UnityEngine;
using UnityEngine.InputSystem;

namespace WebOfPlanets
{
    public class PlanetCreator : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerCamera playerCamera;

        [SerializeField] private float minGravity = 10f;
        [SerializeField] private float maxGravity = 40f;

        private Transform _currentPlanet;

        [SerializeField] private int startingPlanets = 30;
        [SerializeField] private float minSpawnDistance = 1500f;
        [SerializeField] private float maxSpawnDistance = 5000f;
        [SerializeField] private float minPlanetSeparation = 200f;
        [SerializeField] private int maxPlacementAttempts = 30;

        [SerializeField] private Material iceMaterial;
        [SerializeField] private Material miningMaterial;
        [SerializeField] private Material volcanicMaterial;
        [SerializeField] private Material gaseousMaterial;
        [SerializeField] private Material organicMaterial;

        private readonly System.Collections.Generic.List<Vector3> _spawnedPositions = new();

        private static readonly PlanetType[] AllTypes =
        {
            PlanetType.Mining, PlanetType.Organic, PlanetType.Ice, PlanetType.Volcanic, PlanetType.Gaseous
        };

        void Start()
        {
            _currentPlanet = player.currentPlanet;

            if (_currentPlanet == null)
                Debug.LogWarning("PlanetCreator: player.currentPlanet nije postavljen.");

            Vector3 origin = _currentPlanet != null ? _currentPlanet.position : Vector3.zero;
            if (_currentPlanet != null)
                _spawnedPositions.Add(origin);

            // Lančani spawn: svaki planet se sidri na NASUMIČNO odabran već
            // spawnani planet (ili hub) i mora pasti unutar dometa veze od
            // sidra — graf potencijalnih veza je time po konstrukciji povezan,
            // pa je svaki planet dostižan iz huba lancem totema. Sa spawnom
            // uvijek-od-huba (raspon širi od dometa) prosječno je ~12 od 30
            // planeta bilo trajno nedostižno. Domet se čita runtime lookupom
            // (bez novog scene polja), uz 1% margine jer ConnectionManager par
            // odbacuje strogim ">" na točnoj granici. Par sidren na hub mora
            // imati i čistu hub stranu: par totema se uopće ne spawna ako hub
            // točka padne u exclusion zonu (oba-ili-nijedan pravilo).
            // Smjer ovisnosti u ciklusu PlanetCreator↔ConnectionManager: PC ovdje
            // (prije CM-ovog Starta) smije čitati SAMO serijalizirana polja CM-a
            // (MaxConnectionRange, exclusion provjere) — nikakvo runtime stanje.
            // CM-ova serijalizirana polja drže scene overridove (npr.
            // maxConnectionRange 2000 vs default 5000), pa se ovo NE smije
            // preseliti u "config objekt" bez editiranja scene.
            ConnectionManager connectionManager = FindFirstObjectByType<ConnectionManager>();
            if (connectionManager == null)
                Debug.LogWarning("PlanetCreator: ConnectionManager nije pronađen, planeti bez garancije veze.");

            float chainMaxDist = connectionManager != null
                ? Mathf.Min(maxSpawnDistance, connectionManager.MaxConnectionRange * 0.99f)
                : maxSpawnDistance;

            Transform hub = _currentPlanet;
            System.Predicate<Vector3> hubSideClear = connectionManager != null && hub != null
                ? pos => !connectionManager.IsConnectionPointBlocked(hub, pos)
                : null;

            for (int i = 0; i < startingPlanets; i++)
            {
                Vector3 anchor = _spawnedPositions.Count > 0
                    ? _spawnedPositions[Random.Range(0, _spawnedPositions.Count)]
                    : origin;
                bool anchorIsHub = anchor == origin;
                SpawnPlanet(anchor, i, chainMaxDist, anchorIsHub ? hubSideClear : null);
            }
        }

        void Update()
        {
            if (!GameManager.IsPlaying) return;
            if (!GameKeys.WasPressed(GameKeys.DebugSpawnPlanet)) return;
            CreatePlanetAndTeleport();
        }

        // Debug/stress spawnovi (T) moraju imati JEDINSTVENA imena: save
        // referencira planete po imenu, pa bi dva "GeneratedPlanet" na loadu
        // tiho zakačila veze/strojeve/igrača na krivi planet.
        private int _debugSpawnCount;

        private Transform SpawnPlanet(Vector3 origin, int index = -1, float maxDist = -1f, System.Predicate<Vector3> positionValid = null)
        {
            if (maxDist <= 0f) maxDist = maxSpawnDistance;

            float scale   = Random.Range(35f, 100f);
            float gravity = Random.Range(minGravity, maxGravity);
            float minSep  = minPlanetSeparation + scale;

            Vector3 planetPos = FindOpenPosition(origin, minSep, maxDist, positionValid);
            string  name      = index >= 0 ? $"Planet_{index:D2}" : $"GeneratedPlanet_{++_debugSpawnCount:D2}";
            PlanetType type   = AllTypes[Random.Range(0, AllTypes.Length)];

            return CreatePlanetObject(name, planetPos, scale, gravity, type);
        }

        // Load iz save datoteke: planet s točno zadanim svojstvima umjesto nasumičnih.
        public Transform SpawnPlanetFromSave(string name, Vector3 pos, float scale, float gravity, PlanetType type)
            => CreatePlanetObject(name, pos, scale, gravity, type);

        private Transform CreatePlanetObject(string name, Vector3 planetPos, float scale, float gravity, PlanetType type)
        {
            _spawnedPositions.Add(planetPos);

            GameObject planetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            planetGO.name = name;
            planetGO.layer = LayerMask.NameToLayer("Planet");
            planetGO.transform.position = planetPos;
            planetGO.transform.localScale = Vector3.one * scale;

            Rigidbody rb = planetGO.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // Primitivna sfera nosi analitički SphereCollider (savršena kugla), a
            // vidljivi mesh je poligonalna aproksimacija koja između vrhova pada do
            // ~1.3% radijusa ISPOD te kugle (R=50 → ~0.65). Sve što se prizemljuje
            // raycastom sjelo bi na nevidljivu analitičku kuglu i lebdjelo iznad
            // vidljivog tla — ista klasa problema kao convex hull na Hubu
            // (Planet.Awake), samo u suprotnom smjeru. Fizičku površinu zato
            // izjednačavamo s vidljivim mesheom; non-convex MeshCollider smije na
            // kinematic rigidbody. Disable prije Destroy: Destroy je odgođen do kraja
            // framea, a resursi se spawnaju event-lančano još ISTI frame — aktivni
            // SphereCollider bi bio bliži pogodak od mesha i sve bi opet lebdjelo.
            SphereCollider sphereCollider = planetGO.GetComponent<SphereCollider>();
            sphereCollider.enabled = false;
            Destroy(sphereCollider);
            MeshCollider meshCollider = planetGO.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = planetGO.GetComponent<MeshFilter>().sharedMesh;

            Attractor attractor = planetGO.AddComponent<Attractor>();
            attractor.OrientToGravity = false;
            attractor.enabled = false;

            Planet planet = planetGO.AddComponent<Planet>();
            planet.Gravity = gravity;
            planet.Type = type;

            if (planet.Type == PlanetType.Ice && iceMaterial != null)
                planetGO.GetComponent<Renderer>().material = iceMaterial;
            else if (planet.Type == PlanetType.Mining && miningMaterial != null)
            {
                // Seamless proceduralni kamen umjesto venus fotke (imala je vidljiv
                // šav na UV spoju); rotacija za varijaciju kao kod plinovitih.
                planetGO.GetComponent<Renderer>().material = RockPlanetTexture.GetMaterial(miningMaterial);
                planetGO.transform.rotation = Random.rotation;
            }
            else if (planet.Type == PlanetType.Volcanic && volcanicMaterial != null)
                planetGO.GetComponent<Renderer>().material = volcanicMaterial;
            else if (planet.Type == PlanetType.Gaseous && gaseousMaterial != null)
            {
                // Proceduralne trake plinovitog diva umjesto plošnog tinta; dijeljena
                // tekstura, a nasumična rotacija sfere daje varijaciju među planetima
                // (kugla je simetrična pa rotacija ne mijenja ništa osim izgleda).
                planetGO.GetComponent<Renderer>().material = GasPlanetTexture.GetMaterial(gaseousMaterial);
                planetGO.transform.rotation = Random.rotation;
            }
            else if (planet.Type == PlanetType.Organic)
            {
                if (organicMaterial != null)
                {
                    // Proceduralna "priroda" (šume, livade, jezera) umjesto plošnog
                    // tinta; rotacija za varijaciju kao kod plinovitih/kamenih.
                    planetGO.GetComponent<Renderer>().material = OrganicPlanetTexture.GetMaterial(organicMaterial);
                    planetGO.transform.rotation = Random.rotation;
                }
                else
                    // Fallback tint dok materijal nije dodijeljen u Inspectoru.
                    planetGO.GetComponent<Renderer>().material.color = new Color(0.30f, 0.55f, 0.25f);
            }

            return planetGO.transform;
        }

        private Vector3 FindOpenPosition(Vector3 origin, float minSep, float maxDist, System.Predicate<Vector3> positionValid = null)
        {
            // Domet veze može biti manji od minSpawnDistance — donju granicu tada
            // stišćemo pod gornju da planet ipak stane unutar dometa.
            float minDist = Mathf.Min(minSpawnDistance, maxDist);

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                float dist = Random.Range(minDist, maxDist);
                Vector3 candidate = origin + Random.onUnitSphere * dist;

                if (positionValid != null && !positionValid(candidate)) continue;

                bool tooClose = false;
                foreach (Vector3 existing in _spawnedPositions)
                {
                    if (Vector3.Distance(candidate, existing) < minSep)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose) return candidate;
            }

            // All attempts failed — just place at a random far distance. Uvjet
            // pozicije (čista hub strana) i tada pokušavamo ispoštovati: on čuva
            // garanciju veze, a separacija je samo estetika.
            Vector3 fallback = origin + Random.onUnitSphere * maxDist;
            if (positionValid != null)
                for (int attempt = 0; attempt < maxPlacementAttempts && !positionValid(fallback); attempt++)
                    fallback = origin + Random.onUnitSphere * maxDist;
            return fallback;
        }

        private void CreatePlanetAndTeleport()
        {
            if (_currentPlanet != null)
            {
                if (_currentPlanet.TryGetComponent(out Attractor oldAttractor))
                    oldAttractor.enabled = false;
            }

            Vector3 origin = _currentPlanet != null ? _currentPlanet.position : Vector3.zero;
            Transform newPlanet = SpawnPlanet(origin);
            if (newPlanet.TryGetComponent(out Attractor newAttractor))
                newAttractor.enabled = true;

            float scale = newPlanet.localScale.x;
            float radius = scale * 0.5f;
            Vector3 surfaceNormal = (newPlanet.position - origin).normalized;
            Vector3 playerPos = newPlanet.position - surfaceNormal * (radius + 2f);
            Quaternion playerRot = Quaternion.FromToRotation(Vector3.up, -surfaceNormal);

            Rigidbody playerRb = player.rig;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.position = playerPos;
            playerRb.rotation = playerRot;

            _currentPlanet = newPlanet;
            player.SetPlanet(_currentPlanet);
            if (playerCamera != null) playerCamera.SetPlanet(_currentPlanet);
        }

        // Delegacijski shim: izvedba teleporta živi u PlanetTeleporteru, a ova
        // metoda ostaje javna ulazna točka jer scena drži serijalizirane
        // planetCreator reference (ConnectionManager, GameManager, MachinePlacer).
        public void TeleportToPlanet(Transform targetPlanet, Transform fromPlanet = null, Transform destinationMarker = null)
            => _currentPlanet = PlanetTeleporter.Teleport(player, playerCamera, _currentPlanet,
                targetPlanet, fromPlanet, destinationMarker);
    }
}
