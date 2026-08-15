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

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot GrandBayProof Scene")]
        public static void SnapshotGrandBayProof()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var mainCamGo = GameObject.FindWithTag("MainCamera");
            if (mainCamGo == null)
            {
                Debug.LogError("Snapshot: no MainCamera found in GrandBayProof.");
                return;
            }
            var cam = mainCamGo.GetComponent<Camera>();

            // The follow camera positions itself relative to the player in
            // LateUpdate/Play mode; in edit mode we approximate that pose
            // once here so the snapshot matches what the player will see.
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                cam.transform.position = player.transform.position + new Vector3(0f, 6.8f, -7f);
                cam.transform.LookAt(player.transform.position + Vector3.up * 1.4f);
            }

            RenderAndSave(cam, "grandbay-proof-overview.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot GrandBayProof Overview")]
        public static void SnapshotGrandBayProofOverview()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var camGo = new GameObject("OverviewCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(160f, 140f, 30f);
            camGo.transform.LookAt(new Vector3(160f, 0f, 150f));
            cam.fieldOfView = 60f;
            cam.farClipPlane = 600f;

            RenderAndSave(cam, "grandbay-overview-wide.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot GrandBayProof Mid Overview")]
        public static void SnapshotGrandBayProofMid()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var camGo = new GameObject("MidOverviewCamera");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(115f, 34f, 55f);
            camGo.transform.LookAt(new Vector3(135f, 5f, 95f));
            cam.fieldOfView = 60f;
            cam.farClipPlane = 400f;

            RenderAndSave(cam, "grandbay-overview-mid.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot Pause Menu")]
        public static void SnapshotPauseMenu()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            // GameObject.Find only searches active objects, and the panel
            // starts inactive - look it up via the canvas instead.
            var canvasEarly = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            var panel = canvasEarly != null ? canvasEarly.transform.Find("PausePanel") : null;
            if (panel != null) panel.gameObject.SetActive(true);
            else Debug.LogWarning("Snapshot: PausePanel not found under canvas.");

            var mainCamGo = GameObject.FindWithTag("MainCamera");
            var player = GameObject.FindWithTag("Player");
            if (mainCamGo != null && player != null)
            {
                mainCamGo.transform.position = player.transform.position + new Vector3(0f, 6.8f, -7f);
                mainCamGo.transform.LookAt(player.transform.position + Vector3.up * 1.4f);
            }

            var cam = mainCamGo != null ? mainCamGo.GetComponent<Camera>() : null;
            if (cam == null)
            {
                Debug.LogError("Snapshot: no MainCamera found.");
                return;
            }

            // Screen Space - Overlay canvases don't get captured by
            // Camera.Render() to a texture (they draw directly to the
            // screen backbuffer, bypassing any camera) - switch to
            // Screen Space - Camera just for this diagnostic snapshot so
            // the UI is actually visible in the rendered PNG. Not saved.
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1f;
            }

            RenderAndSave(cam, "grandbay-pause-menu.png");
        }

        [MenuItem("Up Iz Up Mini/MINI-011/Snapshot Shanty Town Sample")]
        public static void SnapshotShantyTown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = 1.2f;

            string[] prefabPaths =
            {
                "Assets/ArteriaShantyTown/ShantyTown1/shanty1.fbx",
                "Assets/ArteriaShantyTown/ShantyTown1/shanty5.fbx",
                "Assets/ArteriaShantyTown/ShantyTown1/shanty10.fbx",
                "Assets/ArteriaShantyTown/ShantyTown2_Buildings/BuildingA/BuildingA.fbx",
                "Assets/ArteriaShantyTown/ShantyTown2_Buildings/BuildingE/BuildingE.fbx",
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
            camGo.transform.position = new Vector3(x / 2f, 6f, -12f);
            camGo.transform.LookAt(new Vector3(x / 2f, 1.5f, 0f));
            cam.fieldOfView = 60f;
            cam.farClipPlane = 200f;

            RenderAndSave(cam, "shanty-town-houses.png");
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
