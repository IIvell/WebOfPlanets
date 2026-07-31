using UnityEngine;

namespace WebOfPlanets
{
    // Statički factory svjetskih objekata (strojevi, totemi, markeri, mobovi).
    // Ranije je živio u MachinePlaceru — input komponenti — a zvali su ga
    // SaveSystem, PlanetConnection, ConnectionManager, HubBase, EnemyMobSpawner,
    // RespawnTotem, ComputerMachine i GameManager. Ovdje je i JEDINA tablica
    // fallback boja i default scale-ova po tipu stroja: MachinePlacer i
    // SaveSystem su ranije držali po vlastitu kopiju boja, koje su već
    // divergirale.
    public static class MachineFactory
    {
        // Fallback boje kocke — koriste se samo kad stroj nema prefab.
        public static readonly Color CollectorColor  = new(0.2f, 0.6f, 1f);
        public static readonly Color StorageColor    = new(0.8f, 0.4f, 0f);
        public static readonly Color SmelterColor    = new(0.9f, 0.2f, 0.1f);
        public static readonly Color ExtractorColor  = new(0.1f, 0.8f, 0.5f);
        public static readonly Color UplinkColor     = new(0.2f, 0.8f, 0.9f);
        public static readonly Color TeleporterColor = new(0.6f, 0.3f, 0.9f);
        public static readonly Color TwoWayGateColor = new(1f, 0.6f, 0.1f);

        // Dvosmjerni gate ima svoju boju — SaveSystem je ranije sve teleportere
        // loadao u boji običnog (dokumentirani defekt-fix, PLAN-KOD §1).
        public static Color TeleporterColorFor(TeleporterMachineData data) =>
            data is TwoWayTeleporterMachineData ? TwoWayGateColor : TeleporterColor;

        // Default world scale pri postavljanju po tipu (SaveSystem sprema stvarni
        // localScale pa kod loada koristi spremljeni). Collector jedini čita
        // MachineData.worldScale iz asseta.
        public const float StorageScale    = 150f;
        public const float SmelterScale    = 3f;
        public const float ExtractorScale  = 7f;
        public const float UplinkScale     = 7f;
        public const float TeleporterScale = 7f;
        public const float TotemScale      = 5f;
        // 0.52 = world skala hub Računala u sceni (ista Computer.fbx instanca).
        public const float ComputerScale   = 0.52f;

        // pos mora biti točka na površini planeta; uz zadan planet objekt se
        // prizemlji tako da mu dno stvarne geometrije sjedne na pos, bez obzira
        // gdje je pivot prefaba.
        public static GameObject SpawnObject(GameObject prefab, Vector3 pos, Quaternion rot,
            string fallbackName, Color fallbackColor, float scale = 300f, Quaternion? rotationOffset = null,
            bool fitColliderToRenderer = false, Transform planet = null)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, pos, rot);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetPositionAndRotation(pos, rot);
                go.GetComponent<Renderer>().material.color = fallbackColor;
            }

            go.transform.localScale = Vector3.one * scale;
            go.transform.rotation   = rot * (rotationOffset ?? Quaternion.Euler(-90f, 0f, 0f));
            go.name = fallbackName;

            if (fitColliderToRenderer)
                SurfacePlacement.FitBoxColliderToGeometry(go);
            else if (!go.TryGetComponent<Collider>(out _))
                go.AddComponent<BoxCollider>();

            if (go.TryGetComponent<Rigidbody>(out var rb))
                Object.Destroy(rb);

            // rot je FromToRotation(Vector3.up, normala), pa je rot * up normala površine
            // i kad rotationOffset dodatno zakrene model.
            if (planet != null)
                SurfacePlacement.GroundToSurface(go, planet, pos, rot * Vector3.up);

            // Zajednički put svih postavljanja (strojevi, totemi) — prašina oko baze.
            VfxManager.PlayMachinePlaced(pos, rot * Vector3.up);

            return go;
        }

        // Standardni spawn STROJA: model uspravno na normalu (bez default -90°
        // offseta prefab modela), BoxCollider po stvarnoj geometriji, prizemljen
        // na planet.
        public static GameObject SpawnMachine(GameObject prefab, Vector3 pos, Quaternion rot,
            string name, Color fallbackColor, float scale, Transform planet)
            => SpawnObject(prefab, pos, rot, name, fallbackColor, scale,
                rotationOffset: Quaternion.identity, fitColliderToRenderer: true, planet: planet);

        // Čisto tlo: u radijusu objekta ne smije biti ničeg osim same planete.
        public static bool IsSpotClear(Vector3 pos, Transform planet)
        {
            foreach (var col in Physics.OverlapSphere(pos, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (col.transform != planet) return false;
            return true;
        }
    }

    // Premješteno iz MachineBreakdown.cs (konsolidacija malih datoteka, 31. 7. 2026.).
    // Kvar strojeva: svaki RADNI ciklus stroj baca kocku i može prijeći u Broken
    // stanje — na nestabilnim planetama (Volcanic/Gaseous) šansa je veća. Polomljeni
    // stroj stoji dok ga igrač ne popravi s E na njemu; trošak popravka ide iz
    // inventara igrača (stoji uz stroj), za razliku od održavanja koje ide iz Hub
    // storage-a. Komponenta drži zajedničku mehaniku (roll, popravak, vizual,
    // eventi); Active/Idle/Broken stanje ostaje na samom stroju.
    public class MachineBreakdown : MonoBehaviour
    {
        // Nestabilne planete lome strojeve ovoliko puta češće.
        public const float UnstableChanceMultiplier = 3f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color BrokenTint = new Color(0.4f, 0.14f, 0.1f);

        private string _machineName;
        private Transform _planet;
        private float _chancePerCycle;
        private ConnectionRequirement[] _repairCost;

        // Strojevi se stvaraju runtime (AddComponent u MachinePlaceru), pa se i ova
        // komponenta veže kodom umjesto kroz prefab; ponovni Attach samo osvježi config.
        public static MachineBreakdown Attach(GameObject go, string machineName, Transform planet,
            float chancePerCycle, ConnectionRequirement[] repairCost)
        {
            if (!go.TryGetComponent(out MachineBreakdown b))
                b = go.AddComponent<MachineBreakdown>();
            b._machineName = machineName;
            b._planet = planet;
            b._chancePerCycle = chancePerCycle;
            b._repairCost = repairCost;
            return b;
        }

        // Zove stroj na početku radnog ciklusa; true = upravo se pokvario.
        public bool RollBreakdown()
        {
            float chance = _chancePerCycle;
            if (chance <= 0f) return false;
            if (_planet != null && _planet.TryGetComponent(out Planet p) && p.IsUnstable)
                chance = Mathf.Clamp01(chance * UnstableChanceMultiplier);
            if (Random.value >= chance) return false;

            SetBrokenVisual(true);
            VfxManager.PlayMachineBroken(transform.position, SurfaceUp());
            GameEventBus.RaiseMachineBroken(new MachineEvent
            {
                State = MachineState.Broken,
                Planet = _planet,
                MachineName = _machineName
            });
            Debug.Log($"[{_machineName}] Stroj se pokvario — pritisni E na njemu za popravak.");
            return true;
        }

        // Popravak s E na polomljenom stroju; true = popravljen (stroj se vraća u Active).
        public bool TryRepair()
        {
            if (!TryConsumeRepairCost())
            {
                Debug.Log($"[{_machineName}] Nedovoljno resursa za popravak — treba: {DescribeRepairCost()}");
                return false;
            }

            SetBrokenVisual(false);
            VfxManager.PlayMachinePlaced(transform.position, SurfaceUp());
            // Rezervirano: event trenutno nema pretplatnika (AlertsUI sluša samo
            // OnMachineBroken) — ostaje kao točka za budući toast/zvuk popravka.
            GameEventBus.RaiseMachineRepaired(new MachineEvent
            {
                State = MachineState.Active,
                Planet = _planet,
                MachineName = _machineName
            });
            Debug.Log($"[{_machineName}] Popravljen.");
            return true;
        }

        // Load iz save datoteke: samo vizual polomljenog stroja, bez eventa/toasta.
        public void LoadBroken() => SetBrokenVisual(true);

        private bool TryConsumeRepairCost()
        {
            if (GameManager.TestingMode) return true;
            if (_repairCost == null || _repairCost.Length == 0) return true;
            if (InventorySystem.Instance == null) return false;

            foreach (var req in _repairCost)
            {
                if (req.item == null) continue;
                var inv = InventorySystem.Instance.Get(req.item);
                if (inv == null || inv.GetStackSize() < req.amount) return false;
            }

            foreach (var req in _repairCost)
            {
                if (req.item == null) continue;
                for (int i = 0; i < req.amount; i++)
                    InventorySystem.Instance.Remove(req.item);
            }

            return true;
        }

        private string DescribeRepairCost()
        {
            if (_repairCost == null || _repairCost.Length == 0) return "ništa";

            var sb = new System.Text.StringBuilder();
            foreach (var req in _repairCost)
            {
                if (req.item == null) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append($"{req.amount}x {req.item.displayName}");
            }
            return sb.Length > 0 ? sb.ToString() : "ništa";
        }

        // Modeli su rotirani offsetom pri postavljanju pa transform.up nije pouzdana
        // normala — smjer od centra planete jest.
        private Vector3 SurfaceUp()
        {
            if (_planet != null)
            {
                Vector3 up = transform.position - _planet.position;
                if (up.sqrMagnitude > 0.001f) return up.normalized;
            }
            return transform.up;
        }

        // Tamnocrveni tint preko svih renderera dok je stroj polomljen — property
        // block ne dira dijeljene materijale i čisti se pri popravku.
        private void SetBrokenVisual(bool broken)
        {
            var block = broken ? new MaterialPropertyBlock() : null;
            if (broken)
            {
                block.SetColor(BaseColorId, BrokenTint);
                block.SetColor(ColorId, BrokenTint);
            }

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                r.SetPropertyBlock(block);
            }
        }
    }
}
