using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class ConnectionManager : MonoBehaviour
    {
        [SerializeField] private PlanetCreator planetCreator;
        [SerializeField] private float maxConnectionRange = 5000f;

        [Header("Exclusion Zones (player spawn, chest, computer...)")]
        [SerializeField] private Transform[] exclusionZones;
        [SerializeField] private float exclusionRadius = 20f;

        [Header("Slaba veza (tanka)")]
        [SerializeField] private ConnectionRequirement[] weakCost;
        [SerializeField] private float weakLifespan = 60f;
        [SerializeField] private float weakThickness = 0.4f;

        [Header("Srednja veza")]
        [SerializeField] private ConnectionRequirement[] midCost;
        [SerializeField] private float midLifespan = 180f;
        [SerializeField] private float midThickness = 0.6f;

        [Header("Jaka veza (debela)")]
        [SerializeField] private ConnectionRequirement[] strongCost;
        [SerializeField] private float strongLifespan = 600f;
        [SerializeField] private float strongThickness = 0.9f;

        [Header("Nestabilni planeti (GDD 4.2)")]
        [Tooltip("Dodatna brzina degradacije po nestabilnom kraju veze (vulkanski/plinski planet). 0.5 znači: jedan nestabilan kraj = 1.5x brža degradacija, oba kraja = 2x.")]
        [SerializeField] private float unstableDegradationPerEnd = 0.5f;

        [Header("Teleport (bez veze)")]
        [SerializeField] private ConnectionRequirement[] teleportCost;
        [Tooltip("GDD: teleport je skuplji što je dalje — svakih X jedinica udaljenosti dodaje jednu osnovnu cijenu (množitelj = 1 + floor(d/X)). 0 = fiksna cijena.")]
        [SerializeField] private float teleportCostDistanceStep = 2000f;

        [Header("Potencijalna veza (marker)")]
        [SerializeField] private GameObject potentialConnectionMarkerPrefab;
        [SerializeField] private float markerScale = 3f;
        [SerializeField] private float markerHeight = 3f;

        private readonly List<PlanetConnection> _connections = new();
        private readonly Dictionary<(int, int), List<GameObject>> _potentialMarkers = new();

        private IEnumerator Start()
        {
            yield return null;
            SpawnPotentialMarkers();
        }

        void OnEnable()  => GameEventBus.OnConnectionDestroyed += OnConnectionDestroyed;
        void OnDisable() => GameEventBus.OnConnectionDestroyed -= OnConnectionDestroyed;

        private void OnConnectionDestroyed(ConnectionEvent e)
        {
            _connections.RemoveAll(c => c == null || c.Connects(e.PlanetA, e.PlanetB));

            if (e.ConnectionType == ConnectionType.Ancient) return;
            if (e.PlanetA == null || e.PlanetB == null) return;

            SetPotentialMarkersActive(e.PlanetA, e.PlanetB, true);
        }

        private void SetPotentialMarkersActive(Transform a, Transform b, bool active)
        {
            var key = PairKey(a, b);
            if (!_potentialMarkers.TryGetValue(key, out var markers)) return;
            foreach (var m in markers)
                if (m != null) m.SetActive(active);
        }

        // Poza postojećeg potencijalnog totema za svaku stranu para (null ako je
        // strana bez totema, npr. preskočena zbog exclusion zone). Kojoj planeti
        // totem pripada određuje se udaljenošću — lista strana ne čuva redoslijed
        // kad je jedna null.
        private void GetPotentialMarkerPoses(Transform a, Transform b, out Pose? poseA, out Pose? poseB)
        {
            poseA = null;
            poseB = null;
            if (!_potentialMarkers.TryGetValue(PairKey(a, b), out var markers)) return;

            foreach (var m in markers)
            {
                if (m == null) continue;
                Vector3 p = m.transform.position;
                bool closerToA = (p - a.position).sqrMagnitude <= (p - b.position).sqrMagnitude;
                if (closerToA) poseA = new Pose(p, m.transform.rotation);
                else           poseB = new Pose(p, m.transform.rotation);
            }
        }

        private void SpawnPotentialMarkers()
        {
            Planet[] all = FindObjectsByType<Planet>(FindObjectsSortMode.None);
            int count = 0;

            for (int i = 0; i < all.Length; i++)
            {
                for (int j = i + 1; j < all.Length; j++)
                {
                    Transform a = all[i].transform;
                    Transform b = all[j].transform;

                    if (Vector3.Distance(a.position, b.position) > maxConnectionRange) continue;
                    if (AlreadyConnected(a, b)) continue;

                    SpawnPotentialPair(a, b);
                    count++;
                }
            }

            Debug.Log($"ConnectionManager: spawnirano {count} potencijalnih veza.");
        }

        private void SpawnPotentialPair(Transform a, Transform b)
        {
            var key = PairKey(a, b);
            var markers = new List<GameObject>();

            var ma = CreatePotentialMarker(a, b);
            var mb = CreatePotentialMarker(b, a);

            if (ma != null) markers.Add(ma);
            if (mb != null) markers.Add(mb);

            _potentialMarkers[key] = markers;
        }

        private bool IsInExclusionZone(Vector3 pos)
        {
            if (exclusionZones == null) return false;
            foreach (var zone in exclusionZones)
                if (zone != null && Vector3.Distance(pos, zone.position) < exclusionRadius)
                    return true;
            return false;
        }

        private GameObject CreatePotentialMarker(Transform from, Transform toward)
        {
            if (potentialConnectionMarkerPrefab == null)
            {
                Debug.LogError("ConnectionManager: potentialConnectionMarkerPrefab nije postavljen.");
                return null;
            }

            Vector3 dir = (toward.position - from.position).normalized;

            // Exclusion zonu odlučuje idealna točka (kao i prije), a stvarna
            // pozicija bježi od zauzetog tla — resursi se spawnaju isti frame pa
            // bi totem znao završiti unutar kamena/drveta. Bočni pomak ne smije
            // marker ni naknadno ugurati u exclusion zonu.
            if (IsInExclusionZone(PlanetConnection.SurfacePoint(from, dir))) return null;

            Vector3 pos = PlanetConnection.FindClearSurfacePoint(from, dir);
            if (IsInExclusionZone(pos)) return null;

            // Radijalno uspravno KAO STROJEVI, ne po normali pogođenog trokuta:
            // na low-poly planetima normala trokuta zna vidljivo odstupati od
            // radijalnog "gore", pa visoki totem igraču (koji stoji radijalno po
            // gravitaciji) izgleda iskrivljeno. Nagnute trokute rješava izmjereni
            // ukop u GroundToSurface, a radijalni up je ujedno i "up" za teleport
            // dolazak (destinationMarker.up).
            Vector3 radial = (pos - from.position).normalized;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, radial);

            GameObject marker = Instantiate(potentialConnectionMarkerPrefab, pos, rot, transform);
            marker.name = "PotentialConnectionMarker";
            marker.transform.localScale = Vector3.one * markerScale;

            // Prizemlji po stvarnoj geometriji da dno sjedne na površinu bez obzira
            // na pivot prefaba.
            SurfacePlacement.GroundToSurface(marker, from, pos, radial);

            // Solid collider po stvarnim granicama vizuala (isto kao strojevi):
            // totem mora fizički blokirati igrača. Interakciji trigger ne treba —
            // Interactor cilja OverlapSphere-om po colliderima.
            MachinePlacer.FitColliderToRenderer(marker);

            var interactable = marker.AddComponent<PotentialConnectionInteractable>();
            interactable.Init(this, from, toward);

            return marker;
        }

        public bool TryBuildConnection(Transform a, Transform b, ConnectionType quality)
        {
            if (AlreadyConnected(a, b)) return false;

            ConnectionRequirement[] cost = GetCost(quality);
            if (!HasResources(cost)) return false;

            ConsumeResources(cost);
            // Potencijalni totemi se gase PRIJE spawna aktivnih markera: njihovi
            // collideri stoje točno na idealnoj točki, pa bi FindClearSurfacePoint
            // inače svaki aktivni marker bez potrebe otjerao u stranu.
            SetPotentialMarkersActive(a, b, false);
            CreateConnection(a, b, quality, GetLifespan(quality, a, b));
            return true;
        }

        public ConnectionRequirement[] GetCost(ConnectionType quality) => quality switch
        {
            ConnectionType.Weak   => weakCost,
            ConnectionType.Mid    => midCost,
            ConnectionType.Strong => strongCost,
            _                     => null
        };

        public float GetLifespan(ConnectionType quality) => quality switch
        {
            ConnectionType.Weak   => weakLifespan,
            ConnectionType.Mid    => midLifespan,
            ConnectionType.Strong => strongLifespan,
            _                     => 0f
        };

        // Efektivno trajanje veze za konkretan par planeta: nestabilni krajevi
        // (GDD 4.2) ubrzavaju degradaciju, tj. skraćuju lifespan.
        public float GetLifespan(ConnectionType quality, Transform a, Transform b)
        {
            float baseLifespan = GetLifespan(quality);
            if (baseLifespan <= 0f) return baseLifespan;
            return baseLifespan / GetDegradationMultiplier(a, b);
        }

        public float GetDegradationMultiplier(Transform a, Transform b)
        {
            int unstableEnds = 0;
            if (a != null && a.TryGetComponent(out Planet pa) && pa.IsUnstable) unstableEnds++;
            if (b != null && b.TryGetComponent(out Planet pb) && pb.IsUnstable) unstableEnds++;
            return 1f + unstableDegradationPerEnd * unstableEnds;
        }

        public float GetThickness(ConnectionType quality) => quality switch
        {
            ConnectionType.Weak   => weakThickness,
            ConnectionType.Mid    => midThickness,
            ConnectionType.Strong => strongThickness,
            _                     => midThickness
        };

        public bool CanAfford(ConnectionType quality) => HasResources(GetCost(quality));

        // Efektivna cijena teleporta za konkretan par planeta: osnovna cijena
        // pomnožena množiteljem udaljenosti.
        public ConnectionRequirement[] GetTeleportCost(Transform from, Transform to)
        {
            int multiplier = GetTeleportCostMultiplier(from, to);
            if (multiplier <= 1 || teleportCost == null) return teleportCost;

            var scaled = new ConnectionRequirement[teleportCost.Length];
            for (int i = 0; i < teleportCost.Length; i++)
                scaled[i] = new ConnectionRequirement
                {
                    item = teleportCost[i].item,
                    amount = teleportCost[i].amount * multiplier
                };
            return scaled;
        }

        public int GetTeleportCostMultiplier(Transform from, Transform to) =>
            teleportCostDistanceStep > 0f && from != null && to != null
                ? 1 + Mathf.FloorToInt(Vector3.Distance(from.position, to.position) / teleportCostDistanceStep)
                : 1;

        public bool CanAffordTeleport(Transform from, Transform to) => HasResources(GetTeleportCost(from, to));

        public bool TryTeleport(Transform from, Transform to)
        {
            ConnectionRequirement[] cost = GetTeleportCost(from, to);
            if (!HasResources(cost)) return false;
            ConsumeResources(cost);
            planetCreator.TeleportToPlanet(to, from);
            return true;
        }

        private void RemovePotentialGroup(Transform a, Transform b)
        {
            var key = PairKey(a, b);
            if (!_potentialMarkers.TryGetValue(key, out var markers)) return;

            foreach (var m in markers)
                if (m != null) Destroy(m);

            _potentialMarkers.Remove(key);
        }

        private static Dictionary<Item, int> AggregateCost(ConnectionRequirement[] cost)
        {
            var totals = new Dictionary<Item, int>();
            if (cost == null) return totals;

            foreach (var req in cost)
            {
                if (req.item == null) continue;
                totals.TryGetValue(req.item, out int existing);
                totals[req.item] = existing + req.amount;
            }
            return totals;
        }

        private bool HasResources(ConnectionRequirement[] cost)
        {
            if (GameManager.TestingMode) return true;
            if (cost == null || InventorySystem.current == null) return true;
            foreach (var kvp in AggregateCost(cost))
            {
                var item = InventorySystem.current.Get(kvp.Key);
                if (item == null || item.GetStackSize() < kvp.Value) return false;
            }
            return true;
        }

        private void ConsumeResources(ConnectionRequirement[] cost)
        {
            if (GameManager.TestingMode) return;
            if (cost == null || InventorySystem.current == null) return;
            foreach (var kvp in AggregateCost(cost))
                for (int i = 0; i < kvp.Value; i++)
                    InventorySystem.current.Remove(kvp.Key);
        }

        private void CreateConnection(Transform a, Transform b, ConnectionType type, float lifespan)
        {
            GameObject go = new GameObject($"Connection_{a.name}_{b.name}");
            go.transform.SetParent(transform);

            // Totem prave veze mora osvanuti TOČNO gdje je stajao potencijalni
            // totem na kojem je igrač vezu aktivirao (potencijalni je mogao biti
            // bočno pomaknut od idealne točke zbog resursa) — ponovni izračun
            // pozicije bi ga odveo na drugo nasumično mjesto i igraču bi veza
            // "nestala". Poze se čitaju i s ugašenih totema (transform ostaje).
            GetPotentialMarkerPoses(a, b, out Pose? poseA, out Pose? poseB);

            PlanetConnection conn = go.AddComponent<PlanetConnection>();
            conn.Init(a, b, type, planetCreator, lifespan, potentialConnectionMarkerPrefab, markerScale, markerHeight, GetThickness(type), poseA, poseB);
            _connections.Add(conn);

            GameEventBus.RaiseConnectionCreated(new ConnectionEvent
            {
                PlanetA = a,
                PlanetB = b,
                ConnectionType = type
            });
        }

        private bool AlreadyConnected(Transform a, Transform b)
        {
            foreach (var c in _connections)
                if (c != null && c.Connects(a, b)) return true;
            return false;
        }

        private (int, int) PairKey(Transform a, Transform b)
        {
            int ia = a.GetInstanceID(), ib = b.GetInstanceID();
            return ia < ib ? (ia, ib) : (ib, ia);
        }

        public IReadOnlyList<PlanetConnection> Connections => _connections;
    }
}
