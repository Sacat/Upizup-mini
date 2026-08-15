using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Builds the MINI-001 "GrandBayProof" scene programmatically instead of
    /// hand-editing scene YAML, per D-004 (one scene integrator, repeatable
    /// editor setup scripts). Re-running this menu item rebuilds the scene
    /// from scratch and overwrites the saved asset.
    /// </summary>
    public static class Mini001SceneSetup
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string MaterialFolder = "Assets/UpIzUpMini/Art/Materials";

        [MenuItem("Up Iz Up Mini/MINI-001/Build Grand Bay Proof Scene")]
        public static void BuildScene()
        {
            EnsureFolder(MaterialFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            BuildGround();
            BuildRoadAndBuildings();
            BuildFarmPathAndClearing(out Transform farmPlot);
            BuildNPC();
            Transform player = BuildPlayer();
            BuildCamera(player);

            EnsureFolder("Assets/UpIzUpMini/Scenes");
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(saved
                ? $"MINI-001: GrandBayProof scene built and saved to {ScenePath}."
                : $"MINI-001: Failed to save scene to {ScenePath}.");
        }

        private static void BuildLighting()
        {
            var sunGo = new GameObject("Sun");
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.85f);
            light.intensity = 1.15f;
            sunGo.transform.rotation = Quaternion.Euler(52f, -25f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.68f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.5f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.18f);
        }

        private static void BuildGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(14f, 1f, 14f);
            ground.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                "Ground", new Color(0.29f, 0.45f, 0.22f));
        }

        private static void BuildRoadAndBuildings()
        {
            var roadParent = new GameObject("LalayRoad");

            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "RoadSurface";
            road.transform.SetParent(roadParent.transform);
            road.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            road.transform.localScale = new Vector3(4f, 0.06f, 70f);
            road.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                "RoadAsphalt", new Color(0.32f, 0.32f, 0.33f));

            Material wallMat = GetOrCreateMaterial("BuildingWall", new Color(0.86f, 0.78f, 0.62f));
            Material roofMat = GetOrCreateMaterial("BuildingRoof", new Color(0.62f, 0.25f, 0.16f));

            float[] zPositions = { -22f, -6f, 10f, 26f };
            for (int i = 0; i < zPositions.Length; i++)
            {
                float z = zPositions[i];
                BuildSimpleBuilding(roadParent.transform, $"Building_L{i}", new Vector3(-6.5f, 0f, z), wallMat, roofMat);
                BuildSimpleBuilding(roadParent.transform, $"Building_R{i}", new Vector3(6.5f, 0f, z), wallMat, roofMat);
            }
        }

        private static void BuildSimpleBuilding(Transform parent, string name, Vector3 localPos, Material wallMat, Material roofMat)
        {
            var buildingGo = new GameObject(name);
            buildingGo.transform.SetParent(parent);
            buildingGo.transform.localPosition = localPos;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Walls";
            body.transform.SetParent(buildingGo.transform, false);
            body.transform.localScale = new Vector3(4f, 3f, 4f);
            body.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = wallMat;

            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(buildingGo.transform, false);
            roof.transform.localScale = new Vector3(4.5f, 0.3f, 4.5f);
            roof.transform.localPosition = new Vector3(0f, 3.15f, 0f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMat;
        }

        private static void BuildFarmPathAndClearing(out Transform farmPlot)
        {
            Material dirtMat = GetOrCreateMaterial("DirtyFarmPath", new Color(0.42f, 0.29f, 0.15f));
            Material clearingMat = GetOrCreateMaterial("FarmClearing", new Color(0.36f, 0.42f, 0.2f));
            Material soilMat = GetOrCreateMaterial("FarmSoil", new Color(0.33f, 0.22f, 0.11f));

            var pathParent = new GameObject("MontineFarmPath");

            // Branches off the Lalay road roughly a third of the way up, angled away
            // toward an off-road farm clearing (see Docs/MAP-STRATEGY.md route order).
            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = "DirtyPath";
            path.transform.SetParent(pathParent.transform);
            path.transform.localPosition = new Vector3(11f, 0.025f, 6f);
            path.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            path.transform.localScale = new Vector3(2.4f, 0.05f, 26f);
            path.GetComponent<Renderer>().sharedMaterial = dirtMat;

            GameObject clearing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clearing.name = "FarmClearing";
            clearing.transform.SetParent(pathParent.transform);
            clearing.transform.localPosition = new Vector3(24f, 0.02f, 18f);
            clearing.transform.localScale = new Vector3(14f, 0.04f, 12f);
            clearing.GetComponent<Renderer>().sharedMaterial = clearingMat;

            GameObject plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plot.name = "FarmPlot";
            plot.transform.SetParent(pathParent.transform);
            plot.transform.localPosition = new Vector3(24f, 0.03f, 18f);
            plot.transform.localScale = new Vector3(4f, 0.05f, 3f);
            plot.GetComponent<Renderer>().sharedMaterial = soilMat;
            plot.GetComponent<Collider>().enabled = false;

            var plotInteractable = plot.AddComponent<FarmPlotInteractable>();
            var so = new SerializedObject(plotInteractable);
            so.FindProperty("soilRenderer").objectReferenceValue = plot.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            farmPlot = plot.transform;
        }

        private static void BuildNPC()
        {
            GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NPC_Villager";
            npc.transform.position = new Vector3(-3.2f, 1f, -2f);
            npc.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                "NPCVillager", new Color(0.2f, 0.35f, 0.65f));
            npc.AddComponent<NPCInteractable>();
        }

        private static Transform BuildPlayer()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, -30f);
            player.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(
                "PlayerCapsule", new Color(0.75f, 0.18f, 0.15f));

            // CharacterController replaces the primitive's default CapsuleCollider.
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.5f;

            player.AddComponent<PlayerController>();
            player.AddComponent<InteractionDetector>();

            return player.transform;
        }

        private static void BuildCamera(Transform player)
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            camGo.AddComponent<AudioListener>();

            var follow = camGo.AddComponent<ThirdPersonFollowCamera>();
            follow.SetTarget(player);

            // Place the camera immediately so the scene doesn't open with it
            // sitting at the origin before the first LateUpdate runs.
            camGo.transform.position = player.position + new Vector3(0f, 6.8f, -7f);
            camGo.transform.LookAt(player.position + Vector3.up * 1.4f);
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                return existing;
            }

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath)) return;

            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
