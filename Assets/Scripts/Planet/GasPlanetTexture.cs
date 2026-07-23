using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Proceduralna tekstura plinovitog diva: horizontalne trake s fbm turbulencijom
    // i jednom velikom olujom, u ljubičastoj paleti postojećeg Planet_Gaseous
    // materijala. Generira se jednom pri prvom plinovitom planetu i dijeli među
    // svima (varijaciju daje nasumična rotacija sfere u PlanetCreatoru). Radi bez
    // izmjena scene ili asseta — isti obrazac kao SpaceSkybox.
    public static class GasPlanetTexture
    {
        const int Width = 512, Height = 256;
        const float Bands = 5f;

        // Paleta oko _BaseColor (0.56, 0.44, 0.83) Planet_Gaseous materijala.
        static readonly Color Deep   = new(0.28f, 0.20f, 0.46f);
        static readonly Color Mid    = new(0.56f, 0.44f, 0.83f);
        static readonly Color Light  = new(0.76f, 0.68f, 0.94f);
        static readonly Color Storm  = new(0.38f, 0.18f, 0.42f);

        static Material _material;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _material = null;

        // Klon baznog materijala s generiranom teksturom; bazni asset se ne dira.
        public static Material GetMaterial(Material baseMaterial)
        {
            if (_material != null) return _material;

            _material = new Material(baseMaterial)
            {
                mainTexture = Generate(),
                // Boje traka su već u teksturi — bazni tint bi ih dodatno zatamnio.
                color = Color.white
            };
            return _material;
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
            // Noise se uzorkuje na cilindru da tekstura horizontalno besprijekorno
            // wrapa (u=0 i u=1 su ista točka na sferi).
            float ang = u * 2f * Mathf.PI;
            Vector3 cyl = new(Mathf.Cos(ang), v * 3.1f, Mathf.Sin(ang));

            // Trake: latituda izobličena fbm-om (turbulencija rubova traka),
            // faza traka dodatno "teče" sporim šumom da ne budu savršeni prstenovi.
            float warp  = SpaceSkybox.Fbm(cyl * 2.4f, 4);
            float lat   = v + (warp - 0.5f) * 0.14f;
            float phase = SpaceSkybox.Fbm(cyl * 0.8f + new Vector3(11.3f, 5.7f, 23.1f), 2);
            float s     = Mathf.Sin(lat * Mathf.PI * 2f * Bands + phase * 2.6f);
            float t     = s * 0.5f + 0.5f;

            // Trostopna paleta: tamno → osnovno → svijetlo.
            Color c = t < 0.5f
                ? Color.Lerp(Deep, Mid, t * 2f)
                : Color.Lerp(Mid, Light, (t - 0.5f) * 2f);

            // Fine pruge/struje unutar traka.
            float streaks = SpaceSkybox.Fbm(cyl * 6.5f + new Vector3(3.1f, 41.7f, 9.2f), 3);
            c *= 0.90f + 0.20f * streaks;

            // Velika oluja: meka elipsa s izobličenim rubom.
            float du = Mathf.Abs(u - 0.30f);
            if (du > 0.5f) du = 1f - du; // wrap po longitudi
            float dv = v - 0.62f;
            float d = Mathf.Sqrt((du * du) / (0.11f * 0.11f) + (dv * dv) / (0.055f * 0.055f));
            d += (SpaceSkybox.Fbm(cyl * 5f, 2) - 0.5f) * 0.5f;
            if (d < 1f)
                c = Color.Lerp(c, Storm, Mathf.SmoothStep(1f, 0.3f, d));

            // Blago zatamnjenje polova (trake se tamo vizualno stišću).
            float cap = Mathf.SmoothStep(0f, 0.12f, Mathf.Min(v, 1f - v));
            c *= 0.75f + 0.25f * cap;

            c.a = 1f;
            return c;
        }
    }
}
