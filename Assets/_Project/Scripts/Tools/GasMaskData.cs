using UnityEngine;

namespace WebOfPlanets
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
            var inv = QuickSlotInventory.Instance;
            if (inv == null) return null;
            for (int i = 0; i < QuickSlotInventory.SlotCount; i++)
                if (inv.GetSlot(i) == _worn)
                    return _worn;

            _worn = null;
            return null;
        }

        public static bool IsWorn() => GetWorn() != null;
    }

    // Premješteno iz GasMaskVisual.cs (konsolidacija malih datoteka, 31. 7. 2026.).
    // Prikazuje model gas maske na licu robota dok igrač nosi masku (dok je u
    // hotbaru). Samoinicijalizira se pri pokretanju umjesto scene objekta —
    // editor drži scenu u memoriji pa disk izmjene scene ne prežive.
    public class GasMaskVisual : MonoBehaviour
    {
        private const float CheckInterval = 0.25f;

        private PlayerController _player;
        private GasMaskData _worn;
        private GameObject _visual;
        private float _nextCheckTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<GasMaskVisual>() != null) return;
            new GameObject("GasMaskVisual").AddComponent<GasMaskVisual>();
        }

        void Update()
        {
            if (Time.time < _nextCheckTime) return;
            _nextCheckTime = Time.time + CheckInterval;

            GasMaskData worn = GasMaskData.GetWorn();
            if (worn == _worn && (_visual != null) == (worn != null)) return;
            _worn = worn;

            if (_visual != null)
            {
                Destroy(_visual);
                _visual = null;
            }
            if (worn == null || worn.prefab == null) return;

            if (_player == null)
                _player = FindFirstObjectByType<PlayerController>();
            if (_player == null) return; // player još nije u sceni — retry sljedeći tick

            AttachVisual(worn);
        }

        private void AttachVisual(GasMaskData mask)
        {
            Transform anchor = _player.VisualModel != null ? _player.VisualModel : _player.transform;

            // Lice = prednja strana GLAVE: mjeri se samo gornjih 30% geometrije
            // robota, jer min.z cijelog modela hvata kliješta koja strše naprijed
            // pa maska lebdi ispred lica. Mjeri se PRIJE dodavanja vizuala da maska
            // ne uđe u vlastiti izračun. Lice robota je na -z strani visualModela.
            Vector3 faceLocal = new Vector3(0f, 1.5f, -0.3f);
            if (SurfacePlacement.TryGetLocalBoundsAbove(anchor.gameObject, 0.7f, out Bounds head))
                faceLocal = new Vector3(head.center.x, head.center.y - head.size.y * 0.15f, head.min.z);

            _visual = Instantiate(mask.prefab, anchor);

            // Isti tretman kao vizual alata u ruci: bez kolajdera i interakcija.
            foreach (var col in _visual.GetComponentsInChildren<Collider>())
                col.enabled = false;
            foreach (var interactable in _visual.GetComponentsInChildren<BaseInteractable>())
                Destroy(interactable);

            ApplyTextures(mask);

            // Modeli dolaze u raznim mjerilima — normaliziraj najveću dimenziju na wornSize.
            float scale = 1f;
            if (SurfacePlacement.TryGetLocalBounds(_visual, out Bounds b))
            {
                float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (maxDim > 0.0001f) scale = mask.wornSize / maxDim;
            }

            _visual.transform.localScale = Vector3.one * scale;
            _visual.transform.localRotation = Quaternion.Euler(mask.wornRotationOffset);
            _visual.transform.localPosition = faceLocal + mask.wornPositionOffset;
        }

        private void ApplyTextures(GasMaskData mask)
        {
            if (mask.baseColorTexture == null && mask.normalTexture == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            bool urp = shader != null;
            if (!urp) shader = Shader.Find("Standard");
            if (shader == null) return;

            var mat = new Material(shader);
            if (mask.baseColorTexture != null)
                mat.SetTexture(urp ? "_BaseMap" : "_MainTex", mask.baseColorTexture);
            if (mask.normalTexture != null)
            {
                mat.SetTexture("_BumpMap", mask.normalTexture);
                mat.EnableKeyword("_NORMALMAP");
            }

            foreach (var renderer in _visual.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = mat;
        }
    }
}
