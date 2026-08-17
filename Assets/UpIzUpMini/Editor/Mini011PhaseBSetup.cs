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
using UpIzUpMini.Progression;
using UpIzUpMini.Combat;

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
            // MINI-031: bakes the shared Action/FullBodyOverride layers
            // HumanoidAnimationManager drives at runtime into whichever
            // controller every character is about to share. Idempotent -
            // safe on every rebuild, never touches the base locomotion
            // layer above.
            if (locomotionController is AnimatorController animatorControllerAsset)
            {
                HumanoidAnimationLayerBuilder.EnsureActionLayers(animatorControllerAsset, GetSharedActionEntries());
            }

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
            BuildWorldBoundaries();

            CropDefinition[] crops = BuildEconomyAndCrops();
            BuildBreedingStation(terrain, _farmCenter, crops);
            new GameObject("ProgressionManager").AddComponent<ProgressionManager>();
            new GameObject("ProgressionChoice").AddComponent<ProgressionChoiceController>();
            var risk = new GameObject("LandRisk").AddComponent<LandRiskController>();
            var riskSo = new SerializedObject(risk);
            var riskCrops = riskSo.FindProperty("crops"); riskCrops.arraySize = crops.Length;
            for (int i = 0; i < crops.Length; i++) riskCrops.GetArrayElementAtIndex(i).objectReferenceValue = crops[i];
            // MINI-039: proximity confiscation radius centred on the
            // starting Montine plots - farmPlot is the "out" Transform
            // BuildFarmPathAndClearing just produced above.
            if (farmPlot != null) riskSo.FindProperty("plantationCenter").vector3Value = farmPlot.position;
            riskSo.ApplyModifiedPropertiesWithoutUndo();
            BuildTownNPCs(terrain, roadPoints, locomotionController);
            BuildLalayHouse(terrain, roadPoints);

            Vector3 startPos = _safehouseSpawn != Vector3.zero ? _safehouseSpawn : roadPoints[0];
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
            // Sacat is player one (smart) and Franki player two (strong).
            CharacterSlot smart = BuildControllableCharacter(
                "Sacat", "Sacat", "Assets/UpIzUpMini/Art/Characters/Mainchar.fbx",
                null, startPos, locomotionController, startActive: true);
            CharacterSlot strong = BuildControllableCharacter(
                "Franki", "Franki", "Assets/UpIzUpMini/Art/Characters/Strong.fbx",
                null, startPos + new Vector3(1.4f, 0f, -1.2f),
                locomotionController, startActive: false);
            var frankiMove = new SerializedObject(strong.playerController);
            frankiMove.FindProperty("runSpeed").floatValue = 5.85f;
            frankiMove.ApplyModifiedPropertiesWithoutUndo();
            var frankiVitals = new SerializedObject(strong.vitals);
            frankiVitals.FindProperty("maxStamina").floatValue = 125f;
            frankiVitals.FindProperty("staminaDrainPerSecond").floatValue = 12f;
            frankiVitals.ApplyModifiedPropertiesWithoutUndo();
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
            switchSo.FindProperty("safehouseSpawn").vector3Value = startPos;
            switchSo.ApplyModifiedPropertiesWithoutUndo();

            // Windowed by default so the game can be minimised/resized.
            new GameObject("WindowMode").AddComponent<WindowModeController>();
            new GameObject("WorldSafety").AddComponent<WorldSafetyController>();
            // MINI-040: C calls the inactive boy over, once phone_basic is owned.
            new GameObject("CellPhone").AddComponent<CellPhoneController>();
            // MINI-049: type C#0W@ anywhere for $100,000, every strain/
            // route unlocked, and invincibility+unlimited stamina.
            new GameObject("CheatCode").AddComponent<CheatCodeController>();

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

            BuildHUD(crops);
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

        /// <summary>
        /// MINI-031/MINI-038: the full set of action clips baked into the
        /// shared controller's Action/FullBodyOverride layers. Handed to
        /// every HumanoidAnimationManager built into the scene, player and
        /// NPC alike - a character not calling a given id simply never
        /// plays it, so there is no harm in the list being shared rather
        /// than curated per character type. Adding an eat/aim/vehicle
        /// action later is exactly one more entry here plus whatever
        /// gameplay script calls PlayAction/BeginSustainedAction with that
        /// id - nothing else in this pipeline changes.
        ///
        /// "Melee" is a Kevin Iglesias one-handed sword-swing clip (no
        /// unarmed punch clip exists in the imported packages) - it reads
        /// as a believable punch/backhand with no weapon in hand since the
        /// characters aren't holding anything, but it was authored for a
        /// weapon and is a placeholder pending a real fist animation.
        /// "HitReaction" and "KnockedDown" are real combat clips from the
        /// same pack (CombatDamage01/Death01) - no placeholder caveat.
        /// KnockedDown is full-body and driven via BeginSustainedAction
        /// (see NpcCombatHealth), not PlayAction, since it must hold the
        /// lying-down pose for the whole recovery window rather than
        /// auto-fading out; its clip has Loop Time off, so Mecanim already
        /// holds the last frame on its own once played through once.
        /// </summary>
        private static HumanoidAnimationManager.ActionEntry[] GetSharedActionEntries()
        {
            var meleeClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx");
            var hitReactionClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx");
            var knockedDownClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx");
            return new[]
            {
                new HumanoidAnimationManager.ActionEntry
                {
                    id = SimpleMeleeCombat.ActionId,
                    clip = meleeClip,
                    fullBody = false,
                },
                new HumanoidAnimationManager.ActionEntry
                {
                    id = NpcCombatHealth.HitReactionActionId,
                    clip = hitReactionClip,
                    fullBody = true,
                },
                new HumanoidAnimationManager.ActionEntry
                {
                    id = NpcCombatHealth.KnockedDownActionId,
                    clip = knockedDownClip,
                    fullBody = true,
                },
            };
        }

        /// <summary>
        /// Adds a HumanoidAnimationManager to <paramref name="go"/> wired
        /// to <paramref name="animator"/> and populated with the full
        /// shared action list. One place for the SerializedObject
        /// boilerplate rather than repeating it at every call site (player
        /// characters, police) - see GetSharedActionEntries for why the
        /// list is shared rather than curated per character.
        /// </summary>
        private static HumanoidAnimationManager AddHumanoidAnimationManager(GameObject go, Animator animator)
        {
            var animationManager = go.AddComponent<HumanoidAnimationManager>();
            var animSo = new SerializedObject(animationManager);
            animSo.FindProperty("animator").objectReferenceValue = animator;
            var actionsProp = animSo.FindProperty("actions");
            var sharedActions = GetSharedActionEntries();
            actionsProp.arraySize = sharedActions.Length;
            for (int i = 0; i < sharedActions.Length; i++)
            {
                var element = actionsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = sharedActions[i].id;
                element.FindPropertyRelative("clip").objectReferenceValue = sharedActions[i].clip;
                element.FindPropertyRelative("fullBody").boolValue = sharedActions[i].fullBody;
            }
            animSo.ApplyModifiedPropertiesWithoutUndo();
            return animationManager;
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
        private static Vector3 _safehouseSpawn;
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

        private static void BuildWorldBoundaries()
        {
            var root = new GameObject("InvisibleWorldBoundaries");
            void Wall(string name, Vector3 position, Vector3 scale)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = name;
                wall.transform.SetParent(root.transform);
                wall.transform.position = position;
                wall.transform.localScale = scale;
                Object.DestroyImmediate(wall.GetComponent<Renderer>());
            }
            const float height = 40f;
            Wall("NorthBoundary", new Vector3(TerrainSize * .5f, height * .5f, TerrainSize + 1f), new Vector3(TerrainSize + 8f, height, 2f));
            Wall("SouthBoundary", new Vector3(TerrainSize * .5f, height * .5f, -1f), new Vector3(TerrainSize + 8f, height, 2f));
            Wall("HillBoundary", new Vector3(TerrainSize + 1f, height * .5f, TerrainSize * .5f), new Vector3(2f, height, TerrainSize + 8f));
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

                float halfSpan = (TerrainSize * 0.5f - gapHalfWidth) * 0.5f;
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
                    var weedVisual = BuildWeedVisual(plot.transform);
                    var bananaVisual = BuildBananaVisual(plot.transform);

                    var plotInteractable = plot.AddComponent<FarmPlot>();
                    var so = new SerializedObject(plotInteractable);
                    so.FindProperty("soilRenderer").objectReferenceValue = plotRenderer;
                    so.FindProperty("tomatoVisual").objectReferenceValue = tomatoVisual;
                    so.FindProperty("weedVisual").objectReferenceValue = weedVisual;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    RegisterCropVisual(plotInteractable, "banana", bananaVisual);

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
                var weedVisual = BuildWeedVisual(plot.transform);
                var bananaVisual = BuildBananaVisual(plot.transform);

                var plotComponent = plot.AddComponent<FarmPlot>();
                var pso = new SerializedObject(plotComponent);
                pso.FindProperty("soilRenderer").objectReferenceValue = plotRenderer;
                pso.FindProperty("tomatoVisual").objectReferenceValue = tomatoVisual;
                pso.FindProperty("weedVisual").objectReferenceValue = weedVisual;
                pso.ApplyModifiedPropertiesWithoutUndo();
                RegisterCropVisual(plotComponent, "banana", bananaVisual);

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

        /// <summary>
        /// MINI-051: builds the weed crop visual with mini-grape bud clusters.
        /// Weed previously had fruitCount: 0, so a ripe plant showed no bud
        /// colour at all. This places a dense cluster of small buds in the
        /// plant's upper third — read as a weed bud cluster, like mini grapes —
        /// which CropStageVisual tints to the strain's ripe colour(s) when
        /// ripe (green while unripe, then the strain colour e.g. Black Sugar
        /// orange / Purple purple / Purple Black alternating).
        /// </summary>
        private static CropStageVisual BuildWeedVisual(Transform plot)
        {
            var root = new GameObject("WeedVisual");
            root.transform.SetParent(plot, false);
            root.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            Vector3 plotScale = plot.localScale;
            root.transform.localScale = new Vector3(1f / plotScale.x, 1f / plotScale.y, 1f / plotScale.z);

            var plantRoot = new GameObject("Plant");
            plantRoot.transform.SetParent(root.transform, false);

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/UpIzUpMini/Art/CropMeshes/WeedPlant_LOD.asset");
            if (mesh != null)
            {
                var meshGo = new GameObject("PlantMesh");
                meshGo.transform.SetParent(plantRoot.transform, false);
                meshGo.AddComponent<MeshFilter>().sharedMesh = mesh;
                var plantRenderer = meshGo.AddComponent<MeshRenderer>();
                plantRenderer.sharedMaterial = GetOrCreateMaterial("CropFoliage", new Color(0.22f, 0.42f, 0.16f));
                // Source scan is ~2cm tall; normalize to a real plant height.
                float normalize = 1.9f / Mathf.Max(0.0001f, mesh.bounds.size.y);
                meshGo.transform.localScale = Vector3.one * normalize;
                meshGo.transform.localPosition = new Vector3(0f, -mesh.bounds.min.y * normalize, 0f);
            }

            var visual = root.AddComponent<CropStageVisual>();
            var so = new SerializedObject(visual);
            so.FindProperty("plantRoot").objectReferenceValue = plantRoot.transform;
            so.FindProperty("plantRenderer").objectReferenceValue = root.GetComponentInChildren<Renderer>(true);

            // Dense mini-grape bud cluster placed INSIDE the real weed canopy.
            // Measured (Mini051WeedBoundsProbe): at 1.9m plant height the mesh
            // is a wide bush ~1.7m x 1.9m x 1.64m (x/z extent to ~0.84m, top at
            // ~1.13m above base). Buds are therefore scattered across the upper
            // half of that canopy (y 0.4-1.0) and out to ~0.5m radius so they
            // nestle among the broad leaves rather than floating above/beside
            // the plant (the previous bug: buds at y 1.0-1.5, above the 1.13m
            // canopy top, at a narrow radius, read as floating grapes).
            var fruitsProp = so.FindProperty("fruitRenderers");
            var fruits = new List<Renderer>();
            const int clusterCount = 18;
            for (int i = 0; i < clusterCount; i++)
            {
                // Pseudo-random but stable scatter inside the canopy volume:
                // height between 0.4 and 1.0 (upper half of the bush), radius
                // 0.25-0.5 (within the wide foliage), full arc around the stem.
                float t = i / (float)clusterCount;
                float y = 0.40f + 0.60f * (0.5f + 0.5f * Mathf.Sin(t * 19.7f));
                float radial = 0.25f + 0.25f * (0.5f + 0.5f * Mathf.Sin(t * 41.3f + 2.1f));
                float angle = t * Mathf.PI * 2f + i * 1.3f;

                var bud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bud.name = $"Bud_{i}";
                bud.transform.SetParent(plantRoot.transform, false);
                bud.transform.localPosition = new Vector3(Mathf.Cos(angle) * radial, y, Mathf.Sin(angle) * radial);
                // Small ~8cm buds for a tight mini-grape look.
                bud.transform.localScale = Vector3.one * 0.08f;
                Object.DestroyImmediate(bud.GetComponent<Collider>());
                bud.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("CropFruit", Color.green);
                fruits.Add(bud.GetComponent<Renderer>());
            }
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

        /// <summary>
        /// MINI-050: builds the banana crop visual from the real banana tree
        /// model taken from the larger game (Tropical Nature Pack, already a
        /// low-poly game mesh ~0.2-0.5MB - unlike the photogrammetry scans it
        /// needs no decimation). Banana previously fell through to the tomato
        /// shape recoloured yellow because FarmPlot only chose visual by
        /// legality; this gives it its own tree. The tree is built through the
        /// same CropStageVisual scaling path as tomato/weed and registers in
        /// the new per-crop cropVisuals array keyed by cropId "banana".
        /// </summary>
        private static CropStageVisual BuildBananaVisual(Transform plot)
        {
            // bananatree2 is the highest-detail of the three (2781 verts vs
            // 1477/1479), so it reads best as the crop plant.
            const string meshPath = "Assets/UpIzUpMini/Art/BananaImported/bananatree2.fbx";
            var root = new GameObject("BananaVisual");
            root.transform.SetParent(plot, false);
            root.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            Vector3 plotScale = plot.localScale;
            root.transform.localScale = new Vector3(1f / plotScale.x, 1f / plotScale.y, 1f / plotScale.z);

            var plantRoot = new GameObject("Plant");
            plantRoot.transform.SetParent(root.transform, false);

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null)
            {
                Debug.LogWarning($"Mini011PhaseBSetup: banana tree mesh missing at {meshPath}");
            }
            else
            {
                var meshGo = new GameObject("PlantMesh");
                meshGo.transform.SetParent(plantRoot.transform, false);
                meshGo.AddComponent<MeshFilter>().sharedMesh = mesh;
                var plantRenderer = meshGo.AddComponent<MeshRenderer>();
                plantRenderer.sharedMaterial = GetOrCreateMaterial("BananaTree", new Color(0.28f, 0.54f, 0.20f));

                // The tree is a small decorative mesh (~0.35m); scale it up
                // to a crop-scale plant (~2m tall when ripe).
                float normalize = 2.0f / Mathf.Max(0.0001f, mesh.bounds.size.y);
                meshGo.transform.localScale = Vector3.one * normalize;
                meshGo.transform.localPosition = new Vector3(0f, -mesh.bounds.min.y * normalize, 0f);
            }

            var visual = root.AddComponent<CropStageVisual>();
            var so = new SerializedObject(visual);
            so.FindProperty("plantRoot").objectReferenceValue = plantRoot.transform;
            so.FindProperty("plantRenderer").objectReferenceValue =
                root.GetComponentInChildren<Renderer>(true);
            var fruitsProp = so.FindProperty("fruitRenderers");
            // Three hanging fruit spheres clustered at the top of the plant
            // read as a banana bunch (they ripen to the crop's yellow via
            // CropStageVisual). Staggered heights so it reads as a cluster,
            // not a single floating ball.
            fruitsProp.arraySize = 3;
            float[] ys = { 1.45f, 1.30f, 1.18f };
            float[] xs = { -0.30f, 0.00f, 0.28f };
            for (int i = 0; i < 3; i++)
            {
                var fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fruit.name = $"BananaBunch_{i}";
                fruit.transform.SetParent(plantRoot.transform, false);
                fruit.transform.localPosition = new Vector3(xs[i], ys[i], 0f);
                fruit.transform.localScale = new Vector3(0.34f, 0.50f, 0.34f);
                Object.DestroyImmediate(fruit.GetComponent<Collider>());
                fruit.GetComponent<Renderer>().sharedMaterial =
                    GetOrCreateMaterial("CropFruit", Color.green);
                fruitsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    fruit.GetComponent<Renderer>();
            }
            so.FindProperty("fullScale").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return visual;
        }

        /// <summary>
        /// MINI-050: registers a per-crop visual on a FarmPlot's new
        /// cropVisuals array, inserting/replacing the entry for cropId.
        /// </summary>
        private static void RegisterCropVisual(FarmPlot plot, string cropId, CropStageVisual visual)
        {
            var so = new SerializedObject(plot);
            var arr = so.FindProperty("cropVisuals");
            for (int i = 0; i < arr.arraySize; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                if (el.FindPropertyRelative("cropId").stringValue == cropId)
                {
                    el.FindPropertyRelative("visual").objectReferenceValue = visual;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }
            arr.InsertArrayElementAtIndex(arr.arraySize);
            var ne = arr.GetArrayElementAtIndex(arr.arraySize - 1);
            ne.FindPropertyRelative("cropId").stringValue = cropId;
            ne.FindPropertyRelative("visual").objectReferenceValue = visual;
            so.ApplyModifiedPropertiesWithoutUndo();
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
            _safehouseSpawn = pos + rot * new Vector3(0f, 0.2f, 3.4f);

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

        /// <summary>
        /// MINI-042. A second, purchasable safehouse in town - reuses the
        /// same open-shelter geometry as the free Montine farm one
        /// (BuildOpenSafehouse), gated on owning "prop_safehouse" (already
        /// sold at the Land Office since MINI-013 but, until now, a pure
        /// economy entry with no gameplay effect). Set back further from
        /// the road than the shopfronts/houses to avoid overlapping the
        /// dense house row lining the street.
        /// </summary>
        private static void BuildLalayHouse(Terrain terrain, List<Vector3> roadPoints)
        {
            int index = Mathf.Clamp(4, 1, roadPoints.Count - 2);
            Vector3 dir = (roadPoints[index + 1] - roadPoints[index - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 pos = roadPoints[index] + right * 13f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);
            Quaternion rot = Quaternion.LookRotation(-right, Vector3.up);

            var parent = new GameObject("LalayHouse");
            parent.transform.position = pos;
            parent.transform.rotation = rot;

            BuildOpenSafehouse(parent.transform, pos, rot);

            var restGo = new GameObject("LalayHouse_Rest");
            restGo.transform.SetParent(parent.transform);
            restGo.transform.position = pos + rot * new Vector3(0f, 0.6f, 0f);
            var safehouse = restGo.AddComponent<SafehouseInteractable>();
            var so = new SerializedObject(safehouse);
            so.FindProperty("safehouseName").stringValue = "Lalay House";
            so.FindProperty("requiredItemId").stringValue = "prop_safehouse";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// MINI-047/MINI-048. A small bench beside the farm where paired
        /// strains get interbred into a hybrid's seed - Purple Black,
        /// Sugar Cheese, and Purple Cheese all live on this one station
        /// (see CropBreedingStation.Recipe). Built after crops exist
        /// (BuildFarmPathAndClearing itself runs before
        /// BuildEconomyAndCrops, so this can't live there) - _farmCenter
        /// is set during farm building and still valid by the time this
        /// runs.
        /// </summary>
        private static void BuildBreedingStation(Terrain terrain, Vector3 farmCenter, CropDefinition[] crops)
        {
            if (farmCenter == Vector3.zero) return;

            CropDefinition Find(string id) => System.Array.Find(crops, c => c != null && c.cropId == id);
            CropDefinition purple = Find("purple");
            CropDefinition blackSugar = Find("black_sugar");
            CropDefinition purpleBlack = Find("purple_black");
            CropDefinition blueCheese = Find("blue_cheese");
            CropDefinition sugarCheese = Find("sugar_cheese");
            CropDefinition purpleCheese = Find("purple_cheese");

            var recipeList = new List<CropBreedingStation.Recipe>();
            void AddRecipe(CropDefinition a, CropDefinition b, CropDefinition output)
            {
                if (a == null || b == null || output == null) return;
                recipeList.Add(new CropBreedingStation.Recipe
                {
                    parentA = a, parentB = b, output = output, outputSeedCount = 2,
                });
            }
            AddRecipe(purple, blackSugar, purpleBlack);
            AddRecipe(blackSugar, blueCheese, sugarCheese);
            AddRecipe(purple, blueCheese, purpleCheese);
            if (recipeList.Count == 0) return;

            Vector3 pos = farmCenter + new Vector3(-6f, 0f, 6f);
            pos.y = SampleHeight(terrain, pos.x, pos.z);

            var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "BreedingStation";
            bench.transform.position = pos + Vector3.up * 0.4f;
            bench.transform.localScale = new Vector3(1.6f, 0.8f, 0.9f);
            bench.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial("BreedingBench", new Color(0.32f, 0.24f, 0.15f));

            var station = bench.AddComponent<CropBreedingStation>();
            var so = new SerializedObject(station);
            var recipesProp = so.FindProperty("recipes");
            recipesProp.arraySize = recipeList.Count;
            for (int i = 0; i < recipeList.Count; i++)
            {
                var element = recipesProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("parentA").objectReferenceValue = recipeList[i].parentA;
                element.FindPropertyRelative("parentB").objectReferenceValue = recipeList[i].parentB;
                element.FindPropertyRelative("output").objectReferenceValue = recipeList[i].output;
                element.FindPropertyRelative("outputSeedCount").intValue = recipeList[i].outputSeedCount;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
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

            // MINI-031: shared upper-body action layer. Both boys carry
            // the same action list today - a future ability that only one
            // of them has would simply pass a shorter list here, no change
            // to HumanoidAnimationManager itself.
            var animationManager = AddHumanoidAnimationManager(go, animator);

            var meleeCombat = go.AddComponent<SimpleMeleeCombat>();
            var meleeSo = new SerializedObject(meleeCombat);
            meleeSo.FindProperty("animationManager").objectReferenceValue = animationManager;
            meleeSo.ApplyModifiedPropertiesWithoutUndo();

            var farmhand = go.AddComponent<FarmhandController>();
            var companion = go.AddComponent<CompanionInteractable>();
            var companionSo = new SerializedObject(companion);
            companionSo.FindProperty("farmhand").objectReferenceValue = farmhand;
            companionSo.ApplyModifiedPropertiesWithoutUndo();

            var followController = go.AddComponent<FollowController>();
            var fcSo = new SerializedObject(followController);
            fcSo.FindProperty("animator").objectReferenceValue = animator;
            fcSo.ApplyModifiedPropertiesWithoutUndo();
            followController.FollowingEnabled = !startActive;

            // MINI-052: lets the companion (and by extension any follower)
            // sidestep around a building it's been pushing into, instead of
            // grinding against the wall forever. FollowController/PatrolNPC
            // resolve this via GetComponent, so just adding it is enough.
            go.AddComponent<AntiStuckSteering>();

            // MINI-046: the other half of "stronger together" - while
            // this boy is the follower he throws his own punches at
            // whichever officer is already close, using the same
            // animation manager and melee action as the player's own F key.
            var combatAssist = go.AddComponent<CompanionCombatAssist>();
            var assistSo = new SerializedObject(combatAssist);
            assistSo.FindProperty("animationManager").objectReferenceValue = animationManager;
            assistSo.FindProperty("followController").objectReferenceValue = followController;
            assistSo.FindProperty("farmhand").objectReferenceValue = farmhand;
            assistSo.FindProperty("vitals").objectReferenceValue = vitals;
            assistSo.ApplyModifiedPropertiesWithoutUndo();

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

            BuildNpc(terrain, roadPoints, index: 9, sideMul: -1f, goName: "NPC_Vagrant",
                modelPath: "Assets/Floreswa/Models/male03_1.fbx", role: NpcRole.Vagrant,
                cropsForBuyer: allCrops, animController: animController, patrols: false, reactsToHeat: false);

            BuildNpc(terrain, roadPoints, index: 13, sideMul: -1f, goName: "NPC_BlackMarket",
                modelPath: "Assets/Floreswa/Models/male02_3.fbx", role: NpcRole.BlackMarket,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            // MINI-053: StrainTeacher - an older Rasta who teaches each new
            // strain after you complete his missions (Jamaican-sounding).
            BuildNpc(terrain, roadPoints, index: 4, sideMul: 1f, goName: "NPC_StrainTeacher",
                modelPath: "Assets/Floreswa/Models/male03_2.fbx", role: NpcRole.StrainTeacher,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            // MINI-053: Normy - a crooked cop who gives the player missions.
            BuildNpc(terrain, roadPoints, index: 12, sideMul: 1f, goName: "NPC_Normy",
                modelPath: "Assets/Floreswa/Models/male01_2.fbx", role: NpcRole.Normy,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            // MINI-054: the Gwa Bay Health Center (heal/rest) and La Jol
            // (the police station you get taken to when busted).
            BuildHealthCenter(terrain, roadPoints, animController, index: 10, sideMul: -1f);
            BuildLaJol(terrain, roadPoints, animController, index: 3, sideMul: 1f);

            BuildMarketArea(terrain, roadPoints, index: 5, title: "FOOD", secondTitle: null);
            BuildMarketArea(terrain, roadPoints, index: 11, title: "PHARMACY", secondTitle: null);
            BuildMarketArea(terrain, roadPoints, index: 14, title: "LAND AND SURVEYS", secondTitle: null);
            BuildMarketArea(terrain, roadPoints, index: 15, title: "CAR DEALER", secondTitle: null);

            BuildBossNpc(terrain, roadPoints, allCrops, animController);
            // MINI-053 two-boss redesign: one Boss C replaces the old three
            // strain bosses. He vends the strong strain (black sugar / purple
            // / blue cheese) as a tier-ordered roster.
            BuildBossC(terrain, roadPoints, allCrops, animController);
            // MINI-054: the Dog Life rival gang (fightable, up to 10 on
            // their block in Lalay).
            BuildRivalGang(terrain, roadPoints, animController);
            BuildBoatMan(terrain, allCrops, animController);
            // MINI-033: the TMAX-style scooter (buildable/rideable bike).
            BuildBike(terrain, roadPoints, animController);
        }

        /// <summary>
        /// MINI-053. Boss C - the bigger boss. One NPC that vends the
        /// strong strain as a tier-ordered roster (black sugar -> purple ->
        /// blue cheese), replacing the old three separate strain bosses
        /// (BossM/BossP/BossQ). He wears the most chains and stands near a
        /// big black SUV.
        /// </summary>
        private static void BuildBossC(Terrain terrain, List<Vector3> roadPoints,
            CropDefinition[] crops, RuntimeAnimatorController controller)
        {
            int index = roadPoints.Count / 2 + 2;
            index = Mathf.Clamp(index, 1, roadPoints.Count - 2);
            Vector3 dir = (roadPoints[index + 1] - roadPoints[index - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 pos = roadPoints[index] + right * -8f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);
            var go = new GameObject("NPC_BossC");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(right, Vector3.up);
            InstantiateCharacter("Assets/Floreswa/Models/male02_1.fbx", go.transform, controller, null);

            // Black SUV beside the boss (placeholder box, black painted).
            var suv = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suv.name = "BossC_Suv";
            suv.transform.SetParent(go.transform.parent);
            suv.transform.position = pos + right * 3.5f + Vector3.up * 0.5f;
            suv.transform.localScale = new Vector3(2f, 1.4f, 4.2f);
            suv.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard"))
            { color = new Color(0.04f, 0.05f, 0.06f, 1f) };

            var npc = go.AddComponent<TownNPCInteractable>();
            var so = new SerializedObject(npc);
            so.FindProperty("role").enumValueIndex = (int)NpcRole.StrainBoss;
            so.FindProperty("npcName").stringValue = "Boss C";
            // Seed price for the strong strain.
            so.FindProperty("seedPrice").intValue = 500;

            // Tier-ordered strain roster: black sugar -> purple -> blue cheese.
            var rosterProp = so.FindProperty("strainRoster");
            rosterProp.arraySize = 3;
            string[] tiers = { "black_sugar", "purple", "blue_cheese" };
            for (int i = 0; i < tiers.Length; i++)
            {
                CropDefinition c = System.Array.Find(crops, x => x != null && x.cropId == tiers[i]);
                rosterProp.GetArrayElementAtIndex(i).objectReferenceValue = c;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// MINI-054. The Dog Life rival gang - fightable, up to 10 on their
        /// block in Lalay. Each member is a real Humanoid character with a
        /// CharacterController, AntiStuckSteering, GangMemberMover and
        /// NpcCombatHealth (isOfficer = false, so fighting them does not
        /// spike police heat). Models recoloured to the gang look (brown
        /// bandana/scarf, black shirt, brown pants).
        /// </summary>
        private static void BuildRivalGang(Terrain terrain, List<Vector3> roadPoints,
            RuntimeAnimatorController animController)
        {
            // Dog Life block sits in Lalay near the road midpoint, offset
            // from the main street.
            int blockIndex = Mathf.Clamp(roadPoints.Count / 2 - 2, 1, roadPoints.Count - 2);
            Vector3 dir = (roadPoints[blockIndex + 1] - roadPoints[blockIndex - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 blockCenter = roadPoints[blockIndex] + right * 16f;
            blockCenter.y = SampleHeight(terrain, blockCenter.x, blockCenter.z);

            var controllerGo = new GameObject("DogLifeGang");
            controllerGo.transform.position = blockCenter;
            var rival = controllerGo.AddComponent<Gangs.RivalGangController>();
            var rso = new SerializedObject(rival);
            rso.FindProperty("maxMembers").intValue = Gangs.RivalGangController.MaxMembers;
            rso.FindProperty("blockCenter").vector3Value = blockCenter;
            rso.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < Gangs.RivalGangController.MaxMembers; i++)
            {
                BuildGangMember(terrain, blockCenter, i, animController, controllerGo.transform);
            }
        }

        private static void BuildGangMember(Terrain terrain, Vector3 blockCenter, int i,
            RuntimeAnimatorController animController, Transform parent)
        {
            var go = new GameObject($"DogLife_{i}");
            go.transform.SetParent(parent, false);
            go.transform.position = blockCenter + new Vector3(
                (i % 5 - 2) * 2.2f, 0f, (i / 5 - 1) * 2.2f);
            go.transform.rotation = Quaternion.Euler(0f, i * 37f, 0f);

            // Alternate the two male model variants so they are not clones.
            string model = (i % 2 == 0)
                ? "Assets/Floreswa/Models/male01_2.fbx"
                : "Assets/Floreswa/Models/male02_2.fbx";
            var visual = InstantiateCharacter(model, go.transform, animController, null);

            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.height = 1.85f;
            cc.radius = 0.32f;

            // Anti-stuck + movement + combat health (not an officer).
            go.AddComponent<AntiStuckSteering>();
            var mover = go.AddComponent<Gangs.GangMemberMover>();
            var health = go.AddComponent<NpcCombatHealth>();
            var hso = new SerializedObject(health);
            hso.FindProperty("isOfficer").boolValue = false;
            hso.ApplyModifiedPropertiesWithoutUndo();

            // Dog Life look: brown bandana, black top, brown pants.
            ApplyDogLifeLook(visual, go.transform);
        }

        private static void ApplyDogLifeLook(GameObject visual, Transform parent)
        {
            if (visual == null) return;
            foreach (var sr in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mats = sr.sharedMaterials;
                if (mats == null) continue;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name.ToLowerInvariant();
                    Color? recolor = null;
                    // The Floreswa models name their slots tshirt/pants/shoes
                    // (and male01 also has shirt-ish names). No dedicated
                    // headwear slot exists, so the bandana is added as a
                    // separate mesh below.
                    if (n.Contains("tshirt") || n.Contains("shirt") || n.Contains("top") || n.Contains("upper"))
                        recolor = new Color(0.05f, 0.05f, 0.06f, 1f);   // black shirt
                    else if (n.Contains("pant") || n.Contains("leg") || n.Contains("lower"))
                        recolor = new Color(0.4f, 0.28f, 0.12f, 1f);    // brown pants
                    else if (n.Contains("shoe") || n.Contains("foot"))
                        recolor = new Color(0.13f, 0.09f, 0.05f, 1f);   // dark shoes
                    if (recolor == null) continue;
                    // Replace the slot with a brand-new flat Standard
                    // material of the gang colour, discarding the source
                    // material entirely so no albedo texture can leak green
                    // through the recolor (the Floreswa pants/shoes share
                    // green/tan coloured materials).
                    var flat = new Material(Shader.Find("Standard"));
                    flat.color = recolor.Value;
                    mats[i] = flat;
                    changed = true;
                }
                if (changed) sr.sharedMaterials = mats;
            }

            // Add a visible brown bandana wrapped around the forehead so
            // the gang reads as "brown bandanas/scarfs" per the user's look.
            // The base models have no headwear slot, so it is a small thin
            // brown box placed just above the brow line.
            var bandana = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bandana.name = "DogLife_Bandana";
            bandana.transform.SetParent(parent, false);
            bandana.transform.localPosition = new Vector3(0f, 1.72f, 0.03f);
            bandana.transform.localScale = new Vector3(0.34f, 0.09f, 0.32f);
            var mat = bandana.GetComponent<Renderer>().sharedMaterial =
                new Material(Shader.Find("Standard"));
            mat.color = new Color(0.48f, 0.3f, 0.08f, 1f); // brown bandana
        }

        /// <summary>
        /// MINI-054. The Gwa Bay Health Center - a rest/heal landmark in
        /// town. A small white building with a cross over the door and a
        /// HealthCenterInteractable on the threshold.
        /// </summary>
        private static void BuildHealthCenter(Terrain terrain, List<Vector3> roadPoints,
            RuntimeAnimatorController animController, int index, float sideMul)
        {
            index = Mathf.Clamp(index, 1, roadPoints.Count - 2);
            Vector3 dir = (roadPoints[index + 1] - roadPoints[index - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 pos = roadPoints[index] + right * sideMul * 5.5f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);

            var parent = new GameObject("GwaBayHealthCenter");
            parent.transform.position = pos;

            // Simple white building with a pitched roof and a red cross.
            Material white = GetOrCreateMaterial("HealthWhite", new Color(0.93f, 0.93f, 0.93f));
            Material roof = GetOrCreateMaterial("HealthRoof", new Color(0.1f, 0.45f, 0.2f));
            Material cross = GetOrCreateMaterial("HealthCross", new Color(0.8f, 0.1f, 0.1f));

            var baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "HealthBuilding";
            baseBox.transform.SetParent(parent.transform);
            baseBox.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            baseBox.transform.localScale = new Vector3(5f, 2.2f, 4f);
            baseBox.GetComponent<Renderer>().sharedMaterial = white;

            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "HealthCross";
            sign.transform.SetParent(parent.transform);
            sign.transform.localPosition = new Vector3(0f, 3.1f, 2.05f);
            sign.transform.localScale = new Vector3(0.5f, 1.2f, 0.2f);
            sign.GetComponent<Renderer>().sharedMaterial = cross;

            var rest = new GameObject("HealthCenter_Rest");
            rest.transform.SetParent(parent.transform);
            rest.transform.localPosition = new Vector3(0f, 0.3f, 2.4f);
            rest.AddComponent<HealthCenterInteractable>();
        }

        /// <summary>
        /// MINI-054. La Jol - the Gwa Bay police station. A plain police
        /// building whose threshold is the arrest spawn point; add an
        /// officer-styled NPC so it reads as a station.
        /// </summary>
        private static void BuildLaJol(Terrain terrain, List<Vector3> roadPoints,
            RuntimeAnimatorController animController, int index, float sideMul)
        {
            index = Mathf.Clamp(index, 1, roadPoints.Count - 2);
            Vector3 dir = (roadPoints[index + 1] - roadPoints[index - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 pos = roadPoints[index] + right * sideMul * 6f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);

            var parent = new GameObject("LaJolStation");
            parent.transform.position = pos;
            parent.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            Material grey = GetOrCreateMaterial("LaJolGrey", new Color(0.45f, 0.47f, 0.5f));
            Material dark = GetOrCreateMaterial("LaJolDark", new Color(0.12f, 0.14f, 0.18f));

            var baseBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseBox.name = "LaJolBuilding";
            baseBox.transform.SetParent(parent.transform);
            baseBox.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            baseBox.transform.localScale = new Vector3(6f, 2.4f, 5f);
            baseBox.GetComponent<Renderer>().sharedMaterial = grey;

            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "LaJolBand";
            bar.transform.SetParent(parent.transform);
            bar.transform.localPosition = new Vector3(0f, 1.4f, 2.6f);
            bar.transform.localScale = new Vector3(4f, 3f, 0.2f);
            bar.GetComponent<Renderer>().sharedMaterial = dark;

            // The arrest spawn marker lives just in front of the station.
            var station = parent.AddComponent<LaJolStation>();

            // A grumpy officer stands at the door so it reads as a station.
            var npcGo = new GameObject("NPC_LaJolCop");
            npcGo.transform.SetParent(parent.transform);
            npcGo.transform.localPosition = new Vector3(0f, 0.25f, 3.4f);
            npcGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            InstantiateCharacter("Assets/Floreswa/Models/male01_1.fbx", npcGo.transform, animController, null);
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
        /// MINI-033. The TMAX-style scooter — first rideable vehicle. Built
        /// from primitives as a clean low-poly scooter (body tub, front
        /// cowl, seat, handlebar, two wheels, rear mudguard), the visual is
        /// mesh-agnostic so a real TMAX drop-in replaces the render child
        /// later with no logic change. The BikeVehicle component handles
        /// enter/exit, camera-relative riding, and the wheelie (Space).
        /// </summary>
        private static void BuildBike(Terrain terrain, List<Vector3> roadPoints,
            RuntimeAnimatorController animController)
        {
            // Park the bike next to the Car Dealer (index 15) so it reads as
            // a vehicle lot, just off the road.
            int idx = Mathf.Clamp(15, 1, roadPoints.Count - 2);
            Vector3 dir = (roadPoints[idx + 1] - roadPoints[idx - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 pos = roadPoints[idx] + right * 6f;
            pos.y = SampleHeight(terrain, pos.x, pos.z) + 0.25f;

            var bikeGo = new GameObject("TMAX_Bike");
            bikeGo.transform.position = pos;
            bikeGo.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // Visual root (all bike geometry lives here so a swap is easy).
            var visual = new GameObject("Visual");
            visual.transform.SetParent(bikeGo.transform, false);

            Material bodyMat = GetOrCreateMaterial("TMAX_Red", new Color(0.75f, 0.12f, 0.1f));
            Material darkMat = GetOrCreateMaterial("TMAX_Dark", new Color(0.08f, 0.09f, 0.1f));
            Material tyreMat = GetOrCreateMaterial("TMAX_Tyre", new Color(0.03f, 0.03f, 0.03f));
            Material chromeMat = GetOrCreateMaterial("TMAX_Chrome", new Color(0.75f, 0.78f, 0.82f));

            // Main body tub (the maxi-scooter hull).
            AddBox(visual.transform, "Body", new Vector3(0.34f, 0.30f, 1.25f), new Vector3(0f, 0.55f, -0.1f), bodyMat);
            // Front cowl rise.
            AddBox(visual.transform, "Cowl", new Vector3(0.30f, 0.5f, 0.30f), new Vector3(0f, 0.55f, 0.65f), bodyMat);
            // Seat (rider sits here).
            var seat = AddBox(visual.transform, "Seat", new Vector3(0.22f, 0.10f, 0.50f), new Vector3(0f, 0.78f, -0.05f), darkMat);
            // Floorboard / foot rest.
            AddBox(visual.transform, "Floorboard", new Vector3(0.28f, 0.06f, 0.60f), new Vector3(0f, 0.32f, 0.15f), darkMat);
            // Handlebar stem + bar.
            AddBox(visual.transform, "HandlebarStem", new Vector3(0.05f, 0.35f, 0.05f), new Vector3(0f, 1.0f, 0.72f), darkMat);
            AddBox(visual.transform, "Handlebar", new Vector3(0.42f, 0.05f, 0.05f), new Vector3(0f, 1.18f, 0.74f), chromeMat);
            // Windscreen.
            AddBox(visual.transform, "Screen", new Vector3(0.28f, 0.30f, 0.04f), new Vector3(0f, 0.95f, 0.80f), chromeMat);

            // Two wheels (front + rear) as separate children so they spin and
            // the front can visually lift on a wheelie.
            var frontWheel = MakeWheel(visual.transform, "FrontWheel", new Vector3(0f, 0.32f, 0.80f), tyreMat, chromeMat);
            var backWheel = MakeWheel(visual.transform, "BackWheel", new Vector3(0f, 0.32f, -0.72f), tyreMat, chromeMat);

            // Rider mount; the player parents onto this to sit.
            var riderMount = new GameObject("RiderMount");
            riderMount.transform.SetParent(bikeGo.transform, false);
            riderMount.transform.localPosition = new Vector3(0f, 0.82f, -0.05f);

            // The vehicle logic.
            var bike = bikeGo.AddComponent<Vehicles.BikeVehicle>();
            var so = new SerializedObject(bike);
            so.FindProperty("riderMount").objectReferenceValue = riderMount.transform;
            so.FindProperty("frontWheel").objectReferenceValue = frontWheel.transform;
            so.FindProperty("backWheel").objectReferenceValue = backWheel.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject AddBox(Transform parent, string name, Vector3 scale, Vector3 localPos, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            go.transform.localPosition = localPos;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            // Bikes don't need solid colliders on every part; a single box
            // collider on the body is enough (added by the scene builder if
            // needed). Primitives get a BoxCollider by default; remove it so
            // the whole-vehicle collider isn't doubled.
            var bc = go.GetComponent<Collider>();
            if (bc != null) Object.DestroyImmediate(bc);
            return go;
        }

        private static GameObject MakeWheel(Transform parent, string name, Vector3 localPos, Material tyreMat, Material hubMat)
        {
            var wheel = new GameObject(name);
            wheel.transform.SetParent(parent, false);
            wheel.transform.localPosition = localPos;

            var tyre = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tyre.name = name + "_Tyre";
            tyre.transform.SetParent(wheel.transform, false);
            tyre.transform.localScale = new Vector3(0.34f, 0.18f, 0.34f);
            tyre.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tyre.GetComponent<Renderer>().sharedMaterial = tyreMat;
            var tc = tyre.GetComponent<Collider>();
            if (tc != null) Object.DestroyImmediate(tc);

            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = name + "_Hub";
            hub.transform.SetParent(wheel.transform, false);
            hub.transform.localScale = new Vector3(0.14f, 0.20f, 0.14f);
            hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hub.GetComponent<Renderer>().sharedMaterial = hubMat;
            var hc = hub.GetComponent<Collider>();
            if (hc != null) Object.DestroyImmediate(hc);

            return wheel;
        }

        /// <summary>
        /// Boss J waits near the Montine turnoff, away from the market -
        /// per Docs/STORY.md he watches their deliveries and offers the
        /// higher-paying illegal work. Renamed from Boss K in the MINI-053
        /// two-boss redesign (Boss J and Boss C).
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

            var go = new GameObject("NPC_BossJ");
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
            so.FindProperty("npcName").stringValue = "Boss J";
            so.FindProperty("bossSeedCrop").objectReferenceValue = bushers;
            // MINI-039: first illegal strain, cheapest of the three -
            // see the seed-pricing note by the BossM/BossP calls.
            so.FindProperty("seedPrice").intValue = 150;
            var cropsProp = so.FindProperty("sellableCrops");
            cropsProp.arraySize = allCrops.Length;
            for (int i = 0; i < allCrops.Length; i++)
                cropsProp.GetArrayElementAtIndex(i).objectReferenceValue = allCrops[i];
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

            if (role == NpcRole.Police || role == NpcRole.Normy)
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
                    // MINI-038: hit-reaction stagger + knockdown-and-lie-
                    // down needs the same action layer the player's punch
                    // does, and NpcCombatHealth needs a reference to it.
                    var npcAnimator = npcVisual.GetComponentInChildren<Animator>();
                    var npcAnimationManager = AddHumanoidAnimationManager(npcGo, npcAnimator);

                    var combatHealth = npcGo.AddComponent<NpcCombatHealth>();
                    var chSo = new SerializedObject(combatHealth);
                    chSo.FindProperty("animationManager").objectReferenceValue = npcAnimationManager;
                    chSo.ApplyModifiedPropertiesWithoutUndo();

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
                    oso.FindProperty("combatHealth").objectReferenceValue = combatHealth;
                    oso.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }

                var patrol = npcGo.AddComponent<PatrolNPC>();
                // MINI-052: patrolling NPCs also sidestep around buildings they
                // push into, instead of grinding against a wall forever.
                npcGo.AddComponent<AntiStuckSteering>();
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

        private static readonly (string id, string name, int price, bool illegal, string unripeHex, string ripeHex, string secondaryRipeHex)[] CropSpecs =
        {
            ("tomato", "Tomato", 5, false, "4D8C40", "BF1F1A", null),
            ("banana", "Banana", 6, false, "5C9E3E", "E8D23C", null),
            ("carrot", "Carrot", 4, false, "4D8C40", "E07A1F", null),
            ("bushers", "Bushers", 22, true, "3A6B2E", "5B7A2E", null),
            // MINI-039: ripe hex recoloured per the user's explicit ask -
            // Black Sugar shows orange buds, Purple shows (brighter)
            // purple buds. This table is reapplied unconditionally on
            // every scene build (see BuildEconomyAndCrops below), so it -
            // not a one-off edit of the generated .asset files - is the
            // actual source of truth; a direct .asset edit or a one-off
            // AssetDatabase script both get silently overwritten the next
            // time BuildScene runs.
            ("black_sugar", "Black Sugar", 38, true, "28351F", "D97314", null),
            ("purple", "Purple", 55, true, "35402B", "8C1FAD", null),
            // MINI-047: Purple Black - interbred from Purple + Black
            // Sugar (see CropBreedingStation). secondaryRipeHex set means
            // ripe fruit alternates orange (Black Sugar's own colour) and
            // purple (Purple's) instead of a single flat colour, so both
            // parent strains show at once.
            ("purple_black", "Purple Black", 90, true, "241A2B", "8C1FAD", "D97314"),
            // MINI-048: Blue Cheese - a new base strain (boss-granted, like
            // Black Sugar/Purple), single blue ripe colour. Its two hybrids
            // both alternate blue with their other parent's own colour -
            // Sugar Cheese with Black Sugar's orange, Purple Cheese with
            // Purple's purple - in the order the user asked for them.
            ("blue_cheese", "Blue Cheese", 65, true, "1E3550", "2E6BAD", null),
            ("sugar_cheese", "Sugar Cheese", 110, true, "1E3550", "2E6BAD", "D97314"),
            ("purple_cheese", "Purple Cheese", 130, true, "241A2B", "2E6BAD", "8C1FAD"),
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
                if (!string.IsNullOrEmpty(spec.secondaryRipeHex))
                {
                    ColorUtility.TryParseHtmlString("#" + spec.secondaryRipeHex, out var secondary);
                    crop.secondaryRipeColor = secondary; // parsed hex has alpha 1 - "set"
                }
                else
                {
                    crop.secondaryRipeColor = new Color(0f, 0f, 0f, 0f); // explicit "unset" - single-colour ripe
                }
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
            // MINI-040: one-time unlock for CellPhoneController's "call
            // your partner" key - farm shop rather than a dedicated
            // communications shop that doesn't otherwise exist.
            ("phone_basic", "Basic Cell Phone",    ShopCategory.Communication, 200, null, 0),
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
            // MINI-039: NPC_LandOffice is built at road index 14 (see
            // BuildNpc's LandOffice call) and land_montine is stocked in
            // its own landShop, not the Farm Shop's - the M3 objective
            // below used to send the player to marketPos/"Farm Shop"
            // regardless, a stale leftover from before the Land Office
            // existed. Land purchases now correctly route here.
            Vector3 landOfficePos = roadPoints[Mathf.Clamp(14, 1, roadPoints.Count - 2)];
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
                            instruction = "Buy the Montine Land Plot at Land and Surveys ($600)",
                            markerPosition = landOfficePos,
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
                            targetId = "BossK",
                            instruction = "Take the Bushers back to Boss K - this mission sale adds 50% heat",
                            markerPosition = bossPos,
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
                new Mission
                {
                    missionId = "M6",
                    title = "Build the Stock",
                    briefing = "Keep working the Montine plots and build enough stock for the next move.",
                    rewardMoney = 100,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.HarvestCrop,
                            targetId = "bushers",
                            requiredCount = 6,
                            instruction = "Grow and harvest 6 Bushers before expanding operations",
                            markerPosition = farmCenter,
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.HarvestCrop,
                            targetId = "bushers",
                            requiredCount = 3,
                            instruction = "Collect the Bushers crop after your partner tends the farm",
                            markerPosition = plotPos,
                        },
                    }
                },
                new Mission
                {
                    missionId = "M7",
                    title = "Street Route",
                    briefing = "Boss K pay best, but a vagrant in Lalay buying small amounts with less questions.",
                    rewardMoney = 140,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.TalkTo,
                            targetId = "Vagrant",
                            instruction = "Find the vagrant along the Lalay road",
                            markerPosition = roadPoints[Mathf.Clamp(9, 1, roadPoints.Count - 2)],
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.SellCrop,
                            targetId = "Vagrant",
                            instruction = "Sell the Bushers to the vagrant - off-mission sales add 30% heat",
                            markerPosition = roadPoints[Mathf.Clamp(9, 1, roadPoints.Count - 2)],
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.EscapeHeat,
                            instruction = "Leave the road and lose the heat before returning home",
                            markerPosition = farmCenter,
                        },
                    }
                },
                new Mission
                {
                    missionId = "M8", title = "Choose Your Road",
                    briefing = "Press L to expand legitimate farming, or K to commit to Boss K's weed route.",
                    objectives = new List<MissionObjective> { new MissionObjective { kind = ObjectiveKind.ChoosePath, instruction = "Choose now: [L] Legitimate farming  or  [K] Weed route", hasMarker = false } }
                },
                new Mission
                {
                    missionId = "M9L", title = "Roots in the Soil", requiredPath = CareerPath.LegitimateFarmer,
                    briefing = "Build respect with farmers and expand without Boss K owning allu.", rewardMoney = 300,
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.AssignFarmhand, targetId = "tomato", instruction = "Select Tomato [1], then ask the other boy to manage three plots", markerPosition = farmCenter },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "tomato", requiredCount = 9, instruction = "Harvest 9 tomato for the legitimate market", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.BuyItem, targetId = "land_hillside", instruction = "Buy the Hillside Survey Lot", markerPosition = roadPoints[Mathf.Clamp(14,1,roadPoints.Count-2)] }
                    }
                },
                new Mission
                {
                    missionId = "M9W", title = "Boss K's Cut", requiredPath = CareerPath.WeedRoute,
                    briefing = "Boss K lower the price after allu take the risk. He say loyalty first, payment later.",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BossK", instruction = "Return to Boss K for a worse job", markerPosition = bossPos },
                        new MissionObjective { kind = ObjectiveKind.AssignFarmhand, targetId = "bushers", instruction = "Select Bushers [4], then assign the other boy to manage three plots", markerPosition = farmCenter },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "bushers", requiredCount = 3, instruction = "Grow and harvest 3 Bushers", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.SellCrop, targetId = "BossK", instruction = "Deliver to Boss K - he is cutting your payment", markerPosition = bossPos }
                    }
                },
                new Mission
                {
                    missionId = "M10W", title = "Black Sugar", requiredPath = CareerPath.WeedRoute,
                    briefing = "Boss M will only release Black Sugar after Boss K use allu enough.",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BossM", instruction = "Meet Boss M and unlock Black Sugar [5]", markerPosition = roadPoints[Mathf.Clamp(10,1,roadPoints.Count-2)] },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "black_sugar", requiredCount = 3, instruction = "Grow and harvest Black Sugar [5]", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.SellCrop, targetId = "BossK", instruction = "Deliver it to Boss K - payment may be withheld", markerPosition = bossPos }
                    }
                },
                new Mission
                {
                    missionId = "M11W", title = "Purple Territory", requiredPath = CareerPath.WeedRoute,
                    briefing = "Your gang reputation open a meeting with Boss P. Purple pays most and brings the most heat.",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BossP", instruction = "Meet Boss P and unlock Purple [6]", markerPosition = roadPoints[Mathf.Clamp(13,1,roadPoints.Count-2)] },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "purple", requiredCount = 3, instruction = "Grow and harvest Purple [6]", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BoatMan", instruction = "The Grand Bay route is established - meet the Boat Man", markerPosition = new Vector3(0f, SeaLevelY, TerrainSize*.5f) }
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
                mp.FindPropertyRelative("requiredPath").enumValueIndex = (int)mission.requiredPath;

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

        private static void BuildHUD(CropDefinition[] crops)
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
            infoRect.sizeDelta = new Vector2(330f, 235f);
            infoRect.anchoredPosition = new Vector2(-20f, -20f);
            infoPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            Text moneyLabel = CreateLabel(infoPanel.transform, "$0", 34, new Vector2(0f, -26f), font);
            moneyLabel.rectTransform.anchorMin = moneyLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            moneyLabel.rectTransform.sizeDelta = new Vector2(260f, 40f);

            Text cropLabel = CreateLabel(infoPanel.transform, string.Empty, 24, new Vector2(0f, -68f), font);
            cropLabel.rectTransform.anchorMin = cropLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            cropLabel.rectTransform.sizeDelta = new Vector2(260f, 36f);

            Text inventoryLabel = CreateLabel(infoPanel.transform, string.Empty, 18, new Vector2(0f, -145f), font);
            inventoryLabel.rectTransform.anchorMin = inventoryLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            inventoryLabel.rectTransform.sizeDelta = new Vector2(310f, 145f);
            inventoryLabel.alignment = TextAnchor.UpperLeft;

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
            so.FindProperty("inventoryLabel").objectReferenceValue = inventoryLabel;
            var known = so.FindProperty("knownCrops");
            known.arraySize = crops.Length;
            for (int i = 0; i < crops.Length; i++) known.GetArrayElementAtIndex(i).objectReferenceValue = crops[i];
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
            var blackMarket = BuildShopPanel(canvasGo, "BlackMarketPanel", "BLACK MARKET - SELL CLOTHES", apparelStock, font, true);

            // MINI-053: a single seed-buy dialogue box (used by Boss J,
            // Boss C and the Rasta strain teacher for buying strain seeds).
            var seedBuy = BuildSeedBuyDialogue(canvasGo, font);

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
                    (int)NpcRole.BlackMarket => blackMarket,
                    _ => null
                };
                if (target == null) continue;

                nso.FindProperty("shop").objectReferenceValue = target;

                // MINI-053: give seed-selling NPCs the seed-buy dialogue box.
                int ri = nso.FindProperty("role").enumValueIndex;
                if (ri == (int)NpcRole.Boss || ri == (int)NpcRole.StrainBoss || ri == (int)NpcRole.StrainTeacher)
                {
                    nso.FindProperty("seedBuyDialogue").objectReferenceValue = seedBuy;
                }
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
            // MINI-053: banner made smaller and moved down so it does not
            // swallow the screen; it also stays up longer (see MissionHUD).
            bRect.sizeDelta = new Vector2(880f, 150f);
            bRect.anchoredPosition = new Vector2(0f, 120f);
            var banner = bannerGo.AddComponent<Text>();
            banner.font = font;
            banner.fontSize = 30;
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

            // MINI-055: game-opening "kicked out of school" dialogue cutscene.
            BuildOpeningDialogue(canvasGo, font);
        }

        /// <summary>
        /// MINI-055. Builds the game-start dialogue cutscene panel and wires
        /// the user's exact opening script (Sacat + Franki talking about
        /// being kicked out of school, being hungry, going to Zion). E to
        /// advance; the controller locks player control until it finishes.
        /// </summary>
        private static void BuildOpeningDialogue(GameObject canvasGo, Font font)
        {
            var panel = CreateModalPanel(canvasGo.transform, "OpeningDialoguePanel", new Vector2(900f, 260f));
            panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -300f);

            var text = CreateModalText(panel.transform, font, 30);
            text.alignment = TextAnchor.MiddleLeft;

            var opening = canvasGo.AddComponent<UI.OpeningDialogueController>();
            var so = new SerializedObject(opening);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("bodyText").objectReferenceValue = text;
            so.FindProperty("holdAfterLast").floatValue = 1.2f;

            // The user's exact opening script, in the supplied vocab.
            var linesProp = so.FindProperty("lines");
            linesProp.arraySize = 7;
            string[] speakers = { "Sacat", "Franki", "Sacat", "Franki", "Sacat", "Franki", "Sacat" };
            string[] texts =
            {
                "boi they kick you out of school",
                "boi I hungry, me self doe even think i can go down diah",
                "We need to make ah money wii gasah",
                "boii let us go zion",
                "boi i doe really want to plant zeb yea but we go see",
                "we will plant normal crops and we will see how dat go",
                "Boi i scrub wii fadah",
            };
            for (int i = 0; i < 7; i++)
            {
                var el = linesProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("speaker").stringValue = speakers[i];
                el.FindPropertyRelative("text").stringValue = texts[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ShopPanelController BuildShopPanel(
            GameObject canvasGo, string panelName, string title, ShopItemDefinition[] stock, Font font,
            bool resaleMode = false)
        {
            var panel = CreateModalPanel(canvasGo.transform, panelName, new Vector2(900f, 640f));
            var text = CreateModalText(panel.transform, font, 26);

            var shop = canvasGo.AddComponent<ShopPanelController>();
            var so = new SerializedObject(shop);
            so.FindProperty("shopTitle").stringValue = title;
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("bodyText").objectReferenceValue = text;
            so.FindProperty("resaleMode").boolValue = resaleMode;
            var stockProp = so.FindProperty("stock");
            stockProp.arraySize = stock.Length;
            for (int i = 0; i < stock.Length; i++)
            {
                stockProp.GetArrayElementAtIndex(i).objectReferenceValue = stock[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return shop;
        }

        /// <summary>
        /// MINI-053. Builds the single seed-buy dialogue box used by Boss J,
        /// Boss C and the Rasta strain teacher. A centred panel that shows a
        /// strain's seed offer/price and buys on E, echoes ShopPanelController's
        /// lightweight keyboard-driven pattern for phone compatibility later.
        /// </summary>
        private static UpIzUpMini.UI.SeedBuyDialogue BuildSeedBuyDialogue(GameObject canvasGo, Font font)
        {
            var panel = CreateModalPanel(canvasGo.transform, "SeedBuyPanel", new Vector2(760f, 420f));
            var text = CreateModalText(panel.transform, font, 26);

            var seedBuy = canvasGo.AddComponent<UpIzUpMini.UI.SeedBuyDialogue>();
            var so = new SerializedObject(seedBuy);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("bodyText").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
            return seedBuy;
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
