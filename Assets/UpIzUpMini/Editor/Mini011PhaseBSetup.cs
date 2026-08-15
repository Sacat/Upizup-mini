using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

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

            Transform player = BuildPlayer(terrain, roadPoints, locomotionController);
            BuildNPC(terrain, roadPoints);
            BuildCamera(player);

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

            var plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plot.name = "FarmPlot";
            plot.transform.SetParent(pathParent.transform);
            plot.transform.position = farmCenter + Vector3.up * 0.03f + Vector3.forward * 2f;
            plot.transform.localScale = new Vector3(4f, 0.05f, 3f);
            plot.GetComponent<Renderer>().sharedMaterial = soilMat;
            plot.GetComponent<Collider>().enabled = false;

            var plotInteractable = plot.AddComponent<FarmPlotInteractable>();
            var so = new SerializedObject(plotInteractable);
            so.FindProperty("soilRenderer").objectReferenceValue = plot.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();

            farmPlot = plot.transform;
        }

        // ---------------------------------------------------------------
        // Player / NPC / Camera
        // ---------------------------------------------------------------

        private static Transform BuildPlayer(Terrain terrain, List<Vector3> roadPoints, RuntimeAnimatorController animController)
        {
            Vector3 start = roadPoints[0];
            start.y = SampleHeight(terrain, start.x, start.z);

            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = start;

            var controller = playerGo.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.4f;

            // Smart (this controllable character) is the darker-skinned of
            // the two boys per user direction; Strong (NPC placeholder
            // until Phase C's switching system exists) is lighter.
            var visual = InstantiateCharacter(
                "Assets/Floreswa/Models/male01_1.fbx", playerGo.transform, animController,
                new Color(0.35f, 0.22f, 0.13f));

            var playerController = playerGo.AddComponent<PlayerController>();
            var pcSo = new SerializedObject(playerController);
            pcSo.FindProperty("animator").objectReferenceValue = visual.GetComponentInChildren<Animator>();
            pcSo.ApplyModifiedPropertiesWithoutUndo();

            playerGo.AddComponent<InteractionDetector>();

            return playerGo.transform;
        }

        private static void BuildNPC(Terrain terrain, List<Vector3> roadPoints)
        {
            Vector3 pos = roadPoints[2];
            Vector3 dir = (roadPoints[3] - roadPoints[1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            pos += right * 3.5f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);

            var npcGo = new GameObject("NPC_Villager");
            npcGo.transform.position = pos;
            npcGo.transform.rotation = Quaternion.LookRotation(-right, Vector3.up);

            InstantiateCharacter("Assets/Floreswa/Models/male02_1.fbx", npcGo.transform, null,
                new Color(0.72f, 0.56f, 0.42f));
            npcGo.AddComponent<NPCInteractable>();
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

        private static void BuildCamera(Transform player)
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
