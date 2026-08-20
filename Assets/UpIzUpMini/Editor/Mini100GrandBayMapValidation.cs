using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Farming;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Static migration gate and fixed evidence cameras for MINI-100.</summary>
    public static class Mini100GrandBayMapValidation
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private static readonly string EvidenceFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Tasks", "MINI-100");

        [MenuItem("Up Iz Up Mini/MINI-100/Validate Migrated Grand Bay")]
        public static void Validate()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> problems = CollectProblems(scene);
            if (problems.Count == 0)
                Debug.Log("MINI-100 MIGRATION VALIDATION PASS: approved world, Lalay/Highland density, farm expansion placeholders, gameplay roles, colliders, and script references survived the transfer.");
            else
                Debug.LogError("MINI-100 MIGRATION VALIDATION FAIL:\n- " + string.Join("\n- ", problems));
            if (Application.isBatchMode) EditorApplication.Exit(problems.Count == 0 ? 0 : 1);
        }

        [MenuItem("Up Iz Up Mini/MINI-100/Capture Migrated Grand Bay")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(EvidenceFolder);
            CaptureView("MINI-100-Overview-1600x1000.png", new Vector3(110f, 335f, -95f), new Vector3(110f, 0f, -95f), 1600, 1000, true, 225f);
            CaptureView("MINI-100-Lalay-1280x720.png", new Vector3(20f, 20f, -186f), new Vector3(112f, 2.5f, -170f), 1280, 720, false, 0f);
            CaptureView("MINI-100-Highland-1280x720.png", new Vector3(18f, 74f, -62f), new Vector3(92f, 2f, -126f), 1280, 720, false, 0f);
            GameObject sacat = FindAnywhere("Sacat");
            if (sacat != null)
                CaptureView("MINI-100-Spawn-1280x720.png", sacat.transform.position + new Vector3(-7f, 7f, -10f), sacat.transform.position + Vector3.up * 1.4f, 1280, 720, false, 0f);
            GameObject farm = FindAnywhere("FarmPlot_00");
            if (farm != null)
                CaptureView("MINI-100-HighlandFarm-1280x720.png", farm.transform.position + new Vector3(-20f, 19f, -22f), farm.transform.position + Vector3.up, 1280, 720, false, 0f);
            Debug.Log("MINI-100 MIGRATION CAPTURE PASS: overview, Lalay, Highland, Highland farm, and playable spawn evidence saved.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static List<string> CollectProblems(Scene scene)
        {
            List<string> problems = new List<string>();
            GameObject world = FindRoot(scene, "GrandBayPhase1_ApprovedWorld_VA005");
            Require(world != null, "approved VA-005 world root missing", problems);
            foreach (string oldRoot in new[] { "GrandBayTerrain", "Sea", "CoastAndJetty", "LalayRoad", "LalayHouses", "MontineFarmPath" })
                Require(FindRoot(scene, oldRoot) == null, $"old synthetic root still present: {oldRoot}", problems);

            foreach (string required in new[] { "Sacat", "Franki", "MontineFarm", "FarmSafehouse", "FarmPlot_00", "NPC_BoatMan", "MooredBoat", "MissionSystem", "NavMeshSurface" })
                Require(FindAnywhere(required) != null, $"required gameplay role missing: {required}", problems);

            if (world != null)
            {
                Transform[] mapItems = world.GetComponentsInChildren<Transform>(true);
                Transform lalayRoot = mapItems.FirstOrDefault(t => t.name == "Lalay_Dense_House_Massing");
                Transform highlandRoot = mapItems.FirstOrDefault(t => t.name == "Highland_Sparse_House_Massing");
                int lalayHouses = lalayRoot?.childCount ?? 0;
                int highlandHouses = highlandRoot?.childCount ?? 0;
                int tallHighland = highlandRoot == null ? 0 : highlandRoot.Cast<Transform>()
                    .Count(t => t.name.Contains("TwoStorey") || t.name.Contains("SmallApartment"));
                int futureFarms = mapItems.Count(t => t.name.StartsWith("Future_Farm_Parcel_", StringComparison.Ordinal));
                int roadColliders = mapItems.Count(t => t.GetComponent<Collider>() != null && HasAncestor(t, "Roads_OSM"));
                int bridgeColliders = mapItems.Count(t => t.GetComponent<Collider>() != null && HasAncestor(t, "Bridges"));
                Require(lalayHouses >= 110, $"Lalay density regressed: {lalayHouses}", problems);
                Require(highlandHouses >= 18, $"Highland density regressed: {highlandHouses}", problems);
                Require(tallHighland >= 9, $"Highland needs two-storey/apartment massing: {tallHighland}", problems);
                Require(futureFarms >= 7, $"future farm placeholders regressed: {futureFarms}", problems);
                Require(roadColliders >= 9, $"driveable road collider coverage regressed: {roadColliders}", problems);
                Require(bridgeColliders >= 4, $"bridge collider coverage regressed: {bridgeColliders}", problems);
            }

            Require(UnityEngine.Object.FindObjectsByType<FarmPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 11,
                "gameplay farm plots did not survive migration", problems);
            Require(UnityEngine.Object.FindObjectsByType<TownNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length >= 8,
                "town NPC gameplay roles did not survive migration", problems);

            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                    if (transform.gameObject.GetComponents<Component>().Any(component => component == null))
                        problems.Add("missing script: " + HierarchyPath(transform));
            return problems;
        }

        private static void CaptureView(string fileName, Vector3 position, Vector3 target, int width, int height, bool orthographic, float size)
        {
            GameObject cameraObject = new GameObject("MINI100_EvidenceCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.fieldOfView = 58f;
            camera.farClipPlane = 900f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.14f, 0.18f);
            camera.orthographic = orthographic;
            camera.orthographicSize = size;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 2 };
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(EvidenceFolder, fileName), image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        private static bool HasAncestor(Transform transform, string name)
        {
            while (transform != null) { if (transform.name == name) return true; transform = transform.parent; }
            return false;
        }

        private static GameObject FindAnywhere(string name)
        {
            foreach (Transform transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (transform.name == name) return transform.gameObject;
            return null;
        }

        private static GameObject FindRoot(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        private static void Require(bool condition, string problem, List<string> problems) { if (!condition) problems.Add(problem); }
        private static string HierarchyPath(Transform transform) => transform.parent == null ? transform.name : HierarchyPath(transform.parent) + "/" + transform.name;
    }
}
