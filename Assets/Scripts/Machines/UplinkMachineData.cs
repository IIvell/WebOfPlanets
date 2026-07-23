using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(fileName = "UplinkMachine", menuName = "Machines/Uplink Machine")]
    public class UplinkMachineData : QuickSlotItem
    {
        public GameObject prefab;

        [Tooltip("Sekunde između dva slanja u Hub storage.")]
        [Min(0.1f)] public float transmitInterval = 5f;

        [Tooltip("Koliko resursa se pošalje u Hub storage po ciklusu.")]
        [Min(1)] public int itemsPerCycle = 2;

        [Header("Kvar")]
        [Tooltip("Šansa (0–1) da se stroj pokvari po radnom ciklusu; 0 = nikad. Na nestabilnim planetama množi se s 3.")]
        [Range(0f, 1f)] public float breakdownChancePerCycle = 0.01f;
        [Tooltip("Resursi iz inventara igrača potrebni za popravak (E na polomljenom stroju). Prazno = besplatan popravak.")]
        public ConnectionRequirement[] repairCost;
    }
}
