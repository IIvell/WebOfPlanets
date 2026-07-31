using UnityEngine;

namespace WebOfPlanets
{
    [CreateAssetMenu(fileName = "TeleporterMachine", menuName = "Machines/Teleporter Machine")]
    public class TeleporterMachineData : QuickSlotItem
    {
        public GameObject prefab;

        [Tooltip("Uniformni world scale vizuala pri postavljanju (oba kraja para).")]
        [Min(0.01f)] public float worldScale = MachineFactory.TeleporterScale;
    }

    // Premješteno iz TeleporterMachine.cs (konsolidacija malih datoteka, 31. 7. 2026.).
    // Teleporteri se grade u paru: ulaz na planeti igrača, izlaz na Hubu
    // (MachinePlacer ih postavlja i povezuje). Pritisak na E teleportira
    // igrača do povezanog teleportera.
    public class TeleporterMachine : BaseInteractable
    {
        [SerializeField] private TeleporterMachineData data;

        private PlanetCreator _planetCreator;
        private Transform _planet;
        private TeleporterMachine _linked;

        public TeleporterMachineData Data => data;
        public Transform Planet => _planet;
        public TeleporterMachine Linked => _linked;

        public override float HoldTime => 0f;

        public void Init(TeleporterMachineData machineData, Transform planet, PlanetCreator planetCreator)
        {
            data = machineData;
            _planet = planet;
            _planetCreator = planetCreator;
        }

        public void SetLinkedTeleporter(TeleporterMachine linked)
        {
            _linked = linked;
        }

        public override void Interact()
        {
            if (_linked == null || _linked._planet == null)
            {
                Debug.LogWarning($"[{data?.displayName}] Teleporter nije povezan — nema odredišta.");
                return;
            }

            if (_planetCreator == null)
            {
                Debug.LogWarning($"[{data?.displayName}] PlanetCreator nije postavljen — teleport nije moguć.");
                return;
            }

            _planetCreator.TeleportToPlanet(_linked._planet, _planet, _linked.transform);
        }
    }
}
