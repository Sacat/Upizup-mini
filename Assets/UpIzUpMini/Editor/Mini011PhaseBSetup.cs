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
using UpIzUpMini.Missions;
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

        // Speeds the imported StarterAssets locomotion clips were authored
        // and tuned for (ThirdPersonController.MoveSpeed / SprintSpeed in
        // the larger project). Blend thresholds and the character
        // controller both use these so the feet match the ground.
        public const float LocomotionWalkSpeed = 2.0f;
        public const float LocomotionRunSpeed = 5.335f;

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

            RuntimeAnimatorController locomotionController = LoadLocomotionController();

            BuildLighting();
            Terrain terrain = BuildTerrain();
            BuildSea();

            List<Vector3> roadPoints = BuildRoad(terrain);
            BuildHouses(terrain, roadPoints);
            // Farm is built before vegetation so trees can be kept clear of
            // the plots (previously a tree grew through the plantation).
            BuildFarmPathAndClearing(terrain, roadPoints, out Transform farmPlot);
            BuildVegetation(terrain, roadPoints);

            BuildBeachAndJetty(terrain);

            CropDefinition[] crops = BuildEconomyAndCrops();
            BuildTownNPCs(terrain, roadPoints, locomotionController);

            Vector3 startPos = roadPoints[0];
            startPos.y = SampleHeight(terrain, startPos.x, startPos.z);

            // Franki and Sacat use the larger project's own character
            // models. Measured reason (MINI-016): those avatars map 52
            // human bones over a 67-72 bone skeleton, where the previous
            // Floreswa models - authored Generic and force-converted to
            // Humanoid - mapped only 23 over 39 with an auto-estimated
            // rest pose. Retargeting the shared locomotion clips onto the
            // sparse auto-avatar is what produced the distorted legs.
            // Their own materials are used, so no skin tint / facial-hair
            // hiding is applied here.
            // Deril is the smart one, Franki the strong one (user
            // direction - supersedes the earlier Franki/Sacat naming).
            CharacterSlot smart = BuildControllableCharacter(
                "Deril", "Deril", "Assets/UpIzUpMini/Art/Characters/Mainchar.fbx",
                null, startPos, locomotionController, startActive: true);
            CharacterSlot strong = BuildControllableCharacter(
                "Franki", "Franki", "Assets/UpIzUpMini/Art/Characters/Strong.fbx",
                null, startPos + new Vector3(1.4f, 0f, -1.2f),
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

            // Windowed by default so the game can be minimised/resized.
            new GameObject("WindowMode").AddComponent<WindowModeController>();

            // Heat is driven by proximity to officers, not a timer.
            var heatGo = new GameObject("PoliceHeatController");
            var heatCtrl = heatGo.AddComponent<PoliceHeatController>();
            var heatSo = new SerializedObject(heatCtrl);
            var illegalProp = heatSo.FindProperty("illegalCrops");
            var illegal = new List<CropDefinition>();
            foreach (var c in crops) { if (c != null && c.isIllegal) illegal.Add(c); }
            illegalProp.arraySize = illegal.Count;
            for (int i = 0; i < illegal.Count; i++)
                illegalProp.GetArrayElementAtIndex(i).objectReferenceValue = illegal[i];
            heatSo.ApplyModifiedPropertiesWithoutUndo();

            // Police escalation: extra officers appear as heat climbs.
            var policeNpc = GameObject.Find("NPC_Police");
            if (policeNpc != null)
            {
                var spawnerGo = new GameObject("PoliceReinforcements");
                var spawner = spawnerGo.AddComponent<PoliceReinforcementSpawner>();
                var spSo = new SerializedObject(spawner);
                spSo.FindProperty("officerTemplate").objectReferenceValue = policeNpc;
                spSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var saveGo = new GameObject("SaveLoadSystem");
            var saveSystem = saveGo.AddComponent<SaveLoadSystem>();
            var saveSo = new SerializedObject(saveSystem);
            var knownProp = saveSo.FindProperty("knownCrops");
            knownProp.arraySize = crops.Length;
            for (int i = 0; i < crops.Length; i++)
            {
                knownProp.GetArrayElementAtIndex(i).objectReferenceValue = crops[i];
            }
            saveSo.ApplyModifiedPropertiesWithoutUndo();

            ShopItemDefinition[] farmStock = BuildShopStock(crops);
            ShopItemDefinition[] apparelStock = BuildApparelStock(crops);
            ShopItemDefinition[] landStock = BuildStock(LandSpecs, crops);
            ShopItemDefinition[] dealerStock = BuildStock(DealerSpecs, crops);
            ShopItemDefinition[] foodStock = BuildStock(FoodSpecs, crops);
            ShopItemDefinition[] pharmacyStock = BuildStock(PharmacySpecs, crops);
            BuildStreetSigns(terrain, roadPoints, _farmCenter);
            BuildExtraUI(farmStock, apparelStock, landStock, dealerStock, foodStock, pharmacyStock, roadPoints, _farmCenter);
            BuildMissions(terrain, roadPoints, _farmCenter, farmPlot);

            // Starting seeds so the player can plant before their first
            // shop trip.
            var starterGo = new GameObject("StarterSeeds");
            var starter = starterGo.AddComponent<StarterInventory>();
            var stSo = new SerializedObject(starter);
            var stProp = stSo.FindProperty("startingSeeds");
            stProp.arraySize = crops.Length;
            for (int i = 0; i < crops.Length; i++)
            {
                stProp.GetArrayElementAtIndex(i).objectReferenceValue = crops[i];
            }
            stSo.ApplyModifiedPropertiesWithoutUndo();

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

        private const string StarterControllerPath =
            "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller";

        /// <summary>
        /// Uses the larger project's own authored animator controller
        /// rather than generating one.
        ///
        /// The generated controller was a bare 1D blend tree with no
        /// `MotionSpeed` parameter, so clips played at a fixed rate
        /// regardless of how fast the character actually moved - the feet
        /// could never agree with the ground. The authored controller has
        /// Speed / MotionSpeed / Grounded / Jump / FreeFall and the
        /// transitions these clips were built for. Falls back to the
        /// generated one only if the asset is missing.
        /// </summary>
        private static RuntimeAnimatorController LoadLocomotionController()
        {
            var authored = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StarterControllerPath);
            if (authored != null)
            {
                Debug.Log("Mini011PhaseBSetup: using authored StarterAssetsThirdPerson controller.");
                return authored;
            }

            Debug.LogWarning($"Mini011PhaseBSetup: {StarterControllerPath} missing; " +
                             "falling back to the generated controller (locomotion will look worse).");
            return BuildAnimatorController();
        }

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

            // Locomotion clips come from the larger Up Iz Up project
            // (Unity StarterAssets / Mixamo-sourced). They are in-place
            // clips driven by a real m/s Speed parameter, so the blend
            // thresholds are the actual speeds Unity tuned them for:
            // MoveSpeed 2.0 and SprintSpeed 5.335.
            var idle = LoadClip("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx");
            var walk = LoadClip("Assets/UpIzUpMini/Art/Animations/Locomotion--Walk_N.anim.fbx");
            var run = LoadClip("Assets/UpIzUpMini/Art/Animations/Locomotion--Run_N.anim.fbx");

            var tree = new BlendTree
            {
                name = "Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            if (idle != null) tree.AddChild(idle, 0f);
            if (walk != null) tree.AddChild(walk, LocomotionWalkSpeed);
            if (run != null) tree.AddChild(run, LocomotionRunSpeed);

            var rootMachine = controller.layers[0].stateMachine;
            var state = rootMachine.AddState("Locomotion");
            state.motion = tree;
            rootMachine.defaultState = state;

            AssetDatabase.AddObjectToAsset(tree, controller);

            // Jump: triggered from PlayerController on Space, returns to
            // locomotion once the character is grounded again.
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);

            var jumpClip = LoadClip("Assets/UpIzUpMini/Art/Animations/Jump--Jump.anim.fbx");
            if (jumpClip != null)
            {
                var jumpState = rootMachine.AddState("Jump");
                jumpState.motion = jumpClip;

                var toJump = state.AddTransition(jumpState);
                toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
                toJump.hasExitTime = false;
                toJump.duration = 0.08f;

                var toGround = jumpState.AddTransition(state);
                toGround.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
                toGround.hasExitTime = true;
                toGround.exitTime = 0.7f;
                toGround.duration = 0.15f;
            }
            else
            {
                Debug.LogWarning("Mini011PhaseBSetup: jump clip not found; jump will move but not animate.");
            }

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

        /// <summary>
        /// Levels a circular area of terrain to the height at its centre,
        /// blending back out to the original hillside over `falloff`
        /// metres. Used to terrace the hillside farm so flat plot geometry
        /// sits correctly on it.
        /// </summary>
        private static void FlattenTerrainArea(Terrain terrain, Vector3 worldCenter, float radius, float falloff)
        {
            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, res, res);

            float targetNormalized = data.GetHeight(
                Mathf.RoundToInt(worldCenter.x / data.size.x * (res - 1)),
                Mathf.RoundToInt(worldCenter.z / data.size.z * (res - 1))) / data.size.y;

            float metresPerSample = data.size.x / (res - 1);
            int sampleRadius = Mathf.CeilToInt((radius + falloff) / metresPerSample);
            int cx = Mathf.RoundToInt(worldCenter.x / data.size.x * (res - 1));
            int cz = Mathf.RoundToInt(worldCenter.z / data.size.z * (res - 1));

            for (int z = cz - sampleRadius; z <= cz + sampleRadius; z++)
            {
                if (z < 0 || z >= res) continue;
                for (int x = cx - sampleRadius; x <= cx + sampleRadius; x++)
                {
                    if (x < 0 || x >= res) continue;

                    float dist = Vector2.Distance(new Vector2(x, z), new Vector2(cx, cz)) * metresPerSample;
                    if (dist > radius + falloff) continue;

                    float blend = dist <= radius
                        ? 1f
                        : 1f - Mathf.SmoothStep(0f, 1f, (dist - radius) / falloff);

                    // heights is indexed [z, x].
                    heights[z, x] = Mathf.Lerp(heights[z, x], targetNormalized, blend);
                }
            }

            data.SetHeights(0, 0, heights);
        }

        // ---------------------------------------------------------------
        // Sea (visual only for now - see DECISIONS.md D-007, Guadeloupe
        // sea-trade is an abstraction in this Mini, not a sailed route yet)
        // ---------------------------------------------------------------

        private const float SeaLevelY = 1.2f;

        // Recorded when the farm is built so later passes (vegetation) can
        // keep clear of the plantation.
        private static Vector3 _farmCenter;
        private static float _farmClearRadius = 22f;
        private static Vector3 _expansionPlotPos;
        private static Vector3 _bossPos;

        private static void BuildSea()
        {
            var seaGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            seaGo.name = "Sea";
            Object.DestroyImmediate(seaGo.GetComponent<Collider>());
            seaGo.transform.position = new Vector3(-40f, SeaLevelY, TerrainSize / 2f);
            seaGo.transform.localScale = new Vector3(20f, 1f, 40f);
            var mat = GetOrCreateMaterial("Sea", new Color(0.15f, 0.42f, 0.55f));
            var color = mat.color;
            color.a = 0.9f;
            mat.color = color;
            seaGo.GetComponent<Renderer>().sharedMaterial = mat;
        }

        /// <summary>
        /// Sandy beach strip, a timber jetty running out over the water,
        /// and a small moored boat. The boat is scene dressing for now -
        /// the Guadeloupe run is an abstracted dispatch per DECISIONS.md
        /// D-007, not a sailed route.
        /// </summary>
        private static void BuildBeachAndJetty(Terrain terrain)
        {
            var parent = new GameObject("CoastAndJetty");

            Material sandMat = GetOrCreateMaterial("BeachSand", new Color(0.85f, 0.78f, 0.58f));
            Material plankMat = GetOrCreateMaterial("JettyTimber", new Color(0.44f, 0.32f, 0.2f));
            Material postMat = GetOrCreateMaterial("JettyPost", new Color(0.33f, 0.24f, 0.15f));
            Material hullMat = GetOrCreateMaterial("BoatHull", new Color(0.85f, 0.85f, 0.82f));
            Material hullTrimMat = GetOrCreateMaterial("BoatTrim", new Color(0.15f, 0.35f, 0.6f));

            // Find where the shoreline sits: walk inland until terrain
            // rises above sea level, so the beach is placed on the actual
            // waterline rather than a guessed X.
            float shoreZ = TerrainSize * 0.5f;
            float shoreX = 10f;
            for (float x = 2f; x < RoadX; x += 1f)
            {
                if (SampleHeight(terrain, x, shoreZ) > SeaLevelY)
                {
                    shoreX = x;
                    break;
                }
            }

            var sand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sand.name = "BeachSand";
            sand.transform.SetParent(parent.transform);
            sand.transform.position = new Vector3(shoreX + 5f, SeaLevelY + 0.05f, shoreZ);
            sand.transform.localScale = new Vector3(26f, 0.3f, 70f);
            sand.GetComponent<Renderer>().sharedMaterial = sandMat;

            // Invisible wall at the water's edge. The player should only get
            // out over the sea via the jetty, so swimming is blocked rather
            // than simulated. The wall is split around the jetty mouth so
            // the deck stays walkable.
            float wallX = shoreX - 6f;
            float wallHeight = 6f;
            float gapHalfWidth = 3.4f;

            foreach (int side in new[] { -1, 1 })
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"ShorelineBarrier_{(side < 0 ? "A" : "B")}";
                wall.transform.SetParent(parent.transform);

                float halfSpan = (70f * 0.5f - gapHalfWidth) * 0.5f;
                float centreZ = shoreZ + side * (gapHalfWidth + halfSpan);

                wall.transform.position = new Vector3(wallX, SeaLevelY + wallHeight * 0.5f, centreZ);
                wall.transform.localScale = new Vector3(1.5f, wallHeight, halfSpan * 2f);
                Object.DestroyImmediate(wall.GetComponent<Renderer>());
            }

            // Jetty deck runs from the sand out over the water (-X).
            var jettyParent = new GameObject("Jetty");
            jettyParent.transform.SetParent(parent.transform);

            float deckY = SeaLevelY + 1.1f;
            float deckStartX = shoreX + 2f;
            float deckLength = 26f;

            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "JettyDeck";
            deck.transform.SetParent(jettyParent.transform);
            deck.transform.position = new Vector3(deckStartX - deckLength / 2f, deckY, shoreZ);
            deck.transform.localScale = new Vector3(deckLength, 0.25f, 4.2f);
            deck.GetComponent<Renderer>().sharedMaterial = plankMat;

            // Railings down both sides so the player can't walk off the
            // deck into the sea.
            foreach (int railSide in new[] { -1, 1 })
            {
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = $"JettyRail_{(railSide < 0 ? "A" : "B")}";
                rail.transform.SetParent(jettyParent.transform);
                rail.transform.position = new Vector3(
                    deckStartX - deckLength / 2f, deckY + 0.75f, shoreZ + railSide * 2.05f);
                rail.transform.localScale = new Vector3(deckLength, 1.4f, 0.16f);
                rail.GetComponent<Renderer>().sharedMaterial = postMat;
            }

            // End rail at the seaward tip.
            var endRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            endRail.name = "JettyRail_End";
            endRail.transform.SetParent(jettyParent.transform);
            endRail.transform.position = new Vector3(
                deckStartX - deckLength, deckY + 0.75f, shoreZ);
            endRail.transform.localScale = new Vector3(0.16f, 1.4f, 4.2f);
            endRail.GetComponent<Renderer>().sharedMaterial = postMat;

            for (int i = 0; i < 7; i++)
            {
                float px = deckStartX - 2f - i * 3.8f;
                foreach (int side in new[] { -1, 1 })
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.name = $"JettyPost_{i}_{side}";
                    post.transform.SetParent(jettyParent.transform);
                    post.transform.position = new Vector3(px, SeaLevelY - 0.6f, shoreZ + side * 1.8f);
                    post.transform.localScale = new Vector3(0.28f, 1.4f, 0.28f);
                    post.GetComponent<Renderer>().sharedMaterial = postMat;
                }
            }

            // Small moored boat alongside the jetty's far end.
            var boat = new GameObject("MooredBoat");
            boat.transform.SetParent(parent.transform);
            boat.transform.position = new Vector3(deckStartX - deckLength + 3f, SeaLevelY + 0.25f, shoreZ + 4.2f);
            boat.transform.rotation = Quaternion.Euler(0f, 12f, 0f);

            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.SetParent(boat.transform, false);
            hull.transform.localScale = new Vector3(4.6f, 0.75f, 1.7f);
            hull.GetComponent<Renderer>().sharedMaterial = hullMat;

            var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = "Trim";
            trim.transform.SetParent(boat.transform, false);
            trim.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            trim.transform.localScale = new Vector3(4.4f, 0.18f, 1.55f);
            trim.GetComponent<Renderer>().sharedMaterial = hullTrimMat;

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(boat.transform, false);
            cabin.transform.localPosition = new Vector3(-1.1f, 0.7f, 0f);
            cabin.transform.localScale = new Vector3(1.3f, 0.85f, 1.25f);
            cabin.GetComponent<Renderer>().sharedMaterial = hullTrimMat;
        }

        // ---------------------------------------------------------------
        // Road: a few connected, gently bending segments (not one
        // straight box) following the village shelf.
        // ---------------------------------------------------------------

        private static List<Vector3> BuildRoad(Terrain terrain)
        {
            var points = new List<Vector3>();
            // 16 segments (17 points). With only 8 the higher shop
            // indices all clamped to the same point, piling four shops on
            // one spot and putting the food stall on the Montine turnoff.
            int segments = 16;
            float totalLength = 260f;
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
            // Black asphalt per user direction - the Shanty Town road
            // texture is a pale gravel track, so it's tinted down heavily
            // rather than used at full brightness.
            var roadMat = GetOrCreateTexturedMaterial(
                "RoadSurface", "Assets/ArteriaShantyTown/ShantyTown1/terraintextures/road.jpg",
                new Color(0.5f, 0.48f, 0.46f));
            roadMat.color = new Color(0.13f, 0.13f, 0.14f);

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
                // Overlap generously so no gap shows where segments meet at bends.
                seg.transform.localScale = new Vector3(6f, 0.08f, length + 1.6f);
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

            // Leave the market frontage clear so the shop/buyer stalls
            // aren't buried inside a house (BuildMarketArea uses index 6).
            int marketIndex = Mathf.Clamp(6, 1, roadPoints.Count - 2);
            Vector3 marketPos = roadPoints[marketIndex];

            // The Montine farm track leaves the road at its midpoint
            // (BuildFarmPathAndClearing), so that junction must stay clear.
            Vector3 montineTurnoff = roadPoints[roadPoints.Count / 2];

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

                    // Keep the market frontage open on both sides.
                    if (Vector3.Distance(basePos, marketPos) < 11f) continue;

                    // Keep the Montine turnoff clear - a house was sitting
                    // across the junction where the farm track leaves the
                    // Lalay road, blocking the route.
                    if (Vector3.Distance(basePos, montineTurnoff) < 16f) continue;
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
            // Keep vegetation out of the plantation - a tree growing up
            // through the farm plots looked wrong.
            if (Vector2.Distance(new Vector2(x, z), new Vector2(_farmCenter.x, _farmCenter.z)) < _farmClearRadius)
            {
                return;
            }

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

            // One box around the whole instance was far too generous -
            // shanty structures have overhanging roofs and lean-tos, so a
            // single bounding box blocked the player metres away from the
            // actual walls. Give each renderer its own collider instead,
            // which tracks the real shape much more closely.
            foreach (var renderer in renderers)
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null) continue;

                var go = renderer.gameObject;
                if (go.GetComponent<Collider>() != null) continue;

                var box = go.AddComponent<BoxCollider>();
                // Local-space mesh bounds, so the box follows this
                // renderer's own transform and scale exactly.
                Bounds local = meshFilter.sharedMesh.bounds;
                box.center = local.center;

                // Trim slightly so eaves and thin trim don't push the
                // player away from the wall face.
                box.size = new Vector3(
                    local.size.x * 0.92f,
                    local.size.y,
                    local.size.z * 0.92f);
            }
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

            // Montine is in the hills, so the farm sits inland/uphill
            // (+X is the rising hill side of this terrain - see
            // BuildTerrain) rather than on the flat coastal side.
            Vector3 farmCenter = turnoff + right * 1f * 78f + dir * 24f;

            // Carve a level terrace for the farm. Without this the plots
            // would sit on the raw hillside and float/intersect, since each
            // plot is a flat box sampled at its own height.
            FlattenTerrainArea(terrain, farmCenter, radius: 26f, falloff: 14f);
            farmCenter.y = SampleHeight(terrain, farmCenter.x, farmCenter.z);
            _farmCenter = farmCenter;

            var pathParent = new GameObject("MontineFarmPath");
            Material dirtMat = GetOrCreateTexturedMaterial(
                "DirtyFarmPath", "Assets/ArteriaShantyTown/ShantyTown1/terraintextures/tyretracks.jpg",
                new Color(0.42f, 0.29f, 0.15f));
            Material clearingMat = GetOrCreateMaterial("FarmClearing", new Color(0.36f, 0.42f, 0.2f));
            Material soilMat = GetOrCreateMaterial("FarmSoil", new Color(0.33f, 0.22f, 0.11f));

            // Stop the track at the edge of the plantation rather than
            // running it straight through the middle of the plots.
            Vector3 pathEnd = Vector3.Lerp(turnoff, farmCenter,
                1f - (10f / Mathf.Max(1f, Vector3.Distance(turnoff, farmCenter))));

            int steps = 6;
            for (int i = 0; i < steps; i++)
            {
                float t0 = i / (float)steps;
                float t1 = (i + 1) / (float)steps;
                Vector3 a = Vector3.Lerp(turnoff, pathEnd, t0);
                Vector3 b = Vector3.Lerp(turnoff, pathEnd, t1);
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
                // Keep the collider: the track sits proud of the terrain,
                // so without one the player runs straight through it.
                var segCollider = seg.GetComponent<BoxCollider>();
                if (segCollider != null) segCollider.isTrigger = false;
            }

            // Tilled ground under the plantation, aligned to the plot grid
            // and rotated to match it.
            var clearing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clearing.name = "FarmClearing";
            clearing.transform.SetParent(pathParent.transform);
            clearing.transform.position = farmCenter + Vector3.up * 0.015f;
            clearing.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            clearing.transform.localScale = new Vector3(19f, 0.03f, 13f);
            clearing.GetComponent<Renderer>().sharedMaterial = clearingMat;
            Object.DestroyImmediate(clearing.GetComponent<Collider>());

            // 8 plots in a tidy 4x2 grid aligned to the farm's own axes, so
            // the plantation reads as a laid-out smallholding rather than
            // scattered patches.
            Material soilEmptyMat = GetOrCreateMaterial("FarmSoilEmpty", new Color(0.52f, 0.32f, 0.16f));
            Transform firstPlot = null;

            const int cols = 4;
            const int rows = 2;
            const float spacingX = 3.6f;
            const float spacingZ = 4.0f;
            var farmParent = new GameObject("MontineFarm");
            farmParent.transform.position = farmCenter;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Vector3 plotPos = farmCenter
                        + right * ((col - (cols - 1) * 0.5f) * spacingX)
                        + dir * ((row - (rows - 1) * 0.5f) * spacingZ);
                    plotPos.y = SampleHeight(terrain, plotPos.x, plotPos.z) + 0.03f;

                    var plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plot.name = $"FarmPlot_{row}{col}";
                    plot.transform.SetParent(farmParent.transform);
                    plot.transform.position = plotPos;
                    plot.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    plot.transform.localScale = new Vector3(2.8f, 0.06f, 3.0f);
                    var plotRenderer = plot.GetComponent<Renderer>();
                    plotRenderer.sharedMaterial = soilEmptyMat;
                    plot.GetComponent<Collider>().enabled = false;

                    var tomatoVisual = BuildCropVisual(plot.transform, "TomatoVisual",
                        "Assets/UpIzUpMini/Art/CropMeshes/TomatoPlant_LOD.asset", 1.7f, fruitCount: 4);
                    var weedVisual = BuildCropVisual(plot.transform, "WeedVisual",
                        "Assets/UpIzUpMini/Art/CropMeshes/WeedPlant_LOD.asset", 1.9f, fruitCount: 0);

                    var plotInteractable = plot.AddComponent<FarmPlot>();
                    var so = new SerializedObject(plotInteractable);
                    so.FindProperty("soilRenderer").objectReferenceValue = plotRenderer;
                    so.FindProperty("tomatoVisual").objectReferenceValue = tomatoVisual;
                    so.FindProperty("weedVisual").objectReferenceValue = weedVisual;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    if (firstPlot == null) firstPlot = plot.transform;
                }
            }

            BuildFarmSafehouse(terrain, farmCenter, right, dir);
            BuildExpansionPlots(terrain, farmCenter, right, dir, soilEmptyMat, farmParent.transform);

            farmPlot = firstPlot;
        }

        /// <summary>
        /// Fenced-off expansion plots beside the main farm. They only
        /// become usable once the Montine land is bought, giving the land
        /// purchase a real effect (see LockedFarmPlot).
        /// </summary>
        private static void BuildExpansionPlots(
            Terrain terrain, Vector3 farmCenter, Vector3 right, Vector3 dir,
            Material soilMat, Transform parent)
        {
            Material fenceMat = GetOrCreateMaterial("LandFence", new Color(0.55f, 0.42f, 0.26f));

            for (int col = 0; col < 3; col++)
            {
                Vector3 pos = farmCenter
                              + right * ((col - 1) * 3.6f)
                              + dir * 8.6f;
                pos.y = SampleHeight(terrain, pos.x, pos.z) + 0.03f;

                if (col == 1) _expansionPlotPos = pos;

                var plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plot.name = $"FarmPlot_X{col}";
                plot.transform.SetParent(parent);
                plot.transform.position = pos;
                plot.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                plot.transform.localScale = new Vector3(2.8f, 0.06f, 3.0f);
                var plotRenderer = plot.GetComponent<Renderer>();
                plotRenderer.sharedMaterial = soilMat;
                plot.GetComponent<Collider>().enabled = false;

                var tomatoVisual = BuildCropVisual(plot.transform, "TomatoVisual",
                    "Assets/UpIzUpMini/Art/CropMeshes/TomatoPlant_LOD.asset", 1.7f, fruitCount: 4);
                var weedVisual = BuildCropVisual(plot.transform, "WeedVisual",
                    "Assets/UpIzUpMini/Art/CropMeshes/WeedPlant_LOD.asset", 1.9f, fruitCount: 0);

                var plotComponent = plot.AddComponent<FarmPlot>();
                var pso = new SerializedObject(plotComponent);
                pso.FindProperty("soilRenderer").objectReferenceValue = plotRenderer;
                pso.FindProperty("tomatoVisual").objectReferenceValue = tomatoVisual;
                pso.FindProperty("weedVisual").objectReferenceValue = weedVisual;
                pso.ApplyModifiedPropertiesWithoutUndo();

                // Simple fence marking the land as not yet owned.
                var fence = new GameObject("LockedFence");
                fence.transform.SetParent(plot.transform, false);
                // Counter the plot's non-uniform scale.
                fence.transform.localScale = new Vector3(1f / 2.8f, 1f / 0.06f, 1f / 3.0f);
                for (int i = 0; i < 4; i++)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.transform.SetParent(fence.transform, false);
                    float t = i / 3f;
                    post.transform.localPosition = new Vector3(-1.3f + t * 2.6f, 0.55f, 1.5f);
                    post.transform.localScale = new Vector3(0.1f, 1.1f, 0.1f);
                    post.GetComponent<Renderer>().sharedMaterial = fenceMat;
                    Object.DestroyImmediate(post.GetComponent<Collider>());
                }
                var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.transform.SetParent(fence.transform, false);
                rail.transform.localPosition = new Vector3(0f, 0.95f, 1.5f);
                rail.transform.localScale = new Vector3(2.8f, 0.1f, 0.08f);
                rail.GetComponent<Renderer>().sharedMaterial = fenceMat;
                Object.DestroyImmediate(rail.GetComponent<Collider>());

                var locked = plot.AddComponent<LockedFarmPlot>();
                var lso = new SerializedObject(locked);
                lso.FindProperty("requiredItemId").stringValue = "land_montine";
                lso.FindProperty("lockedVisual").objectReferenceValue = fence;
                lso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Builds a crop visual from a decimated real plant mesh plus
        /// separate ripening fruit spheres. The plot cube is non-uniformly
        /// scaled, so the visual root counter-scales to keep the plant's
        /// proportions correct.
        /// </summary>
        private static CropStageVisual BuildCropVisual(
            Transform plot, string name, string meshPath, float plantHeightMetres, int fruitCount)
        {
            var root = new GameObject(name);
            root.transform.SetParent(plot, false);
            root.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            Vector3 plotScale = plot.localScale;
            root.transform.localScale = new Vector3(1f / plotScale.x, 1f / plotScale.y, 1f / plotScale.z);

            var plantRoot = new GameObject("Plant");
            plantRoot.transform.SetParent(root.transform, false);

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            Renderer plantRenderer = null;
            if (mesh != null)
            {
                var meshGo = new GameObject("PlantMesh");
                meshGo.transform.SetParent(plantRoot.transform, false);
                meshGo.AddComponent<MeshFilter>().sharedMesh = mesh;
                plantRenderer = meshGo.AddComponent<MeshRenderer>();
                plantRenderer.sharedMaterial = GetOrCreateMaterial("CropFoliage", new Color(0.22f, 0.42f, 0.16f));

                // Source scans are authored ~2cm tall; normalize to a real
                // plant height so they aren't invisible specks.
                float normalize = plantHeightMetres / Mathf.Max(0.0001f, mesh.bounds.size.y);
                meshGo.transform.localScale = Vector3.one * normalize;

                // The scan is centred on its origin, so half the plant sat
                // below the soil and the fruit ended up hovering above its
                // visible top. Lift it so the base rests at y=0.
                meshGo.transform.localPosition = new Vector3(0f, -mesh.bounds.min.y * normalize, 0f);
            }
            else
            {
                Debug.LogWarning($"Mini011PhaseBSetup: crop mesh missing at {meshPath}");
            }

            var fruits = new List<Renderer>();
            for (int i = 0; i < fruitCount; i++)
            {
                float angle = (i / (float)Mathf.Max(1, fruitCount)) * Mathf.PI * 2f;
                var fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fruit.name = $"Fruit_{i}";
                fruit.transform.SetParent(plantRoot.transform, false);
                // Keep fruit inside the plant's real height band (base at
                // y=0, top at plantHeightMetres) so it hangs on the plant
                // rather than floating above it.
                fruit.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * plantHeightMetres * 0.16f,
                    plantHeightMetres * (0.30f + 0.11f * i),
                    Mathf.Sin(angle) * plantHeightMetres * 0.16f);
                fruit.transform.localScale = Vector3.one * (plantHeightMetres * 0.15f);
                Object.DestroyImmediate(fruit.GetComponent<Collider>());
                var fr = fruit.GetComponent<Renderer>();
                fr.sharedMaterial = GetOrCreateMaterial("CropFruit", Color.green);
                fruits.Add(fr);
            }

            var visual = root.AddComponent<CropStageVisual>();
            var so = new SerializedObject(visual);
            so.FindProperty("plantRoot").objectReferenceValue = plantRoot.transform;
            so.FindProperty("plantRenderer").objectReferenceValue = plantRenderer;
            var fruitsProp = so.FindProperty("fruitRenderers");
            fruitsProp.arraySize = fruits.Count;
            for (int i = 0; i < fruits.Count; i++)
            {
                fruitsProp.GetArrayElementAtIndex(i).objectReferenceValue = fruits[i];
            }
            so.FindProperty("fullScale").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return visual;
        }

        /// <summary>Simple enterable-looking safehouse beside the hillside farm.</summary>
        private static void BuildFarmSafehouse(Terrain terrain, Vector3 farmCenter, Vector3 right, Vector3 dir)
        {
            Vector3 pos = farmCenter + right * 14f - dir * 8f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);

            var rot = Quaternion.LookRotation(-right, Vector3.up);
            var parent = new GameObject("FarmSafehouse");
            parent.transform.position = pos;
            parent.transform.rotation = rot;

            BuildOpenSafehouse(parent.transform, pos, rot);

            // Rest point sits on the bed itself.
            var restGo = new GameObject("FarmSafehouse_Rest");
            restGo.transform.SetParent(parent.transform);
            restGo.transform.position = pos + rot * new Vector3(0f, 0.6f, 0f);
            restGo.AddComponent<SafehouseInteractable>();
        }

        /// <summary>
        /// An open-fronted shelter with a bed - deliberately no door, so
        /// the player can simply walk in and rest (user direction).
        /// </summary>
        private static void BuildOpenSafehouse(Transform parent, Vector3 pos, Quaternion rot)
        {
            var house = new GameObject("FarmSafehouse_Building");
            house.transform.SetParent(parent);
            house.transform.position = pos;
            house.transform.rotation = rot;

            Material wallMat = GetOrCreateMaterial("SafehouseWall", new Color(0.84f, 0.78f, 0.62f));
            Material roofMat = GetOrCreateMaterial("SafehouseRoof", new Color(0.45f, 0.22f, 0.16f));
            Material bedMat = GetOrCreateMaterial("SafehouseBed", new Color(0.85f, 0.85f, 0.88f));
            Material frameMat = GetOrCreateMaterial("SafehouseFrame", new Color(0.35f, 0.24f, 0.15f));

            float w = 5.2f, d = 4.6f, h = 2.9f;

            // Three walls, front left open.
            void Wall(string name, Vector3 localPos, Vector3 scale)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(house.transform, false);
                go.transform.localPosition = localPos;
                go.transform.localScale = scale;
                go.GetComponent<Renderer>().sharedMaterial = wallMat;
            }

            Wall("BackWall", new Vector3(0f, h / 2f, -d / 2f), new Vector3(w, h, 0.25f));
            Wall("SideWallL", new Vector3(-w / 2f, h / 2f, 0f), new Vector3(0.25f, h, d));
            Wall("SideWallR", new Vector3(w / 2f, h / 2f, 0f), new Vector3(0.25f, h, d));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(house.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            floor.transform.localScale = new Vector3(w, 0.1f, d);
            floor.GetComponent<Renderer>().sharedMaterial = frameMat;

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(house.transform, false);
            roof.transform.localPosition = new Vector3(0f, h + 0.1f, 0f);
            roof.transform.localScale = new Vector3(w + 0.6f, 0.2f, d + 0.6f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMat;

            // Bed.
            var bedFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bedFrame.name = "BedFrame";
            bedFrame.transform.SetParent(house.transform, false);
            bedFrame.transform.localPosition = new Vector3(0f, 0.3f, -0.9f);
            bedFrame.transform.localScale = new Vector3(1.4f, 0.4f, 2.4f);
            bedFrame.GetComponent<Renderer>().sharedMaterial = frameMat;

            var mattress = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mattress.name = "Mattress";
            mattress.transform.SetParent(house.transform, false);
            mattress.transform.localPosition = new Vector3(0f, 0.55f, -0.9f);
            mattress.transform.localScale = new Vector3(1.3f, 0.2f, 2.3f);
            mattress.GetComponent<Renderer>().sharedMaterial = bedMat;

            var pillow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillow.name = "Pillow";
            pillow.transform.SetParent(house.transform, false);
            pillow.transform.localPosition = new Vector3(0f, 0.7f, -1.8f);
            pillow.transform.localScale = new Vector3(1f, 0.16f, 0.5f);
            pillow.GetComponent<Renderer>().sharedMaterial = bedMat;
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
            string goName, string displayName, string modelPath, Color? skinTint,
            Vector3 position, RuntimeAnimatorController animController, bool startActive)
        {
            var go = new GameObject(goName);
            if (startActive) go.tag = "Player";
            go.transform.position = position;

            var controller = go.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.height = 2f;
            controller.radius = 0.4f;

            // Facial hair is only hidden on the Floreswa models, whose
            // beard/moustache/goatee live on separate material slots. The
            // larger project's characters use their own authored materials.
            var visual = InstantiateCharacter(modelPath, go.transform, animController, skinTint,
                hideFacialHair: skinTint.HasValue);
            var animator = visual.GetComponentInChildren<Animator>();

            var vitals = go.AddComponent<CharacterVitals>();
            // Swimming removed - the sea is now walled off at the shoreline
            // and reachable only along the jetty (user direction).

            // Shows purchased apparel on the character.
            var equipment = go.AddComponent<CharacterEquipment>();
            var eqSo = new SerializedObject(equipment);
            eqSo.FindProperty("animator").objectReferenceValue = animator;
            eqSo.ApplyModifiedPropertiesWithoutUndo();

            var playerController = go.AddComponent<PlayerController>();
            var pcSo = new SerializedObject(playerController);
            pcSo.FindProperty("animator").objectReferenceValue = animator;
            pcSo.FindProperty("vitals").objectReferenceValue = vitals;
            pcSo.ApplyModifiedPropertiesWithoutUndo();
            playerController.IsControlled = startActive;

            go.AddComponent<FarmhandController>();

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

        private static void BuildTownNPCs(
            Terrain terrain, List<Vector3> roadPoints, RuntimeAnimatorController animController)
        {
            CropDefinition[] allCrops = LoadAllCropDefinitions();

            // Walks along the road itself (sideMul 0) rather than through
            // the yards, where it was clipping houses.
            BuildNpc(terrain, roadPoints, index: 2, sideMul: 0f, goName: "NPC_Villager",
                modelPath: "Assets/Floreswa/Models/male03_1.fbx", role: NpcRole.Villager,
                cropsForBuyer: null, animController: animController, patrols: true, reactsToHeat: false);

            // Two officers so there is always one visible: one patrolling
            // the lower road, one posted by the shops.
            BuildNpc(terrain, roadPoints, index: 7, sideMul: -1f, goName: "NPC_PoliceShops",
                modelPath: "Assets/Floreswa/Models/male01_1.fbx", role: NpcRole.Police,
                cropsForBuyer: null, animController: animController, patrols: true, reactsToHeat: true);

            // Police patrols the Lalay road and speeds up when heat is high.
            BuildNpc(terrain, roadPoints, index: 3, sideMul: -1f, goName: "NPC_Police",
                modelPath: "Assets/Floreswa/Models/male01_2.fbx", role: NpcRole.Police,
                cropsForBuyer: null, animController: animController, patrols: true, reactsToHeat: true);

            // Farm shop, crop buyer, and the separate apparel shop each get
            // their own stall and stay put beside it.
            BuildNpc(terrain, roadPoints, index: 6, sideMul: 1f, goName: "NPC_FarmShop",
                modelPath: "Assets/Floreswa/Models/male02_2.fbx", role: NpcRole.FarmShop,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            BuildNpc(terrain, roadPoints, index: 6, sideMul: -1f, goName: "NPC_Buyer",
                modelPath: "Assets/Floreswa/Models/male03_2.fbx", role: NpcRole.Buyer,
                cropsForBuyer: allCrops, animController: animController, patrols: false, reactsToHeat: false);

            BuildNpc(terrain, roadPoints, index: 12, sideMul: 1f, goName: "NPC_ApparelShop",
                modelPath: "Assets/Floreswa/Models/male01_3.fbx", role: NpcRole.ApparelShop,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            BuildMarketArea(terrain, roadPoints, index: 6, title: "FARM SHOP", secondTitle: "PRODUCE BUYER");
            BuildMarketArea(terrain, roadPoints, index: 12, title: "CLOTHES", secondTitle: null);

            // Land office and car dealer are their own locations further
            // along the road, separate from the farm/clothes shops.
            BuildNpc(terrain, roadPoints, index: 14, sideMul: -1f, goName: "NPC_LandOffice",
                modelPath: "Assets/Floreswa/Models/male03_3.fbx", role: NpcRole.LandOffice,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            BuildNpc(terrain, roadPoints, index: 15, sideMul: 1f, goName: "NPC_CarDealer",
                modelPath: "Assets/Floreswa/Models/male01_3.fbx", role: NpcRole.CarDealer,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            BuildNpc(terrain, roadPoints, index: 5, sideMul: 1f, goName: "NPC_FoodShop",
                modelPath: "Assets/Floreswa/Models/male02_1.fbx", role: NpcRole.FoodShop,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            BuildNpc(terrain, roadPoints, index: 11, sideMul: 1f, goName: "NPC_Pharmacy",
                modelPath: "Assets/Floreswa/Models/male03_2.fbx", role: NpcRole.Pharmacy,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            BuildMarketArea(terrain, roadPoints, index: 5, title: "FOOD", secondTitle: null);
            BuildMarketArea(terrain, roadPoints, index: 11, title: "PHARMACY", secondTitle: null);
            BuildMarketArea(terrain, roadPoints, index: 14, title: "LAND AND SURVEYS", secondTitle: null);
            BuildMarketArea(terrain, roadPoints, index: 15, title: "CAR DEALER", secondTitle: null);

            BuildBossNpc(terrain, roadPoints, allCrops, animController);
            BuildBoatMan(terrain, allCrops, animController);
        }

        /// <summary>
        /// The captain who runs produce to Guadeloupe, standing at the end
        /// of the jetty. See GuadeloupeTrade / DECISIONS.md D-007.
        /// </summary>
        private static void BuildBoatMan(
            Terrain terrain, CropDefinition[] allCrops, RuntimeAnimatorController animController)
        {
            var deck = GameObject.Find("JettyDeck");
            if (deck == null)
            {
                Debug.LogWarning("Mini011PhaseBSetup: JettyDeck not found; skipping boat man.");
                return;
            }

            Vector3 pos = deck.transform.position
                          + new Vector3(-deck.transform.localScale.x * 0.35f, 0f, 0f);
            pos.y = deck.transform.position.y + deck.transform.localScale.y * 0.5f;

            var go = new GameObject("NPC_BoatMan");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            InstantiateCharacter("Assets/Floreswa/Models/male02_3.fbx", go.transform, animController, null);

            var npc = go.AddComponent<TownNPCInteractable>();
            var so = new SerializedObject(npc);
            so.FindProperty("role").enumValueIndex = (int)NpcRole.BoatMan;
            so.FindProperty("npcName").stringValue = "BoatMan";
            so.ApplyModifiedPropertiesWithoutUndo();

            // The trade system itself.
            var tradeGo = new GameObject("GuadeloupeTrade");
            var trade = tradeGo.AddComponent<GuadeloupeTrade>();
            var tso = new SerializedObject(trade);
            var cropsProp = tso.FindProperty("sellableCrops");
            cropsProp.arraySize = allCrops.Length;
            for (int i = 0; i < allCrops.Length; i++)
            {
                cropsProp.GetArrayElementAtIndex(i).objectReferenceValue = allCrops[i];
            }
            tso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Boss K waits near the Montine turnoff, away from the market -
        /// per Docs/STORY.md he watches their deliveries and offers the
        /// higher-paying illegal work.
        /// </summary>
        private static void BuildBossNpc(
            Terrain terrain, List<Vector3> roadPoints, CropDefinition[] allCrops,
            RuntimeAnimatorController animController)
        {
            int mid = roadPoints.Count / 2;
            Vector3 dir = (roadPoints[mid + 1] - roadPoints[mid - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            Vector3 pos = roadPoints[mid] + right * 7.5f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);
            _bossPos = pos;

            var go = new GameObject("NPC_BossK");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(-right, Vector3.up);

            InstantiateCharacter("Assets/Floreswa/Models/male02_3.fbx", go.transform, animController, null);

            CropDefinition bushers = null;
            foreach (var c in allCrops)
            {
                if (c != null && c.isIllegal) { bushers = c; break; }
            }

            var npc = go.AddComponent<TownNPCInteractable>();
            var so = new SerializedObject(npc);
            so.FindProperty("role").enumValueIndex = (int)NpcRole.Boss;
            so.FindProperty("npcName").stringValue = "BossK";
            so.FindProperty("bossSeedCrop").objectReferenceValue = bushers;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildNpc(
            Terrain terrain, List<Vector3> roadPoints, int index, float sideMul, string goName,
            string modelPath, NpcRole role, CropDefinition[] cropsForBuyer,
            RuntimeAnimatorController animController, bool patrols, bool reactsToHeat)
        {
            index = Mathf.Clamp(index, 1, roadPoints.Count - 2);
            Vector3 pos = roadPoints[index];
            Vector3 dir = (roadPoints[index + 1] - roadPoints[index - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            pos += right * sideMul * 3.8f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);

            var npcGo = new GameObject(goName);
            npcGo.transform.position = pos;
            npcGo.transform.rotation = Quaternion.LookRotation(-right * sideMul, Vector3.up);

            // Passing the locomotion controller is what stops NPCs standing
            // in the model's default T-pose with arms out - they now play
            // the same idle/walk blend the players use.
            var npcVisual = InstantiateCharacter(modelPath, npcGo.transform, animController, null);

            if (role == NpcRole.Police)
            {
                ApplyPoliceUniform(npcVisual);
            }

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

            if (patrols)
            {
                // Patrolling NPCs move via CharacterController so they
                // collide with buildings rather than walking through them.
                var cc = npcGo.AddComponent<CharacterController>();
                cc.center = new Vector3(0f, 0.95f, 0f);
                cc.height = 1.85f;
                cc.radius = 0.32f;

                if (role == NpcRole.Police)
                {
                    // Officers pace the road by the sellers and give chase
                    // when heat is up; PoliceOfficer also steers around
                    // buildings, which plain patrolling did not.
                    var officer = npcGo.AddComponent<PoliceOfficer>();

                    // Pace a stretch of road centred on the market.
                    int marketIdx = Mathf.Clamp(6, 1, roadPoints.Count - 2);
                    Vector3 beatStart = roadPoints[Mathf.Max(1, marketIdx - 3)];
                    Vector3 beatEnd = roadPoints[Mathf.Min(roadPoints.Count - 2, marketIdx + 3)];
                    beatStart.y = SampleHeight(terrain, beatStart.x, beatStart.z);
                    beatEnd.y = SampleHeight(terrain, beatEnd.x, beatEnd.z);
                    officer.SetPatrol(beatStart, beatEnd);

                    var oso = new SerializedObject(officer);
                    oso.FindProperty("patrolA").vector3Value = beatStart;
                    oso.FindProperty("patrolB").vector3Value = beatEnd;
                    oso.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }

                var patrol = npcGo.AddComponent<PatrolNPC>();
                Vector3 a = roadPoints[Mathf.Max(1, index - 2)] + right * sideMul * 3.8f;
                Vector3 b = roadPoints[Mathf.Min(roadPoints.Count - 2, index + 2)] + right * sideMul * 3.8f;
                a.y = SampleHeight(terrain, a.x, a.z);
                b.y = SampleHeight(terrain, b.x, b.z);

                var pso = new SerializedObject(patrol);
                var wp = pso.FindProperty("waypoints");
                wp.arraySize = 2;
                wp.GetArrayElementAtIndex(0).vector3Value = a;
                wp.GetArrayElementAtIndex(1).vector3Value = b;
                pso.FindProperty("reactsToHeat").boolValue = reactsToHeat;
                pso.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Blue shirt, black trousers and a black cap, so officers read as
        /// police at a glance on the road.
        /// </summary>
        private static void ApplyPoliceUniform(GameObject instance)
        {
            var shirt = new Color(0.12f, 0.22f, 0.55f);
            var trousers = new Color(0.08f, 0.08f, 0.10f);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name.ToLowerInvariant();

                    Color? tint = null;
                    if (n.Contains("tshirt") || n.Contains("shirt")) tint = shirt;
                    else if (n.Contains("pants") || n.Contains("trouser")) tint = trousers;
                    else if (n.Contains("shoes")) tint = trousers;

                    if (tint == null) continue;

                    // Clone so only this officer is recoloured.
                    mats[i] = new Material(mats[i]) { color = tint.Value };
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = mats;
            }

            // Black cap on the head bone.
            var animator = instance.GetComponentInChildren<Animator>();
            var head = animator != null && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.Head)
                : null;
            if (head == null) return;

            var capMat = GetOrCreateMaterial("PoliceCap", trousers);

            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "PoliceCap";
            crown.transform.SetParent(head, false);
            crown.transform.localPosition = new Vector3(0f, 0.15f, 0.01f);
            crown.transform.localScale = new Vector3(0.2f, 0.12f, 0.2f);
            crown.GetComponent<Renderer>().sharedMaterial = capMat;
            Object.DestroyImmediate(crown.GetComponent<Collider>());

            var peak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            peak.name = "PoliceCapPeak";
            peak.transform.SetParent(head, false);
            peak.transform.localPosition = new Vector3(0f, 0.13f, 0.12f);
            peak.transform.localScale = new Vector3(0.19f, 0.02f, 0.12f);
            peak.GetComponent<Renderer>().sharedMaterial = capMat;
            Object.DestroyImmediate(peak.GetComponent<Collider>());
        }

        /// <summary>
        /// Makes the shop/market area actually readable: a stall canopy and
        /// crates beside the road where the shopkeeper and buyer stand, so
        /// the player can see where selling happens.
        /// </summary>
        private static void BuildMarketArea(
            Terrain terrain, List<Vector3> roadPoints, int index, string title, string secondTitle)
        {
            index = Mathf.Clamp(index, 1, roadPoints.Count - 2);
            Vector3 basePos = roadPoints[index];
            Vector3 dir = (roadPoints[index + 1] - roadPoints[index - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;

            var parent = new GameObject($"Market_{title}");

            Material canopyMat = GetOrCreateMaterial("MarketCanopy", new Color(0.85f, 0.32f, 0.25f));
            Material postMat = GetOrCreateMaterial("MarketPost", new Color(0.42f, 0.32f, 0.22f));
            Material crateMat = GetOrCreateMaterial("MarketCrate", new Color(0.6f, 0.45f, 0.28f));

            foreach (int side in new[] { -1, 1 })
            {
                // Only build the second stall when this market has one.
                if (side < 0 && string.IsNullOrEmpty(secondTitle)) continue;

                Vector3 stallPos = basePos + right * side * 5.6f;
                stallPos.y = SampleHeight(terrain, stallPos.x, stallPos.z);

                string stallName = side < 0 ? secondTitle : title;
                var stall = new GameObject($"Stall_{stallName}");
                stall.transform.SetParent(parent.transform);
                stall.transform.position = stallPos;
                stall.transform.rotation = Quaternion.LookRotation(-right * side, Vector3.up);

                var canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                canopy.name = "Canopy";
                canopy.transform.SetParent(stall.transform, false);
                canopy.transform.localPosition = new Vector3(0f, 2.5f, 0f);
                canopy.transform.localScale = new Vector3(4.2f, 0.12f, 3f);
                canopy.GetComponent<Renderer>().sharedMaterial = canopyMat;

                // Shopfront sign so the player can tell the farm shop, the
                // produce buyer and the clothes shop apart at a glance.
                var signGo = new GameObject("StallSign");
                signGo.transform.SetParent(stall.transform, false);
                // The stall's +Z faces the road. TextMesh reads correctly
                // from its own -Z side, so the sign sits on the road side
                // and is turned to face back into the stall; otherwise it
                // renders mirrored to anyone standing on the road.
                signGo.transform.localPosition = new Vector3(0f, 2.9f, 1.6f);
                signGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                var stm = signGo.AddComponent<TextMesh>();
                stm.text = stallName;
                stm.characterSize = 0.05f;
                stm.fontSize = 80;
                stm.anchor = TextAnchor.MiddleCenter;
                stm.alignment = TextAlignment.Center;
                stm.color = Color.white;

                foreach (float px in new[] { -1.9f, 1.9f })
                {
                    foreach (float pz in new[] { -1.3f, 1.3f })
                    {
                        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        post.name = "Post";
                        post.transform.SetParent(stall.transform, false);
                        post.transform.localPosition = new Vector3(px, 1.25f, pz);
                        post.transform.localScale = new Vector3(0.12f, 1.25f, 0.12f);
                        post.GetComponent<Renderer>().sharedMaterial = postMat;
                    }
                }

                var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
                table.name = "Table";
                table.transform.SetParent(stall.transform, false);
                table.transform.localPosition = new Vector3(0f, 0.85f, 0.6f);
                table.transform.localScale = new Vector3(3.4f, 0.12f, 1f);
                table.GetComponent<Renderer>().sharedMaterial = crateMat;

                for (int c = 0; c < 3; c++)
                {
                    var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    crate.name = $"Crate_{c}";
                    crate.transform.SetParent(stall.transform, false);
                    crate.transform.localPosition = new Vector3(-1.2f + c * 1.2f, 0.3f, -0.9f);
                    crate.transform.localScale = Vector3.one * 0.6f;
                    crate.GetComponent<Renderer>().sharedMaterial = crateMat;
                }
            }
        }

        /// <summary>
        /// Hides facial-hair submeshes by making their material fully
        /// transparent. The character pack puts beard/moustache/goatee on
        /// separate material slots of one shared skinned renderer, so they
        /// can't be removed by deleting a GameObject - and editing the
        /// shared material would strip the beard from every character.
        /// Used to keep Smart and Strong reading as 18-year-olds rather
        /// than bearded older men.
        /// </summary>
        private static void HideFacialHair(GameObject instance)
        {
            string[] hairSlots = { "beard", "mustache", "goatee" };

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string matName = mats[i].name.ToLowerInvariant();
                    bool isFacialHair = false;
                    foreach (var slot in hairSlots)
                    {
                        if (matName.Contains(slot)) { isFacialHair = true; break; }
                    }
                    if (!isFacialHair) continue;

                    var clear = new Material(mats[i]) { name = mats[i].name + "_Hidden" };
                    // Standard shader needs explicit transparent setup to
                    // respect alpha at runtime.
                    clear.SetFloat("_Mode", 3f);
                    clear.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    clear.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    clear.SetInt("_ZWrite", 0);
                    clear.DisableKeyword("_ALPHATEST_ON");
                    clear.EnableKeyword("_ALPHABLEND_ON");
                    clear.renderQueue = 3000;
                    clear.color = new Color(0f, 0f, 0f, 0f);

                    mats[i] = clear;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = mats;
            }
        }

        private static GameObject InstantiateCharacter(
            string fbxPath, Transform parent, RuntimeAnimatorController animController,
            Color? skinTint = null, bool hideFacialHair = false)
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

            if (hideFacialHair)
            {
                HideFacialHair(instance);
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

        // Fictional near-miss brand names per the user's direction, so no
        // real trademark is used.
        private static readonly (string id, string name, ShopCategory cat, int price, string seedCrop, int qty)[] ShopSpecs =
        {
            ("seed_tomato", "Tomato Seeds (x5)",   ShopCategory.Seed,      12,  "tomato", 5),
            ("seed_banana", "Banana Suckers (x3)", ShopCategory.Seed,      20,  "banana", 3),
            ("seed_carrot", "Carrot Seeds (x5)",   ShopCategory.Seed,      10,  "carrot", 5),
        };

        // Land is sold from its own "Land and Surveys" office, not the
        // farm shop (user direction).
        private static readonly (string id, string name, ShopCategory cat, int price, string seedCrop, int qty)[] LandSpecs =
        {
            ("land_montine",  "Montine Land Plot",     ShopCategory.Land,     600,  null, 0),
            ("land_hillside", "Hillside Survey Lot",   ShopCategory.Land,     1400, null, 0),
            ("prop_safehouse","Montine Safehouse Deed",ShopCategory.Property, 2200, null, 0),
        };

        // Food shop - healing. Consumed on purchase.
        private static readonly (string id, string name, ShopCategory cat, int price, string seedCrop, int qty)[] FoodSpecs =
        {
            ("food_bakes",   "Bakes and Saltfish", ShopCategory.Food, 12, null, 0),
            ("food_broth",   "Fish Broth",         ShopCategory.Food, 20, null, 0),
            ("food_provision","Ground Provision",  ShopCategory.Food, 30, null, 0),
            ("food_juice",   "Sorrel Juice",       ShopCategory.Food,  8, null, 0),
        };

        // Pharmacy - temporary enhancements. Consumed on purchase.
        private static readonly (string id, string name, ShopCategory cat, int price, string seedCrop, int qty)[] PharmacySpecs =
        {
            ("pill_energy",  "Energy Pills",   ShopCategory.Enhancement, 45,  null, 0),
            ("pill_stamina", "Stamina Tonic",  ShopCategory.Enhancement, 70,  null, 0),
            ("pill_focus",   "Focus Capsules", ShopCategory.Enhancement, 110, null, 0),
        };

        // Vehicles and boats come from a dealer, and are priced so they
        // are a later-game purchase (user direction).
        private static readonly (string id, string name, ShopCategory cat, int price, string seedCrop, int qty)[] DealerSpecs =
        {
            ("bike_scrambler", "Scrambler Bike",   ShopCategory.Vehicle, 1800, null, 0),
            ("van_pickup",     "Pickup Van",       ShopCategory.Vehicle, 3200, null, 0),
            ("boat_pirogue",   "Fishing Pirogue",  ShopCategory.Boat,    2600, null, 0),
        };

        // Separate apparel shopfront - kept distinct from the farm shop.
        private static readonly (string id, string name, ShopCategory cat, int price, string seedCrop, int qty)[] ApparelSpecs =
        {
            ("cap_mike",    "Mike Cap",            ShopCategory.Clothing,  45,  null, 0),
            ("shirt_lacos", "Lacostes Polo",       ShopCategory.Clothing,  80,  null, 0),
            ("shorts_adibas","Adibas Shorts",      ShopCategory.Clothing,  65,  null, 0),
            ("shoes_mike",  "Mike Air Kicks",      ShopCategory.Footwear,  150, null, 0),
            ("shoes_pumba", "Pumba Runners",       ShopCategory.Footwear,  120, null, 0),
            ("chain_gold",  "Gold Chain",          ShopCategory.Accessory, 320, null, 0),
            ("shades_ray",  "Ray-Bam Shades",      ShopCategory.Accessory, 95,  null, 0),
            ("watch_rollie","Rollex Watch",        ShopCategory.Accessory, 480, null, 0),
        };

        private static ShopItemDefinition[] BuildShopStock(CropDefinition[] crops)
            => BuildStock(ShopSpecs, crops);

        private static ShopItemDefinition[] BuildApparelStock(CropDefinition[] crops)
            => BuildStock(ApparelSpecs, crops);

        private static ShopItemDefinition[] BuildStock(
            (string id, string name, ShopCategory cat, int price, string seedCrop, int qty)[] specs,
            CropDefinition[] crops)
        {
            EnsureFolder("Assets/UpIzUpMini/Data/Shop");
            var items = new ShopItemDefinition[specs.Length];

            for (int i = 0; i < specs.Length; i++)
            {
                var spec = specs[i];
                string path = $"Assets/UpIzUpMini/Data/Shop/{spec.id}.asset";
                var item = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(path);
                if (item == null)
                {
                    item = ScriptableObject.CreateInstance<ShopItemDefinition>();
                    AssetDatabase.CreateAsset(item, path);
                }

                item.itemId = spec.id;
                item.displayName = spec.name;
                item.category = spec.cat;
                item.price = spec.price;
                item.seedQuantity = spec.qty;
                item.grantsCrop = null;

                // Consumable tuning by category.
                switch (spec.cat)
                {
                    case ShopCategory.Food:
                        item.healAmount = Mathf.Clamp(spec.price * 1.6f, 15f, 100f);
                        item.staminaBoost = 0f;
                        item.regenMultiplier = 1f;
                        break;
                    case ShopCategory.Enhancement:
                        item.healAmount = 0f;
                        item.staminaBoost = Mathf.Clamp(spec.price * 0.5f, 20f, 60f);
                        item.regenMultiplier = 1.8f;
                        item.boostSeconds = 45f;
                        break;
                    default:
                        item.healAmount = 0f;
                        item.staminaBoost = 0f;
                        item.regenMultiplier = 1f;
                        break;
                }
                if (!string.IsNullOrEmpty(spec.seedCrop))
                {
                    foreach (var c in crops)
                    {
                        if (c != null && c.cropId == spec.seedCrop) { item.grantsCrop = c; break; }
                    }
                }
                EditorUtility.SetDirty(item);
                items[i] = item;
            }

            return items;
        }

        /// <summary>
        /// Builds Mission 1 ("A Start in Montine") as a chain of small,
        /// explicitly-worded objectives with world markers - the GTA-style
        /// bit-by-bit instruction flow. Mirrors Docs/STORY.md Mission 1.
        /// </summary>
        private static void BuildMissions(
            Terrain terrain, List<Vector3> roadPoints, Vector3 farmCenter, Transform firstPlot)
        {
            Vector3 marketPos = roadPoints[Mathf.Clamp(6, 1, roadPoints.Count - 2)];
            Vector3 apparelPos = roadPoints[Mathf.Clamp(12, 1, roadPoints.Count - 2)];
            Vector3 plotPos = firstPlot != null ? firstPlot.position : farmCenter;
            Vector3 policePos = roadPoints[Mathf.Clamp(3, 1, roadPoints.Count - 2)];
            Vector3 expansionPos = _expansionPlotPos != Vector3.zero ? _expansionPlotPos : farmCenter;
            Vector3 bossPos = _bossPos != Vector3.zero ? _bossPos : farmCenter;

            var missions = new List<Mission>
            {
                new Mission
                {
                    missionId = "M1",
                    title = "A Start in Montine",
                    briefing = "Franki and Sacat leaving school to make their own money. Start with the land.",
                    rewardMoney = 60,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.Switch,
                            instruction = "Press Tab to switch between Franki and Sacat",
                            hasMarker = false,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.TalkTo,
                            targetId = "FarmShop",
                            instruction = "Go to the Farm Shop on the Lalay road",
                            markerPosition = marketPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.BuySeeds,
                            instruction = "Buy a pack of tomato seeds",
                            markerPosition = marketPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.ReachArea,
                            instruction = "Follow the dirt track up to the Montine farm",
                            markerPosition = farmCenter,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.PlantCrop,
                            targetId = "tomato",
                            instruction = "Plant tomato in a plot  [ 1 ] to select, [ E ] to plant",
                            markerPosition = plotPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.WaterAny,
                            instruction = "Water the plot you just planted  [ E ]",
                            markerPosition = plotPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.HarvestCrop,
                            targetId = "tomato",
                            requiredCount = 3,
                            instruction = "Wait for it to ripen red, then harvest  [ E ]",
                            markerPosition = plotPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.SellCrop,
                            instruction = "Take the tomatoes back to the Produce Buyer in Lalay",
                            markerPosition = marketPos,
                        },
                    }
                },
                new Mission
                {
                    missionId = "M2",
                    title = "Look Sharp",
                    briefing = "Money does talk, but so does how you look. Go see what the clothes shop have.",
                    rewardMoney = 40,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.TalkTo,
                            targetId = "ApparelShop",
                            instruction = "Find the Clothes shop further up the Lalay road",
                            markerPosition = apparelPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.HarvestCrop,
                            targetId = "tomato",
                            requiredCount = 6,
                            instruction = "Grow and harvest 6 more tomato to build up your money",
                            markerPosition = plotPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.SellCrop,
                            instruction = "Sell the crop to the Produce Buyer",
                            markerPosition = marketPos,
                        },
                    }
                },
                new Mission
                {
                    missionId = "M3",
                    title = "More Land",
                    briefing = "Dat small plot cannot hold allu forever. Buy the land next to it.",
                    rewardMoney = 80,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.BuyItem,
                            targetId = "land_montine",
                            instruction = "Buy the Montine Land Plot from the Farm Shop ($600)",
                            markerPosition = marketPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.PlantCrop,
                            instruction = "Plant something on your new land",
                            markerPosition = expansionPos,
                        },
                    }
                },
                new Mission
                {
                    missionId = "M4",
                    title = "The Offer",
                    briefing = "A man name Boss K been watching allu deliveries. He waiting up by the farm track.",
                    rewardMoney = 0,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.TalkTo,
                            targetId = "BossK",
                            instruction = "Go and hear what Boss K have to say",
                            markerPosition = bossPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.PlantCrop,
                            targetId = "bushers",
                            instruction = "Plant the Bushers up at Montine  [ 4 ] to select",
                            markerPosition = plotPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.HarvestCrop,
                            targetId = "bushers",
                            requiredCount = 3,
                            instruction = "Water it, let it grow, then harvest the Bushers",
                            markerPosition = plotPos,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.SellCrop,
                            instruction = "Sell it in Lalay - but know dis raise police heat",
                            markerPosition = marketPos,
                        },
                    }
                },
                new Mission
                {
                    missionId = "M5",
                    title = "Cool Down",
                    briefing = "Police watching allu now. Stay off di road till dey lose interest.",
                    rewardMoney = 120,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.EscapeHeat,
                            instruction = "Get away from Lalay and let the heat cool right down",
                            markerPosition = farmCenter,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.TalkTo,
                            targetId = "Police",
                            instruction = "Walk past the officer clean - talk to him with low heat",
                            markerPosition = policePos,
                        },
                    }
                },
            };

            var go = new GameObject("MissionSystem");
            var system = go.AddComponent<MissionSystem>();
            var so = new SerializedObject(system);
            var listProp = so.FindProperty("missions");
            listProp.arraySize = missions.Count;

            for (int m = 0; m < missions.Count; m++)
            {
                var mp = listProp.GetArrayElementAtIndex(m);
                var mission = missions[m];
                mp.FindPropertyRelative("missionId").stringValue = mission.missionId;
                mp.FindPropertyRelative("title").stringValue = mission.title;
                mp.FindPropertyRelative("briefing").stringValue = mission.briefing;
                mp.FindPropertyRelative("rewardMoney").intValue = mission.rewardMoney;

                var objProp = mp.FindPropertyRelative("objectives");
                objProp.arraySize = mission.objectives.Count;
                for (int o = 0; o < mission.objectives.Count; o++)
                {
                    var op = objProp.GetArrayElementAtIndex(o);
                    var obj = mission.objectives[o];
                    op.FindPropertyRelative("kind").enumValueIndex = (int)obj.kind;
                    op.FindPropertyRelative("instruction").stringValue = obj.instruction;
                    op.FindPropertyRelative("targetId").stringValue = obj.targetId ?? string.Empty;
                    op.FindPropertyRelative("requiredCount").intValue = Mathf.Max(1, obj.requiredCount);
                    op.FindPropertyRelative("markerPosition").vector3Value = obj.markerPosition;
                    op.FindPropertyRelative("hasMarker").boolValue = obj.hasMarker;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildObjectiveMarker();
        }

        /// <summary>Bobbing arrow + ground ring pointing at the current objective.</summary>
        private static void BuildObjectiveMarker()
        {
            var root = new GameObject("ObjectiveMarker");

            Material markerMat = GetOrCreateMaterial("ObjectiveMarker", new Color(1f, 0.82f, 0.15f));
            markerMat.EnableKeyword("_EMISSION");
            markerMat.SetColor("_EmissionColor", new Color(0.9f, 0.7f, 0.1f));

            // Downward-pointing cone: a cylinder tapered by scaling a
            // primitive cone isn't available, so use a stretched pyramid
            // approximation built from a cube rotated 45 degrees, which
            // reads clearly as an arrow from the three-quarter camera.
            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(root.transform, false);
            // Sits above the target; ObjectiveMarker bobs it at runtime,
            // but it needs a sane resting height for edit-mode inspection.
            arrow.transform.localPosition = new Vector3(0f, 2.6f, 0f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(arrow.transform, false);
            head.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            head.GetComponent<Renderer>().sharedMaterial = markerMat;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Shaft";
            shaft.transform.SetParent(arrow.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            shaft.transform.localScale = new Vector3(0.22f, 0.7f, 0.22f);
            shaft.GetComponent<Renderer>().sharedMaterial = markerMat;
            Object.DestroyImmediate(shaft.GetComponent<Collider>());

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Ring";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            ring.transform.localScale = new Vector3(2.6f, 0.03f, 2.6f);
            ring.GetComponent<Renderer>().sharedMaterial = markerMat;
            Object.DestroyImmediate(ring.GetComponent<Collider>());

            var marker = root.AddComponent<ObjectiveMarker>();
            var so = new SerializedObject(marker);
            so.FindProperty("arrow").objectReferenceValue = arrow.transform;
            so.FindProperty("ring").objectReferenceValue = ring.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Roadside signs naming Lalay and the Montine turnoff.</summary>
        private static void BuildStreetSigns(Terrain terrain, List<Vector3> roadPoints, Vector3 farmCenter)
        {
            var parent = new GameObject("StreetSigns");
            Material postMat = GetOrCreateMaterial("SignPost", new Color(0.35f, 0.35f, 0.37f));
            Material boardMat = GetOrCreateMaterial("SignBoard", new Color(0.1f, 0.35f, 0.18f));

            BuildSign(parent.transform, terrain, roadPoints[1], "LALAY", postMat, boardMat);

            int mid = roadPoints.Count / 2;
            BuildSign(parent.transform, terrain, roadPoints[mid], "MONTINE", postMat, boardMat);
            BuildSign(parent.transform, terrain,
                farmCenter + (roadPoints[mid] - farmCenter).normalized * 16f,
                "MONTINE FARM", postMat, boardMat);
        }

        private static void BuildSign(
            Transform parent, Terrain terrain, Vector3 near, string text, Material postMat, Material boardMat)
        {
            Vector3 pos = near + new Vector3(4.6f, 0f, 0f);
            pos.y = SampleHeight(terrain, pos.x, pos.z);

            var sign = new GameObject($"Sign_{text}");
            sign.transform.SetParent(parent);
            sign.transform.position = pos;

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(sign.transform, false);
            post.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            post.transform.localScale = new Vector3(0.1f, 1.2f, 0.1f);
            post.GetComponent<Renderer>().sharedMaterial = postMat;

            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(sign.transform, false);
            board.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            board.transform.localScale = new Vector3(2.4f, 0.55f, 0.08f);
            board.GetComponent<Renderer>().sharedMaterial = boardMat;
            Object.DestroyImmediate(board.GetComponent<Collider>());

            // World-space label on the board.
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(sign.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 2.5f, -0.07f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = text;
            // characterSize scales the glyphs in world units - 0.08 made
            // the label many times wider than its board.
            tm.characterSize = 0.022f;
            tm.fontSize = 90;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
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

            Image healthFill = CreateMeter(canvasGo.transform, "Health", new Vector2(20f, -20f), new Color(0.20f, 0.80f, 0.25f), font, out Text healthPct);
            Image staminaFill = CreateMeter(canvasGo.transform, "Energy", new Vector2(20f, -50f), new Color(0.95f, 0.85f, 0.15f), font, out Text staminaPct);
            Image heatFill = CreateMeter(canvasGo.transform, "Heat", new Vector2(20f, -80f), new Color(0.90f, 0.15f, 0.12f), font, out Text heatPct);

            // Money/crop sit against bright sky, so they get a dark backing
            // panel - white-on-sky was unreadable at some camera angles.
            var infoPanel = new GameObject("InfoPanel");
            infoPanel.transform.SetParent(canvasGo.transform, false);
            var infoRect = infoPanel.AddComponent<RectTransform>();
            infoRect.anchorMin = infoRect.anchorMax = new Vector2(1f, 1f);
            infoRect.pivot = new Vector2(1f, 1f);
            infoRect.sizeDelta = new Vector2(280f, 100f);
            infoRect.anchoredPosition = new Vector2(-20f, -20f);
            infoPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            Text moneyLabel = CreateLabel(infoPanel.transform, "$0", 34, new Vector2(0f, -26f), font);
            moneyLabel.rectTransform.anchorMin = moneyLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            moneyLabel.rectTransform.sizeDelta = new Vector2(260f, 40f);

            Text cropLabel = CreateLabel(infoPanel.transform, string.Empty, 24, new Vector2(0f, -68f), font);
            cropLabel.rectTransform.anchorMin = cropLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            cropLabel.rectTransform.sizeDelta = new Vector2(260f, 36f);

            Text nameLabel = CreateLabel(canvasGo.transform, "Smart", 26, new Vector2(130f, -112f), font);
            nameLabel.rectTransform.anchorMin = nameLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            nameLabel.alignment = TextAnchor.MiddleLeft;

            var hud = canvasGo.AddComponent<HUDController>();
            var so = new SerializedObject(hud);
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("staminaFill").objectReferenceValue = staminaFill;
            so.FindProperty("heatFill").objectReferenceValue = heatFill;
            so.FindProperty("moneyLabel").objectReferenceValue = moneyLabel;
            so.FindProperty("characterNameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("cropSelectionLabel").objectReferenceValue = cropLabel;
            so.FindProperty("healthPercent").objectReferenceValue = healthPct;
            so.FindProperty("staminaPercent").objectReferenceValue = staminaPct;
            so.FindProperty("heatPercent").objectReferenceValue = heatPct;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Shop panel, H-controls overlay, and the fading area-name label.</summary>
        private static void BuildExtraUI(
            ShopItemDefinition[] farmStock, ShopItemDefinition[] apparelStock,
            ShopItemDefinition[] landStock, ShopItemDefinition[] dealerStock,
            ShopItemDefinition[] foodStock, ShopItemDefinition[] pharmacyStock,
            List<Vector3> roadPoints, Vector3 farmCenter)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("GameplayUICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Two separate shopfronts with separate stock and panels.
            var farmShop = BuildShopPanel(canvasGo, "FarmShopPanel", "FARM SHOP", farmStock, font);
            var apparelShop = BuildShopPanel(canvasGo, "ApparelShopPanel", "CLOTHES SHOP", apparelStock, font);
            var landShop = BuildShopPanel(canvasGo, "LandShopPanel", "LAND AND SURVEYS", landStock, font);
            var dealerShop = BuildShopPanel(canvasGo, "DealerShopPanel", "CAR DEALER", dealerStock, font);
            var foodShop = BuildShopPanel(canvasGo, "FoodShopPanel", "FOOD SHOP", foodStock, font);
            var pharmacyShop = BuildShopPanel(canvasGo, "PharmacyPanel", "PHARMACY", pharmacyStock, font);

            // Hand each shopkeeper NPC its own shop panel.
            foreach (var npc in Object.FindObjectsByType<TownNPCInteractable>(FindObjectsSortMode.None))
            {
                var nso = new SerializedObject(npc);
                int roleIndex = nso.FindProperty("role").enumValueIndex;
                ShopPanelController target = roleIndex switch
                {
                    (int)NpcRole.FarmShop => farmShop,
                    (int)NpcRole.ApparelShop => apparelShop,
                    (int)NpcRole.LandOffice => landShop,
                    (int)NpcRole.CarDealer => dealerShop,
                    (int)NpcRole.FoodShop => foodShop,
                    (int)NpcRole.Pharmacy => pharmacyShop,
                    _ => null
                };
                if (target == null) continue;

                nso.FindProperty("shop").objectReferenceValue = target;
                nso.ApplyModifiedPropertiesWithoutUndo();
            }

            // Mission HUD: objective card plus a large transient banner.
            var objectivePanel = new GameObject("ObjectivePanel");
            objectivePanel.transform.SetParent(canvasGo.transform, false);
            var opRect = objectivePanel.AddComponent<RectTransform>();
            opRect.anchorMin = opRect.anchorMax = new Vector2(1f, 0.5f);
            opRect.pivot = new Vector2(1f, 0.5f);
            opRect.sizeDelta = new Vector2(430f, 130f);
            opRect.anchoredPosition = new Vector2(-24f, 60f);
            objectivePanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var objectiveText = CreateModalText(objectivePanel.transform, font, 26);
            objectiveText.alignment = TextAnchor.MiddleLeft;

            var bannerGo = new GameObject("MissionBanner");
            bannerGo.transform.SetParent(canvasGo.transform, false);
            var bRect = bannerGo.AddComponent<RectTransform>();
            bRect.anchorMin = bRect.anchorMax = new Vector2(0.5f, 0.5f);
            bRect.sizeDelta = new Vector2(1200f, 220f);
            bRect.anchoredPosition = new Vector2(0f, 180f);
            var banner = bannerGo.AddComponent<Text>();
            banner.font = font;
            banner.fontSize = 44;
            banner.fontStyle = FontStyle.Bold;
            banner.alignment = TextAnchor.MiddleCenter;
            banner.color = new Color(1f, 1f, 1f, 0f);

            var missionHud = canvasGo.AddComponent<MissionHUD>();
            var mhSo = new SerializedObject(missionHud);
            mhSo.FindProperty("objectiveText").objectReferenceValue = objectiveText;
            mhSo.FindProperty("bannerText").objectReferenceValue = banner;
            mhSo.FindProperty("objectivePanel").objectReferenceValue = objectivePanel;
            mhSo.ApplyModifiedPropertiesWithoutUndo();

            // Controls (H)
            var controlsPanel = CreateModalPanel(canvasGo.transform, "ControlsPanel", new Vector2(880f, 640f));
            var controlsText = CreateModalText(controlsPanel.transform, font, 26);
            var controls = canvasGo.AddComponent<ControlsPanelController>();
            var cso = new SerializedObject(controls);
            cso.FindProperty("panel").objectReferenceValue = controlsPanel;
            cso.FindProperty("bodyText").objectReferenceValue = controlsText;
            cso.ApplyModifiedPropertiesWithoutUndo();

            // Area name banner
            var areaGo = new GameObject("AreaName");
            areaGo.transform.SetParent(canvasGo.transform, false);
            var areaRect = areaGo.AddComponent<RectTransform>();
            areaRect.anchorMin = areaRect.anchorMax = new Vector2(0.5f, 0f);
            areaRect.pivot = new Vector2(0.5f, 0f);
            areaRect.sizeDelta = new Vector2(900f, 90f);
            areaRect.anchoredPosition = new Vector2(0f, 140f);
            var areaText = areaGo.AddComponent<Text>();
            areaText.font = font;
            areaText.fontSize = 54;
            areaText.fontStyle = FontStyle.Bold;
            areaText.alignment = TextAnchor.MiddleCenter;
            areaText.color = new Color(1f, 1f, 1f, 0f);

            var area = canvasGo.AddComponent<AreaNameDisplay>();
            var aso = new SerializedObject(area);
            aso.FindProperty("label").objectReferenceValue = areaText;
            var zonesProp = aso.FindProperty("zones");
            zonesProp.arraySize = 2;

            var lalay = zonesProp.GetArrayElementAtIndex(0);
            lalay.FindPropertyRelative("areaName").stringValue = "Lalay";
            lalay.FindPropertyRelative("center").vector3Value = roadPoints[roadPoints.Count / 3];
            lalay.FindPropertyRelative("radius").floatValue = 70f;

            var montine = zonesProp.GetArrayElementAtIndex(1);
            montine.FindPropertyRelative("areaName").stringValue = "Montine";
            montine.FindPropertyRelative("center").vector3Value = farmCenter;
            montine.FindPropertyRelative("radius").floatValue = 55f;

            aso.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ShopPanelController BuildShopPanel(
            GameObject canvasGo, string panelName, string title, ShopItemDefinition[] stock, Font font)
        {
            var panel = CreateModalPanel(canvasGo.transform, panelName, new Vector2(900f, 640f));
            var text = CreateModalText(panel.transform, font, 26);

            var shop = canvasGo.AddComponent<ShopPanelController>();
            var so = new SerializedObject(shop);
            so.FindProperty("shopTitle").stringValue = title;
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("bodyText").objectReferenceValue = text;
            var stockProp = so.FindProperty("stock");
            stockProp.arraySize = stock.Length;
            for (int i = 0; i < stock.Length; i++)
            {
                stockProp.GetArrayElementAtIndex(i).objectReferenceValue = stock[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return shop;
        }

        private static GameObject CreateModalPanel(Transform parent, string name, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.93f);
            panel.SetActive(false);
            return panel;
        }

        private static Text CreateModalText(Transform parent, Font font, int fontSize)
        {
            var go = new GameObject("Body");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(36f, 30f);
            rect.offsetMax = new Vector2(-36f, -30f);

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.supportRichText = true;
            return text;
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
            bgRect.sizeDelta = new Vector2(260f, 24f);
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
            labelText.fontSize = 16;
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
