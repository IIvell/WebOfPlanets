using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    [CreateAssetMenu(menuName = "Inventory/Item")]
    public class Item : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        [Tooltip("Prefab za pickup verziju resursa (instant, bez mining timera).")]
        public GameObject pickupPrefab;
        [Tooltip("Prefab za mining verziju resursa (zahtijeva mining timer i, opcionalno, alat).")]
        public GameObject miningPrefab;
        [Min(0f)] public float miningTime = 0f;
        public Vector3 miningWorldScale = Vector3.one;
        public Vector3 pickupWorldScale = Vector3.one;
        [Tooltip("Alat koji je potreban za minanje ovog resursa. Prazno = bilo koji alat.")]
        public Tool requiredTool;
        [Tooltip("Sekunde do regeneracije resursa. 0 = resurs se trajno uklanja.")]
        [Min(0f)] public float regenerationTime = 0f;
        [Tooltip("Minimalan broj komada ovog itema koji se dobije po jednom minanju.")]
        [Min(1)] public int minMiningYield = 1;
        [Tooltip("Maksimalan broj komada ovog itema koji se dobije po jednom minanju.")]
        [Min(1)] public int maxMiningYield = 1;
        [Tooltip("Dodatni item koji postoji šansa da se dobije uz glavni item pri minanju (npr. Iron unutar Stone-a).")]
        public Item bonusMiningItem;
        [Tooltip("Vjerojatnost (0-1) da se bonusMiningItem dobije po minanju.")]
        [Range(0f, 1f)] public float bonusMiningChance = 0f;
        [Tooltip("Lokalna os prefaba koja pri postavljanju na planet treba gledati suprotno od centra planeta (\"gore\" u mesh prostoru). Standardno Y, ali neki modeli imaju drugu os kao \"gore\".")]
        public Vector3 surfaceUpAxis = Vector3.up;
        [Tooltip("Uključi ako model ima pivot u sredini mesha umjesto na dnu, pa bi inače pola njega završilo ukopano u planet.")]
        public bool pivotAtMeshCenter = false;
    }
}
