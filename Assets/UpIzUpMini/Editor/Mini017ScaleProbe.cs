using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Measures the real world-space height of every character model and
    /// the CharacterController capsule they are placed inside.
    ///
    /// Two symptoms depend on this: a protagonist visibly smaller than the
    /// NPCs, and locomotion that "walks weird". The second follows from the
    /// first - if the rendered model is shorter than the 2m
    /// CharacterController capsule that actually moves through the world,
    /// the feet cannot line up with the ground and the stride will not
    /// match the distance travelled, however good the retargeting is.
    /// </summary>
    public static class Mini017ScaleProbe
    {
        [MenuItem("Up Iz Up Mini/MINI-017/Probe Character Scale")]
        public static void Probe()
        {
            string[] models =
            {
                "Assets/UpIzUpMini/Art/Characters/Mainchar.fbx",
                "Assets/UpIzUpMini/Art/Characters/Strong.fbx",
                "Assets/Floreswa/Models/male01_2.fbx",
                "Assets/Floreswa/Models/male02_2.fbx",
                "Assets/Floreswa/Models/male03_1.fbx",
            };

            foreach (var path in models)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { Debug.LogWarning($"SCALE: missing {path}"); continue; }

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
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

                Debug.Log($"SCALE {System.IO.Path.GetFileName(path)}: " +
                          $"height={b.size.y:F3}m width={b.size.x:F3}m " +
                          $"importScale={importer?.globalScale ?? -1f} " +
                          $"useFileScale={importer?.useFileScale}");

                Object.DestroyImmediate(instance);
            }

            // What the scene actually builds the characters with.
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/UpIzUpMini/Scenes/GrandBayProof.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            foreach (var name in new[] { "Franki", "Sacat", "NPC_Police", "NPC_FarmShop" })
            {
                var go = GameObject.Find(name);
                if (go == null) { Debug.LogWarning($"SCALE scene: {name} not found"); continue; }

                var cc = go.GetComponent<CharacterController>();
                Bounds b = default;
                bool first = true;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (first) { b = r.bounds; first = false; }
                    else b.Encapsulate(r.bounds);
                }

                Debug.Log($"SCALE scene {name}: renderedHeight={b.size.y:F3}m " +
                          $"rootScale={go.transform.localScale} " +
                          $"ccHeight={(cc != null ? cc.height.ToString("F2") : "-")} " +
                          $"ccRadius={(cc != null ? cc.radius.ToString("F2") : "-")} " +
                          $"ccCenterY={(cc != null ? cc.center.y.ToString("F2") : "-")}");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
