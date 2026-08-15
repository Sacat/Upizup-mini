using System.IO;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Converts the larger project's photogrammetry crop scans into
    /// mobile-viable meshes and saves them as project assets.
    ///
    /// Source: `Assets/UpIzUpMini/Art/CropModels/*.fbx`, copied from
    /// `E:\Unity\Up iz up\Assets\Imported Plants\` (recorded in
    /// Docs/ASSET-REGISTER.md). Each source is ~2,000,000 triangles in a
    /// single submesh - unusable as-is. This decimates them to a few
    /// thousand triangles via MeshDecimator, preserving the real plant
    /// silhouette that makes the crops read as actual plants rather than
    /// primitives.
    ///
    /// Run once (or after changing the target resolution); the resulting
    /// .asset meshes are what the scene builder references, so the giant
    /// source FBXs are never loaded at runtime.
    /// </summary>
    public static class Mini012CropAssetBuilder
    {
        private const string OutFolder = "Assets/UpIzUpMini/Art/CropMeshes";

        [MenuItem("Up Iz Up Mini/MINI-012/Build Decimated Crop Meshes")]
        public static void BuildAll()
        {
            EnsureFolder(OutFolder);

            bool ok = true;
            ok &= BuildOne("Assets/UpIzUpMini/Art/CropModels/Tomato plant.fbx", "TomatoPlant_LOD", 26);
            ok &= BuildOne("Assets/UpIzUpMini/Art/CropModels/Weed plant.fbx", "WeedPlant_LOD", 26);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(ok ? "MINI-012 CROP MESHES: OK" : "MINI-012 CROP MESHES: one or more sources missing");
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool BuildOne(string fbxPath, string outName, int gridResolution)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (go == null)
            {
                Debug.LogWarning($"MINI-012: source not found, skipping: {fbxPath}");
                return false;
            }

            Mesh source = null;
            foreach (var f in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (f.sharedMesh != null) { source = f.sharedMesh; break; }
            }
            if (source == null)
            {
                Debug.LogWarning($"MINI-012: no mesh inside {fbxPath}");
                return false;
            }

            Mesh decimated = MeshDecimator.Decimate(source, gridResolution);
            if (decimated == null)
            {
                Debug.LogError($"MINI-012: decimation produced nothing for {fbxPath}");
                return false;
            }

            string outPath = $"{OutFolder}/{outName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(outPath);
            if (existing != null) AssetDatabase.DeleteAsset(outPath);
            AssetDatabase.CreateAsset(decimated, outPath);

            int srcTris = source.triangles.Length / 3;
            int outTris = decimated.triangles.Length / 3;
            Debug.Log($"MINI-012 DECIMATED {outName}: {srcTris} -> {outTris} tris " +
                      $"({100f * outTris / Mathf.Max(1, srcTris):F2}% kept), verts={decimated.vertexCount}");
            return true;
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;
            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
