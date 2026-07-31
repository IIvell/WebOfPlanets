using System;
using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    [CreateAssetMenu(menuName = "Machines/Smelter Machine")]
    public class SmelterMachineData : QuickSlotItem
    {
        [Serializable]
        public struct SmeltRecipe
        {
            public Item input;
            [Min(1)] public int inputAmount;
            public Item output;
            [Min(1)] public int outputAmount;
        }

        [Tooltip("Prefab vizuala stroja. Prazno = kocka.")]
        public GameObject prefab;

        [Tooltip("Uniformni world scale vizuala pri postavljanju.")]
        [Min(0.01f)] public float worldScale = MachineFactory.SmelterScale;

        [Header("Prerada")]
        [Tooltip("Recepti koje ovaj stroj zna pretopiti. Svaki ciklus se pokušaju svi redom.")]
        public SmeltRecipe[] recipes;
        [Tooltip("Sekunde između svakog ciklusa prerade.")]
        [Min(0.1f)] public float processInterval = 8f;

        [Header("Kvar")]
        [Tooltip("Šansa (0–1) da se stroj pokvari po radnom ciklusu; 0 = nikad. Na nestabilnim planetama množi se s 3.")]
        [Range(0f, 1f)] public float breakdownChancePerCycle = 0.015f;
        [Tooltip("Resursi iz inventara igrača potrebni za popravak (E na polomljenom stroju). Prazno = besplatan popravak.")]
        public ConnectionRequirement[] repairCost;
    }

    // Premješteno iz SmelterMachine.cs (konsolidacija malih datoteka, 31. 7. 2026.).
    // Pretvara sirovine iz ulaznog spremnika u pretopljene resurse po receptima.
    public class SmelterMachine : ProductionMachine
    {
        [SerializeField] private SmelterMachineData data;

        private Transform _planet;

        private readonly ItemStackList _input = new();
        private readonly ItemStackList _output = new();

        public SmelterMachineData Data => data;
        public override Transform Planet => _planet;
        public IReadOnlyList<InventoryItem> InputItems => _input.Items;
        public IReadOnlyList<InventoryItem> OutputItems => _output.Items;

        protected override bool HasData => data != null;
        protected override string DisplayName => data.displayName;
        protected override float CycleInterval => data.processInterval;
        protected override float BreakdownChancePerCycle => data.breakdownChancePerCycle;
        protected override ConnectionRequirement[] RepairCost => data.repairCost;

        public void Init(SmelterMachineData machineData, Transform planet)
        {
            data = machineData;
            _planet = planet;
            _state = MachineState.Active;
        }

        protected override void TryCycle()
        {
            if (data.recipes == null || data.recipes.Length == 0) return;

            // Kvar samo dok stroj stvarno radi — prazan smelter se ne troši.
            if (_input.Count > 0 && _breakdown.RollBreakdown())
            {
                _state = MachineState.Broken;
                return;
            }

            bool processedAny = false;
            foreach (var recipe in data.recipes)
            {
                if (recipe.input == null || recipe.output == null) continue;
                if (!_input.TryConsume(recipe.input, recipe.inputAmount)) continue;

                for (int i = 0; i < recipe.outputAmount; i++)
                    _output.Add(recipe.output);

                processedAny = true;
                Debug.Log($"[{data.displayName}] Pretopio {recipe.inputAmount}x {recipe.input.displayName} -> " +
                          $"{recipe.outputAmount}x {recipe.output.displayName}");
            }

            _state = processedAny ? MachineState.Active : MachineState.Idle;
        }

        // Igrač pritisne E: prvo pokupi gotove pretopljene resurse, zatim ubaci iz svog
        // inventara sve sirovine koje ovaj stroj zna preraditi. Bez ovoga stroj nema
        // šta preraditi — ne vuče sirovine sam. Na polomljenom stroju E pokušava popravak.
        public override void Interact()
        {
            if (TryRepairInteract()) return;

            CollectOutput();
            DepositInputFromPlayer();
        }

        private void CollectOutput()
        {
            if (InventorySystem.Instance == null || _output.Count == 0) return;

            int kinds = _output.Count;
            TransferAllToPlayer(_output);
            Debug.Log($"[{data?.displayName}] Preuzeto {kinds} vrsta pretopljenih resursa.");
        }

        private void DepositInputFromPlayer()
        {
            if (data?.recipes == null) return;

            int deposited = DepositAllFromPlayer(_input, AcceptsInput);
            if (deposited > 0)
                Debug.Log($"[{data?.displayName}] Ubačeno {deposited} sirovina za preradu.");
        }

        // ── Save/load ─────────────────────────────────────────────────────────

        public void LoadInputItem(Item item, int count)
        {
            if (item == null) return;
            for (int i = 0; i < count; i++)
                _input.Add(item);
        }

        public void LoadOutputItem(Item item, int count)
        {
            if (item == null) return;
            for (int i = 0; i < count; i++)
                _output.Add(item);
        }

        private bool AcceptsInput(Item item)
        {
            foreach (var recipe in data.recipes)
                if (recipe.input == item) return true;
            return false;
        }
    }
}
