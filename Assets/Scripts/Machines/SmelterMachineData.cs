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

        [Header("Kvar")]
        [Tooltip("Šansa (0–1) da se stroj pokvari po radnom ciklusu; 0 = nikad. Na nestabilnim planetama množi se s 3.")]
        [Range(0f, 1f)] public float breakdownChancePerCycle = 0.015f;
        [Tooltip("Resursi iz inventara igrača potrebni za popravak (E na polomljenom stroju). Prazno = besplatan popravak.")]
        public ConnectionRequirement[] repairCost;
    }
}
