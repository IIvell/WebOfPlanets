using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(menuName = "Machines/Storage Machine")]
    public class StorageMachineData : QuickSlotItem
    {
        [Tooltip("Prefab vizuala storage stroja. Prazno = kocka.")]
        public GameObject prefab;

        [Tooltip("Maksimalan broj resursa u stroju; kad je pun, povezani collector pauzira skupljanje.")]
        [Min(1)] public int capacity = 60;
    }
}
