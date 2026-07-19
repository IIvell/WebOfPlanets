using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Zaštitna oprema — ne postavlja se na planetu i ne troši se korištenjem.
    // Tipka P na odabranom slotu s maskom (MachinePlacer) je stavlja odnosno
    // skida; jednom stavljena ostaje na glavi i kad se odabere drugi slot.
    // Dok se nosi: otrovna atmosfera plinskih planeta ne radi štetu
    // (GasPlanetAtmosphere), a model maske se prikazuje na glavi robota
    // (GasMaskVisual).
    [CreateAssetMenu(fileName = "GasMask", menuName = "Inventory/Gas Mask")]
    public class GasMaskData : QuickSlotItem
    {
        private static GasMaskData _worn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _worn = null;

        [Tooltip("3D model maske koji se prikazuje na glavi robota dok se maska nosi.")]
        public GameObject prefab;
        [Tooltip("Base color tekstura modela — primjenjuje se runtime materijalom na sve renderere vizuala.")]
        public Texture2D baseColorTexture;
        [Tooltip("Normal mapa modela.")]
        public Texture2D normalTexture;

        [Tooltip("Ciljna veličina vizuala maske (najveća dimenzija) — model se auto-skalira na ovu mjeru.")]
        [Min(0.01f)] public float wornSize = 0.5f;
        [Tooltip("Lokalni pomak vizuala od izračunate točke lica robota.")]
        public Vector3 wornPositionOffset = Vector3.zero;
        [Tooltip("Lokalna rotacija (Euler) vizuala na glavi.")]
        public Vector3 wornRotationOffset = Vector3.zero;

        public static void ToggleWorn(GasMaskData mask)
        {
            _worn = _worn == mask ? null : mask;
        }

        public static GasMaskData GetWorn()
        {
            if (_worn == null) return null;

            // Maska koja je izašla iz hotbara više se ne nosi.
            var inv = QuickSlotInventory.current;
            if (inv == null) return null;
            for (int i = 0; i < QuickSlotInventory.SlotCount; i++)
                if (inv.GetSlot(i) == _worn)
                    return _worn;

            _worn = null;
            return null;
        }

        public static bool IsWorn() => GetWorn() != null;
    }
}
