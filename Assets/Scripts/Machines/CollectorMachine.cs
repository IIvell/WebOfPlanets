using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class CollectorMachine : BaseInteractable
    {
        [SerializeField] private MachineData data;
        [SerializeField] private Transform planet;

        private StorageMachine _outputStorage;
        private Transform _linkedPlanet;
        private MachineState _state = MachineState.Idle;
        private MachineBreakdown _breakdown;
        private float _timer;
        private bool _storageFullNotified;

        private readonly Dictionary<Item, InventoryItem> _dict = new();
        private readonly List<InventoryItem> _storedItems = new();

        public MachineState State => _state;
        public MachineData Data => data;
        public Transform Planet => planet;
        public Transform LinkedPlanet => _linkedPlanet;
        public StorageMachine OutputStorage => _outputStorage;
        public IReadOnlyList<InventoryItem> StoredItems => _storedItems;

        public override float HoldTime => 0f;

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

        void Update()
        {
            if (data == null || planet == null || _state == MachineState.Broken) return;

            EnsureBreakdown();

            _timer += Time.deltaTime;
            if (_timer >= data.collectionInterval)
            {
                _timer = 0f;
                TryCycle();
            }
        }

        private void TryCycle()
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

            if (!TryConsumeMaintenance())
            {
                _state = MachineState.Idle;
                Debug.Log($"[{data.displayName}] Nema resursa za održavanje — stroj čeka.");
                return;
            }

            CollectFromPlanet();
            _state = MachineState.Active;
        }

        private bool TryConsumeMaintenance()
        {
            if (GameManager.TestingMode) return true;
            if (data.maintenanceCost == null || data.maintenanceCost.Length == 0) return true;
            if (HubStorage.current == null) return false;

            foreach (var req in data.maintenanceCost)
            {
                if (req.item == null) continue;
                var inv = HubStorage.current.Get(req.item);
                if (inv == null || inv.GetStackSize() < req.amount) return false;
            }

            foreach (var req in data.maintenanceCost)
            {
                if (req.item == null) continue;
                for (int i = 0; i < req.amount; i++)
                    HubStorage.current.Remove(req.item);
            }

            return true;
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
                        StoreItem(item);
                    collected++;
                    Debug.Log($"[{data.displayName}] Skupio: {item.displayName}");
                }
            }
        }

        // Lazy umjesto u Init-u da pokrije i eventualne scene-serijalizirane strojeve.
        private void EnsureBreakdown()
        {
            if (_breakdown == null)
                _breakdown = MachineBreakdown.Attach(gameObject, data.displayName, planet,
                    data.breakdownChancePerCycle, data.repairCost);
        }

        // ── Save/load ─────────────────────────────────────────────────────────

        public void LoadStoredItem(Item item, int count)
        {
            if (item == null) return;
            for (int i = 0; i < count; i++)
                StoreItem(item);
        }

        // Vraća Broken stanje bez eventa/toasta.
        public void LoadBroken()
        {
            if (data == null) return;
            EnsureBreakdown();
            _breakdown.LoadBroken();
            _state = MachineState.Broken;
        }

        private void StoreItem(Item item)
        {
            if (_dict.TryGetValue(item, out var existing))
                existing.AddToStack();
            else
            {
                var inv = new InventoryItem(item);
                _storedItems.Add(inv);
                _dict[item] = inv;
            }
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
            if (_state == MachineState.Broken)
            {
                if (_breakdown != null && _breakdown.TryRepair())
                    _state = MachineState.Active;
                return;
            }

            if (InventorySystem.current == null) return;

            if (_storedItems.Count == 0)
            {
                Debug.Log($"[{data?.displayName}] Nema skupljenih resursa.");
                return;
            }

            foreach (var inv in _storedItems)
                for (int i = 0; i < inv.GetStackSize(); i++)
                    InventorySystem.current.Add(inv.data);

            Debug.Log($"[{data?.displayName}] Preuzeto {_storedItems.Count} vrsta resursa.");
            _dict.Clear();
            _storedItems.Clear();
        }
    }
}
