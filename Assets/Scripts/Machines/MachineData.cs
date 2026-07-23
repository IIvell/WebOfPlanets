using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
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
}
