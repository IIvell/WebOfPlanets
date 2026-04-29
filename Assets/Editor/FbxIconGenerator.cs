using UnityEngine;
using UnityEditor;
using System.IO;

namespace xyz.germanfica.unity.planet.gravity
{
    public static class FbxIconGenerator
    {
        [MenuItem("Assets/Generate Icon from FBX", true)]
        static bool Validate() => Selection.activeObject is GameObject;

        [MenuItem("Assets/Generate Icon from FBX")]
        static void Generate()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is not GameObject go) continue;

                Texture2D preview = AssetPreview.GetAssetPreview(go);

                // Preview se ponekad generira async — pokušaj odmah, inače čekaj
                if (preview == null)
                {
                    EditorUtility.DisplayDialog("Info",
                        $"Preview za '{go.name}' još nije spreman. Pokušaj opet za sekundu.", "OK");
                    continue;
                }

                SaveIcon(go.name, preview);
            }
        }

        static void SaveIcon(string assetName, Texture2D source)
        {
            string dir = "Assets/Icons";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Kopiraj u readable texturu
            RenderTexture rt = RenderTexture.GetTemporary(128, 128);
            Graphics.Blit(source, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D copy = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, 128, 128), 0, 0);
            copy.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            string path = $"{dir}/{assetName}_icon.png";
            File.WriteAllBytes(path, copy.EncodeToPNG());
            AssetDatabase.Refresh();

            // Postavi kao Sprite
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();

            Debug.Log($"Icon spremljen: {path}");
        }
    }
}
