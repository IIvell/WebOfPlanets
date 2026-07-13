using System;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(fileName = "ExtractorMachine", menuName = "Machines/Extractor Machine")]
    public class ExtractorMachineData : QuickSlotItem
    {
        [Serializable]
        public struct OutputYield
        {
            public Item item;
            [Min(1)] public int amount;
        }

        public GameObject prefab;

        [Tooltip("Resursi koje stroj proizvodi svaki ciklus — ne trebaju spawnovi na planeti.")]
        public OutputYield[] outputs;

        [Tooltip("Sekunde po ciklusu proizvodnje.")]
        [Min(0.1f)] public float extractionInterval = 15f;

        [Tooltip("Maksimalan broj resursa u internom spremištu; kad je puno, proizvodnja staje.")]
        [Min(1)] public int maxStored = 25;

        [Tooltip("Resursi koji se troše iz Hub storage-a svaki ciklus (prazno = besplatno).")]
        public ConnectionRequirement[] maintenanceCost;
    }
}
