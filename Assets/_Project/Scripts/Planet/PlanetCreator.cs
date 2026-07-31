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
                // Jupiter fotka (equirectangular, horizontalno seamless — venus je
                // imala šav) dolazi iz Planet_Mining.mat asseta umjesto proceduralnog
                // kamena; rotacija za varijaciju kao kod plinovitih.
                planetGO.GetComponent<Renderer>().material = miningMaterial;
                planetGO.transform.rotation = Random.rotation;
            }
            else if (planet.Type == PlanetType.Volcanic && volcanicMaterial != null)
                planetGO.GetComponent<Renderer>().material = volcanicMaterial;
            else if (planet.Type == PlanetType.Gaseous && gaseousMaterial != null)
            {
                // Planeta_Base_Color + Planeta_MetallicSmoothness (1 - roughness u
                // alphi) iz Planet_Gaseous.mat asseta umjesto proceduralnih traka;
                // nasumična rotacija sfere daje varijaciju među planetima.
                planetGO.GetComponent<Renderer>().material = gaseousMaterial;
                planetGO.transform.rotation = Random.rotation;
            }
            else if (planet.Type == PlanetType.Organic)
            {
                if (organicMaterial != null)
                {
                    // QonoS teksture (green_textures: Diff/Normal/Smoothness/Emit) iz
                    // Planet_Organic.mat asseta umjesto proceduralne "prirode";
                    // rotacija za varijaciju kao kod plinovitih/kamenih.
                    planetGO.GetComponent<Renderer>().material = organicMaterial;
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

    // Premješteno iz PlanetTeleporter.cs (konsolidacija malih datoteka, 31. 7. 2026.).
    // Izvedba teleporta igrača na planet — zove se za veze, teleporter strojeve,
    // respawn i load. PlanetCreator ostaje javna ulazna točka (scena drži tri
    // serijalizirane planetCreator reference pa bi puna selidba bila visok
    // rizik), ali sama odgovornost živi ovdje kao imenovani servis.
    internal static class PlanetTeleporter
    {
        // Vraća novu trenutnu planetu (uvijek targetPlanet); pozivatelj je dužan
        // zapamtiti povratnu vrijednost kao svoje novo stanje.
        public static Transform Teleport(PlayerController player, PlayerCamera playerCamera,
            Transform currentPlanet, Transform targetPlanet, Transform fromPlanet, Transform destinationMarker)
        {
            if (currentPlanet != null)
            {
                if (currentPlanet.TryGetComponent(out Attractor oldAttractor))
                    oldAttractor.enabled = false;
            }

            if (targetPlanet.TryGetComponent(out Attractor newAttractor))
                newAttractor.enabled = true;

            Vector3 playerPos;
            Vector3 playerUp;

            if (destinationMarker != null)
            {
                // Sletimo blizu stvarne pozicije markera/totema (umjesto da ponovno
                // računamo površinu iz centra planeta, što na nesferičnim meshovima
                // zna promašiti stvarnu točku markera).
                Vector3 markerUp = destinationMarker.up;
                Vector3 tangent = Vector3.Cross(markerUp, Vector3.up);
                if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(markerUp, Vector3.right);
                tangent.Normalize();

                // Marker sa solid colliderom (teleporter gate, totemi veza): sleti duž
                // forward osi tik uz rub collidera — izvan njega (inače fizika izbaci
                // igrača), ali ne dalje nego što je nužno. Za eventualni trigger marker
                // bez solid collidera ostaje paušalnih 2 m.
                float lateral = 2f;
                if (destinationMarker.TryGetComponent(out Collider markerCollider) && !markerCollider.isTrigger)
                {
                    tangent = destinationMarker.forward;

                    float probe = markerCollider.bounds.extents.magnitude + 2f;
                    Ray edgeRay = new Ray(destinationMarker.position + markerUp * 1f + tangent * probe, -tangent);
                    lateral = markerCollider.Raycast(edgeRay, out RaycastHit edgeHit, probe)
                        ? (probe - edgeHit.distance) + 1.5f
                        : markerCollider.bounds.extents.magnitude + 1.5f;
                }

                Vector3 rayOrigin = destinationMarker.position + markerUp * 2f + tangent * lateral;
                if (Physics.Raycast(rayOrigin, -markerUp, out RaycastHit hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    // +2, ne +1: dno kapsule je 1.69 ISPOD pivota, pa bi +1 kapsulu
                    // startno ukopao 0.69 u pogođenu površinu. Zraka pogađa SVE solid
                    // collidere — na niskom objektu (npr. pickup kamen, krov ~0.6) bi
                    // se surface-lock uhvatio u penetraciji i zaglavio u deadlocku sa
                    // PhysX depenetracijom. S +2 stopala kreću ~0.3 iznad pogođenog:
                    // meki pad na tlo, a niski objekt ostaje "krov" (negrounded) s
                    // kojeg igrač normalno siđe. Isto radi i sibling T-spawn (+2).
                    playerPos = hit.point + hit.normal * 2f;
                    playerUp = hit.normal;
                }
                else
                {
                    playerPos = destinationMarker.position + tangent * lateral;
                    playerUp = markerUp;
                }
            }
            else
            {
                // localScale laže za mesh planete (Hub: localScale 1000, stvarni radijus
                // ~19) — fallback bi igrača ostavio stotine jedinica iznad površine.
                float radius = SurfacePlacement.GetPlanetRadius(targetPlanet);

                Vector3 surfaceNormal = fromPlanet != null
                    ? (fromPlanet.position - targetPlanet.position).normalized
                    : Random.onUnitSphere;

                Vector3 tangent = Vector3.Cross(surfaceNormal, Vector3.up);
                if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(surfaceNormal, Vector3.right);
                tangent.Normalize();

                float lateralOffset = Mathf.Min(6f, radius * 0.5f);
                Vector3 aimDirection = (surfaceNormal * radius + tangent * lateralOffset).normalized;

                Vector3 rayOrigin = targetPlanet.position + aimDirection * (radius * 1.5f);
                if (Physics.Raycast(rayOrigin, -aimDirection, out RaycastHit hit, radius * 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    // +2 iz istog razloga kao marker-grana gore (dno kapsule -1.69).
                    playerPos = hit.point + hit.normal * 2f;
                    playerUp = hit.normal;
                }
                else
                {
                    playerPos = targetPlanet.position + aimDirection * (radius + 2f);
                    playerUp = aimDirection;
                }
            }

            Quaternion playerRot = Quaternion.FromToRotation(Vector3.up, playerUp);

            Rigidbody playerRb = player.rig;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.position = playerPos;
            playerRb.rotation = playerRot;

            player.SetPlanet(targetPlanet);
            if (playerCamera != null) playerCamera.SetPlanet(targetPlanet);

            // Centralna točka svih teleporta (veze, strojevi, respawn); trenutni
            // subscriber (AudioManager) treba samo činjenicu teleporta.
            GameEventBus.Raise(new PlayerTeleportEvent { FromPlanet = fromPlanet, ToPlanet = targetPlanet });

            return targetPlanet;
        }
    }
}
