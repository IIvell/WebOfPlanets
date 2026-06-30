using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(menuName = "Inventory/Tool")]
    public class Tool : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public GameObject prefab;
        [Tooltip("Koliko puta brže se mine s ovim alatom. 2.0 = dvostruko brže.")]
        [Min(1f)] public float miningSpeedMultiplier = 1f;
        [Tooltip("Broj resursa koje alat može skupiti prije nego se pokvari. 0 = beskonačno.")]
        [Min(0)] public int maxDurability = 0;
    }
}
