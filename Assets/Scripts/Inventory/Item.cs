using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public GameObject prefab;
        [Min(0f)] public float miningTime = 0f;
        public Vector3 worldScale = Vector3.one;
        [Tooltip("Alat koji je potreban za minanje ovog resursa. Prazno = bilo koji alat.")]
        public Tool requiredTool;
        [Tooltip("Sekunde do regeneracije resursa. 0 = resurs se trajno uklanja.")]
        [Min(0f)] public float regenerationTime = 0f;
    }
}
