using System;
using System.Collections.Generic;
using UnityEngine;

namespace WebOfPlanets
{
    // Zajednički dio tri proceduralne planet-teksture (Gas/Rock/Organic): petlja
    // generiranja je bila byte-identična u sve tri klase, a keš materijala je u
    // Gas verziji tiho divergirao (jedan statički materijal umjesto klona po
    // baznom materijalu — točno zamka koju je RockPlanetTexture komentar opisao).
    // Svaka tekstura zadržava samo svoj Sample(u, v) i vlastite statike keša.
    internal static class PlanetTextureUtil
    {
        public const int Width = 512, Height = 256;

        // RGBA32 s mip lancem, horizontalni wrap (u=0 i u=1 su ista točka na
        // sferi), vertikalni clamp, trilinear; sample(u, v) daje boju piksela.
        public static Texture2D Generate(Func<float, float, Color> sample)
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
                    pixels[y * Width + x] = sample(u, v);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        // Klon baznog materijala s generiranom teksturom, keširan PO BAZNOM
        // materijalu (PlanetCreator i hub Planet.Awake mogu proslijediti
        // različite bazne); tekstura se generira jednom i dijeli. Bazni asset se
        // ne dira; color = white jer su boje već u teksturi (bazni tint bi ih
        // dodatno zatamnio).
        public static Material GetMaterial(Dictionary<Material, Material> cache,
            ref Texture2D texture, Material baseMaterial, Func<float, float, Color> sample)
        {
            if (cache.TryGetValue(baseMaterial, out Material cached) && cached != null)
                return cached;

            if (texture == null) texture = Generate(sample);

            var material = new Material(baseMaterial)
            {
                mainTexture = texture,
                color = Color.white
            };
            cache[baseMaterial] = material;
            return material;
        }
    }

    // ── Tri Sample implementacije ─────────────────────────────────────────────
    // Konsolidirane iz GasPlanetTexture.cs / RockPlanetTexture.cs /
    // OrganicPlanetTexture.cs (čišćenje malih datoteka, srpanj 2026.). Statični
    // razredi bez serijalizacije — ime datoteke za njih nije nosivo.

    // Proceduralna tekstura plinovitog diva: horizontalne trake s fbm turbulencijom
    // i jednom velikom olujom, u ljubičastoj paleti postojećeg Planet_Gaseous
    // materijala. Generira se jednom pri prvom plinovitom planetu i dijeli među
    // svima (varijaciju daje nasumična rotacija sfere u PlanetCreatoru). Radi bez
    // izmjena scene ili asseta — isti obrazac kao SpaceSkybox.
    public static class GasPlanetTexture
    {
        const float Bands = 5f;

        // Paleta oko _BaseColor (0.56, 0.44, 0.83) Planet_Gaseous materijala.
        static readonly Color Deep   = new(0.28f, 0.20f, 0.46f);
        static readonly Color Mid    = new(0.56f, 0.44f, 0.83f);
        static readonly Color Light  = new(0.76f, 0.68f, 0.94f);
        static readonly Color Storm  = new(0.38f, 0.18f, 0.42f);

        static readonly Dictionary<Material, Material> _materials = new();
        static Texture2D _texture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _materials.Clear(); _texture = null; }

        // Klon baznog materijala s generiranom teksturom; bazni asset se ne dira.
        public static Material GetMaterial(Material baseMaterial)
            => PlanetTextureUtil.GetMaterial(_materials, ref _texture, baseMaterial, Sample);

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

    // Proceduralna tekstura kamenog (Mining) planeta: višeslojno kamenje s tamnim
    // rudnim žilama i ponekim kraterom, u rđastoj paleti dosadašnje venus fotke.
    // Zamjenjuje venus-surface1.jpeg koja nije tileabilna pa se na UV šavu sfere
    // vidjela crta — noise na cilindru wrapa besprijekorno. Isti obrazac kao
    // GasPlanetTexture: jedna dijeljena tekstura, bez izmjena scene i asseta.
    public static class RockPlanetTexture
    {
        static readonly Color DarkRock  = new(0.25f, 0.16f, 0.11f);
        static readonly Color Rock      = new(0.50f, 0.34f, 0.22f);
        static readonly Color LightRock = new(0.74f, 0.58f, 0.42f);
        static readonly Color Vein      = new(0.15f, 0.11f, 0.09f);

        static readonly Dictionary<Material, Material> _materials = new();
        static Texture2D _texture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _materials.Clear(); _texture = null; }

        // Klon baznog materijala s generiranom teksturom; bazni asset se ne dira.
        public static Material GetMaterial(Material baseMaterial)
            => PlanetTextureUtil.GetMaterial(_materials, ref _texture, baseMaterial, Sample);

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

    // Proceduralna tekstura organskog (nature) planeta: vegetacijske regije od tamne
    // šume do svijetlih livada, jezera s pješčanim obalama i sitni "lisnati" šum.
    // Isti obrazac kao GasPlanetTexture/RockPlanetTexture: noise na cilindru (bez
    // šava), jedna dijeljena tekstura, klon materijala — bez izmjena scene i asseta.
    public static class OrganicPlanetTexture
    {
        // Razina "mora": ispod ovog praga regionalnog šuma je voda.
        const float WaterLevel = 0.38f;
        const float ShoreWidth = 0.045f;

        static readonly Color DeepWater  = new(0.09f, 0.28f, 0.36f);
        static readonly Color Shallow    = new(0.18f, 0.46f, 0.50f);
        static readonly Color Sand       = new(0.62f, 0.57f, 0.36f);
        static readonly Color DeepForest = new(0.09f, 0.27f, 0.13f);
        static readonly Color Forest     = new(0.19f, 0.43f, 0.19f);
        static readonly Color Meadow     = new(0.47f, 0.62f, 0.28f);

        static readonly Dictionary<Material, Material> _materials = new();
        static Texture2D _texture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _materials.Clear(); _texture = null; }

        // Klon baznog materijala s generiranom teksturom; bazni asset se ne dira.
        public static Material GetMaterial(Material baseMaterial)
            => PlanetTextureUtil.GetMaterial(_materials, ref _texture, baseMaterial, Sample);

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
