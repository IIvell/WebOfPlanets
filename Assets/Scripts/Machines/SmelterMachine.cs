using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    public class SmelterMachine : BaseInteractable
    {
        [SerializeField] private SmelterMachineData data;

        private MachineState _state = MachineState.Idle;
        private float _timer;

        private readonly Dictionary<Item, InventoryItem> _inputDict = new();
        private readonly List<InventoryItem> _inputItems = new();

        private readonly Dictionary<Item, InventoryItem> _outputDict = new();
        private readonly List<InventoryItem> _outputItems = new();

        public MachineState State => _state;
        public SmelterMachineData Data => data;
        public IReadOnlyList<InventoryItem> InputItems => _inputItems;
        public IReadOnlyList<InventoryItem> OutputItems => _outputItems;

        public override float HoldTime => 0f;

        public void Init(SmelterMachineData machineData)
        {
            data = machineData;
            _state = MachineState.Active;
        }

        void Update()
        {
            if (data == null || _state == MachineState.Broken) return;

            _timer += Time.deltaTime;
            if (_timer >= data.processInterval)
            {
                _timer = 0f;
                TryCycle();
            }
        }

        private void TryCycle()
        {
            if (data.recipes == null || data.recipes.Length == 0) return;

            bool processedAny = false;
            foreach (var recipe in data.recipes)
            {
                if (recipe.input == null || recipe.output == null) continue;
                if (!TryConsumeInput(recipe.input, recipe.inputAmount)) continue;

                for (int i = 0; i < recipe.outputAmount; i++)
                    StoreOutput(recipe.output);

                processedAny = true;
                Debug.Log($"[{data.displayName}] Pretopio {recipe.inputAmount}x {recipe.input.displayName} -> " +
                          $"{recipe.outputAmount}x {recipe.output.displayName}");
            }

            _state = processedAny ? MachineState.Active : MachineState.Idle;
        }

        // Igrač pritisne E: prvo pokupi gotove pretopljene resurse, zatim ubaci iz svog
        // inventara sve sirovine koje ovaj stroj zna preraditi. Bez ovoga stroj nema
        // šta preraditi — ne vuče sirovine sam.
        public override void Interact()
        {
            CollectOutput();
            DepositInputFromPlayer();
        }

        private void CollectOutput()
        {
            if (InventorySystem.current == null || _outputItems.Count == 0) return;

            foreach (var inv in _outputItems)
                for (int i = 0; i < inv.GetStackSize(); i++)
                    InventorySystem.current.Add(inv.data);

            Debug.Log($"[{data?.displayName}] Preuzeto {_outputItems.Count} vrsta pretopljenih resursa.");
            _outputDict.Clear();
            _outputItems.Clear();
        }

        private void DepositInputFromPlayer()
        {
            var playerInventory = InventorySystem.current;
            if (playerInventory == null || data?.recipes == null) return;

            int deposited = 0;
            var items = new List<InventoryItem>(playerInventory.GetInventory());
            foreach (var inventoryItem in items)
            {
                if (!AcceptsInput(inventoryItem.data)) continue;

                int stack = inventoryItem.GetStackSize();
                for (int i = 0; i < stack; i++)
                {
                    AddInput(inventoryItem.data);
                    playerInventory.Remove(inventoryItem.data);
                    deposited++;
                }
            }

            if (deposited > 0)
                Debug.Log($"[{data?.displayName}] Ubačeno {deposited} sirovina za preradu.");
        }

        private bool AcceptsInput(Item item)
        {
            foreach (var recipe in data.recipes)
                if (recipe.input == item) return true;
            return false;
        }

        private void AddInput(Item item)
        {
            if (_inputDict.TryGetValue(item, out var existing))
                existing.AddToStack();
            else
            {
                var inv = new InventoryItem(item);
                _inputItems.Add(inv);
                _inputDict[item] = inv;
            }
        }

        private bool TryConsumeInput(Item item, int amount)
        {
            if (!_inputDict.TryGetValue(item, out var existing) || existing.GetStackSize() < amount)
                return false;

            for (int i = 0; i < amount; i++)
                existing.RemoveFromStack();

            if (existing.GetStackSize() == 0)
            {
                _inputItems.Remove(existing);
                _inputDict.Remove(item);
            }

            return true;
        }

        private void StoreOutput(Item item)
        {
            if (_outputDict.TryGetValue(item, out var existing))
                existing.AddToStack();
            else
            {
                var inv = new InventoryItem(item);
                _outputItems.Add(inv);
                _outputDict[item] = inv;
            }
        }
    }
}
