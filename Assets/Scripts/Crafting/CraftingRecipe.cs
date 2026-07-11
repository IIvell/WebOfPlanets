using System;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(fileName = "Recipe", menuName = "Crafting/Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        public enum ResultType { Tool, CollectorMachine, StorageMachine, SmelterMachine }

        [Serializable]
        public struct Ingredient
        {
            public Item item;
            public int amount;
        }

        public string displayName;
        public ResultType resultType;

        public Tool resultTool;
        public MachineData resultMachine;
        public StorageMachineData resultStorageMachine;
        public SmelterMachineData resultSmelterMachine;

        public Ingredient[] ingredients;

        public bool CanAfford()
        {
            if (InventorySystem.current == null) return true;
            foreach (var ing in ingredients)
            {
                if (ing.item == null) continue;
                var inv = InventorySystem.current.Get(ing.item);
                if (inv == null || inv.GetStackSize() < ing.amount) return false;
            }
            return true;
        }

        public void ConsumeIngredients()
        {
            if (InventorySystem.current == null) return;
            foreach (var ing in ingredients)
            {
                if (ing.item == null) continue;
                for (int i = 0; i < ing.amount; i++)
                    InventorySystem.current.Remove(ing.item);
            }
        }
    }
}
