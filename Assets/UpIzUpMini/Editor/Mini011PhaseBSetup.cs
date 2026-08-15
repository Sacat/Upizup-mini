using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.Farming;
using UpIzUpMini.Interaction;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-011 Phase B: rebuilds GrandBayProof.unity with real assets
    /// instead of MINI-001's primitives - a sculpted terrain (Grand Bay's
    /// coast-to-hills shape, approximate per Docs/MINI-011-VISUAL-PLAN.md),
    /// a bending road, a mix of real Shanty Town structures and hand-built
    /// modular houses, real vegetation, a visible sea, and rigged
    /// Humanoid characters in place of capsules. Supersedes
    /// Mini001SceneSetup.BuildScene for this scene's content; that script
    /// is kept for MINI-001 history, not deleted.
    /// </summary>
    public static class Mini011PhaseBSetup
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string MaterialFolder = "Assets/UpIzUpMini/Art/Materials";
        private const string ControllerPath = "Assets/UpIzUpMini/Art/PlayerLocomotion.controller";

        private const float TerrainSize = 320f;
        private const float TerrainHeight = 36f;
        private const int HeightRes = 129;

        // Road runs along local Z at roughly worldX = RoadX, with a gentle
        // bend. Village shelf is flat-ish around RoadX; sea sits toward
        // worldX = 0, hills rise toward worldX = TerrainSize.
        private const float RoadX = 130f;

        [MenuItem("Up Iz Up Mini/MINI-011/Phase B - Build Grand Bay Environment")]
        public static void BuildScene()
        {
            EnsureFolder(MaterialFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RuntimeAnimatorController locomotionController = BuildAnimatorController();

            BuildLighting();
            Terrain terrain = BuildTerrain();
            BuildSea();

            List<Vector3> roadPoints = BuildRoad(terrain);
            BuildHouses(terrain, roadPoints);
            BuildVegetation(terrain, roadPoints);
            BuildFarmPathAndClearing(terrain, roadPoints, out Transform farmPlot);

            BuildEconomyAndCrops();
            BuildTownNPCs(terrain, roadPoints);

            Vector3 startPos = roadPoints[0];
            startPos.y = SampleHeight(terrain, startPos.x, startPos.z);

            // Smart (darker-skinned per user direction) starts controllable;
            // Strong (lighter-skinned) starts as a following companion.
            // Docs/STORY.md: "the player can switch between Smart and
            // Strong... retain separate health, stamina, position."
            CharacterSlot smart = BuildControllableCharacter(
                "Smart", "Smart", "Assets/Floreswa/Models/male01_1.fbx",
                new Color(0.35f, 0.22f, 0.13f), startPos, locomotionController, startActive: true);
            CharacterSlot strong = BuildControllableCharacter(
                "Strong", "Strong", "Assets/Floreswa/Models/male02_1.fbx",
                new Color(0.72f, 0.56f, 0.42f), startPos + new Vector3(1.4f, 0f, -1.2f),
                locomotionController, startActive: false);
            strong.followController.FollowTarget = smart.root.transform;

            ThirdPersonFollowCamera camera = BuildCamera(smart.root.transform);

            var switchGo = new GameObject("CharacterSwitchManager");
            var switchManager = switchGo.AddComponent<CharacterSwitchManager>();
            var switchSo = new SerializedObject(switchManager);
            var slotsProp = switchSo.FindProperty("slots");
            slotsProp.arraySize = 2;
            WriteSlot(slotsProp.GetArrayElementAtIndex(0), smart);
            WriteSlot(slotsProp.GetArrayElementAtIndex(1), strong);
            switchSo.FindProperty("followCamera").objectReferenceValue = camera;
            switchSo.ApplyModifiedPropertiesWithoutUndo();

            BuildHUD();
            BuildPauseMenu();

            EnsureFolder("Assets/UpIzUpMini/Scenes");
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(saved
                ? $"MINI-011 Phase B: GrandBayProof rebuilt and saved to {ScenePath}."
                : $"MINI-011 Phase B: failed to save scene to {ScenePath}.");
        }

        // ---------------------------------------------------------------
        // Animator
        // ---------------------------------------------------------------

        private static RuntimeAnimatorController BuildAnimatorController()
        {
            // Always rebuild fresh rather than reusing a cached asset -
            // during iteration a stale controller from an earlier run could
            // silently mask fixes to this method.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            EnsureFolder("Assets/UpIzUpMini/Art");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var idle = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx");
            var walk = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx");
            var run = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx");

            var tree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            if (idle != null) tree.AddChild(idle, 0f);
            if (walk != null) tree.AddChild(walk, 1f);
            if (run != null) tree.AddChild(run, 2f);

            var rootMachine = controller.layers[0].stateMachine;
            var state = rootMachine.AddState("Locomotion");
            state.motion = tree;
            rootMachine.defaultState = state;

            AssetDatabase.AddObjectToAsset(tree, controller);
            EditorUtility.SetDirty(controller);

            return controller;
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            Debug.LogWarning($"Mini011PhaseBSetup: no AnimationClip found at {fbxPath}");
            return null;
        }

        // ---------------------------------------------------------------
        // Lighting
        // ---------------------------------------------------------------

        private static void BuildLighting()
        {
            var sunGo = new GameObject("Sun");
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.82f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.72f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.52f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.24f, 0.19f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.72f, 0.82f, 0.85f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 260f;
        }

        // ---------------------------------------------------------------
        // Terrain: coast (low X) -> village shelf (mid X) -> hills (high X)
        // ---------------------------------------------------------------

        private static Terrain BuildTerrain()
        {
            var data = new TerrainData
            {
                heightmapResolution = HeightRes,
                size = new Vector3(TerrainSize, TerrainHeight, TerrainSize)
            };

            var heights = new float[HeightRes, HeightRes];
            for (int hz = 0; hz < HeightRes; hz++)
            {
                for (int hx = 0; hx < HeightRes; hx++)
                {
                    float worldX = (hx / (float)(HeightRes - 1)) * TerrainSize;
                    float u = hx / (float)(HeightRes - 1);
                    float v = hz / (float)(HeightRes - 1);

                    float shelfHeight = 0.24f;
                    float h;

                    if (worldX < RoadX - 40f)
                    {
                        // Coastal slope down to sea level near worldX = 0.
                        float t = Mathf.Clamp01(worldX / (RoadX - 40f));
                        h = Mathf.Lerp(0.03f, shelfHeight, t);
                        h += (Mathf.PerlinNoise(u * 6f, v * 6f) - 0.5f) * 0.02f;
                    }
                    else if (worldX < RoadX + 40f)
                    {
                        // Village shelf: fairly flat, gentle noise only.
                        h = shelfHeight + (Mathf.PerlinNoise(u * 10f, v * 10f) - 0.5f) * 0.03f;
                    }
                    else
                    {
                        // Hills rising inland, inspired by Grand Bay's
                        // mountainous backdrop (see Grandbay entire.jpg
                        // reference) - approximate, not surveyed.
                        float t = Mathf.Clamp01((worldX - (RoadX + 40f)) / (TerrainSize - (RoadX + 40f)));
                        float ridge = Mathf.PerlinNoise(u * 3.5f, v * 3.5f);
                        h = shelfHeight + t * 0.45f + ridge * 0.25f * t;
                    }

                    heights[hz, hx] = Mathf.Clamp01(h);
                }
            }
            data.SetHeights(0, 0, heights);

            data.terrainLayers = new[] { GetOrCreateGrassLayer() };

            var terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "GrandBayTerrain";
            terrainGo.transform.position = new Vector3(0f, 0f, 0f);
            var terrain = terrainGo.GetComponent<Terrain>();
            return terrain;
        }

        private static TerrainLayer GetOrCreateGrassLayer()
        {
            const string layerPath = "Assets/UpIzUpMini/Art/GrassGround.terrainlayer";
            var existing = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (existing != null) return existing;

            EnsureFolder("Assets/UpIzUpMini/Art");
            var layer = new TerrainLayer
            {
                diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    "Assets/Polytope Studio/Lowpoly_Environments/Sources/Textures/PT_Ground_Grass_Green_01.png"),
                tileSize = new Vector2(18f, 18f)
            };
            AssetDatabase.CreateAsset(layer, layerPath);
            return layer;
        }

        private static float SampleHeight(Terrain terrain, float worldX, float worldZ)
        {
            return terrain.SampleHeight(new Vector3(worldX, 0f, worldZ));
        }

        // ---------------------------------------------------------------
        // Sea (visual only for now - see DECISIONS.md D-007, Guadeloupe
        // sea-trade is an abstraction in this Mini, not a sailed route yet)
        // ---------------------------------------------------------------

        private static void BuildSea()
        {
            var seaGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            seaGo.name = "Sea";
            Object.DestroyImmediate(seaGo.GetComponent<Collider>());
            seaGo.transform.position = new Vector3(-40f, 1.2f, TerrainSize / 2f);
            seaGo.transform.localScale = new Vector3(20f, 1f, 40f);
            var mat = GetOrCreateMaterial("Sea", new Color(0.15f, 0.42f, 0.55f));
            var color = mat.color;
            color.a = 0.9f;
            mat.color = color;
            seaGo.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // ---------------------------------------------------------------
        // Road: a few connected, gently bending segments (not one
        // straight box) following the village shelf.
        // ---------------------------------------------------------------

        private static List<Vector3> BuildRoad(Terrain terrain)
        {
            var points = new List<Vector3>();
            int segments = 8;
            float totalLength = 220f;
            float startZ = 20f;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float z = startZ + t * totalLength;
                float x = RoadX + Mathf.Sin(t * 3.4f) * 14f;
                float y = SampleHeight(terrain, x, z) + 0.05f;
                points.Add(new Vector3(x, y, z));
            }

            var roadParent = new GameObject("LalayRoad");
            var roadMat = GetOrCreateTexturedMaterial(
                "RoadSurface", "Assets/ArteriaShantyTown/ShantyTown1/terraintextures/road.jpg",
                new Color(0.5f, 0.48f, 0.46f));

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                Vector3 mid = (a + b) * 0.5f;
                float length = Vector3.Distance(a, b);
                Vector3 dir = (b - a).normalized;

                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"RoadSegment_{i}";
                seg.transform.SetParent(roadParent.transform);
                seg.transform.position = mid;
                seg.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                seg.transform.localScale = new Vector3(6f, 0.08f, length + 0.5f);
                seg.GetComponent<Renderer>().sharedMaterial = roadMat;
                Object.DestroyImmediate(seg.GetComponent<Collider>());
            }

            return points;
        }

        // ---------------------------------------------------------------
        // Houses: real Shanty Town structures + hand-built modular houses
        // for the taller two-storey buildings, flanking the road.
        // ---------------------------------------------------------------

        // Only variants 1-14 have a standalone Materials/shantyN.mat in the
        // source pack (confirmed by inspecting the pack's Materials folder).
        // 15/16/18/19/20 have no matching material and import white/pink -
        // that was the "white shanty house" bug. Excluded.
        private static readonly int[] ShantyVariants = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        private const float ShantyScale = 2.1f; // source meshes import undersized relative to a ~2m-tall human

        private static void BuildHouses(Terrain terrain, List<Vector3> roadPoints)
        {
            var parent = new GameObject("LalayHouses");
            int variantIndex = 0;

            // Dense placement: sample points every ~7m along the road
            // polyline (not just at the coarse mesh-segment vertices), so
            // houses read as a packed village row rather than scattered
            // dots, per the brief's "more house density" / "close to both
            // sides of the road".
            List<Vector3> placementPoints = ResamplePolyline(roadPoints, 7f);

            for (int i = 1; i < placementPoints.Count - 1; i++)
            {
                Vector3 roadPos = placementPoints[i];
                Vector3 dir = (placementPoints[i + 1] - placementPoints[i - 1]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

                foreach (int side in new[] { -1, 1 })
                {
                    float lateral = 6.5f + Random(i * 7 + side, 0f, 3f);
                    Vector3 basePos = roadPos + right * side * lateral;
                    basePos.y = SampleHeight(terrain, basePos.x, basePos.z);

                    bool tallBuilding = (i + (side > 0 ? 1 : 0)) % 7 == 0;
                    bool skipForYardGap = (i + side) % 9 == 0; // occasional gap between houses
                    Quaternion rot = Quaternion.LookRotation(-right * side, Vector3.up)
                                      * Quaternion.Euler(0f, Random(i * 3 + side, -12f, 12f), 0f);

                    if (skipForYardGap) continue;

                    if (tallBuilding)
                    {
                        BuildProceduralHouse(parent.transform, basePos, rot, storeys: 2,
                            name: $"House_Tall_{i}_{side}");
                    }
                    else if (i % 3 != 0)
                    {
                        int variant = ShantyVariants[variantIndex % ShantyVariants.Length];
                        variantIndex++;
                        string path = $"Assets/ArteriaShantyTown/ShantyTown1/shanty{variant}.fbx";
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
                            instance.name = $"Shanty_{variant}_{i}_{side}";
                            instance.transform.position = basePos;
                            instance.transform.rotation = rot;
                            instance.transform.localScale = Vector3.one * ShantyScale;
                            AddBoundsCollider(instance);
                        }
                    }
                    else
                    {
                        BuildProceduralHouse(parent.transform, basePos, rot, storeys: 1,
                            name: $"House_{i}_{side}");
                    }
                }
            }

            ScatterShantyProps(parent.transform, terrain, roadPoints);
        }

        /// <summary>Interpolates a coarse polyline into points spaced ~stepLength apart.</summary>
        private static List<Vector3> ResamplePolyline(List<Vector3> points, float stepLength)
        {
            var result = new List<Vector3> { points[0] };
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                float segLength = Vector3.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.RoundToInt(segLength / stepLength));
                for (int s = 1; s <= steps; s++)
                {
                    result.Add(Vector3.Lerp(a, b, s / (float)steps));
                }
            }
            return result;
        }

        private static void BuildProceduralHouse(Transform parent, Vector3 pos, Quaternion rot, int storeys, string name)
        {
            var houseGo = new GameObject(name);
            houseGo.transform.SetParent(parent);
            houseGo.transform.position = pos;
            houseGo.transform.rotation = rot;

            float storeyHeight = 2.9f;
            float wallHeight = storeyHeight * storeys;
            float width = 5.2f;
            float depth = 4.4f;

            Color[] palette =
            {
                new Color(0.86f, 0.8f, 0.63f), new Color(0.72f, 0.82f, 0.78f),
                new Color(0.83f, 0.68f, 0.55f), new Color(0.7f, 0.78f, 0.6f),
                new Color(0.88f, 0.88f, 0.82f)
            };
            Color wallColor = palette[Mathf.Abs(name.GetHashCode()) % palette.Length];
            Material wallMat = GetOrCreateMaterial($"HouseWall_{ColorUtility.ToHtmlStringRGB(wallColor)}", wallColor);

            var walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walls.name = "Walls";
            walls.transform.SetParent(houseGo.transform, false);
            walls.transform.localScale = new Vector3(width, wallHeight, depth);
            walls.transform.localPosition = new Vector3(0f, wallHeight / 2f, 0f);
            walls.GetComponent<Renderer>().sharedMaterial = wallMat;

            // Door: inset dark panel proud of the front face.
            Material doorMat = GetOrCreateMaterial("HouseDoor", new Color(0.28f, 0.19f, 0.13f));
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.transform.SetParent(houseGo.transform, false);
            door.transform.localScale = new Vector3(0.9f, 1.9f, 0.08f);
            door.transform.localPosition = new Vector3(0f, 0.95f, depth / 2f + 0.02f);
            door.GetComponent<Renderer>().sharedMaterial = doorMat;
            Object.DestroyImmediate(door.GetComponent<Collider>());

            // Windows per storey.
            Material windowMat = GetOrCreateMaterial("HouseWindow", new Color(0.55f, 0.7f, 0.72f));
            for (int s = 0; s < storeys; s++)
            {
                float wy = storeyHeight * s + storeyHeight * 0.55f;
                foreach (float wx in new[] { -width * 0.28f, width * 0.28f })
                {
                    var win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    win.name = $"Window_{s}";
                    win.transform.SetParent(houseGo.transform, false);
                    win.transform.localScale = new Vector3(0.8f, 0.8f, 0.06f);
                    win.transform.localPosition = new Vector3(wx, wy, depth / 2f + 0.02f);
                    win.GetComponent<Renderer>().sharedMaterial = windowMat;
                    Object.DestroyImmediate(win.GetComponent<Collider>());
                }
            }

            BuildGableRoof(houseGo.transform, width + 0.6f, depth + 0.6f, wallHeight, 26f);
        }

        private static void BuildGableRoof(Transform parent, float width, float depth, float wallHeight, float pitchDeg)
        {
            Color[] roofColors =
            {
                new Color(0.55f, 0.2f, 0.14f), new Color(0.62f, 0.62f, 0.66f),
                new Color(0.2f, 0.32f, 0.5f), new Color(0.24f, 0.42f, 0.28f)
            };
            Color roofColor = roofColors[Mathf.Abs(parent.name.GetHashCode()) % roofColors.Length];
            Material roofMat = GetOrCreateMaterial($"HouseRoof_{ColorUtility.ToHtmlStringRGB(roofColor)}", roofColor);

            float slopeLen = (width / 2f) / Mathf.Cos(pitchDeg * Mathf.Deg2Rad) + 0.4f;
            float ridgeRise = (width / 2f) * Mathf.Tan(pitchDeg * Mathf.Deg2Rad);

            foreach (int side in new[] { -1, 1 })
            {
                var slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slope.name = side < 0 ? "RoofSlopeA" : "RoofSlopeB";
                slope.transform.SetParent(parent, false);
                slope.transform.localScale = new Vector3(slopeLen, 0.14f, depth + 0.4f);
                // Ridge must be the HIGH edge and the eave (outer edge) the
                // LOW edge - rotating by +side*pitch put the ridge edge
                // down (an inverted "valley" roof); -side*pitch is correct.
                slope.transform.localRotation = Quaternion.Euler(0f, 0f, -side * pitchDeg);
                float xOff = side * (width / 4f);
                slope.transform.localPosition = new Vector3(xOff, wallHeight + ridgeRise / 2f, 0f);
                slope.GetComponent<Renderer>().sharedMaterial = roofMat;
                Object.DestroyImmediate(slope.GetComponent<Collider>());
            }
        }

        private static void ScatterShantyProps(Transform parent, Terrain terrain, List<Vector3> roadPoints)
        {
            string[] propPaths =
            {
                "Assets/ArteriaShantyTown/ShantyTown1/barrell.fbx",
                "Assets/ArteriaShantyTown/ShantyTown1/barrellB.fbx",
                "Assets/ArteriaShantyTown/ShantyTown1/clothesline.fbx",
                "Assets/ArteriaShantyTown/ShantyTown1/container.fbx",
                "Assets/ArteriaShantyTown/ShantyTown1/fencea.fbx",
                "Assets/ArteriaShantyTown/ShantyTown1/tyre.fbx",
            };

            var propsParent = new GameObject("ShantyProps");
            propsParent.transform.SetParent(parent);

            for (int i = 2; i < roadPoints.Count - 2; i += 2)
            {
                Vector3 roadPos = roadPoints[i];
                Vector3 dir = (roadPoints[i + 1] - roadPoints[i - 1]).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                int side = (i % 4 == 0) ? -1 : 1;

                string path = propPaths[i % propPaths.Length];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                Vector3 pos = roadPos + right * side * (4.5f + Random(i, 0f, 2f));
                pos.y = SampleHeight(terrain, pos.x, pos.z);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, propsParent.transform);
                instance.transform.position = pos;
                instance.transform.rotation = Quaternion.Euler(0f, Random(i, 0f, 360f), 0f);
            }
        }

        // ---------------------------------------------------------------
        // Vegetation
        // ---------------------------------------------------------------

        private static void BuildVegetation(Terrain terrain, List<Vector3> roadPoints)
        {
            var parent = new GameObject("Vegetation");

            string[] hillTrees =
            {
                "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Fruit_Tree_01_green.prefab",
            };
            string[] coastPalms =
            {
                "Assets/Aquaset/LowPolyTropicalBeach/Prefabs/PalmTree.prefab",
                "Assets/Aquaset/LowPolyTropicalBeach/Prefabs/PalmTree3.prefab",
            };

            // Palms along the coastal band.
            for (int i = 0; i < 18; i++)
            {
                float x = Random(i * 11, 5f, RoadX - 55f);
                float z = Random(i * 13 + 1, 10f, TerrainSize - 10f);
                PlacePrefab(coastPalms[i % coastPalms.Length], parent.transform, terrain, x, z, i);
            }

            // Fruit trees on the hill side.
            for (int i = 0; i < 22; i++)
            {
                float x = Random(i * 17 + 2, RoadX + 55f, TerrainSize - 10f);
                float z = Random(i * 19 + 3, 10f, TerrainSize - 10f);
                PlacePrefab(hillTrees[i % hillTrees.Length], parent.transform, terrain, x, z, i + 100);
            }
        }

        private static void PlacePrefab(string path, Transform parent, Terrain terrain, float x, float z, int seed)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = new Vector3(x, SampleHeight(terrain, x, z), z);
            instance.transform.rotation = Quaternion.Euler(0f, Random(seed, 0f, 360f), 0f);
            float scale = Random(seed + 500, 0.85f, 1.25f);
            instance.transform.localScale = Vector3.one * scale;
            SwapUrpMaterialsForBuiltIn(instance);
        }

        /// <summary>
        /// FBX-imported meshes (unlike primitives) never get a Collider
        /// automatically. Shanty Town structures had none, which is why
        /// the player could walk straight through them - fixed by adding
        /// a BoxCollider sized to the instance's actual rendered bounds
        /// (in local space, so it scales correctly with the instance).
        /// </summary>
        private static void AddBoundsCollider(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                worldBounds.Encapsulate(renderers[i].bounds);
            }

            var collider = instance.AddComponent<BoxCollider>();
            // Convert world-space bounds into this transform's local space
            // so the collider stays correct if the instance is scaled.
            Vector3 localCenter = instance.transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = new Vector3(
                worldBounds.size.x / Mathf.Max(0.0001f, instance.transform.lossyScale.x),
                worldBounds.size.y / Mathf.Max(0.0001f, instance.transform.lossyScale.y),
                worldBounds.size.z / Mathf.Max(0.0001f, instance.transform.lossyScale.z));
            collider.center = localCenter;
            collider.size = localSize;
        }

        /// <summary>
        /// Some packs (Aquaset Low Poly Tropical Beach) ship both a
        /// Materials/URP/ and Materials/Built-In/ variant of every
        /// material but wire prefabs to the URP one by default. This
        /// project has no URP package installed (see PROJECT-HANDOFF.md),
        /// so URP/Lit materials render magenta. Swap to the sibling
        /// Built-In material by filename when one exists.
        /// </summary>
        private static void SwapUrpMaterialsForBuiltIn(GameObject instance)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string matPath = AssetDatabase.GetAssetPath(mats[i]);
                    if (string.IsNullOrEmpty(matPath) || !matPath.Contains("/URP/")) continue;

                    string builtInPath = matPath.Replace("/URP/", "/Built-In/");
                    var builtInMat = AssetDatabase.LoadAssetAtPath<Material>(builtInPath);
                    if (builtInMat != null)
                    {
                        mats[i] = builtInMat;
                        changed = true;
                    }
                }
                if (changed) renderer.sharedMaterials = mats;
            }
        }

        // ---------------------------------------------------------------
        // Farm path + clearing (Montine turnoff)
        // ---------------------------------------------------------------

        private static void BuildFarmPathAndClearing(Terrain terrain, List<Vector3> roadPoints, out Transform farmPlot)
        {
            Vector3 turnoff = roadPoints[roadPoints.Count / 2];
            Vector3 dir = (roadPoints[roadPoints.Count / 2 + 1] - roadPoints[roadPoints.Count / 2 - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 farmCenter = turnoff + right * -1f * 70f + dir * 20f;
            farmCenter.y = SampleHeight(terrain, farmCenter.x, farmCenter.z);

            var pathParent = new GameObject("MontineFarmPath");
            Material dirtMat = GetOrCreateTexturedMaterial(
                "DirtyFarmPath", "Assets/ArteriaShantyTown/ShantyTown1/terraintextures/tyretracks.jpg",
                new Color(0.42f, 0.29f, 0.15f));
            Material clearingMat = GetOrCreateMaterial("FarmClearing", new Color(0.36f, 0.42f, 0.2f));
            Material soilMat = GetOrCreateMaterial("FarmSoil", new Color(0.33f, 0.22f, 0.11f));

            int steps = 6;
            for (int i = 0; i < steps; i++)
            {
                float t0 = i / (float)steps;
                float t1 = (i + 1) / (float)steps;
                Vector3 a = Vector3.Lerp(turnoff, farmCenter, t0);
                Vector3 b = Vector3.Lerp(turnoff, farmCenter, t1);
                a.y = SampleHeight(terrain, a.x, a.z) + 0.04f;
                b.y = SampleHeight(terrain, b.x, b.z) + 0.04f;
                Vector3 mid = (a + b) * 0.5f;

                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"DirtyPathSeg_{i}";
                seg.transform.SetParent(pathParent.transform);
                seg.transform.position = mid;
                seg.transform.rotation = Quaternion.LookRotation((b - a).normalized, Vector3.up);
                seg.transform.localScale = new Vector3(3.2f, 0.06f, Vector3.Distance(a, b) + 1f);
                seg.GetComponent<Renderer>().sharedMaterial = dirtMat;
                Object.DestroyImmediate(seg.GetComponent<Collider>());
            }

            var clearing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clearing.name = "FarmClearing";
            clearing.transform.SetParent(pathParent.transform);
            clearing.transform.position = farmCenter + Vector3.up * 0.02f;
            clearing.transform.localScale = new Vector3(16f, 0.04f, 14f);
            clearing.GetComponent<Renderer>().sharedMaterial = clearingMat;
            Object.DestroyImmediate(clearing.GetComponent<Collider>());

            // 6 plots in a 3x2 grid, per the brief's "six usable planting
            // spots" minimum - a single plot is explicitly not enough.
            Material soilEmptyMat = GetOrCreateMaterial("FarmSoilEmpty", new Color(0.62f, 0.5f, 0.35f));
            Vector3 gridRight = right;
            Vector3 gridForward = dir;
            Transform firstPlot = null;

            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    Vector3 plotPos = farmCenter
                        + gridRight * ((col - 1) * 3.4f)
                        + gridForward * (row * 3.4f - 1.5f);
                    plotPos.y = SampleHeight(terrain, plotPos.x, plotPos.z) + 0.03f;

                    var plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plot.name = $"FarmPlot_{row}_{col}";
                    plot.transform.SetParent(pathParent.transform);
                    plot.transform.position = plotPos;
                    plot.transform.localScale = new Vector3(2.6f, 0.05f, 2.6f);
                    var plotRenderer = plot.GetComponent<Renderer>();
                    plotRenderer.sharedMaterial = soilEmptyMat;
                    plot.GetComponent<Collider>().enabled = false;

                    var cropVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    cropVisual.name = "CropVisual";
                    cropVisual.transform.SetParent(plot.transform, false);
                    cropVisual.transform.localPosition = new Vector3(0f, 0.35f, 0f);
                    cropVisual.transform.localScale = Vector3.one * 0.15f;
                    Object.DestroyImmediate(cropVisual.GetComponent<Collider>());
                    var cropMat = new Material(Shader.Find("Standard")) { color = Color.green };
                    cropVisual.GetComponent<Renderer>().sharedMaterial = cropMat;
                    cropVisual.SetActive(false);

                    var plotInteractable = plot.AddComponent<FarmPlot>();
                    var so = new SerializedObject(plotInteractable);
                    so.FindProperty("soilRenderer").objectReferenceValue = plotRenderer;
                    so.FindProperty("cropVisualRoot").objectReferenceValue = cropVisual.transform;
                    so.FindProperty("cropRenderer").objectReferenceValue = cropVisual.GetComponent<Renderer>();
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (firstPlot == null) firstPlot = plot.transform;
                }
            }

            farmPlot = firstPlot;
        }

        // ---------------------------------------------------------------
        // Player / NPC / Camera
        // ---------------------------------------------------------------

        /// <summary>
        /// Builds one of the two controllable boys with CharacterController,
        /// PlayerController, FollowController (used while this one is NOT
        /// active), CharacterVitals, InteractionDetector, and a visual
        /// model. Both components stay on both characters at all times;
        /// CharacterSwitchManager toggles which is "live" via
        /// PlayerController.IsControlled / FollowController.FollowingEnabled
        /// rather than adding/removing components.
        /// </summary>
        private static CharacterSlot BuildControllableCharacter(
            string goName, string displayName, string modelPath, Color skinTint,
            Vector3 position, RuntimeAnimatorController animController, bool startActive)
        {
            var go = new GameObject(goName);
            if (startActive) go.tag = "Player";
            go.transform.position = position;

            var controller = go.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.4f;

            var visual = InstantiateCharacter(modelPath, go.transform, animController, skinTint);
            var animator = visual.GetComponentInChildren<Animator>();

            var vitals = go.AddComponent<CharacterVitals>();

            var playerController = go.AddComponent<PlayerController>();
            var pcSo = new SerializedObject(playerController);
            pcSo.FindProperty("animator").objectReferenceValue = animator;
            pcSo.FindProperty("vitals").objectReferenceValue = vitals;
            pcSo.ApplyModifiedPropertiesWithoutUndo();
            playerController.IsControlled = startActive;

            var followController = go.AddComponent<FollowController>();
            var fcSo = new SerializedObject(followController);
            fcSo.FindProperty("animator").objectReferenceValue = animator;
            fcSo.ApplyModifiedPropertiesWithoutUndo();
            followController.FollowingEnabled = !startActive;

            var interactionDetector = go.AddComponent<InteractionDetector>();
            interactionDetector.enabled = startActive;

            return new CharacterSlot
            {
                displayName = displayName,
                root = go,
                playerController = playerController,
                followController = followController,
                vitals = vitals,
                interactionDetector = interactionDetector
            };
        }

        private static void BuildTownNPCs(Terrain terrain, List<Vector3> roadPoints)
        {
            CropDefinition[] allCrops = LoadAllCropDefinitions();

            BuildNpc(terrain, roadPoints, index: 2, sideMul: 1f, goName: "NPC_Villager",
                modelPath: "Assets/Floreswa/Models/male03_1.fbx", role: NpcRole.Villager, cropsForBuyer: null);

            BuildNpc(terrain, roadPoints, index: 4, sideMul: -1f, goName: "NPC_Police",
                modelPath: "Assets/Floreswa/Models/male01_2.fbx", role: NpcRole.Police, cropsForBuyer: null);

            BuildNpc(terrain, roadPoints, index: 6, sideMul: 1f, goName: "NPC_Shopkeeper",
                modelPath: "Assets/Floreswa/Models/male02_2.fbx", role: NpcRole.Shopkeeper, cropsForBuyer: null);

            BuildNpc(terrain, roadPoints, index: 8, sideMul: -1f, goName: "NPC_Buyer",
                modelPath: "Assets/Floreswa/Models/male03_2.fbx", role: NpcRole.Buyer, cropsForBuyer: allCrops);
        }

        private static void BuildNpc(
            Terrain terrain, List<Vector3> roadPoints, int index, float sideMul, string goName,
            string modelPath, NpcRole role, CropDefinition[] cropsForBuyer)
        {
            index = Mathf.Clamp(index, 1, roadPoints.Count - 2);
            Vector3 pos = roadPoints[index];
            Vector3 dir = (roadPoints[index + 1] - roadPoints[index - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            pos += right * sideMul * 3.5f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);

            var npcGo = new GameObject(goName);
            npcGo.transform.position = pos;
            npcGo.transform.rotation = Quaternion.LookRotation(-right * sideMul, Vector3.up);

            InstantiateCharacter(modelPath, npcGo.transform, null, null);

            var npc = npcGo.AddComponent<TownNPCInteractable>();
            var so = new SerializedObject(npc);
            so.FindProperty("role").enumValueIndex = (int)role;
            so.FindProperty("npcName").stringValue = goName.Replace("NPC_", "");
            if (cropsForBuyer != null)
            {
                var arrProp = so.FindProperty("sellableCrops");
                arrProp.arraySize = cropsForBuyer.Length;
                for (int i = 0; i < cropsForBuyer.Length; i++)
                {
                    arrProp.GetArrayElementAtIndex(i).objectReferenceValue = cropsForBuyer[i];
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject InstantiateCharacter(
            string fbxPath, Transform parent, RuntimeAnimatorController animController, Color? skinTint = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = "Visual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            if (animController != null) animator.runtimeAnimatorController = animController;
            animator.applyRootMotion = false;

            if (skinTint.HasValue)
            {
                ApplySkinTint(instance, skinTint.Value);
            }

            return instance;
        }

        /// <summary>
        /// Low Poly Character Pack renderers carry a material slot literally
        /// named "skin" (confirmed by inspection). Clone it per-instance
        /// (materials are shared assets by default - editing sharedMaterial
        /// directly would recolour every character using this prefab) so
        /// Smart and the NPC/Strong can read as visually distinct people,
        /// per the corrective brief's "clearly different... body
        /// silhouettes" requirement and the user's explicit skin-tone ask.
        /// </summary>
        private static void ApplySkinTint(GameObject instance, Color tint)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].name.ToLowerInvariant().Contains("skin"))
                    {
                        var clone = new Material(mats[i]) { color = tint };
                        mats[i] = clone;
                    }
                }
                renderer.sharedMaterials = mats;
            }
        }

        private static ThirdPersonFollowCamera BuildCamera(Transform player)
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();

            var follow = camGo.AddComponent<ThirdPersonFollowCamera>();
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();

            camGo.transform.position = player.position + new Vector3(0f, 6.8f, -7f);
            camGo.transform.LookAt(player.position + Vector3.up * 1.4f);

            return follow;
        }

        private static void WriteSlot(SerializedProperty slotProp, CharacterSlot slot)
        {
            slotProp.FindPropertyRelative("displayName").stringValue = slot.displayName;
            slotProp.FindPropertyRelative("root").objectReferenceValue = slot.root;
            slotProp.FindPropertyRelative("playerController").objectReferenceValue = slot.playerController;
            slotProp.FindPropertyRelative("followController").objectReferenceValue = slot.followController;
            slotProp.FindPropertyRelative("vitals").objectReferenceValue = slot.vitals;
            slotProp.FindPropertyRelative("interactionDetector").objectReferenceValue = slot.interactionDetector;
        }

        // ---------------------------------------------------------------
        // Economy / crops
        // ---------------------------------------------------------------

        private static readonly (string id, string name, int price, bool illegal, string unripeHex, string ripeHex)[] CropSpecs =
        {
            ("tomato", "Tomato", 5, false, "4D8C40", "BF1F1A"),
            ("banana", "Banana", 6, false, "5C9E3E", "E8D23C"),
            ("carrot", "Carrot", 4, false, "4D8C40", "E07A1F"),
            ("bushers", "Bushers", 22, true, "3A6B2E", "5B7A2E"),
        };

        private static CropDefinition[] BuildEconomyAndCrops()
        {
            EnsureFolder("Assets/UpIzUpMini/Data/Crops");
            var crops = new CropDefinition[CropSpecs.Length];

            for (int i = 0; i < CropSpecs.Length; i++)
            {
                var spec = CropSpecs[i];
                string path = $"Assets/UpIzUpMini/Data/Crops/{spec.id}.asset";
                var crop = AssetDatabase.LoadAssetAtPath<CropDefinition>(path);
                if (crop == null)
                {
                    crop = ScriptableObject.CreateInstance<CropDefinition>();
                    AssetDatabase.CreateAsset(crop, path);
                }

                crop.cropId = spec.id;
                crop.displayName = spec.name;
                crop.sellPrice = spec.price;
                crop.isIllegal = spec.illegal;
                crop.growDurationSeconds = 18f;
                ColorUtility.TryParseHtmlString("#" + spec.unripeHex, out var unripe);
                ColorUtility.TryParseHtmlString("#" + spec.ripeHex, out var ripe);
                crop.unripeColor = unripe;
                crop.ripeColor = ripe;
                EditorUtility.SetDirty(crop);

                crops[i] = crop;
            }

            var economyGo = new GameObject("EconomyManager");
            economyGo.AddComponent<EconomyManager>();

            var selectionGo = new GameObject("CropSelectionController");
            var selection = selectionGo.AddComponent<CropSelectionController>();
            var so = new SerializedObject(selection);
            var cropsProp = so.FindProperty("crops");
            cropsProp.arraySize = crops.Length;
            for (int i = 0; i < crops.Length; i++)
            {
                cropsProp.GetArrayElementAtIndex(i).objectReferenceValue = crops[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return crops;
        }

        private static CropDefinition[] LoadAllCropDefinitions()
        {
            var crops = new CropDefinition[CropSpecs.Length];
            for (int i = 0; i < CropSpecs.Length; i++)
            {
                crops[i] = AssetDatabase.LoadAssetAtPath<CropDefinition>(
                    $"Assets/UpIzUpMini/Data/Crops/{CropSpecs[i].id}.asset");
            }
            return crops;
        }

        // ---------------------------------------------------------------
        // HUD
        // ---------------------------------------------------------------

        private static void BuildHUD()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("HUDCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            Image healthFill = CreateMeter(canvasGo.transform, "Health", new Vector2(20f, -20f), new Color(0.8f, 0.15f, 0.15f), font, out _);
            Image staminaFill = CreateMeter(canvasGo.transform, "Stamina", new Vector2(20f, -50f), new Color(0.2f, 0.55f, 0.85f), font, out _);
            Image heatFill = CreateMeter(canvasGo.transform, "Heat", new Vector2(20f, -80f), new Color(0.9f, 0.55f, 0.1f), font, out _);

            Text moneyLabel = CreateLabel(canvasGo.transform, "$0", 30, new Vector2(-140f, -20f), font);
            AnchorTopRight(moneyLabel.rectTransform);

            Text nameLabel = CreateLabel(canvasGo.transform, "Smart", 26, new Vector2(140f, -110f), font);
            nameLabel.rectTransform.anchorMin = nameLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            nameLabel.alignment = TextAnchor.MiddleLeft;

            Text cropLabel = CreateLabel(canvasGo.transform, string.Empty, 26, new Vector2(-140f, -60f), font);
            AnchorTopRight(cropLabel.rectTransform);

            var hud = canvasGo.AddComponent<HUDController>();
            var so = new SerializedObject(hud);
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("staminaFill").objectReferenceValue = staminaFill;
            so.FindProperty("heatFill").objectReferenceValue = heatFill;
            so.FindProperty("moneyLabel").objectReferenceValue = moneyLabel;
            so.FindProperty("characterNameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("cropSelectionLabel").objectReferenceValue = cropLabel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AnchorTopRight(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        }

        private static Image CreateMeter(Transform parent, string label, Vector2 anchoredPos, Color fillColor, Font font, out Text labelText)
        {
            var bgGo = new GameObject($"Meter_{label}_BG");
            bgGo.transform.SetParent(parent, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.sizeDelta = new Vector2(220f, 22f);
            bgRect.anchoredPosition = anchoredPos;
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.5f);

            var fillGo = new GameObject($"Meter_{label}_Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;

            var labelGo = new GameObject($"Meter_{label}_Label");
            labelGo.transform.SetParent(bgGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            labelText = labelGo.AddComponent<Text>();
            labelText.text = label;
            labelText.font = font;
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;

            return fillImage;
        }

        /// <summary>
        /// Esc-to-pause menu (Resume/Quit) and the EventSystem it needs for
        /// button clicks/keyboard navigation. Uses legacy UGUI Text (no
        /// TextMeshPro package in this project yet - see Phase C scope in
        /// TASKS.md) with Unity's built-in Arial font.
        /// </summary>
        private static void BuildPauseMenu()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("PauseMenuCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("PausePanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.72f);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var title = CreateLabel(panelGo.transform, "PAUSED", 56, new Vector2(0f, 160f), builtinFont);
            title.alignment = TextAnchor.MiddleCenter;

            var resumeBtn = CreateButton(panelGo.transform, "Resume", new Vector2(0f, 20f), builtinFont);
            var quitBtn = CreateButton(panelGo.transform, "Quit (Q)", new Vector2(0f, -70f), builtinFont);

            var controller = canvasGo.AddComponent<PauseMenuController>();
            var so = new SerializedObject(controller);
            so.FindProperty("panel").objectReferenceValue = panelGo;
            so.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            so.FindProperty("quitButton").objectReferenceValue = quitBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(resumeBtn.onClick, controller.ResumeGame);
            UnityEventTools.AddPersistentListener(quitBtn.onClick, controller.QuitGame);

            panelGo.SetActive(false);
        }

        private static Text CreateLabel(Transform parent, string text, int fontSize, Vector2 anchoredPos, Font font)
        {
            var go = new GameObject($"Label_{text}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 80f);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;

            var label = go.AddComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            return label;
        }

        private static Button CreateButton(Transform parent, string text, Vector2 anchoredPos, Font font)
        {
            var go = new GameObject($"Button_{text}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 64f);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.24f, 0.95f);
            var button = go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;

            return button;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static float Random(int seed, float min, float max)
        {
            var rng = new System.Random(seed * 7919 + 104729);
            return Mathf.Lerp(min, max, (float)rng.NextDouble());
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

        private static Material GetOrCreateTexturedMaterial(string name, string texturePath, Color fallbackTint)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var mat = new Material(shader) { color = Color.white };
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex != null)
            {
                mat.mainTexture = tex;
                mat.mainTextureScale = new Vector2(1f, 8f);
            }
            else
            {
                mat.color = fallbackTint;
            }
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

            var list = new List<EditorBuildSettingsScene>(scenes)
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
