using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
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
            if (InventorySystem.current == null) return false;

            foreach (var req in _repairCost)
            {
                if (req.item == null) continue;
                var inv = InventorySystem.current.Get(req.item);
                if (inv == null || inv.GetStackSize() < req.amount) return false;
            }

            foreach (var req in _repairCost)
            {
                if (req.item == null) continue;
                for (int i = 0; i < req.amount; i++)
                    InventorySystem.current.Remove(req.item);
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
