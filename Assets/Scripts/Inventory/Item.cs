using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        public string id;
        public string displayName;
        public GameObject prefab;
        [Min(0f)] public float miningTime = 0f;
        public Vector3 worldScale = Vector3.one;
    }
}
