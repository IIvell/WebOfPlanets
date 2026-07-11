using System;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
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

        [Header("Prerada")]
        [Tooltip("Recepti koje ovaj stroj zna pretopiti. Svaki ciklus se pokušaju svi redom.")]
        public SmeltRecipe[] recipes;
        [Tooltip("Sekunde između svakog ciklusa prerade.")]
        [Min(0.1f)] public float processInterval = 8f;
    }
}
