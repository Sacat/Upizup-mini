using System.Text;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-064 Pass 1 (inspect only, no editing) - dumps everything the
    /// brief asked to check before touching the model: hierarchy, mesh
    /// count, whether wheels are separate transforms, pivot/bounds,
    /// material count, and total triangle count, plus a scale comparison
    /// against the real TMAX 560's ~2.195m length / ~0.780m width /
    /// ~1.575m wheelbase.
    /// </summary>
    public static class Mini064TmaxInspect
    {
        private const string GlbPath = "Assets/Tmax 560.glb";

        [MenuItem("Up Iz Up Mini/MINI-064/Inspect TMAX GLB")]
        public static void Inspect()
        {
            var sb = new StringBuilder();
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(GlbPath);
            if (allAssets == null || allAssets.Length == 0)
            {
                Debug.LogError($"MINI-064 INSPECT FAIL: no sub-assets loaded from {GlbPath} - importer likely not resolved yet (needs com.unity.cloud.gltfast).");
                return;
            }

            GameObject root = null;
            int meshCount = 0;
            int totalTris = 0;
            int materialCount = 0;
            var meshNames = new System.Collections.Generic.List<string>();
            var materialNames = new System.Collections.Generic.HashSet<string>();

            foreach (var asset in allAssets)
            {
                switch (asset)
                {
                    case GameObject go when go.transform.parent == null:
                        root = go;
                        break;
                    case Mesh mesh:
                        meshCount++;
                        totalTris += mesh.triangles.Length / 3;
                        meshNames.Add($"{mesh.name} (tris={mesh.triangles.Length / 3}, verts={mesh.vertexCount})");
                        break;
                    case Material mat:
                        materialCount++;
                        materialNames.Add(mat.name);
                        break;
                }
            }

            sb.AppendLine("=== MINI-064 TMAX GLB INSPECTION ===");
            sb.AppendLine($"Path: {GlbPath}");
            sb.AppendLine($"Sub-assets loaded: {allAssets.Length}");
            sb.AppendLine($"Meshes: {meshCount}, total triangles: {totalTris}");
            sb.AppendLine($"Materials: {materialCount} -> {string.Join(", ", materialNames)}");
            sb.AppendLine();

            if (root == null)
            {
                Debug.LogError("MINI-064 INSPECT FAIL: no root GameObject found among sub-assets.");
                Debug.Log(sb.ToString());
                return;
            }

            sb.AppendLine($"Root: {root.name}");
            sb.AppendLine("Hierarchy:");
            DumpHierarchy(root.transform, sb, 1);

            // Instantiate temporarily to compute real world-space bounds
            // (renderer bounds on the raw asset are in unscaled local
            // space and won't reflect any import scale factor).
            var instance = (GameObject)UnityEngine.Object.Instantiate(root);
            try
            {
                var renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    sb.AppendLine("No renderers found on instantiated root - cannot compute bounds.");
                }
                else
                {
                    Bounds bounds = renderers[0].bounds;
                    foreach (var r in renderers) bounds.Encapsulate(r.bounds);

                    sb.AppendLine();
                    sb.AppendLine("=== BOUNDS (world space, at import scale) ===");
                    sb.AppendLine($"Size: X(width)={bounds.size.x:F3}m  Y(height)={bounds.size.y:F3}m  Z(length)={bounds.size.z:F3}m");
                    sb.AppendLine($"Center: {bounds.center}");
                    sb.AppendLine();
                    sb.AppendLine("=== SCALE COMPARISON vs real TMAX 560 ===");
                    sb.AppendLine($"Real: length~2.195m, width~0.780m, wheelbase~1.575m");
                    sb.AppendLine($"Model bounding box implies scale factors (assuming Z=length, X=width):");
                    sb.AppendLine($"  length ratio (real/model Z): {(bounds.size.z > 0.001f ? 2.195f / bounds.size.z : 0f):F3}");
                    sb.AppendLine($"  width ratio  (real/model X): {(bounds.size.x > 0.001f ? 0.780f / bounds.size.x : 0f):F3}");
                }

                // Look for likely wheel transforms by name.
                sb.AppendLine();
                sb.AppendLine("=== WHEEL-LIKE TRANSFORMS (name contains 'wheel') ===");
                var allTransforms = instance.GetComponentsInChildren<Transform>();
                bool anyWheel = false;
                foreach (var t in allTransforms)
                {
                    if (t.name.ToLowerInvariant().Contains("wheel"))
                    {
                        anyWheel = true;
                        var mf = t.GetComponent<MeshFilter>();
                        sb.AppendLine($"  {t.name} | localPos={t.localPosition} | worldPos={t.position} | ownMesh={(mf != null ? mf.sharedMesh?.name : "none")}");
                    }
                }
                if (!anyWheel) sb.AppendLine("  (none found by name - wheels may be merged into the body mesh, or named differently.)");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            Debug.Log(sb.ToString());
        }

        private static void DumpHierarchy(Transform t, StringBuilder sb, int depth)
        {
            var mf = t.GetComponent<MeshFilter>();
            var mr = t.GetComponent<MeshRenderer>();
            var smr = t.GetComponent<SkinnedMeshRenderer>();
            string meshInfo = mf != null && mf.sharedMesh != null ? $" [mesh:{mf.sharedMesh.name}]"
                : smr != null && smr.sharedMesh != null ? $" [skinnedMesh:{smr.sharedMesh.name}]" : "";
            string matInfo = mr != null && mr.sharedMaterial != null ? $" [mat:{mr.sharedMaterial.name}]" : "";
            sb.AppendLine($"{new string(' ', depth * 2)}- {t.name}{meshInfo}{matInfo} (localPos={t.localPosition}, localScale={t.localScale})");
            for (int i = 0; i < t.childCount; i++)
            {
                DumpHierarchy(t.GetChild(i), sb, depth + 1);
            }
        }
    }
}
