using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Svemirska pozadina: proceduralni starfield cubemap (zvijezde + suptilna nebula)
    // generiran pri startu i postavljen kao RenderSettings.skybox. Radi bez ikakvih
    // izmjena scene (RuntimeInitializeOnLoadMethod) — vidi feedback o scene YAML editima.
    // Namjerno NE zovemo DynamicGI.UpdateEnvironment() da ambijentalno svjetlo
    // na planetima ostane kakvo je bilo prije zamjene skyboxa.
    public static class SpaceSkybox
    {
        const int FaceSize = 512;

        // Sitne guste zvijezde / krupnije rijetke sjajne zvijezde (skala = gustoća ćelija po sferi).
        const float SmallStarScale = 56f;
        const float BigStarScale = 14f;
        const float SmallStarChance = 0.38f;
        const float BigStarChance = 0.14f;

        static readonly Color BaseSpace = new Color(0.008f, 0.009f, 0.016f);
        static readonly Color NebulaBlue = new Color(0.020f, 0.045f, 0.110f);
        static readonly Color NebulaPurple = new Color(0.075f, 0.025f, 0.105f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            Shader shader = Shader.Find("Skybox/Cubemap");
            if (shader == null)
            {
                Debug.LogWarning("SpaceSkybox: Skybox/Cubemap shader nije dostupan, preskačem.");
                return;
            }

            var mat = new Material(shader);
            mat.SetTexture("_Tex", GenerateCubemap());
            RenderSettings.skybox = mat;

            var cam = Camera.main;
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
        }

        static Cubemap GenerateCubemap()
        {
            var cube = new Cubemap(FaceSize, TextureFormat.RGBA32, false);
            var pixels = new Color[FaceSize * FaceSize];

            foreach (CubemapFace face in new[]
            {
                CubemapFace.PositiveX, CubemapFace.NegativeX,
                CubemapFace.PositiveY, CubemapFace.NegativeY,
                CubemapFace.PositiveZ, CubemapFace.NegativeZ
            })
            {
                for (int y = 0; y < FaceSize; y++)
                {
                    for (int x = 0; x < FaceSize; x++)
                    {
                        Vector3 dir = FaceDirection(face, x, y);
                        pixels[y * FaceSize + x] = SampleSpace(dir);
                    }
                }
                cube.SetPixels(pixels, face);
            }

            cube.Apply(false, true);
            return cube;
        }

        // Smjer pogleda za piksel (x,y) zadanog lica cubemape (Unityjeva left-handed konvencija).
        static Vector3 FaceDirection(CubemapFace face, int x, int y)
        {
            float u = (x + 0.5f) / FaceSize * 2f - 1f;
            float v = (y + 0.5f) / FaceSize * 2f - 1f;

            switch (face)
            {
                case CubemapFace.PositiveX: return new Vector3(1f, -v, -u).normalized;
                case CubemapFace.NegativeX: return new Vector3(-1f, -v, u).normalized;
                case CubemapFace.PositiveY: return new Vector3(u, 1f, v).normalized;
                case CubemapFace.NegativeY: return new Vector3(u, -1f, -v).normalized;
                case CubemapFace.PositiveZ: return new Vector3(u, -v, 1f).normalized;
                default: return new Vector3(-u, -v, -1f).normalized;
            }
        }

        static Color SampleSpace(Vector3 dir)
        {
            // Nebula: jedan noise kanal za intenzitet, drugi (pomaknut) za miješanje boje.
            float n = Fbm(dir * 2.6f, 4);
            float mixNoise = Fbm(dir * 1.9f + new Vector3(31.7f, 47.3f, 12.9f), 3);
            float nebulaAmount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((n - 0.45f) * 2.2f));
            Color nebula = Color.Lerp(NebulaBlue, NebulaPurple, mixNoise);

            Color c = BaseSpace + nebula * nebulaAmount;

            float star = StarLayer(dir, SmallStarScale, SmallStarChance, 0.16f, 1.0f)
                       + StarLayer(dir, BigStarScale, BigStarChance, 0.10f, 1.6f);

            if (star > 0f)
            {
                // Blaga varijacija boje zvijezda: hladno plavkasta do toplo bijela.
                float warm = Hash(Vector3Int.FloorToInt(dir * SmallStarScale), 77);
                Color starColor = Color.Lerp(new Color(0.75f, 0.85f, 1f), new Color(1f, 0.93f, 0.82f), warm);
                c += starColor * star;
            }

            c.r = Mathf.Clamp01(c.r);
            c.g = Mathf.Clamp01(c.g);
            c.b = Mathf.Clamp01(c.b);
            c.a = 1f;
            return c;
        }

        // Jedna zvijezda po ćeliji 3D grida (hash pozicija unutar ćelije, dovoljno od ruba
        // da ne bude odsječena — zato ne treba provjera susjednih ćelija).
        static float StarLayer(Vector3 dir, float scale, float chance, float radius, float intensity)
        {
            Vector3 p = dir * scale;
            Vector3Int cell = Vector3Int.FloorToInt(p);

            if (Hash(cell, 0) > chance) return 0f;

            Vector3 starPos = new Vector3(
                cell.x + 0.2f + 0.6f * Hash(cell, 1),
                cell.y + 0.2f + 0.6f * Hash(cell, 2),
                cell.z + 0.2f + 0.6f * Hash(cell, 3));

            float d = (p - starPos).magnitude;
            if (d > radius) return 0f;

            float falloff = 1f - d / radius;
            float brightness = 0.3f + 0.7f * Hash(cell, 4);
            return falloff * falloff * brightness * intensity;
        }

        // Noise helperi su internal — dijeli ih GasPlanetTexture (isti value-noise obrazac).
        internal static float Hash(Vector3Int cell, int seed)
        {
            unchecked
            {
                int h = cell.x * 73856093 ^ cell.y * 19349663 ^ cell.z * 83492791 ^ seed * (int)2654435761u;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7FFFFFFF) / (float)int.MaxValue;
            }
        }

        // Value noise (trilinearna interpolacija hash vrijednosti na 3D gridu) + fbm oktave.
        internal static float ValueNoise(Vector3 p)
        {
            Vector3Int i = Vector3Int.FloorToInt(p);
            Vector3 f = p - i;
            f = new Vector3(Smooth(f.x), Smooth(f.y), Smooth(f.z));

            float c000 = Hash(i, 9), c100 = Hash(i + new Vector3Int(1, 0, 0), 9);
            float c010 = Hash(i + new Vector3Int(0, 1, 0), 9), c110 = Hash(i + new Vector3Int(1, 1, 0), 9);
            float c001 = Hash(i + new Vector3Int(0, 0, 1), 9), c101 = Hash(i + new Vector3Int(1, 0, 1), 9);
            float c011 = Hash(i + new Vector3Int(0, 1, 1), 9), c111 = Hash(i + new Vector3Int(1, 1, 1), 9);

            float x00 = Mathf.Lerp(c000, c100, f.x);
            float x10 = Mathf.Lerp(c010, c110, f.x);
            float x01 = Mathf.Lerp(c001, c101, f.x);
            float x11 = Mathf.Lerp(c011, c111, f.x);
            float y0 = Mathf.Lerp(x00, x10, f.y);
            float y1 = Mathf.Lerp(x01, x11, f.y);
            return Mathf.Lerp(y0, y1, f.z);
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        internal static float Fbm(Vector3 p, int octaves)
        {
            float sum = 0f, amp = 0.5f, freq = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise(p * freq) * amp;
                norm += amp;
                amp *= 0.5f;
                freq *= 2.1f;
            }
            return sum / norm;
        }
    }
}
