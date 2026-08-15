using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Caps the imported character textures for a mobile-first target.
    ///
    /// The character maps copied from the larger project are 4K PNGs (some
    /// normal maps are 30MB each, ~380MB total). That is far beyond this
    /// project's stated mobile/WebGL budget (DECISIONS.md D-008). Import
    /// settings are capped rather than the source files being re-encoded,
    /// so the originals stay intact and the cap can be raised later for a
    /// desktop build.
    /// </summary>
    public static class Mini016TextureBudget
    {
        private const string Folder = "Assets/UpIzUpMini/Art/Characters";

        [MenuItem("Up Iz Up Mini/MINI-016/Cap Character Texture Budget")]
        public static void Apply()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
            int changed = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool isNormal = path.Contains("_Normal");

                importer.maxTextureSize = isNormal ? 512 : 1024;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.mipmapEnabled = true;

                if (isNormal)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                }

                importer.SaveAndReimport();
                changed++;
            }

            Debug.Log($"TEXBUDGET: capped {changed} character textures (normals 512, colour 1024, compressed)");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
