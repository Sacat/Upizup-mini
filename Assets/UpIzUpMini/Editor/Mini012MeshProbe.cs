using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Diagnostic: report triangle/vertex counts and sub-object names of an
    /// imported model, so an asset's real runtime cost is measured rather
    /// than guessed at from its file size.
    /// </summary>
    public static class Mini012MeshProbe
    {
        [MenuItem("Up Iz Up Mini/MINI-012/Probe Decimated Crop Meshes")]
        public static void Probe()
        {
            string[] paths =
            {
                "Assets/UpIzUpMini/Art/CropMeshes/TomatoPlant_LOD.asset",
                "Assets/UpIzUpMini/Art/CropMeshes/WeedPlant_LOD.asset",
            };

            foreach (var path in paths)
            {
                var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (m == null) { Debug.LogWarning($"PROBE: missing {path}"); continue; }
                Debug.Log($"PROBE {m.name}: tris={m.triangles.Length / 3} verts={m.vertexCount} " +
                          $"boundsSize={m.bounds.size} boundsCenter={m.bounds.center}");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
