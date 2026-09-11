using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-154: report the actual, Unity-imported (deterministic) hierarchy of
    /// Mainchar.fbx (Franki) and Strong.fbx (Sacat) so garment work is based on
    /// what Unity really sees, not an ad hoc re-import of the raw FBX in another
    /// tool. See Docs/WorkPackets/MINI-154.md.
    /// </summary>
    public static class Mini154InspectGarmentSource
    {
        private static readonly string[] Models =
        {
            "Assets/UpIzUpMini/Art/Characters/Mainchar.fbx",
            "Assets/UpIzUpMini/Art/Characters/Strong.fbx",
        };

        [MenuItem("Up Iz Up Mini/MINI-154/Inspect Garment Source")]
        public static void Inspect()
        {
            foreach (var path in Models)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null)
                {
                    Debug.LogError($"MINI154: missing {path}");
                    continue;
                }

                Debug.Log($"MINI154: ==== {path} ====");
                var renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Debug.Log($"MINI154: SkinnedMeshRenderer count = {renderers.Length}");
                foreach (var r in renderers)
                {
                    var mesh = r.sharedMesh;
                    var mats = r.sharedMaterials.Select(m => m != null ? m.name : "null").ToArray();
                    Debug.Log($"MINI154:  renderer '{r.name}' mesh='{(mesh != null ? mesh.name : "null")}' " +
                              $"verts={(mesh != null ? mesh.vertexCount : 0)} subMeshCount={(mesh != null ? mesh.subMeshCount : 0)} " +
                              $"materials=[{string.Join(",", mats)}] bones={r.bones.Length} rootBone={(r.rootBone != null ? r.rootBone.name : "null")}");

                    if (mesh != null)
                    {
                        var verts = mesh.vertices;
                        for (int si = 0; si < mesh.subMeshCount; si++)
                        {
                            var sm = mesh.GetSubMesh(si);
                            var tris = mesh.GetTriangles(si);
                            var used = new System.Collections.Generic.HashSet<int>(tris);
                            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                            foreach (var vi in used)
                            {
                                var v = verts[vi];
                                min = Vector3.Min(min, v);
                                max = Vector3.Max(max, v);
                            }
                            var matName = si < mats.Length ? mats[si] : $"idx{si}";
                            Debug.Log($"MINI154:    submesh {si} ('{matName}'): verts={used.Count} tris={tris.Length / 3} " +
                                      $"bbox min={min} max={max}");
                        }
                    }
                }

                var animator = go.GetComponent<Animator>();
                Debug.Log($"MINI154:  Animator avatar valid={(animator != null && animator.avatar != null && animator.avatar.isValid)} " +
                          $"isHuman={(animator != null && animator.avatar != null && animator.avatar.isHuman)}");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
