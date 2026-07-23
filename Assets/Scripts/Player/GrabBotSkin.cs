using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Tekstura za Grab-Bot robota (playable character). FBX ima UV-e, ali samo
    // jedan materijal bez teksture (siva "FrontColor"), pa robot izgleda golo.
    // UV raspored je auto-unwrap s nepoznatim otocima — zato tekstura koristi
    // samo sitne uzorke (brušeni metal, mrlje trošenja) koji dobro čitaju na
    // bilo kakvim UV-ima; krupni paneli/naljepnice bi se raspali po šavovima.
    // Primjena pri startu bez izmjena scene (RuntimeInitializeOnLoadMethod) —
    // vidi feedback o scene YAML editima.
    public static class GrabBotSkin
    {
        const int Size = 256;

        static readonly Color PaintLight = new Color(0.78f, 0.77f, 0.74f);
        static readonly Color PaintDark = new Color(0.55f, 0.56f, 0.60f);
        static readonly Color WearColor = new Color(0.32f, 0.30f, 0.28f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var controller = Object.FindFirstObjectByType<PlayerController>();
            Transform visual = controller != null ? controller.VisualModel : null;
            if (visual == null)
            {
                Debug.LogWarning("GrabBotSkin: PlayerController/VisualModel nije pronađen, preskačem.");
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            bool urp = shader != null;
            if (!urp) shader = Shader.Find("Standard");
            if (shader == null) return;

            var mat = new Material(shader);
            mat.SetTexture(urp ? "_BaseMap" : "_MainTex", GenerateTexture());
            if (urp)
            {
                mat.SetFloat("_Metallic", 0.35f);
                mat.SetFloat("_Smoothness", 0.45f);
            }

            // U ovom trenutku (prije Starta/opremanja alata) su pod vizualom samo
            // rendereri robota, pa alat u ruci kasnije zadrži svoj materijal.
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = mat;
        }

        static Texture2D GenerateTexture()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            var pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size;
                    float v = (y + 0.5f) / Size;

                    // Krupnija varijacija tona (kao ploče različitih šarži lima)
                    // + horizontalno razvučen šum za dojam brušenog metala.
                    float panel = Fbm(u * 7f, v * 7f, 3);
                    float brush = Fbm(u * 60f, v * 6f, 2);

                    Color c = Color.Lerp(PaintLight, PaintDark,
                        Mathf.Clamp01(panel * 0.7f + (brush - 0.5f) * 0.35f));

                    // Rijetke tamne točkice/ogrebotine — istrošenost od rada.
                    float wear = Fbm(u * 90f, v * 90f, 2);
                    if (wear > 0.62f)
                        c = Color.Lerp(c, WearColor, Mathf.Clamp01((wear - 0.62f) * 4f));

                    pixels[y * Size + x] = new Color(c.r, c.g, c.b, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        // 2D value noise + fbm (tileable nije nužan — UV otoci ionako ne diraju rubove).
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663 ^ seed * (int)2654435761u;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7FFFFFFF) / (float)int.MaxValue;
            }
        }

        static float ValueNoise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float c00 = Hash(ix, iy, 5), c10 = Hash(ix + 1, iy, 5);
            float c01 = Hash(ix, iy + 1, 5), c11 = Hash(ix + 1, iy + 1, 5);
            return Mathf.Lerp(Mathf.Lerp(c00, c10, fx), Mathf.Lerp(c01, c11, fx), fy);
        }

        static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f, amp = 0.5f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(x * freq, y * freq) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2.1f;
            }
            return sum / norm;
        }
    }
}
