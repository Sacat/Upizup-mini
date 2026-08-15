using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Renders a camera view of a temporary scene straight to a PNG file
    /// from batch mode, so imported assets can be visually inspected without
    /// depending on a focused desktop window or the Editor Play-mode bug
    /// documented elsewhere in this project. Must be run WITHOUT
    /// -nographics (a real graphics device is required to render).
    /// </summary>
    public static class Mini011AssetSnapshot
    {
        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot Demo City Sample")]
        public static void SnapshotDemoCity()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = 1.2f;

            string[] prefabPaths =
            {
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/small_house_1.prefab",
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/small_house_2.prefab",
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/small_house_3.prefab",
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/mid_house_1.prefab",
                "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/mid_house_2.prefab",
            };

            float x = 0f;
            foreach (var path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Snapshot: could not load {path}");
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = new Vector3(x, 0f, 0f);
                x += 8f;
            }

            var camGo = new GameObject("SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(x / 2f, 8f, -14f);
            camGo.transform.LookAt(new Vector3(x / 2f, 1.5f, 0f));
            cam.fieldOfView = 55f;
            cam.farClipPlane = 200f;

            RenderAndSave(cam, "demo-city-houses.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot Character Pack Sample")]
        public static void SnapshotCharacterPack()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = 1.2f;

            string[] modelPaths =
            {
                "Assets/Floreswa/Models/male01_1.fbx",
                "Assets/Floreswa/Models/male02_1.fbx",
            };

            float x = 0f;
            foreach (var path in modelPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Snapshot: could not load {path}");
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = new Vector3(x, 0f, 0f);
                x += 2f;
            }

            var camGo = new GameObject("SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(x / 2f, 1.6f, -4f);
            camGo.transform.LookAt(new Vector3(x / 2f, 1f, 0f));
            cam.fieldOfView = 45f;
            cam.farClipPlane = 100f;

            RenderAndSave(cam, "character-pack-sample.png");
        }

        private static void RenderAndSave(Camera cam, string fileName)
        {
            const int width = 1280;
            const int height = 720;

            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Snapshots");
            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, fileName);
            File.WriteAllBytes(outPath, png);

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);

            Debug.Log($"Snapshot saved: {outPath}");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
