using System.IO;
using UnityEditor;
using UnityEngine;

namespace WebOfPlanets
{
    // Dorada vizuala, srpanj 2026.: vegetacijski (Organic) planet je izgledao
    // mutno iz dva razloga koja ovaj alat rješava jednim menu itemom:
    //
    // 1. QonoS ground teksture su na disku 4096x2048, ali importer ih je rezao
    //    na maxTextureSize 2048 — pola rezolucije bačeno. Alat diže limit na
    //    4096 (importer API umjesto ručnog editiranja .meta datoteka).
    // 2. I 4K raspoređen po cijelom planetu izbliza je mutan (~13 px po world
    //    jedinici na planetu radijusa 50). URP Lit "Detail Inputs" to rješava:
    //    sitno tileano platno (48x24 ponavljanja) množi bazu izbliza, a na
    //    daljinu ga mipovi utope. Detail albedo + normal se generiraju ovdje
    //    proceduralno (periodični value-noise fbm, bešavan po definiciji) pa
    //    nema vanjskih asseta; oko 0.5 sive jer _DETAIL_MULX2 množi s 2x.
    //
    // Pokreće se iz Tools/Web of Planets menija; idempotentno (ponovno
    // pokretanje samo prepiše iste vrijednosti).
    public static class PlanetTextureEnhancer
    {
        private const int UncappedSize = 4096;
        private const int DetailSize = 1024;
        private const int DetailBasePeriod = 8;   // krupnoća najkrupnije oktave
        private const float DetailContrast = 0.35f;
        private const float NormalStrength = 2.2f;
        // Sferni UV: U pokriva 360°, V samo 180° — U tiling 2x V drži texele kvadratnima.
        private static readonly Vector2 DetailTiling = new(48f, 24f);

        private static readonly string[] GroundTexturePaths =
        {
            "Assets/ThirdParty/PlanetModels/green_textures/QonoS_Ground_Diff.png",
            "Assets/ThirdParty/PlanetModels/green_textures/QonoS_Ground_Diff_NoIce.png",
            "Assets/ThirdParty/PlanetModels/green_textures/QonoS_Ground_Normal.png",
            "Assets/ThirdParty/PlanetModels/green_textures/QonoS_Ground_Normal_NoIce.png",
            "Assets/ThirdParty/PlanetModels/green_textures/QonoS_Ground_Emit.png",
            "Assets/ThirdParty/PlanetModels/green_textures/QonoS_Ground_Emit_NoIce.png",
            "Assets/ThirdParty/PlanetModels/green_textures/QonoS_Ground_MetallicSmoothness.png",
        };

        private const string DetailAlbedoPath = "Assets/_Project/Art/Textures/T_PlanetDetail_Albedo.png";
        private const string DetailNormalPath = "Assets/_Project/Art/Textures/T_PlanetDetail_Normal.png";
        private const string OrganicMaterialPath = "Assets/_Project/Art/Materials/M_Planet_Organic.mat";

        [MenuItem("Tools/Web of Planets/Poboljšaj teksture planeta (Organic)")]
        public static void Enhance()
        {
            int uncapped = UncapImportSizes();
            GenerateDetailTextures();
            WireDetailIntoMaterial();

            Debug.Log($"[PlanetTextureEnhancer] Gotovo: {uncapped} tekstura odčepljeno na {UncappedSize}, " +
                      $"detail mape generirane ({DetailSize}px, tiling {DetailTiling.x}x{DetailTiling.y}) " +
                      "i spojene u M_Planet_Organic.");
        }

        // ── 1. Import limit ──────────────────────────────────────────────────

        private static int UncapImportSizes()
        {
            int changed = 0;
            foreach (string path in GroundTexturePaths)
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    Debug.LogWarning($"[PlanetTextureEnhancer] Nema TextureImportera na: {path}");
                    continue;
                }
                if (importer.maxTextureSize >= UncappedSize) continue;

                importer.maxTextureSize = UncappedSize;
                importer.SaveAndReimport();
                changed++;
            }
            return changed;
        }

        // ── 2. Generiranje detail mapa ───────────────────────────────────────

        private static void GenerateDetailTextures()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DetailAlbedoPath)!);

            // Zajedničko visinsko polje za albedo i normal — detalj tako
            // "svjetli na izbočinama", umjesto dvije nepovezane šare.
            float[] height = new float[DetailSize * DetailSize];
            for (int y = 0; y < DetailSize; y++)
            {
                float fy = (float)y / DetailSize * DetailBasePeriod;
                for (int x = 0; x < DetailSize; x++)
                {
                    float fx = (float)x / DetailSize * DetailBasePeriod;
                    height[y * DetailSize + x] = Fbm(fx, fy, DetailBasePeriod, seed: 1337);
                }
            }

            WriteAlbedo(height);
            WriteNormal(height);

            AssetDatabase.ImportAsset(DetailAlbedoPath);
            AssetDatabase.ImportAsset(DetailNormalPath);
            ConfigureDetailImporter(DetailAlbedoPath, isNormal: false);
            ConfigureDetailImporter(DetailNormalPath, isNormal: true);
        }

        private static void WriteAlbedo(float[] height)
        {
            var pixels = new Color32[DetailSize * DetailSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                // Oko 0.5 (neutralno za MULX2), kontrast drži mrlje suptilnima;
                // blagi zeleni pomak da detalj ne posivi vegetaciju.
                float v = 0.5f + (height[i] - 0.5f) * DetailContrast;
                byte r = ToByte(v * 0.98f);
                byte g = ToByte(v * 1.03f);
                byte b = ToByte(v * 0.94f);
                pixels[i] = new Color32(r, g, b, 255);
            }
            WritePng(DetailAlbedoPath, pixels);
        }

        private static void WriteNormal(float[] height)
        {
            var pixels = new Color32[DetailSize * DetailSize];
            for (int y = 0; y < DetailSize; y++)
            {
                for (int x = 0; x < DetailSize; x++)
                {
                    // Centralne razlike s wrapom — normal mapa ostaje bešavna.
                    float hl = height[y * DetailSize + (x - 1 + DetailSize) % DetailSize];
                    float hr = height[y * DetailSize + (x + 1) % DetailSize];
                    float hd = height[((y - 1 + DetailSize) % DetailSize) * DetailSize + x];
                    float hu = height[((y + 1) % DetailSize) * DetailSize + x];

                    Vector3 n = new Vector3((hl - hr) * NormalStrength, (hd - hu) * NormalStrength, 1f).normalized;
                    pixels[y * DetailSize + x] = new Color32(
                        ToByte(n.x * 0.5f + 0.5f), ToByte(n.y * 0.5f + 0.5f), ToByte(n.z * 0.5f + 0.5f), 255);
                }
            }
            WritePng(DetailNormalPath, pixels);
        }

        private static void WritePng(string path, Color32[] pixels)
        {
            var tex = new Texture2D(DetailSize, DetailSize, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureDetailImporter(string path, bool isNormal)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;

            importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.maxTextureSize = DetailSize;
            importer.mipmapEnabled = true; // mipovi utapaju detalj na daljinu — bez njih tiling treperi
            importer.SaveAndReimport();
        }

        // ── 3. Spajanje u materijal ──────────────────────────────────────────

        private static void WireDetailIntoMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OrganicMaterialPath);
            if (mat == null)
            {
                Debug.LogWarning($"[PlanetTextureEnhancer] Nema materijala na: {OrganicMaterialPath}");
                return;
            }

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(DetailAlbedoPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(DetailNormalPath);

            mat.SetTexture("_DetailAlbedoMap", albedo);
            mat.SetTexture("_DetailNormalMap", normal);
            // URP Lit detail UV čita _DetailAlbedoMap_ST za OBJE detail mape.
            mat.SetTextureScale("_DetailAlbedoMap", DetailTiling);
            mat.SetFloat("_DetailAlbedoMapScale", 1f);
            mat.SetFloat("_DetailNormalMapScale", 1f);
            mat.EnableKeyword("_DETAIL_MULX2");

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
        }

        // ── Periodični value-noise (bešavan tile po konstrukciji) ────────────

        private static float Fbm(float x, float y, int period, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int octave = 0; octave < 5; octave++)
            {
                sum += amp * ValueNoise(x, y, period, seed + octave * 101);
                norm += amp;
                x *= 2f; y *= 2f; period *= 2; amp *= 0.5f;
            }
            return sum / norm;
        }

        private static float ValueNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = Smooth(x - x0), ty = Smooth(y - y0);

            float v00 = Hash01(Wrap(x0, period), Wrap(y0, period), seed);
            float v10 = Hash01(Wrap(x0 + 1, period), Wrap(y0, period), seed);
            float v01 = Hash01(Wrap(x0, period), Wrap(y0 + 1, period), seed);
            float v11 = Hash01(Wrap(x0 + 1, period), Wrap(y0 + 1, period), seed);

            return Mathf.Lerp(Mathf.Lerp(v00, v10, tx), Mathf.Lerp(v01, v11, tx), ty);
        }

        private static int Wrap(int v, int period) => (v % period + period) % period;

        private static float Smooth(float t) => t * t * t * (t * (t * 6f - 15f) + 10f); // quintic

        private static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return h / 4294967295f;
            }
        }

        private static byte ToByte(float v) => (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
    }
}
