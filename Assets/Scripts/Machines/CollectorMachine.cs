using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Periodički skuplja resurse s površine planete u vlastiti spremnik ili u
    // povezani storage; troši održavanje iz Hub storage-a i može se pokvariti.
    public class CollectorMachine : ProductionMachine
    {
        [SerializeField] private MachineData data;
        [SerializeField] private Transform planet;

        private StorageMachine _outputStorage;
        private Transform _linkedPlanet;
        private bool _storageFullNotified;

        private readonly ItemStackList _stored = new();

        public MachineData Data => data;
        public override Transform Planet => planet;
        public Transform LinkedPlanet => _linkedPlanet;
        public StorageMachine OutputStorage => _outputStorage;
        public IReadOnlyList<InventoryItem> StoredItems => _stored.Items;

        protected override bool HasData => data != null;
        protected override string DisplayName => data.displayName;
        protected override float CycleInterval => data.collectionInterval;
        protected override float BreakdownChancePerCycle => data.breakdownChancePerCycle;
        protected override ConnectionRequirement[] RepairCost => data.repairCost;

        protected override bool ReadyForCycle() => base.ReadyForCycle() && planet != null;

        void OnEnable()  => GameEventBus.OnConnectionDestroyed += OnConnectionDestroyed;
        void OnDisable() => GameEventBus.OnConnectionDestroyed -= OnConnectionDestroyed;

        private void OnConnectionDestroyed(ConnectionEvent e)
        {
            if (_outputStorage == null) return;
            bool involves = (e.PlanetA == planet && e.PlanetB == _linkedPlanet)
                         || (e.PlanetB == planet && e.PlanetA == _linkedPlanet);
            if (involves)
            {
                _outputStorage = null;
                Debug.Log($"[{data?.displayName}] Veza prekinuta — prijenos u storage onemogućen.");
            }
        }

        public void Init(MachineData machineData, Transform planetTransform)
        {
            data = machineData;
            planet = planetTransform;
            _state = MachineState.Active;
        }

        public void SetLinkedPlanet(Transform target)
        {
            _linkedPlanet = target;
        }

        public void SetOutputStorage(StorageMachine storage)
        {
            _outputStorage = storage;
        }

        protected override void TryCycle()
        {
            // Pun izlazni storage zaustavlja skupljanje — resursi ostaju na planeti,
            // održavanje se ne troši dok stroj čeka da igrač isprazni storage.
            if (_outputStorage != null && _outputStorage.IsFull)
            {
                if (!_storageFullNotified)
                {
                    _storageFullNotified = true;
                    Debug.Log($"[{data.displayName}] Izlazni storage je pun — skupljanje pauzirano.");
                }
                _state = MachineState.Idle;
                return;
            }
            _storageFullNotified = false;

            if (_breakdown.RollBreakdown())
            {
                _state = MachineState.Broken;
                return;
            }

            if (!TryConsumeMaintenance(data.maintenanceCost))
            {
                _state = MachineState.Idle;
                Debug.Log($"[{data.displayName}] Nema resursa za održavanje — stroj čeka.");
                return;
            }

            CollectFromPlanet();
            _state = MachineState.Active;
        }

        private void CollectFromPlanet()
        {
            // 1.5x stvarnog radijusa pokriva resurse na površini (localScale.x je
            // promjer za primitivne sfere, pa je stara pretraga išla do 3x radijusa).
            float radius = SurfacePlacement.GetPlanetRadius(planet);
            Collider[] hits = Physics.OverlapSphere(
                planet.position, radius * 1.5f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

            int collected = 0;
            foreach (var col in hits)
            {
                if (collected >= data.amountPerCycle) break;
                if (!col.TryGetComponent<ItemInteractable>(out var interactable)) continue;
                if (!IsCollectable(interactable.ReferenceItem)) continue;

                // Storage se može napuniti usred ciklusa — stani prije nego što se
                // resurs potroši s planete, inače bi zaobišao kapacitet.
                if (_outputStorage != null && _outputStorage.IsFull) break;

                if (interactable.TryCollectByMachine(out Item item))
                {
                    // Ili u povezani storage ili interno — nikad oboje (duplikacija resursa)
                    if (_outputStorage == null || !_outputStorage.Add(item))
                        _stored.Add(item);
                    collected++;
                    Debug.Log($"[{data.displayName}] Skupio: {item.displayName}");
                }
            }
        }

        // ── Save/load ─────────────────────────────────────────────────────────

        public void LoadStoredItem(Item item, int count)
        {
            if (item == null) return;
            for (int i = 0; i < count; i++)
                _stored.Add(item);
        }

        private bool IsCollectable(Item item)
        {
            if (item == null) return false;
            foreach (var c in data.collectableItems)
                if (c == item) return true;
            return false;
        }

        // Igrač pritisne E na stroju da preuzme sve skupljene resurse;
        // na polomljenom stroju E umjesto toga pokušava popravak.
        public override void Interact()
        {
            if (TryRepairInteract()) return;

            if (InventorySystem.Instance == null) return;

            if (_stored.Count == 0)
            {
                Debug.Log($"[{data?.displayName}] Nema skupljenih resursa.");
                return;
            }

            int kinds = _stored.Count;
            TransferAllToPlayer(_stored);
            Debug.Log($"[{data?.displayName}] Preuzeto {kinds} vrsta resursa.");
        }
    }
}
