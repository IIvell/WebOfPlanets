using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Proceduralna tekstura organskog (nature) planeta: vegetacijske regije od tamne
    // šume do svijetlih livada, jezera s pješčanim obalama i sitni "lisnati" šum.
    // Isti obrazac kao GasPlanetTexture/RockPlanetTexture: noise na cilindru (bez
    // šava), jedna dijeljena tekstura, klon materijala — bez izmjena scene i asseta.
    public static class OrganicPlanetTexture
    {
        const int Width = 512, Height = 256;

        // Razina "mora": ispod ovog praga regionalnog šuma je voda.
        const float WaterLevel = 0.38f;
        const float ShoreWidth = 0.045f;

        static readonly Color DeepWater  = new(0.09f, 0.28f, 0.36f);
        static readonly Color Shallow    = new(0.18f, 0.46f, 0.50f);
        static readonly Color Sand       = new(0.62f, 0.57f, 0.36f);
        static readonly Color DeepForest = new(0.09f, 0.27f, 0.13f);
        static readonly Color Forest     = new(0.19f, 0.43f, 0.19f);
        static readonly Color Meadow     = new(0.47f, 0.62f, 0.28f);

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

            // Regionalni šum crta "kontinente" vegetacije i jezera; rub jezera se
            // dodatno mreška sitnijim šumom da obale ne budu glatke elipse.
            float region = SpaceSkybox.Fbm(cyl * 1.8f, 4)
                         + (SpaceSkybox.Fbm(cyl * 5.5f + new Vector3(13.7f, 7.9f, 29.3f), 3) - 0.5f) * 0.12f;

            if (region < WaterLevel)
            {
                // Voda: dublje prema sredini jezera, uz blago mreškanje površine.
                float depth = Mathf.Clamp01((WaterLevel - region) / WaterLevel * 2.2f);
                Color water = Color.Lerp(Shallow, DeepWater, depth);
                float ripple = SpaceSkybox.Fbm(cyl * 8f + new Vector3(41.1f, 3.3f, 17.7f), 2);
                water *= 0.94f + 0.12f * ripple;
                water.a = 1f;
                return water;
            }

            if (region < WaterLevel + ShoreWidth)
            {
                // Pješčana obala: uski pojas između vode i vegetacije.
                float t0 = (region - WaterLevel) / ShoreWidth;
                Color shore = Color.Lerp(Sand, Forest, Mathf.SmoothStep(0.35f, 1f, t0));
                shore.a = 1f;
                return shore;
            }

            // Vegetacija: gustoća šume iz regije + zaseban šum za livadne proplanke.
            float density = Mathf.Clamp01((region - WaterLevel) / (1f - WaterLevel));
            float glade = SpaceSkybox.Fbm(cyl * 3.4f + new Vector3(5.1f, 23.9f, 11.3f), 3);
            float t = Mathf.Clamp01(0.55f * (1f - density) + 0.45f * glade);

            Color c = t < 0.5f
                ? Color.Lerp(DeepForest, Forest, t * 2f)
                : Color.Lerp(Forest, Meadow, (t - 0.5f) * 2f);

            // Sitni "lisnati" šum — krošnje/tlo, da površina ne bude plastična.
            float foliage = SpaceSkybox.Fbm(cyl * 9f + new Vector3(2.7f, 37.1f, 8.9f), 3);
            c *= 0.88f + 0.24f * foliage;

            c.a = 1f;
            return c;
        }
    }
}
