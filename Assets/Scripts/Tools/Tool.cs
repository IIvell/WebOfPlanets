using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(menuName = "Inventory/Tool")]
    public class Tool : QuickSlotItem
    {
        public enum ToolClass { Mining, Woodcutting }

        public string id;
        public GameObject prefab;
        [Tooltip("Koliko puta brže se mine s ovim alatom. 2.0 = dvostruko brže.")]
        [Min(1f)] public float miningSpeedMultiplier = 1f;
        [Tooltip("Broj resursa koje alat može skupiti prije nego se pokvari. 0 = beskonačno.")]
        [Min(0)] public int maxDurability = 0;
        [Tooltip("Rang alata za kopanje — alat moze kopati sve resurse ciji requiredTool ima manji ili jednak rang unutar iste klase alata.")]
        [Min(0)] public int miningTier = 0;
        [Tooltip("Klasa alata — resurs mogu skupljati samo alati iste klase kao njegov requiredTool (npr. drvo traži Woodcutting, ruda Mining).")]
        public ToolClass toolClass = ToolClass.Mining;

        [Tooltip("Boja kojom se tonira vizual alata u ruci (bijelo = originalne boje prefaba).")]
        public Color tintColor = Color.white;

        [Tooltip("Lokalni pomak vizuala alata od hold pointa (za centriranje u ruci).")]
        public Vector3 holdPositionOffset = Vector3.zero;
        [Tooltip("Lokalna rotacija (Euler) vizuala alata na hold pointu.")]
        public Vector3 holdRotationOffset = Vector3.zero;
        [Tooltip("Scale vizuala alata na hold pointu.")]
        [Min(0.01f)] public float holdScale = 5f;
    }
}
