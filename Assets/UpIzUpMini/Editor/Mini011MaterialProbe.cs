using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>One-off diagnostic: list material slot names on a prefab's renderers.</summary>
    public static class Mini011MaterialProbe
    {
        [MenuItem("Up Iz Up Mini/MINI-011/Probe Character Materials")]
        public static void Probe()
        {
            foreach (var path in new[] { "Assets/Floreswa/Prefabs/male01_1.prefab", "Assets/Floreswa/Prefabs/male02_1.prefab" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { Debug.LogWarning($"missing {path}"); continue; }
                foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        Debug.Log($"PROBE {path} :: renderer={r.name} material={(m == null ? "null" : m.name)}");
                    }
                }
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
