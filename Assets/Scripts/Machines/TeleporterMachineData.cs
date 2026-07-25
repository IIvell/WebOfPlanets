using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(fileName = "TeleporterMachine", menuName = "Machines/Teleporter Machine")]
    public class TeleporterMachineData : QuickSlotItem
    {
        public GameObject prefab;

        [Tooltip("Uniformni world scale vizuala pri postavljanju (oba kraja para).")]
        [Min(0.01f)] public float worldScale = MachineFactory.TeleporterScale;
    }
}
