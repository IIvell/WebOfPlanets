using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(menuName = "Machines/Storage Machine")]
    public class StorageMachineData : ScriptableObject
    {
        public string displayName = "Storage Machine";
        [Tooltip("Prefab vizuala storage stroja. Prazno = kocka.")]
        public GameObject prefab;
    }
}
