using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Proceduralna tekstura kamenog (Mining) planeta: višeslojno kamenje s tamnim
    // rudnim žilama i ponekim kraterom, u rđastoj paleti dosadašnje venus fotke.
    // Zamjenjuje venus-surface1.jpeg koja nije tileabilna pa se na UV šavu sfere
    // vidjela crta — noise na cilindru wrapa besprijekorno. Isti obrazac kao
    // GasPlanetTexture: jedna dijeljena tekstura, bez izmjena scene i asseta.
    public static class RockPlanetTexture
    {
        const int Width = 512, Height = 256;

        static readonly Color DarkRock  = new(0.25f, 0.16f, 0.11f);
        static readonly Color Rock      = new(0.50f, 0.34f, 0.22f);
        static readonly Color LightRock = new(0.74f, 0.58f, 0.42f);
        static readonly Color Vein      = new(0.15f, 0.11f, 0.09f);

        // Tekstura se generira jednom; klon materijala po baznom materijalu
        // (PlanetCreator i hub Planet.Awake mogu proslijediti različite bazne).
        static readonly System.Collections.Generic.Dictionary<Material, Material> _materials = new();
        static Texture2D _texture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _materials.Clear(); _texture = null; }

        // Klon baznog materijala s generiranom teksturom; bazni asset se ne dira.
        public static Material GetMaterial(Material baseMaterial)
        {
            if (_materials.TryGetValue(baseMaterial, out Material cached) && cached != null)
                return cached;

            if (_texture == null) _texture = Generate();

            var material = new Material(baseMaterial)
            {
                mainTexture = _texture,
                color = Color.white
            };
            _materials[baseMaterial] = material;
            return material;
        }

        static Texture2D Generate()
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, true)
            {
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear
            };

            var pixels = new Color[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                float v = (y + 0.5f) / Height;
                for (int x = 0; x < Width; x++)
                {
                    float u = (x + 0.5f) / Width;
                    pixels[y * Width + x] = Sample(u, v);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        static Color Sample(float u, float v)
        {
            float ang = u * 2f * Mathf.PI;
            Vector3 cyl = new(Mathf.Cos(ang), v * 3f, Mathf.Sin(ang));

            // Velike regije (svjetlija/tamnija područja) + sitna kamena struktura.
            float region = SpaceSkybox.Fbm(cyl * 1.6f, 3);
            float detail = SpaceSkybox.Fbm(cyl * 6f + new Vector3(7.3f, 19.1f, 3.7f), 4);
            float t = Mathf.Clamp01(0.55f * region + 0.45f * detail);

            Color c = t < 0.5f
                ? Color.Lerp(DarkRock, Rock, t * 2f)
                : Color.Lerp(Rock, LightRock, (t - 0.5f) * 2f);

            // Rudne žile: ridged noise (1-|2n-1|) je visok duž tankih linija.
            float ridge = 1f - Mathf.Abs(2f * SpaceSkybox.Fbm(cyl * 3.2f + new Vector3(31.7f, 2.9f, 15.3f), 4) - 1f);
            if (ridge > 0.80f)
                c = Color.Lerp(c, Vein, Mathf.SmoothStep(0.80f, 0.95f, ridge) * 0.85f);

            // Poneki krater: tamno dno, svijetli rub.
            float crater = Crater(cyl * 9f);
            if (crater < 0f) c *= 1f + 0.45f * crater;
            else if (crater > 0f) c = Color.Lerp(c, LightRock, crater * 0.6f);

            c.a = 1f;
            return c;
        }

        // Jedan mogući krater po ćeliji 3D grida (isti scatter obrazac kao
        // SpaceSkybox.StarLayer). Vraća <0 za dno, >0 za rub, 0 izvan kratera.
        static float Crater(Vector3 p)
        {
            Vector3Int cell = Vector3Int.FloorToInt(p);
            if (SpaceSkybox.Hash(cell, 51) > 0.16f) return 0f;

            Vector3 center = new(
                cell.x + 0.3f + 0.4f * SpaceSkybox.Hash(cell, 52),
                cell.y + 0.3f + 0.4f * SpaceSkybox.Hash(cell, 53),
                cell.z + 0.3f + 0.4f * SpaceSkybox.Hash(cell, 54));

            float radius = 0.16f + 0.18f * SpaceSkybox.Hash(cell, 55);
            float d = (p - center).magnitude;
            if (d > radius) return 0f;

            float x = d / radius;
            return x < 0.7f
                ? -(1f - x / 0.7f)                          // dno: 0 → -1 prema centru
                : Mathf.Sin((x - 0.7f) / 0.3f * Mathf.PI);  // rub: mekani svijetli prsten
        }
    }
}
