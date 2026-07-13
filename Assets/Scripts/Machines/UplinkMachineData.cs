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
    }
}
