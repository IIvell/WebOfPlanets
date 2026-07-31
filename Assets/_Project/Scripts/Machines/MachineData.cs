using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    [CreateAssetMenu(menuName = "Machines/Collector Machine")]
    public class MachineData : QuickSlotItem
    {
        [Tooltip("Prefab vizuala stroja. Prazno = sfera.")]
        public GameObject prefab;
        [Tooltip("Uniformni scale vizuala pri postavljanju — modeli raznih packova imaju razne nativne veličine.")]
        [Min(0.01f)] public float worldScale = 7f;

        [Header("Skupljanje")]
        [Tooltip("Koje vrste resursa ovaj stroj skuplja s planete.")]
        public List<Item> collectableItems = new();
        [Tooltip("Sekunde između svakog ciklusa skupljanja.")]
        [Min(0.1f)] public float collectionInterval = 10f;
        [Tooltip("Koliko resursa se skupi po ciklusu.")]
        [Min(1)] public int amountPerCycle = 1;

        [Header("Održavanje")]
        [Tooltip("Resursi koji se troše iz HubStorage-a svakog ciklusa da stroj radi. Prazno = besplatno održavanje.")]
        public ConnectionRequirement[] maintenanceCost;

        [Header("Kvar")]
        [Tooltip("Šansa (0–1) da se stroj pokvari po radnom ciklusu; 0 = nikad. Na nestabilnim planetama množi se s 3.")]
        [Range(0f, 1f)] public float breakdownChancePerCycle = 0.02f;
        [Tooltip("Resursi iz inventara igrača potrebni za popravak (E na polomljenom stroju). Prazno = besplatan popravak.")]
        public ConnectionRequirement[] repairCost;
    }

    // Premješteno iz ProductionMachine.cs (konsolidacija malih datoteka, 31. 7. 2026.).
    // Zajednički kostur četiri proizvodna stroja (Collector/Smelter/Extractor/
    // Uplink): timer-ciklus u Update-u, vezanje MachineBreakdown komponente,
    // popravak s E, povratak Broken stanja iz save-a, trošak održavanja i
    // dict+lista spremnik. Prije baze je taj kostur bio ručno kopiran u sva
    // četiri stroja i već je počeo divergirati.
    //
    // SO tipovi podataka NEMAJU zajedničku baznu klasu i ne smiju je dobiti
    // (MachinePlacer ih razlikuje pattern-matchingom po tipu), pa baza do
    // vrijednosti dolazi kroz apstraktne accessore, a serijalizirana polja
    // (data, planet) ostaju u podklasama — ništa serijalizirano se ne mijenja.
    //
    // NAMJERNE razlike ostaju u podklasama i baza ih ne unificira: Smelter i
    // Uplink bacaju kocku kvara samo dok stvarno rade (prazan stroj se ne
    // troši), Collector/Extractor svaki ciklus; pauze (pun storage, prazan
    // buffer, pun hub) su semantika svakog stroja u njegovom TryCycle.
    public abstract class ProductionMachine : BaseInteractable
    {
        protected MachineState _state = MachineState.Idle;
        protected MachineBreakdown _breakdown;
        private float _timer;

        public MachineState State => _state;
        public override float HoldTime => 0f;

        // Vrijednosti iz konkretnog SO-a podataka.
        protected abstract bool HasData { get; }
        protected abstract string DisplayName { get; }
        protected abstract float CycleInterval { get; }
        protected abstract float BreakdownChancePerCycle { get; }
        protected abstract ConnectionRequirement[] RepairCost { get; }
        public abstract Transform Planet { get; }

        // Jedan radni ciklus stroja; poziva se svakih CycleInterval sekundi.
        protected abstract void TryCycle();

        // Collector dodatno traži planet != null.
        protected virtual bool ReadyForCycle() => HasData && _state != MachineState.Broken;

        void Update()
        {
            if (!ReadyForCycle()) return;

            EnsureBreakdown();

            _timer += Time.deltaTime;
            if (_timer >= CycleInterval)
            {
                _timer = 0f;
                TryCycle();
            }
        }

        // Lazy umjesto u Init-u da pokrije i eventualne scene-serijalizirane strojeve.
        protected void EnsureBreakdown()
        {
            if (_breakdown == null)
                _breakdown = MachineBreakdown.Attach(gameObject, DisplayName, Planet,
                    BreakdownChancePerCycle, RepairCost);
        }

        // E na polomljenom stroju pokušava popravak umjesto redovne interakcije;
        // true = interakcija time potrošena (stroj je bio polomljen).
        protected bool TryRepairInteract()
        {
            if (_state != MachineState.Broken) return false;
            if (_breakdown != null && _breakdown.TryRepair())
                _state = MachineState.Active;
            return true;
        }

        // Vraća Broken stanje iz save datoteke bez eventa/toasta.
        public void LoadBroken()
        {
            if (!HasData) return;
            EnsureBreakdown();
            _breakdown.LoadBroken();
            _state = MachineState.Broken;
        }

        // Dvofazni trošak održavanja iz Hub storage-a (prvo provjeri sve, pa
        // potroši sve). TestingMode i prazan trošak prolaze besplatno; bez hub
        // skladišta ne prolazi ništa (fail-closed — semantika zadržana iz
        // Collectora/Extractora koji su ovu metodu ranije nosili kao kopije).
        protected static bool TryConsumeMaintenance(ConnectionRequirement[] cost)
        {
            if (GameManager.TestingMode) return true;
            if (cost == null || cost.Length == 0) return true;
            if (HubStorage.Instance == null) return false;

            foreach (var req in cost)
            {
                if (req.item == null) continue;
                var inv = HubStorage.Instance.Get(req.item);
                if (inv == null || inv.GetStackSize() < req.amount) return false;
            }

            foreach (var req in cost)
            {
                if (req.item == null) continue;
                for (int i = 0; i < req.amount; i++)
                    HubStorage.Instance.Remove(req.item);
            }

            return true;
        }

        // ── Spremnik stroja ───────────────────────────────────────────────────

        // Par lista+dictionary: lista čuva redoslijed (UI, save, slanje s početka),
        // dictionary daje brzi lookup za stackiranje po Itemu. Prije je isti "add"
        // blok postojao u pet ručnih kopija po strojevima.
        protected sealed class ItemStackList
        {
            private readonly Dictionary<Item, InventoryItem> _dict = new();
            private readonly List<InventoryItem> _list = new();

            public IReadOnlyList<InventoryItem> Items => _list;
            public int Count => _list.Count;
            public InventoryItem First => _list[0];

            public void Add(Item item)
            {
                if (_dict.TryGetValue(item, out var existing))
                    existing.AddToStack();
                else
                {
                    var inv = new InventoryItem(item);
                    _list.Add(inv);
                    _dict[item] = inv;
                }
            }

            // Potroši amount komada ili ništa (smelter recepti).
            public bool TryConsume(Item item, int amount)
            {
                if (!_dict.TryGetValue(item, out var existing) || existing.GetStackSize() < amount)
                    return false;

                for (int i = 0; i < amount; i++)
                    existing.RemoveFromStack();

                if (existing.GetStackSize() == 0)
                {
                    _list.Remove(existing);
                    _dict.Remove(item);
                }

                return true;
            }

            // Skine jedan komad s prvog stacka u listi (uplink šalje redom).
            public void RemoveOneFromFirst()
            {
                var inv = _list[0];
                inv.RemoveFromStack();
                if (inv.GetStackSize() == 0)
                {
                    _dict.Remove(inv.data);
                    _list.RemoveAt(0);
                }
            }

            // Ukupan broj komada preko svih stackova (extractor kapacitet).
            public int TotalStacked()
            {
                int total = 0;
                foreach (var inv in _list)
                    total += inv.GetStackSize();
                return total;
            }

            public void Clear()
            {
                _dict.Clear();
                _list.Clear();
            }
        }

        // "Preuzmi sve": sav sadržaj spremnika u inventar igrača, pa isprazni.
        protected static void TransferAllToPlayer(ItemStackList stacks)
        {
            foreach (var inv in stacks.Items)
                for (int i = 0; i < inv.GetStackSize(); i++)
                    InventorySystem.Instance.Add(inv.data);
            stacks.Clear();
        }

        // "Ubaci sve iz inventara igrača" u spremnik stroja (Smelter filtrira po
        // receptima, Uplink uzima sve); vraća broj ubačenih komada.
        protected static int DepositAllFromPlayer(ItemStackList target, System.Predicate<Item> accepts = null)
        {
            var playerInventory = InventorySystem.Instance;
            if (playerInventory == null) return 0;

            int deposited = 0;
            var items = new List<InventoryItem>(playerInventory.GetInventory());
            foreach (var inventoryItem in items)
            {
                if (accepts != null && !accepts(inventoryItem.data)) continue;

                int stack = inventoryItem.GetStackSize();
                for (int i = 0; i < stack; i++)
                {
                    target.Add(inventoryItem.data);
                    playerInventory.Remove(inventoryItem.data);
                    deposited++;
                }
            }
            return deposited;
        }
    }

    // Premješteno iz CollectorMachine.cs (konsolidacija malih datoteka, 31. 7. 2026.).
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
