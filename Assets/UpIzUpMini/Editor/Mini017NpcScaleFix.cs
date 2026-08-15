using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Normalises the Floreswa NPC models to a realistic human height.
    ///
    /// Measured by Mini017ScaleProbe: those models import at 2.73-2.83m
    /// tall, against 1.93-1.97m for the protagonists (Mainchar/Strong) and
    /// a 2m CharacterController capsule. That is why the protagonists
    /// looked small next to the NPCs - the NPCs are roughly 45% oversized,
    /// not the protagonists undersized.
    ///
    /// Fixed at import via ModelImporter.globalScale so every use of the
    /// model is correct, rather than scaling instances in the scene
    /// builder and having to remember it at each call site.
    /// </summary>
    public static class Mini017NpcScaleFix
    {
        private const float TargetHeight = 1.85f;

        private static readonly string[] Models =
        {
            "Assets/Floreswa/Models/male01_1.fbx",
            "Assets/Floreswa/Models/male01_2.fbx",
            "Assets/Floreswa/Models/male01_3.fbx",
            "Assets/Floreswa/Models/male02_1.fbx",
            "Assets/Floreswa/Models/male02_2.fbx",
            "Assets/Floreswa/Models/male02_3.fbx",
            "Assets/Floreswa/Models/male03_1.fbx",
            "Assets/Floreswa/Models/male03_2.fbx",
            "Assets/Floreswa/Models/male03_3.fbx",
        };

        [MenuItem("Up Iz Up Mini/MINI-017/Normalise NPC Height")]
        public static void Normalise()
        {
            foreach (var path in Models)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (importer == null || prefab == null)
                {
                    Debug.LogWarning($"NPCSCALE: missing {path}");
                    continue;
                }

                float current = MeasureHeight(prefab);
                if (current <= 0.01f)
                {
                    Debug.LogWarning($"NPCSCALE: could not measure {path}");
                    continue;
                }

                // globalScale multiplies the existing import, so fold the
                // current value in rather than overwriting it.
                float factor = TargetHeight / current;
                float newScale = importer.globalScale * factor;

                importer.useFileScale = false;
                importer.globalScale = newScale;
                importer.SaveAndReimport();

                float after = MeasureHeight(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                Debug.Log($"NPCSCALE {System.IO.Path.GetFileName(path)}: " +
                          $"{current:F3}m -> {after:F3}m (globalScale={newScale:F4})");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static float MeasureHeight(GameObject prefab)
        {
            if (prefab == null) return 0f;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = Vector3.zero;
            instance.transform.localScale = Vector3.one;

            Bounds b = default;
            bool first = true;
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }

            Object.DestroyImmediate(instance);
            return first ? 0f : b.size.y;
        }
    }
}
