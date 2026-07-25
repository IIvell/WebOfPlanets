using System;
using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
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
}
