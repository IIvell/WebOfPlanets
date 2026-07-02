using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(menuName = "Machines/Collector Machine")]
    public class MachineData : QuickSlotItem
    {
        [Tooltip("Prefab vizuala stroja. Prazno = sfera.")]
        public GameObject prefab;

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
    }
}
