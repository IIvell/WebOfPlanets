using UnityEngine;
using UnityEditor;

namespace WebOfPlanets
{
    // Priprema UI sprite-ova iz SunGraphica "Game UI collection FREE version"
    // (itch.io, besplatan pack) za runtime upotrebu kroz UiTheme.
    //
    // Zašto ovako: UI se u projektu gradi iz koda (runtime bootstrap obrazac),
    // pa sprite-ovi moraju biti dohvatljivi kroz Resources.Load po imenu — isti
    // razlog zbog kojeg su Machines/Resources i Prefabs/Resources tamo gdje jesu.
    // Ovaj alat kopira odabrane PNG-ove u _Project/Art/UI/Resources/UISprites/
    // sa stabilnim imenima (bez boje u imenu — promjena sheme boja ne smije
    // tražiti preimenovanje) i postavlja TextureImporter na Sprite (2D and UI)
    // s 9-slice borderom gdje treba. Idempotentno: ponovno pokretanje samo
    // ponovno primijeni import postavke.
    public static class UiSpriteSetup
    {
        private const string SourceRoot = "Assets/_Project/Art/UI/Game UI collection FREE version/PNG";
        private const string DestFolder = "Assets/_Project/Art/UI/Resources/UISprites";

        private struct Entry
        {
            public string source;   // relativno od SourceRoot
            public string name;     // ime u Resources/UISprites (bez ekstenzije)
            public Vector4 border;  // 9-slice (L, B, R, T); zero = bez slicea

            public Entry(string source, string name, Vector4 border)
            {
                this.source = source;
                this.name = name;
                this.border = border;
            }
        }

        // Plava shema (odabir 1.8.2026.), bijeli bar fill da ga runtime može
        // tintati bojama zdravlja, žuti okvir za upozorenja (AlertsUI).
        private static readonly Entry[] Entries =
        {
            new Entry("button/Blue/1x/Asset 16.png",              "ui_frame",      new Vector4(24f, 24f, 24f, 24f)),
            new Entry("button/Yellow/1x/Asset 16.png",            "ui_frame_warn", new Vector4(24f, 24f, 24f, 24f)),
            new Entry("button/Blue/1x/Asset 17.png",              "ui_slot",       Vector4.zero),
            new Entry("Borders/Blue/1080/Group 5 copy@0,5x.png",  "ui_panel",      new Vector4(240f, 170f, 240f, 170f)),
            new Entry("Borders/Blue/1080/Group 4 copy@0,5x.png",  "ui_panel_tall", new Vector4(170f, 220f, 170f, 220f)),
            new Entry("Bars/white/x1/Asset 2.png",                "ui_bar_fill",   Vector4.zero),
            new Entry("Bars/Blue/x1/Asset 4.png",                 "ui_accent",     Vector4.zero),
        };

        [MenuItem("Tools/Web of Planets/Uvezi UI sprite-ove (SunGraphica)")]
        public static void Import()
        {
            EnsureFolder("Assets/_Project/Art/UI/Resources");
            EnsureFolder(DestFolder);

            int ok = 0, failed = 0;
            foreach (var e in Entries)
            {
                string src = SourceRoot + "/" + e.source;
                string dst = DestFolder + "/" + e.name + ".png";

                if (AssetDatabase.LoadAssetAtPath<Texture2D>(src) == null)
                {
                    Debug.LogError($"[UiSpriteSetup] Izvor ne postoji: {src}");
                    failed++;
                    continue;
                }

                // Kopiraj samo ako cilj još ne postoji (ponovno pokretanje ne smije
                // mijenjati GUID — iako ništa ne referencira ove assete po GUID-u).
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(dst) == null &&
                    !AssetDatabase.CopyAsset(src, dst))
                {
                    Debug.LogError($"[UiSpriteSetup] Kopiranje nije uspjelo: {src} -> {dst}");
                    failed++;
                    continue;
                }

                if (ApplyImportSettings(dst, e.border)) ok++;
                else failed++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[UiSpriteSetup] Gotovo: {ok} sprite-ova spremno u {DestFolder}" +
                      (failed > 0 ? $", {failed} neuspjelo (vidi greške iznad)." : "."));
        }

        private static bool ApplyImportSettings(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[UiSpriteSetup] Nema TextureImportera za {path}");
                return false;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            // UI grafika s tankim linijama — bez kompresije da rubovi ostanu čisti.
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // 9-slice zahtijeva FullRect
            settings.spriteBorder = border;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
