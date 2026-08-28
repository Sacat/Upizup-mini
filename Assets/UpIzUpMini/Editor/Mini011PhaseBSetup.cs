using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Dialogue;
using UpIzUpMini.Economy;
using UpIzUpMini.Farming;
using UpIzUpMini.Interaction;
using UpIzUpMini.Missions;
using UpIzUpMini.UI;
using UpIzUpMini.Progression;
using UpIzUpMini.Combat;
using UpIzUpMini.Vehicles;

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
            BuildLalayEstate(terrain, roadPoints);

            Vector3 startPos = _safehouseSpawn != Vector3.zero ? _safehouseSpawn : roadPoints[0];
            // MINI-062: only re-sample ground height for the roadPoints[0]
            // fallback - _safehouseSpawn already carries a real sampled
            // terrain height (see BuildFarmSafehouse) plus a small fixed
            // offset, so resampling it here at very slightly different X/Z
            // used to silently drift its Y a few centimetres away from the
            // exact value FarmSafehouse_Rest's own spawnPoint field uses,
            // making "select the farm as your respawn" a not-quite-no-op
            // even though it's the same point.
            if (_safehouseSpawn == Vector3.zero) startPos.y = SampleHeight(terrain, startPos.x, startPos.z);

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
                null, startPos, locomotionController, startActive: true,
                visualScale: TargetCharacterHeightM / SacatMeasuredHeightM);
            CharacterSlot strong = BuildControllableCharacter(
                "Franki", "Franki", "Assets/UpIzUpMini/Art/Characters/Strong.fbx",
                null, startPos + new Vector3(1.4f, 0f, -1.2f),
                locomotionController, startActive: false,
                visualScale: TargetCharacterHeightM / FrankiMeasuredHeightM);
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

            // MINI-073: register the full Food/Pharmacy catalog with the
            // economy so a later UseConsumable(itemId) can look effect
            // numbers back up from a bare id, and give the inventory panel
            // the crop list so it can show what is held. EconomyManager was
            // already built by BuildEconomyAndCrops above; found by name
            // rather than threading a reference through, matching the
            // project's existing GameObject.Find(dealerNpcName) pattern.
            var economyGoRef = GameObject.Find("EconomyManager");
            var economyRef = economyGoRef != null ? economyGoRef.GetComponent<EconomyManager>() : null;
            economyRef?.RegisterConsumableCatalog(foodStock);
            economyRef?.RegisterConsumableCatalog(pharmacyStock);

            var invGo = new GameObject("InventoryPanel");
            var invPanel = invGo.AddComponent<InventoryPanelController>();
            var invSo = new SerializedObject(invPanel);
            var invCropsProp = invSo.FindProperty("allCrops");
            invCropsProp.arraySize = crops.Length;
            for (int i = 0; i < crops.Length; i++) invCropsProp.GetArrayElementAtIndex(i).objectReferenceValue = crops[i];
            invSo.ApplyModifiedPropertiesWithoutUndo();

            BuildStreetSigns(terrain, roadPoints, _farmCenter);
            BuildExtraUI(farmStock, apparelStock, landStock, dealerStock, foodStock, pharmacyStock, roadPoints, _farmCenter);
            BuildVehicleSpawner();
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
            new GameObject("GameplayHints").AddComponent<GameplayHintController>();
            BuildPauseMenu();

            // MINI-059: every FarmPlot (main + expansion) already exists by
            // this point - collects them all for the plantation theft risk.
            BuildPlantationTheft();

            // MINI-100: replace the old synthetic environment only after every
            // gameplay role exists, then relocate those stable roots onto VA-005.
            // The isolated map-lab and rollback commit remain untouched.
            Mini100GrandBayMapMigration.ApplyToOpenScene(scene);
            RegisterGrandBayMiniMapMarkers();

            // MINI-052: bake last, after every static obstacle (buildings,
            // terrain, farm, coast) is in place, so NavPathSteerer (the
            // companion/villager/police steering) has real path coverage
            // around houses instead of a straight line.
            BuildNavigationMesh();

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
        /// <summary>Exposes the shared action list so other build tools (the
        /// MINI-066 test-scene rider) populate a character with exactly the
        /// same clips the real game characters get, rather than a
        /// hand-maintained subset that could drift.</summary>
        public static HumanoidAnimationManager.ActionEntry[] GetSharedActionEntriesPublic() => GetSharedActionEntries();

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

                // MINI-066 motorcycle riding, from the purchased Animo Mocap
                // pack. Every one of these is fullBody: sitting on a bike must
                // replace locomotion entirely, not layer over a walk cycle -
                // exactly the case HumanoidAnimationManager's FullBodyOverride
                // layer was built for.
                //
                // The pack names transition clips "<from>_<to>", which is how
                // the mount/dismount were found: "Idle_MOTOIdle01" is standing
                // idle INTO the seated bike pose (5.33s) and "MOTOIdle01_Idle"
                // is the reverse (6.33s). Every other transition in the pack is
                // 0.33-2s because it only shifts between seated poses; these
                // two are long because they are whole-body get-on/get-off
                // moves. So no clip needed reversing after all.
                new HumanoidAnimationManager.ActionEntry
                {
                    id = MountActionId,
                    clip = LoadNamedClip(MotoIdleFbx, "AA_MOTO_Idle_MOTOIdle01"),
                    fullBody = true,
                },
                new HumanoidAnimationManager.ActionEntry
                {
                    id = DismountActionId,
                    clip = LoadNamedClip(MotoIdleFbx, "AA_MOTO_MOTOIdle01_Idle"),
                    fullBody = true,
                },
                // Riding pose is MOTOIdle02, not MOTOIdle01 - the user
                // identified MOTOIdle01 as the mounting pose from the pack's
                // own preview video, and the clip list independently confirms
                // it: every lean, look and cheer in the pack transitions to or
                // from MOTOIdle02 ("MOTOIdle02_Left01", "cheer01_MOTOIdle02",
                // ...) and never from MOTOIdle01. MOTOIdle02 is the hub pose
                // the rider actually sits in, which is why the pack also ships
                // MOTOIdle01_MOTOIdle02 (8.00s, settling in after mounting).
                new HumanoidAnimationManager.ActionEntry
                {
                    id = RideBikeActionId,
                    clip = LoadNamedClip(MotoIdleFbx, "AA_MOTO_MOTOIdle02_Loop"),
                    fullBody = true,
                },
                // Pillion: the user's own suggestion to repurpose a cheer as
                // "holding on behind". cheer02_Loop is the longest (6.00s) so
                // it reads as an idle rather than a repeating twitch.
                new HumanoidAnimationManager.ActionEntry
                {
                    id = RidePillionActionId,
                    clip = LoadNamedClip(MotoAction03Fbx, "AA_MOTO_cheer02_Loop"),
                    fullBody = true,
                },

                // Stopped-but-mounted. Measured torso pitch confirms the
                // user's read of the pack's preview video: MOTOIdle01 sits at
                // -38.8deg from vertical (upright, engine idling) while
                // MOTOIdle02 is -59.8deg (crouched over the bars, moving).
                new HumanoidAnimationManager.ActionEntry
                {
                    id = BikeStoppedActionId,
                    clip = LoadNamedClip(MotoIdleFbx, "AA_MOTO_MOTOIdle01_Loop"),
                    fullBody = true,
                },

                // The wheelie pose. Named "cheer01" in the pack, but it is
                // measurably NOT a celebration: the torso swings from -59.8deg
                // (normal riding) to +1.7deg - bolt upright - a 61-degree
                // change no other clip in the pack comes close to, and exactly
                // what a rider does when the front wheel comes up. Found by
                // the user watching the preview video; confirmed numerically
                // before wiring (see the MINI-066 handoff entry).
                new HumanoidAnimationManager.ActionEntry
                {
                    id = BikeWheelieActionId,
                    clip = LoadNamedClip(MotoAction02Fbx, "AA_MOTO_cheer01_Loop"),
                    fullBody = true,
                },

                // Cornering leans. Left02/Right02 measure as a symmetric
                // +/-6.6deg lateral tilt with riding pitch unchanged, i.e.
                // genuine mirrored leans rather than two unrelated poses.
                new HumanoidAnimationManager.ActionEntry
                {
                    id = BikeLeanLeftActionId,
                    clip = LoadNamedClip(MotoTurn30Fbx, "AA_MOTO_Left02_Loop"),
                    fullBody = true,
                },
                new HumanoidAnimationManager.ActionEntry
                {
                    id = BikeLeanRightActionId,
                    clip = LoadNamedClip(MotoTurn30Fbx, "AA_MOTO_Right02_Loop"),
                    fullBody = true,
                },

                // Stop / rest / pull-away transitions (see the id constants
                // for why these particular clips). One-shots, not held poses:
                // each is a move BETWEEN states, and BikeRiderAnimation
                // sequences them.
                new HumanoidAnimationManager.ActionEntry
                {
                    id = BikeStopSettleActionId,
                    clip = LoadNamedClip(MotoIdleFbx, "AA_MOTO_MOTOIdle02_MOTOIdle01"),
                    fullBody = true,
                },
                new HumanoidAnimationManager.ActionEntry
                {
                    id = BikeRestActionId,
                    clip = LoadNamedClip(MotoIdleFbx, "AA_MOTO_MOTOIdle01_Idle"),
                    fullBody = true,
                },
                new HumanoidAnimationManager.ActionEntry
                {
                    id = BikePullAwayActionId,
                    clip = LoadNamedClip(MotoIdleFbx, "AA_MOTO_MOTOIdle01_MOTOIdle02"),
                    fullBody = true,
                },
            };
        }

        // MINI-066. Action ids shared between the seat definitions on the
        // TMAX prefab (see Mini065TmaxPhysicsTest.WireSeat) and the Animator
        // states baked here - kept as constants so the two cannot drift apart
        // silently, which would show up only as a rider with no pose.
        // MINI-066 follow-up: the stopped-at-a-standstill sequence the user
        // described from the pack's preview video. MOTOIdle02 is the riding
        // hub pose and MOTOIdle01 the stopped one, so the pack's own
        // "<from>_<to>" transition clips are exactly the settle/pull-away
        // moves: 02->01 when the bike comes to rest, 01->Idle if the rider
        // keeps sitting there, and 01->02 when they pull away again.
        public const string BikeStopSettleActionId = "BikeStopSettle";
        public const string BikeRestActionId = "BikeRest";
        public const string BikePullAwayActionId = "BikePullAway";
        public const string MountActionId = "MountBike";
        public const string DismountActionId = "DismountBike";
        public const string RideBikeActionId = "RideBike";
        public const string RidePillionActionId = "RidePillion";
        public const string BikeStoppedActionId = "BikeStopped";
        public const string BikeWheelieActionId = "BikeWheelie";
        public const string BikeLeanLeftActionId = "BikeLeanLeft";
        public const string BikeLeanRightActionId = "BikeLeanRight";

        private const string MotoIdleFbx = "Assets/UpIzUpMini/Art/Animations/Moto/AA_MOTO_Idle.fbx";
        private const string MotoAction02Fbx = "Assets/UpIzUpMini/Art/Animations/Moto/AA_MOTO_Action02.fbx";
        private const string MotoAction03Fbx = "Assets/UpIzUpMini/Art/Animations/Moto/AA_MOTO_Action03.fbx";
        private const string MotoTurn30Fbx = "Assets/UpIzUpMini/Art/Animations/Moto/AA_MOTO_TurnRight30.fbx";

        /// <summary>
        /// Loads one specific clip by name out of an FBX that contains
        /// several. The moto pack ships 30 clips across 6 files, so the
        /// plain "first clip in the file" approach LoadClip uses would pick
        /// an arbitrary one.
        /// </summary>
        private static AnimationClip LoadNamedClip(string fbxPath, string clipName)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var o in all)
            {
                if (o is AnimationClip c && c.name == clipName) return c;
            }
            Debug.LogWarning($"MINI-066: clip '{clipName}' not found in {fbxPath} - that riding action will be missing.");
            return null;
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
        // MINI-052: NavMesh bake for NavPathSteerer (companion/villager/
        // police stuck-recovery + path-around-houses).
        // ---------------------------------------------------------------

        /// <summary>
        /// Bakes one walkable NavMesh from every collider in the finished
        /// scene (terrain + building/prop colliders, via PhysicsColliders
        /// collection) so NavPathSteerer's static NavMesh.CalculatePath
        /// calls have real coverage at runtime. com.unity.ai.navigation is
        /// already an installed package (Packages/manifest.json) - reused,
        /// not newly added. The surface GameObject is kept in the built
        /// scene (not destroyed after baking): NavMeshSurface adds its
        /// baked data on OnEnable, which is how the baked mesh becomes
        /// available to the static NavMesh API at Play/runtime.
        /// </summary>
        private static void BuildNavigationMesh()
        {
            MarkClosedMapHousesNotWalkable("Lalay_Dense_House_Massing");
            MarkClosedMapHousesNotWalkable("Highland_Sparse_House_Massing");
            var go = new GameObject("NavMeshSurface");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
            // Officers/companion are roughly capsule-radius 0.3-0.35m
            // (see PoliceOfficer/PatrolNPC CharacterController radius) -
            // keep the default humanoid agent settings, which comfortably
            // clear the narrow farm/road paths built elsewhere.
            surface.BuildNavMesh();
        }

        private static void MarkClosedMapHousesNotWalkable(string rootName)
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null) return;
            int notWalkable = NavMesh.GetAreaFromName("Not Walkable");
            foreach (Transform house in root.transform)
            {
                NavMeshModifier modifier = house.GetComponent<NavMeshModifier>();
                if (modifier == null) modifier = house.gameObject.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = notWalkable;
            }
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
        private static Vector3 _safehousePos;
        private static Quaternion _safehouseRot = Quaternion.identity;
        private static float _farmClearRadius = 22f;
        private static Vector3 _expansionPlotPos;
        private static Vector3 _bossPos;
        private static Vector3 _bossCPos;
        // MINI-109: Rasta's real built position, so M13's marker can read
        // it directly instead of recomputing the placement formula
        // (see BuildRastaMentor's own note - a prior duplication of that
        // formula there was missing the farmRight*3f offset, landing the
        // marker 3m off from where Rasta actually stands).
        private static Vector3 _rastaPos;

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
                    var weedVisual = BuildWeedCropVisual(plot.transform, "WeedVisual",
                        "Assets/UpIzUpMini/Art/CropMeshes/WeedPlant_LOD.asset", 1.9f);
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
            BuildFarmPrivacyScreen(terrain, farmCenter, right, dir, farmParent.transform);

            farmPlot = firstPlot;
        }

        /// <summary>
        /// A cheap, mobile-friendly living screen around the Highland plots.
        /// The bushes are visual only, so companions cannot become trapped, and
        /// the generous gap facing the dirt track remains the obvious entrance.
        /// </summary>
        private static void BuildFarmPrivacyScreen(
            Terrain terrain, Vector3 farmCenter, Vector3 right, Vector3 dir, Transform parent)
        {
            var screen = new GameObject("HighlandFarmPrivacyBushes");
            screen.transform.SetParent(parent);
            Material foliage = GetOrCreateMaterial("FarmPrivacyFoliage", new Color(0.055f, 0.22f, 0.07f));

            void AddHedge(Vector3 position, Vector3 size)
            {
                position.y = SampleHeight(terrain, position.x, position.z) + size.y * 0.5f;
                var hedge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hedge.name = "WalkThroughHedge";
                hedge.transform.SetParent(screen.transform);
                hedge.transform.position = position;
                hedge.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                hedge.transform.localScale = size;
                hedge.GetComponent<Renderer>().sharedMaterial = foliage;
                Object.DestroyImmediate(hedge.GetComponent<Collider>());
            }

            // Clean rectangular enclosure. The two front runs stop short of the
            // centre, leaving a 6m entrance exactly where the dirt road arrives.
            const float halfWidth = 11.2f;
            const float frontDepth = -7.6f;
            const float rearDepth = 14.5f;
            const float enclosureDepth = rearDepth - frontDepth;
            const float enclosureCentreDepth = (rearDepth + frontDepth) * 0.5f;
            const float hedgeHeight = 2.8f;
            const float hedgeDepth = 1.25f;
            const float entranceWidth = 6f;
            AddHedge(farmCenter + dir * rearDepth,
                new Vector3(halfWidth * 2f + hedgeDepth, hedgeHeight, hedgeDepth));
            AddHedge(farmCenter - right * halfWidth + dir * enclosureCentreDepth,
                new Vector3(hedgeDepth, hedgeHeight, enclosureDepth));
            AddHedge(farmCenter + right * halfWidth + dir * enclosureCentreDepth,
                new Vector3(hedgeDepth, hedgeHeight, enclosureDepth));
            float frontRun = halfWidth - entranceWidth * 0.5f;
            AddHedge(farmCenter + dir * frontDepth - right * (entranceWidth * 0.5f + frontRun * 0.5f),
                new Vector3(frontRun, hedgeHeight, hedgeDepth));
            AddHedge(farmCenter + dir * frontDepth + right * (entranceWidth * 0.5f + frontRun * 0.5f),
                new Vector3(frontRun, hedgeHeight, hedgeDepth));
        }

        /// <summary>
        /// Fenced-off expansion plots beside the main farm. They only
        /// become usable through two Highland land purchases, giving both
        /// survey-lot purchases a real effect (see LockedFarmPlot).
        /// </summary>
        private static void BuildExpansionPlots(
            Terrain terrain, Vector3 farmCenter, Vector3 right, Vector3 dir,
            Material soilMat, Transform parent)
        {
            Material fenceMat = GetOrCreateMaterial("LandFence", new Color(0.55f, 0.42f, 0.26f));

            for (int row = 0; row < 2; row++)
            for (int col = 0; col < 3; col++)
            {
                Vector3 pos = farmCenter
                              + right * ((col - 1) * 3.6f)
                              + dir * (8.6f + row * 3.9f);
                pos.y = SampleHeight(terrain, pos.x, pos.z) + 0.03f;

                if (col == 1) _expansionPlotPos = pos;

                var plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plot.name = row == 0 ? $"FarmPlot_X{col}" : $"FarmPlot_Y{col}";
                plot.transform.SetParent(parent);
                plot.transform.position = pos;
                plot.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                plot.transform.localScale = new Vector3(2.8f, 0.06f, 3.0f);
                var plotRenderer = plot.GetComponent<Renderer>();
                plotRenderer.sharedMaterial = soilMat;
                plot.GetComponent<Collider>().enabled = false;

                var tomatoVisual = BuildCropVisual(plot.transform, "TomatoVisual",
                    "Assets/UpIzUpMini/Art/CropMeshes/TomatoPlant_LOD.asset", 1.7f, fruitCount: 4);
                var weedVisual = BuildWeedCropVisual(plot.transform, "WeedVisual",
                    "Assets/UpIzUpMini/Art/CropMeshes/WeedPlant_LOD.asset", 1.9f);
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
                lso.FindProperty("requiredItemId").stringValue = row == 0 ? "land_montine" : "land_hillside";
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
        /// MINI-051: builds the Zeb (weed) crop visual with real bud/cola
        /// detail instead of the old flat "fruitCount: 0" (no bud
        /// representation at all - the plant's designated colour never
        /// showed, which was the actual bug). Foliage stays the plain
        /// green plant mesh (not recoloured); three cola clusters (one
        /// apical, two lower lateral) each get a tapering 3-sphere bud
        /// stack (dense/clustered rather than a single ball) plus two
        /// pistil-hair accents, and CropStageVisual applies the strain's
        /// primary/secondary ripe colour to the buds and a frost/trichome
        /// lighten+gloss pass at full ripeness.
        /// </summary>
        private static CropStageVisual BuildWeedCropVisual(
            Transform plot, string name, string meshPath, float plantHeightMetres)
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

                float normalize = plantHeightMetres / Mathf.Max(0.0001f, mesh.bounds.size.y);
                meshGo.transform.localScale = Vector3.one * normalize;
                meshGo.transform.localPosition = new Vector3(0f, -mesh.bounds.min.y * normalize, 0f);
            }
            else
            {
                Debug.LogWarning($"Mini011PhaseBSetup: crop mesh missing at {meshPath}");
            }

            var budMat = GetOrCreateMaterial("CropBud", new Color(0.3f, 0.55f, 0.25f));
            var pistilMat = GetOrCreateMaterial("CropPistil", new Color(0.90f, 0.85f, 0.72f));

            var fruits = new List<Renderer>();
            var pistils = new List<Renderer>();

            // One apical (top, biggest) cola and two smaller lower lateral
            // colas - real plants flower densest at the top and lighter
            // further down, not a uniform ring.
            (float heightFrac, float lateralFrac, float scale)[] colaSpecs =
            {
                (0.92f, 0.00f, 1.00f),
                (0.66f, 0.34f, 0.72f),
                (0.66f, -0.34f, 0.72f),
            };

            foreach (var cola in colaSpecs)
            {
                var colaRoot = new GameObject("Cola");
                colaRoot.transform.SetParent(plantRoot.transform, false);
                colaRoot.transform.localPosition = new Vector3(
                    cola.lateralFrac * plantHeightMetres * 0.22f,
                    plantHeightMetres * cola.heightFrac,
                    0f);

                float budBase = plantHeightMetres * 0.11f * cola.scale;
                for (int i = 0; i < 3; i++)
                {
                    float segT = i / 2f; // 0 at the base, 1 at the tip
                    var bud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bud.name = $"Bud_{i}";
                    bud.transform.SetParent(colaRoot.transform, false);
                    bud.transform.localPosition = new Vector3(0f, segT * budBase * 1.4f, 0f);
                    float budScale = budBase * (1f - segT * 0.4f);
                    bud.transform.localScale = Vector3.one * budScale;
                    Object.DestroyImmediate(bud.GetComponent<Collider>());
                    var br = bud.GetComponent<Renderer>();
                    br.sharedMaterial = budMat;
                    fruits.Add(br);
                }

                // Two pistil hairs per cola, angled outward from the top
                // bud - cream while flowering, rust-orange once mature
                // (CropStageVisual), independent of the strain's bud colour.
                for (int p = 0; p < 2; p++)
                {
                    float angle = p == 0 ? 35f : -145f;
                    var pistil = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    pistil.name = $"Pistil_{p}";
                    pistil.transform.SetParent(colaRoot.transform, false);
                    pistil.transform.localPosition = new Vector3(0f, budBase * 1.3f, 0f);
                    pistil.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                    pistil.transform.localScale = new Vector3(budBase * 0.12f, budBase * 0.55f, budBase * 0.12f);
                    Object.DestroyImmediate(pistil.GetComponent<Collider>());
                    var pr = pistil.GetComponent<Renderer>();
                    pr.sharedMaterial = pistilMat;
                    pistils.Add(pr);
                }
            }

            var visual = root.AddComponent<CropStageVisual>();
            var so = new SerializedObject(visual);
            so.FindProperty("plantRoot").objectReferenceValue = plantRoot.transform;
            so.FindProperty("plantRenderer").objectReferenceValue = plantRenderer;
            var fruitsProp = so.FindProperty("fruitRenderers");
            fruitsProp.arraySize = fruits.Count;
            for (int i = 0; i < fruits.Count; i++)
                fruitsProp.GetArrayElementAtIndex(i).objectReferenceValue = fruits[i];
            var pistilsProp = so.FindProperty("pistilRenderers");
            pistilsProp.arraySize = pistils.Count;
            for (int i = 0; i < pistils.Count; i++)
                pistilsProp.GetArrayElementAtIndex(i).objectReferenceValue = pistils[i];
            so.FindProperty("applyFrostEffect").boolValue = true;
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
            // MINI-108 visual correction: tree1 is the cleaner, lower-detail
            // variant. It keeps the crown at the top without the dense wall
            // of leaves that hid the fruit on tree2, and is cheaper on mobile.
            const string meshPath = "Assets/UpIzUpMini/Art/BananaImported/bananatree1.fbx";
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
            // MINI-108: a small hanging hand of individually curved bananas.
            // Each fruit uses three short capsules along an upward curve,
            // which still stays cheap but reads unmistakably as banana rather
            // than the old cluster of floating spheres.
            const int bananaCount = 5;
            const int segmentsPerBanana = 3;
            fruitsProp.arraySize = bananaCount * segmentsPerBanana;
            int fruitIndex = 0;
            for (int banana = 0; banana < bananaCount; banana++)
            {
                float angle = banana * Mathf.PI * 2f / bananaCount;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                for (int segment = 0; segment < segmentsPerBanana; segment++)
                {
                    var fruit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    fruit.name = $"Banana_{banana + 1}_Segment_{segment + 1}";
                    fruit.transform.SetParent(plantRoot.transform, false);
                    float along = segment * 0.16f;
                    fruit.transform.localPosition = new Vector3(0f, 1.82f - along, 0f)
                        + radial * (0.20f + segment * 0.08f);
                    fruit.transform.localRotation = Quaternion.LookRotation(radial, Vector3.up)
                        * Quaternion.Euler(18f + segment * 15f, 0f, 90f);
                    fruit.transform.localScale = new Vector3(0.095f, 0.16f, 0.095f);
                    Object.DestroyImmediate(fruit.GetComponent<Collider>());
                    Renderer renderer = fruit.GetComponent<Renderer>();
                    renderer.sharedMaterial = GetOrCreateMaterial("CropFruit", Color.green);
                    fruitsProp.GetArrayElementAtIndex(fruitIndex++).objectReferenceValue = renderer;
                }
            }

            // The brown rachis/stalk makes the hand visibly attach to the
            // plant instead of reading as floating yellow fruit.
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "Banana_BrownStem";
            stem.transform.SetParent(plantRoot.transform, false);
            stem.transform.localPosition = new Vector3(0f, 2.02f, 0f);
            stem.transform.localScale = new Vector3(0.11f, 0.16f, 0.11f);
            Object.DestroyImmediate(stem.GetComponent<Collider>());
            stem.GetComponent<Renderer>().sharedMaterial =
                GetOrCreateMaterial("BananaStemBrown", new Color(0.52f, 0.31f, 0.13f));

            // A deliberately small five-leaf crown replaces the overly dense
            // original canopy while keeping leaves above the fruit hand.
            Material leafMaterial = GetOrCreateMaterial("BananaLeafCrown", new Color(0.22f, 0.58f, 0.16f));
            for (int leafIndex = 0; leafIndex < 5; leafIndex++)
            {
                float angle = leafIndex * Mathf.PI * 2f / 5f;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leaf.name = $"Banana_TopLeaf_{leafIndex + 1}";
                leaf.transform.SetParent(plantRoot.transform, false);
                leaf.transform.localPosition = new Vector3(0f, 2.22f, 0f) + radial * 0.24f;
                leaf.transform.localRotation = Quaternion.FromToRotation(
                    Vector3.up, (radial * 0.88f + Vector3.up * 0.34f).normalized);
                leaf.transform.localScale = new Vector3(0.13f, 0.48f, 0.045f);
                Object.DestroyImmediate(leaf.GetComponent<Collider>());
                leaf.GetComponent<Renderer>().sharedMaterial = leafMaterial;
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
            // Keep the house.s own position AND rotation - the bike needs to be
            // parked relative to which way the shelter actually faces. Offsetting
            // in world space from the bed spawn (the first attempt) put it
            // through a wall and inside the house.
            _safehousePos = pos;
            _safehouseRot = rot;

            BuildOpenSafehouse(parent.transform, pos, rot);

            // Rest point sits on the bed itself.
            var restGo = new GameObject("FarmSafehouse_Rest");
            restGo.transform.SetParent(parent.transform);
            restGo.transform.position = pos + rot * new Vector3(0f, 0.6f, 0f);
            var farmSafehouse = restGo.AddComponent<SafehouseInteractable>();
            // MINI-062: matches this safehouse's own spawnPoint field to
            // the same offset already used as CharacterSwitchManager's
            // default respawn (_safehouseSpawn below) - so choosing
            // "[4] Set Respawn" here is a genuine no-op the first time,
            // and a real choice again once the player has picked another
            // house instead.
            var fsSo = new SerializedObject(farmSafehouse);
            fsSo.FindProperty("safehouseName").stringValue = "Highland Safehouse";
            fsSo.FindProperty("spawnPoint").vector3Value = _safehouseSpawn;
            fsSo.ApplyModifiedPropertiesWithoutUndo();
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

        /// <summary>MINI-065: wires the one-time TMAX purchase/spawn hook
        /// (VehicleSpawnController) to the real TMAX_560 prefab. A no-op
        /// (logs a warning, doesn't fail the whole scene build) if the
        /// prefab hasn't been built yet via MINI-064/065's own menu
        /// items, so this never blocks a scene rebuild on the vehicle
        /// work being present.</summary>
        private static void BuildVehicleSpawner()
        {
            var tmaxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Vehicles/TMAX_560.prefab");
            if (tmaxPrefab == null)
            {
                Debug.LogWarning("BuildVehicleSpawner: TMAX_560.prefab not found - buying the TMAX from the Car Dealer won't spawn anything until MINI-064/065's prefab exists.");
                return;
            }

            var go = new GameObject("VehicleSpawner");
            var spawner = go.AddComponent<VehicleSpawnController>();

            // MINI-080: "remove the test bike, test range and take out the
            // test screens until we are testing again." The visible parked
            // TEST instances are now gated behind IncludeParkedTestVehicles
            // (off) - buying either vehicle from the Car Dealer still works
            // exactly as before via VehicleSpawnController, this only removes
            // the free-to-reach ones sitting by the safehouse. The
            // BikeHomePoint marker itself is built UNCONDITIONALLY below,
            // regardless of the toggle - MINI-068's save/respawn system
            // (ReturnBikeHome) needs a real home pose to exist even when no
            // test bike is parked there, or a legitimately purchased bike
            // would have nowhere to return to on load.
            const bool IncludeParkedTestVehicles = false;

            // Out the FRONT of the shelter and off to one side, in the house's
            // own frame - 7m forward clears the open front and the bed area
            // entirely. The bed spawn is 3.4m forward, so anything less than
            // that is still under the roof.
            Vector3 parkPos = _safehousePos + _safehouseRot * new Vector3(2.2f, 0f, 7.0f);
            if (Physics.Raycast(parkPos + Vector3.up * 40f, Vector3.down, out RaycastHit parkHit, 90f))
                parkPos = parkHit.point;
            Vector3 bikeHomePos = parkPos + Vector3.up * 0.04f;
            Quaternion bikeHomeRot = _safehouseRot * Quaternion.Euler(0f, 90f, 0f);

            // MINI-068: this parking spot is the bike's HOME regardless of
            // whether a test bike is actually sitting there right now - the
            // user's "once i have bought the bike it should be in my safe
            // house exact where you have it even every respawn."
            var home = new GameObject("BikeHomePoint");
            home.transform.SetPositionAndRotation(bikeHomePos, bikeHomeRot);

            var roverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UpIzUpMini/Art/Vehicles/RangeRover_Vehicle.prefab");
            if (roverPrefab == null)
            {
                Debug.LogWarning("MINI-071: RangeRover_Vehicle.prefab missing - buying the Rover from the Car Dealer won't spawn anything until MINI-071's prefab exists.");
            }

            if (IncludeParkedTestVehicles)
            {
                var placed = (GameObject)PrefabUtility.InstantiatePrefab(tmaxPrefab);
                placed.name = "TMAX_560_Parked";
                placed.transform.SetPositionAndRotation(bikeHomePos, bikeHomeRot);

                if (roverPrefab != null)
                {
                    var parkedRover = (GameObject)PrefabUtility.InstantiatePrefab(roverPrefab);
                    parkedRover.name = "RangeRover_Parked";
                    // 9m out from the bike, not 3.4 - see MINI-076: closer
                    // than 5.8m (mountRange 3.2 + doorRange 2.6) let a single F
                    // press mount both vehicles on the same character at once.
                    Vector3 roverPos = _safehousePos + _safehouseRot * new Vector3(-9.0f, 0f, 7.0f);
                    if (Physics.Raycast(roverPos + Vector3.up * 40f, Vector3.down, out RaycastHit roverHit, 90f))
                        roverPos = roverHit.point;
                    parkedRover.transform.position = roverPos + Vector3.up * 0.15f;
                    parkedRover.transform.rotation = _safehouseRot * Quaternion.Euler(0f, 90f, 0f);
                }
            }

            // MINI-119, user: "i want to test this in my actual game scene
            // to get the full gist." Purely a dev test-spawn reference -
            // never touches tmaxPrefab/roverPrefab above, and the Range
            // Rover pipeline is completely unaffected, per the user's own
            // "the range rova system should be separate as its a car."
            var superMotoTestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UpIzUpMini/Vehicles/TMAX_560_SuperMoto.prefab");
            if (superMotoTestPrefab == null)
                Debug.LogWarning("MINI-119: TMAX_560_SuperMoto.prefab not found - run 'Up Iz Up Mini/MINI-119/Build SuperMoto Bike Prefab' first if you want it to test-spawn.");

            var so = new SerializedObject(spawner);
            so.FindProperty("tmaxPrefab").objectReferenceValue = tmaxPrefab;
            so.FindProperty("roverPrefab").objectReferenceValue = roverPrefab;
            so.FindProperty("superMotoTestPrefab").objectReferenceValue = superMotoTestPrefab;
            so.FindProperty("bikeHome").objectReferenceValue = home.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// MINI-042. A second, purchasable safehouse in town - reuses the
        /// a detailed two-storey Lalay home, gated on owning "prop_safehouse" (already
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

            BuildProceduralHouse(parent.transform, pos, rot, storeys: 2, name: "LalaySafehouse_TwoStorey");

            var restGo = new GameObject("LalayHouse_Rest");
            restGo.transform.SetParent(parent.transform);
            // The interaction point is at the front door; the locked property
            // no longer exposes a bed/open shelter directly onto Lalay road.
            restGo.transform.position = pos + rot * new Vector3(0f, 0.6f, 3.25f);
            var safehouse = restGo.AddComponent<SafehouseInteractable>();
            var so = new SerializedObject(safehouse);
            so.FindProperty("safehouseName").stringValue = "Lalay House";
            so.FindProperty("requiredItemId").stringValue = "prop_safehouse";
            // MINI-062: this house's own respawn point (same bed-offset
            // math the farm safehouse's _safehouseSpawn uses) - lets the
            // player pick it via "[4] Set Respawn" once they own it,
            // instead of the game only ever knowing the farm's spawn.
            so.FindProperty("spawnPoint").vector3Value = pos + rot * new Vector3(0f, 0.2f, 4.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// MINI-073. "put a two story house with a garage for a vehicle on
        /// the lalay, you can make space because you dont want it ito clash
        /// with other buildings for two story sale and then save rest
        /// features that the montine farm house has."
        ///
        /// Placed well clear of the dense ambient house row (BuildHouses caps
        /// its lateral offset at ~9.5m either side of the road) and every
        /// other special building on this stretch - LalayHouse at index 4,
        /// the market at index 6, the Montine turnoff at the midpoint, and
        /// Boss C/the recruiter/Chevy/the Range Rover all clustered at index
        /// 13. Index 9, opposite side from Boss C's cluster, at a generous
        /// 20m lateral offset, was empty ground in every one of those.
        ///
        /// "save rest features that the montine farm house has" - the same
        /// SafehouseInteractable component the farm safehouse and the
        /// existing LalayHouse both use, so resting here works identically
        /// (heal, heat removal, save, set-respawn). Reuses
        /// BuildProceduralHouse(storeys: 2) rather than a bespoke building -
        /// that function already exists and is exactly a two-storey house.
        ///
        /// The GARAGE is a real open bay sized to fit a parked car and a
        /// simple gable roof over it, built next to the house - but it is
        /// VISUAL ONLY. It does not become a save/respawn home point for a
        /// purchased vehicle; VehicleSpawnController's bikeHome (MINI-068) is
        /// still the only wired home point. Hooking a vehicle to park here
        /// specifically is real follow-up work, not attempted in this pass -
        /// see the MINI-073 PROJECT-HANDOFF.md entry.
        /// </summary>
        private static void BuildLalayEstate(Terrain terrain, List<Vector3> roadPoints)
        {
            int index = Mathf.Clamp(9, 1, roadPoints.Count - 2);
            Vector3 dir = (roadPoints[index + 1] - roadPoints[index - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            // Opposite side from Boss C's cluster (side +1 there), well
            // outside the ambient row's ~9.5m lateral cap.
            Vector3 pos = roadPoints[index] + right * -20f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);
            Quaternion rot = Quaternion.LookRotation(right, Vector3.up);

            var parent = new GameObject("LalayEstate");
            parent.transform.position = pos;
            parent.transform.rotation = rot;

            BuildProceduralHouse(parent.transform, pos, rot, storeys: 2, name: "EstateHouse");

            // Garage: an open-fronted bay beside the house, roughly a car's
            // footprint (the Range Rover measures 4.95 x 2.10 x 1.79m, see
            // Mini070RangeRoverPrep) plus clearance.
            Vector3 garagePos = pos + rot * new Vector3(5.4f, 0f, -1.5f);
            garagePos.y = SampleHeight(terrain, garagePos.x, garagePos.z);
            BuildGarage(parent.transform, garagePos, rot);

            var restGo = new GameObject("LalayEstate_Rest");
            restGo.transform.SetParent(parent.transform);
            restGo.transform.position = pos + rot * new Vector3(0f, 0.6f, 0f);
            var safehouse = restGo.AddComponent<SafehouseInteractable>();
            var so = new SerializedObject(safehouse);
            so.FindProperty("safehouseName").stringValue = "Lalay Estate";
            so.FindProperty("requiredItemId").stringValue = "prop_lalay_estate";
            so.FindProperty("spawnPoint").vector3Value = pos + rot * new Vector3(0f, 0.2f, 3.6f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// A simple open-fronted garage bay: three walls, a flat roof, no
        /// front wall/door - a vehicle just parks in under it. Visual only
        /// (see BuildLalayEstate's own remarks on why it is not yet a real
        /// parking home point).
        /// </summary>
        private static void BuildGarage(Transform parent, Vector3 pos, Quaternion rot)
        {
            const float width = 3.2f;
            const float depth = 5.6f;
            const float height = 2.6f;

            var garage = new GameObject("Garage");
            garage.transform.SetParent(parent, false);
            garage.transform.position = pos;
            garage.transform.rotation = rot;

            Material wallMat = GetOrCreateMaterial("GarageWall", new Color(0.62f, 0.60f, 0.56f));
            Material roofMat = GetOrCreateMaterial("GarageRoof", new Color(0.30f, 0.30f, 0.32f));

            void Wall(string name, Vector3 localPos, Vector3 scale)
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.name = name;
                w.transform.SetParent(garage.transform, false);
                w.transform.localPosition = localPos;
                w.transform.localScale = scale;
                w.GetComponent<Renderer>().sharedMaterial = wallMat;
            }

            // Back and two side walls; the front (facing -Z, toward the
            // house's own forward) is left open for a vehicle to drive in.
            Wall("Wall_Back", new Vector3(0f, height / 2f, depth / 2f), new Vector3(width, height, 0.25f));
            Wall("Wall_Left", new Vector3(-width / 2f, height / 2f, 0f), new Vector3(0.25f, height, depth));
            Wall("Wall_Right", new Vector3(width / 2f, height / 2f, 0f), new Vector3(0.25f, height, depth));

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(garage.transform, false);
            roof.transform.localPosition = new Vector3(0f, height + 0.1f, 0f);
            roof.transform.localScale = new Vector3(width + 0.5f, 0.2f, depth + 0.5f);
            roof.GetComponent<Renderer>().sharedMaterial = roofMat;
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
            Vector3 position, RuntimeAnimatorController animController, bool startActive,
            float visualScale = 1f)
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
                hideFacialHair: skinTint.HasValue, uniformScale: visualScale);
            var animator = visual.GetComponentInChildren<Animator>();

            var vitals = go.AddComponent<CharacterVitals>();
            // Swimming removed - the sea is now walled off at the shoreline
            // and reachable only along the jetty (user direction).

            // MINI-069: both main characters can brawl with Dog Life. The one
            // the player is actually driving is skipped at runtime (he punches
            // with F himself), so in practice this is "my other main character
            // fights when he sees any dog life gang member".
            var brawler = go.AddComponent<Combat.FactionBrawler>();
            brawler.Allegiance = Combat.FactionBrawler.Side.NotAhWord;

            // Shows purchased apparel on the character.
            var equipment = go.AddComponent<CharacterEquipment>();
            var eqSo = new SerializedObject(equipment);
            eqSo.FindProperty("animator").objectReferenceValue = animator;
            eqSo.FindProperty("characterIndex").intValue = string.Equals(displayName, "Franki", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            // MINI-067: the real 18k model replaces the generated ring of
            // spheres. Left null-tolerant on the runtime side, so a character
            // built before this still wears the placeholder rather than nothing.
            eqSo.FindProperty("chainPrefab").objectReferenceValue = LoadGoldChainPrefab();
            eqSo.FindProperty("chainPlacement").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(
                    $"Assets/UpIzUpMini/Data/Equipment/{displayName}ChainPlacement.asset");
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

            // MINI-053: demonstrates the new data-driven dialogue
            // foundation on one real NPC without touching how any other
            // NPC's lines work. Purely additive - if this ever fails to
            // find the villager or the asset can't be built, the villager
            // just keeps using its plain villagerLines array as before.
            WireVillagerDialogueSet();

            // Two officers so there is always one visible: one patrolling
            // the lower road, one posted by the shops.
            BuildNpc(terrain, roadPoints, index: 7, sideMul: -1f, goName: "NPC_PoliceShops",
                modelPath: "Assets/Floreswa/Models/male01_1.fbx", role: NpcRole.Police,
                cropsForBuyer: null, animController: animController, patrols: true, reactsToHeat: true);

            // Police patrols the Lalay road and speeds up when heat is high.
            BuildNpc(terrain, roadPoints, index: 3, sideMul: -1f, goName: "NPC_Police",
                modelPath: "Assets/Floreswa/Models/male01_2.fbx", role: NpcRole.Police,
                cropsForBuyer: null, animController: animController, patrols: true, reactsToHeat: true);

            BuildNpc(terrain, roadPoints, index: 10, sideMul: 1f, goName: "NPC_PoliceEast",
                modelPath: "Assets/Floreswa/Models/male01_1.fbx", role: NpcRole.Police,
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
            StyleParo(GameObject.Find("NPC_Vagrant"));

            BuildNpc(terrain, roadPoints, index: 13, sideMul: -1f, goName: "NPC_BlackMarket",
                modelPath: "Assets/Floreswa/Models/male02_3.fbx", role: NpcRole.BlackMarket,
                cropsForBuyer: null, animController: animController, patrols: false, reactsToHeat: false);

            // MINI-057: Normy - stationary, not part of PoliceOfficer's
            // patrol/chase system (see NpcRole.Normy's own comment).
            BuildNpc(terrain, roadPoints, index: 4, sideMul: 1f, goName: "NPC_Normy",
                modelPath: "Assets/Floreswa/Models/male01_1.fbx", role: NpcRole.Normy,
                cropsForBuyer: null, animController: animController, patrols: true, reactsToHeat: false);
            WireNormy();

            BuildMarketArea(terrain, roadPoints, index: 5, title: "FOOD", secondTitle: null);
            BuildMarketArea(terrain, roadPoints, index: 11, title: "PHARMACY", secondTitle: null);
            BuildMarketArea(terrain, roadPoints, index: 14, title: "LAND AND SURVEYS", secondTitle: null);
            BuildMarketArea(terrain, roadPoints, index: 15, title: "CAR DEALER", secondTitle: null);

            BuildBossNpc(terrain, roadPoints, allCrops, animController);
            // MINI-039: seed prices scale with the strain's own sellPrice
            // rarity ordering (bushers 22 < black_sugar 38 < purple 55) -
            // per the user's "make them expensive" ask, each tier costs
            // roughly 7x its own sell price rather than being free.
            // MINI-055: Boss consolidation. Previously three separate NPCs
            // (BossM/BossP/BossQ), one per strain - now one "Boss C" NPC
            // offering all three as they unlock, matching the brief's
            // "move toward two major bosses" (Boss J for Bushers, Boss C
            // for everything above it). Prices/crop pairings unchanged
            // from the original three calls, just consolidated onto one
            // physical NPC at Boss P's old road position (13).
            BuildBossC(terrain, roadPoints, allCrops, animController, index: 13, side: 1f,
                cropIds: new[] { "black_sugar", "purple", "blue_cheese" },
                seedPrices: new[] { 320, 500, 650 });
            // MINI-060 follow-up: Gardey Zafeh's reveal/readings are now
            // handled by the boat man himself (TownNPCInteractable.
            // HandleBoatMan) - no separate NPC_GardeyZafeh built anymore.
            BuildBoatMan(terrain, allCrops, animController);
            BuildBrakesPriest(terrain, roadPoints, animController);
            // MINI-056: the Rasta mentor - a distinct Jamaican-Patois voice
            // (per Docs/DIALECT-LEXICON.md's own note that this was
            // reserved until a real character existed to check it
            // against) watching over the plantation, narratively "teaching
            // advanced strain work" by congratulating the player on
            // whichever tier they've actually reached.
            BuildRastaMentor(terrain, roadPoints, animController, allCrops);

            // MINI-058: factions. Not Ah Word (player gang, recruitable up
            // to the roster's own size) and Dog Life (rival gang,
            // pooled/distance-activated near the Lalay block they control).
            //
            // MINI-060 follow-up-2: the recruiter (and the block he's tied
            // to) used to sit at roadPoints[0], which overlapped Dog Life's
            // own territory - per the user, "the gang recruiter... is on
            // the rival side bring him to the boss C by chevy area and
            // make that my block as well." Both now cluster around Boss
            // C's own road position (13), same side as Chevy so it reads
            // as one gang's block rather than two unrelated spots.
            int bossCIndex = Mathf.Clamp(13, 1, roadPoints.Count - 2);
            Vector3 bossCDir = (roadPoints[bossCIndex + 1] - roadPoints[bossCIndex - 1]).normalized;
            Vector3 bossCRight = Vector3.Cross(Vector3.up, bossCDir).normalized;
            Vector3 blockHome = roadPoints[bossCIndex] + bossCRight * 1f * 6f;
            blockHome.y = SampleHeight(terrain, blockHome.x, blockHome.z);
            var roster = BuildNotAhWordRoster(animController, _farmCenter, blockHome, terrain, roadPoints);
            BuildGangRecruiter(roster, animController, terrain, roadPoints[bossCIndex], bossCRight);
            BuildDogLifeGang(terrain, roadPoints, animController);
        }

        /// <summary>
        /// MINI-058: builds the Not Ah Word roster. Follow-up (per the
        /// user): Chevy is no longer part of the paid recruiter's hidden
        /// pool - he's visible in the world near Boss C from the start,
        /// and only GrandBayGangs reputation ("respect"), not money,
        /// recruits him (GangMemberInteractable.recruitViaReputation).
        /// The other three (Reds/Ju/Skeng) keep the original pooled/
        /// inactive-until-paid-for path from TownNPCInteractable.
        /// HandleRecruit. Every member carries real combat
        /// (NpcCombatHealth + HumanoidAnimationManager, MINI-038) so
        /// "Unavailable/injured" is something that can actually happen in
        /// a fight, not a manually-picked state pretending to be one.
        /// </summary>
        private static GangMemberController[] BuildNotAhWordRoster(
            RuntimeAnimatorController animController, Vector3 guardPos, Vector3 homePos,
            Terrain terrain, List<Vector3> roadPoints)
        {
            // MINI-073: renamed at the user's request - Chevy -> Zoomy,
            // Reds -> Deluxe, Ju -> Draco, and (confirmed in a follow-up)
            // Skeng -> Rio.
            string[] names = { "Zoomy", "Deluxe", "Draco", "Rio" };
            string[] models =
            {
                "Assets/Floreswa/Models/male01_2.fbx",
                "Assets/Floreswa/Models/male03_3.fbx",
                "Assets/Floreswa/Models/male02_2.fbx",
                "Assets/Floreswa/Models/male01_3.fbx",
            };

            var rosterParent = new GameObject("NotAhWordRoster");
            var paidMembers = new List<GangMemberController>();

            for (int i = 0; i < names.Length; i++)
            {
                var go = new GameObject($"NotAhWord_{names[i]}");
                go.transform.SetParent(rosterParent.transform);

                var cc = go.AddComponent<CharacterController>();
                cc.center = new Vector3(0f, 0.95f, 0f);
                cc.height = 1.85f;
                cc.radius = 0.32f;

                var visual = InstantiateCharacter(models[i], go.transform, animController, null);

                var animationManager = AddHumanoidAnimationManager(go, visual.GetComponentInChildren<Animator>());
                var combatHealth = go.AddComponent<NpcCombatHealth>();
                // MINI-119 follow-up, user: "if i hit the npc they will
                // behave like ragdoll... you can do it will the police,
                // villagers, gang members."
                var ragdoll = go.AddComponent<NpcRagdoll>();
                var chSo = new SerializedObject(combatHealth);
                chSo.FindProperty("animationManager").objectReferenceValue = animationManager;
                chSo.FindProperty("ragdoll").objectReferenceValue = ragdoll;
                chSo.ApplyModifiedPropertiesWithoutUndo();

                var member = go.AddComponent<GangMemberController>();
                member.MemberName = names[i];
                member.SetGuardPosition(guardPos);
                member.SetHomePosition(homePos);

                // MINI-069: recruits fight Dog Life too, per "even those i
                // recruit and they are following me and they are around".
                var recruitBrawler = go.AddComponent<Combat.FactionBrawler>();
                recruitBrawler.Allegiance = Combat.FactionBrawler.Side.NotAhWord;

                var interactable = go.AddComponent<GangMemberInteractable>();
                var giSo = new SerializedObject(interactable);
                giSo.FindProperty("member").objectReferenceValue = member;

                if (i == 0)
                {
                    // Chevy: positioned by Boss C (further out on the same
                    // side than Boss C himself, so they don't overlap),
                    // visible immediately, respect-gated rather than
                    // pooled/paid.
                    int bossCIndex = Mathf.Clamp(13, 1, roadPoints.Count - 2);
                    Vector3 dir = (roadPoints[bossCIndex + 1] - roadPoints[bossCIndex - 1]).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
                    // MINI-069, user: "Chevy should be placed next to the
                    // recruiter." The recruiter stands at right * 15 (see
                    // BuildGangRecruiter), so Chevy goes 2m past him - close
                    // enough to read as a pair, far enough not to overlap.
                    Vector3 pos = roadPoints[bossCIndex] + right * 1f * 17f;
                    pos.y = SampleHeight(terrain, pos.x, pos.z);
                    go.transform.position = pos;
                    go.transform.rotation = Quaternion.LookRotation(-right, Vector3.up);

                    giSo.FindProperty("recruitViaReputation").boolValue = true;
                    giSo.FindProperty("recruitReputationThreshold").intValue = 20;
                    giSo.ApplyModifiedPropertiesWithoutUndo();

                    go.SetActive(true);
                }
                else
                {
                    giSo.ApplyModifiedPropertiesWithoutUndo();
                    go.SetActive(false); // unrecruited until the player pays for them
                    paidMembers.Add(member);
                }
            }

            return paidMembers.ToArray();
        }

        /// <summary>MINI-058: the recruiter NPC, wired to the pooled roster.
        /// MINI-060 follow-up-2: repositioned to Boss C/Chevy's cluster
        /// (previously a fixed position overlapping Dog Life's block, see
        /// the call site's comment) and priced at $2000/member, per the
        /// user's explicit ask.</summary>
        private static void BuildGangRecruiter(
            GangMemberController[] roster, RuntimeAnimatorController animController,
            Terrain terrain, Vector3 bossCPoint, Vector3 bossCRight)
        {
            Vector3 pos = bossCPoint + bossCRight * 1f * 15f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);
            Quaternion rot = Quaternion.LookRotation(-bossCRight, Vector3.up);

            var go = new GameObject("NPC_GangRecruiter");
            go.transform.position = pos;
            go.transform.rotation = rot;

            InstantiateCharacter("Assets/Floreswa/Models/male02_3.fbx", go.transform, animController, null);

            var npc = go.AddComponent<TownNPCInteractable>();
            var so = new SerializedObject(npc);
            so.FindProperty("role").enumValueIndex = (int)NpcRole.GangRecruiter;
            so.FindProperty("npcName").stringValue = "GangRecruiter";
            var poolProp = so.FindProperty("recruitPool");
            poolProp.arraySize = roster.Length;
            for (int i = 0; i < roster.Length; i++)
                poolProp.GetArrayElementAtIndex(i).objectReferenceValue = roster[i];
            // MINI-060 follow-up-2: "change his price to 2000 per member."
            so.FindProperty("recruitCost").intValue = 2000;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// MINI-058: Dog Life - the rival gang controlling Lalay initially.
        /// A pool of ambient, territorial NPCs near the road's start,
        /// activated/deactivated by player distance via RivalGangSpawner
        /// rather than left active across the whole map. Dialogue is
        /// deliberately vague/territorial only - no theft-plot reveal here,
        /// that is MINI-059/060 scope per the roadmap's own "do not reveal
        /// Dog Life immediately" instruction.
        /// </summary>
        private static void BuildDogLifeGang(
            Terrain terrain, List<Vector3> roadPoints, RuntimeAnimatorController animController)
        {
            int blockIndex = Mathf.Clamp(1, 1, roadPoints.Count - 2);
            Vector3 blockCentre = roadPoints[blockIndex];
            blockCentre.y = SampleHeight(terrain, blockCentre.x, blockCentre.z);

            var spawnerGo = new GameObject("DogLifeSpawner");
            var spawner = spawnerGo.AddComponent<RivalGangSpawner>();
            spawner.SetBlockCentre(blockCentre);
            // MINI-112: "a small group may occasionally leave the block
            // and walk down Lalay together" - the same road-forward
            // direction the rest of Lalay's roadside placement already uses.
            spawner.SetRoadDirection(roadPoints[Mathf.Min(blockIndex + 1, roadPoints.Count - 1)] - roadPoints[Mathf.Max(blockIndex - 1, 0)]);

            EnsureFolder("Assets/UpIzUpMini/Data/Dialogue");
            const string path = "Assets/UpIzUpMini/Data/Dialogue/DogLifeLines.asset";
            var set = AssetDatabase.LoadAssetAtPath<DialogueSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<DialogueSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            set.lines = new List<DialogueLine>
            {
                // MINI-060: once Gardey Zafeh reveals them as the real
                // source of the plantation theft, Dog Life drops the vague
                // territorial act - "openly active" per the brief, shown
                // here as tone, since no attack AI was built for them.
                new DialogueLine
                {
                    category = DialogueCategory.Faction,
                    speaker = "Dog Life",
                    text = "Yea, is we been taking allu Zeb. Nutting allu can do bout it, nuh. Dis block still belong to us.",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition { type = DialogueConditionType.DogLifeRevealed }
                    }
                },
                new DialogueLine { category = DialogueCategory.Faction, speaker = "Dog Life", text = "Dis block belong to us, nuh. Allu just passing through - keep it dat way." },
                new DialogueLine { category = DialogueCategory.Normal, speaker = "Dog Life", text = "Watch yuhself round here, mn." },
                // MINI-112: "Dog Life becomes jealous as the boys gain
                // strains, stock and Grand Bay market share." Reuses the
                // existing CropUnlocked condition (already true via either
                // the Boss-exploitation path or Rasta's ladder, MINI-111)
                // rather than a new condition type - eligible only once
                // revealed, so it reads as escalation, not a random line
                // firing before the plot justifies it.
                new DialogueLine
                {
                    category = DialogueCategory.Faction,
                    speaker = "Dog Life",
                    text = "Allu doing good for yuhself lately, eh? Black Sugar, Purple... Grand Bay getting small for two gangs.",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition { type = DialogueConditionType.DogLifeRevealed },
                        new DialogueCondition { type = DialogueConditionType.CropUnlocked, cropId = "black_sugar" }
                    }
                },
            };
            EditorUtility.SetDirty(set);

            string[] models =
            {
                "Assets/Floreswa/Models/male03_2.fbx",
                "Assets/Floreswa/Models/male02_2.fbx",
                "Assets/Floreswa/Models/male01_1.fbx",
                "Assets/Floreswa/Models/male03_1.fbx",
            };

            for (int i = 0; i < models.Length; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 pos = blockCentre + new Vector3(Mathf.Cos(angle) * 3.5f, 0f, Mathf.Sin(angle) * 3.5f);
                pos.y = SampleHeight(terrain, pos.x, pos.z);

                var go = new GameObject($"NPC_DogLife_{i}");
                go.transform.position = pos;

                var cc = go.AddComponent<CharacterController>();
                cc.center = new Vector3(0f, 0.95f, 0f);
                cc.height = 1.85f;
                cc.radius = 0.32f;

                var visual = InstantiateCharacter(models[i], go.transform, animController, null);
                ApplyGangTint(visual);

                var animationManager = AddHumanoidAnimationManager(go, visual.GetComponentInChildren<Animator>());
                var combatHealth = go.AddComponent<NpcCombatHealth>();
                var ragdoll = go.AddComponent<NpcRagdoll>();
                var chSo = new SerializedObject(combatHealth);
                chSo.FindProperty("animationManager").objectReferenceValue = animationManager;
                chSo.FindProperty("ragdoll").objectReferenceValue = ragdoll;
                chSo.FindProperty("despawnOnDefeat").boolValue = true;
                chSo.ApplyModifiedPropertiesWithoutUndo();

                // MINI-069: "dont forget doglife should fight back as well".
                // Same component as the player's crew, opposite side - so both
                // gangs use the identical see/close/punch behaviour rather than
                // one being an attacker and the other a passive victim.
                var dogBrawler = go.AddComponent<Combat.FactionBrawler>();
                dogBrawler.Allegiance = Combat.FactionBrawler.Side.DogLife;

                var npc = go.AddComponent<TownNPCInteractable>();
                var so = new SerializedObject(npc);
                so.FindProperty("role").enumValueIndex = (int)NpcRole.Villager;
                so.FindProperty("npcName").stringValue = $"DogLife{i}";
                so.FindProperty("dialogueSet").objectReferenceValue = set;
                so.ApplyModifiedPropertiesWithoutUndo();

                // MINI-058 follow-up: the user caught all four members
                // walking left together, stopping together, walking right
                // together - every one of them shared the exact same
                // world-space waypoint offset (2f, 0f, 0f) and the same
                // default pause/speed, so they were in lockstep by
                // construction. Each member now gets its own randomised
                // wander angle/radius, walk speed, and pause duration -
                // different directions and different timing means they
                // can no longer stay in sync.
                var patrol = go.AddComponent<PatrolNPC>();
                float wanderAngle = Random(i * 97 + 11, 0f, 360f) * Mathf.Deg2Rad;
                float wanderRadius = Random(i * 131 + 23, 1.6f, 3.2f);
                Vector3 loopPoint = pos + new Vector3(Mathf.Cos(wanderAngle), 0f, Mathf.Sin(wanderAngle)) * wanderRadius;
                loopPoint.y = SampleHeight(terrain, loopPoint.x, loopPoint.z);
                patrol.SetWaypoints(new[] { pos, loopPoint });

                var patrolSo = new SerializedObject(patrol);
                patrolSo.FindProperty("walkSpeed").floatValue = Random(i * 59 + 5, 1.5f, 2.3f);
                patrolSo.FindProperty("pauseAtWaypointSeconds").floatValue = Random(i * 173 + 41, 1.0f, 3.2f);
                patrolSo.ApplyModifiedPropertiesWithoutUndo();

                spawner.AddMember(go);
            }
        }

        /// <summary>
        /// MINI-058: same shirt/pants/shoes recolour-by-material-name
        /// technique as ApplyPoliceUniform (proven working), a deep red
        /// rather than police blue, and deliberately WITHOUT that
        /// function's head-bone cap logic - MINI-057 already found that
        /// piece buggy on this rig once, no need to risk it again for a
        /// prop Dog Life doesn't need.
        /// </summary>
        /// <summary>
        /// MINI-073: recolours the shirt slot only, green - the colour of the
        /// liberation flag - leaving trousers/shoes untouched. Same
        /// keyword-match-and-clone technique as ApplyGangTint/ApplyPoliceUniform.
        /// </summary>
        private static void ApplyLiberationShirt(GameObject instance)
        {
            var shirt = new Color(0.06f, 0.42f, 0.14f);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name.ToLowerInvariant();
                    if (!n.Contains("tshirt") && !n.Contains("shirt")) continue;

                    mats[i] = new Material(mats[i]) { color = shirt };
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = mats;
            }
        }

        private static void ApplyGangTint(GameObject instance)
        {
            var shirt = new Color(0.55f, 0.08f, 0.08f);
            var trousers = new Color(0.08f, 0.08f, 0.08f);

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

                    mats[i] = new Material(mats[i]) { color = tint.Value };
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = mats;
            }
        }

        /// <summary>
        /// MINI-056: an older Rasta mentor near the plantation. Reuses the
        /// Villager role's existing dialogueSet path from MINI-053 rather
        /// than adding a new NpcRole or mechanic - "teaches advanced strain
        /// work after missions" is delivered as a DialogueSet whose lines
        /// are gated on the SAME CropUnlocked progression thresholds that
        /// already gate Boss C's seed offers (Docs/STORY.md's existing
        /// unlock model), so the mentor's dialogue always reflects the
        /// deepest strain the player has actually earned - stacking each
        /// tier's conditions (rather than one condition each) is what
        /// makes DialogueSet's specificity tie-break always prefer the
        /// deepest-earned line over a shallower one that's also still true.
        /// Does not grant seeds or money - purely narrative "teaching," so
        /// it can't undercut Boss C's paid economy.
        /// </summary>
        private static void BuildRastaMentor(Terrain terrain, List<Vector3> roadPoints, RuntimeAnimatorController animController, CropDefinition[] allCrops)
        {
            if (_farmCenter == Vector3.zero)
            {
                Debug.LogWarning("Mini011PhaseBSetup: farm centre not set; skipping MINI-056 Rasta mentor.");
                return;
            }

            // MINI-056 follow-up: the user found him standing on the farm
            // itself, mixed in among the plots. Recomputes the same
            // turnoff/dir the farm access track uses (BuildFarmPathAndClearing)
            // and places him partway down that road instead - lower down,
            // watching the plantation from a distance rather than standing
            // in it.
            Vector3 turnoff = roadPoints[roadPoints.Count / 2];
            Vector3 farmDir = (roadPoints[roadPoints.Count / 2 + 1] - roadPoints[roadPoints.Count / 2 - 1]).normalized;
            Vector3 farmRight = Vector3.Cross(Vector3.up, farmDir).normalized;
            Vector3 pos = Vector3.Lerp(turnoff, _farmCenter, 0.4f) + farmRight * 3f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);
            _rastaPos = pos;

            var go = new GameObject("NPC_RastaMentor");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation((_farmCenter - pos).normalized, Vector3.up);

            // Facial hair kept (not hidden, unlike Sacat/Franki) - a simple,
            // no-new-geometry way to read as "older" on the same character
            // pack used for every other ambient NPC.
            var rastaVisual = InstantiateCharacter("Assets/Floreswa/Models/male01_2.fbx", go.transform, animController, null);
            // MINI-073: "give him ... a liberation shirt instead" - green,
            // the colour associated with the liberation flag, replacing
            // whatever the base character pack's shirt material was.
            ApplyLiberationShirt(rastaVisual);

            var npc = go.AddComponent<TownNPCInteractable>();
            var so = new SerializedObject(npc);
            so.FindProperty("role").enumValueIndex = (int)NpcRole.Villager;
            // MINI-073: nicknamed "Rasta" per the user.
            so.FindProperty("npcName").stringValue = "Rasta";
            // MINI-119, user: "rasta would have to sell blue cheese seeds.
            // he can say try out this new strain i have blue cheese" -
            // wired so HandleRastaBlueCheeseOffer (TownNPCInteractable) can
            // look up the real CropDefinition to sell seeds of.
            var rastaCropsProp = so.FindProperty("sellableCrops");
            rastaCropsProp.arraySize = allCrops.Length;
            for (int i = 0; i < allCrops.Length; i++)
                rastaCropsProp.GetArrayElementAtIndex(i).objectReferenceValue = allCrops[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/UpIzUpMini/Data/Dialogue");
            const string path = "Assets/UpIzUpMini/Data/Dialogue/RastaMentorLines.asset";
            var set = AssetDatabase.LoadAssetAtPath<DialogueSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<DialogueSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            // Jamaican Patois markers per Docs/DIALECT-LEXICON.md's Rasta
            // style note - I an' I, Iyah/bredrin, seen, nuh true?, Zion,
            // livity - deliberately NOT the Dominican/Gwa Bay register
            // (mn/nuh/wii/allu) every other NPC uses, so he reads as a
            // genuinely different voice, not the same slang re-painted.
            set.lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    category = DialogueCategory.StoryReveal,
                    speaker = "Rasta",
                    text = "Blue Cheese a di top a di mountain, Iyah. I an' I see how far yuh come - from likkle Bushers to dis. Yuh reach real strain work now, seen.",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition { type = DialogueConditionType.CropUnlocked, cropId = "black_sugar" },
                        new DialogueCondition { type = DialogueConditionType.CropUnlocked, cropId = "purple" },
                        new DialogueCondition { type = DialogueConditionType.CropUnlocked, cropId = "blue_cheese" },
                    }
                },
                new DialogueLine
                {
                    category = DialogueCategory.Faction,
                    speaker = "Rasta",
                    text = "Purple work no easy, bredrin, but yuh show seen. Di plant know when a man serious.",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition { type = DialogueConditionType.CropUnlocked, cropId = "black_sugar" },
                        new DialogueCondition { type = DialogueConditionType.CropUnlocked, cropId = "purple" },
                    }
                },
                new DialogueLine
                {
                    category = DialogueCategory.Normal,
                    speaker = "Rasta",
                    text = "Black Sugar a grow good in yuh hand now, Iyah. Dat a real start.",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition { type = DialogueConditionType.CropUnlocked, cropId = "black_sugar" },
                    }
                },
                new DialogueLine { category = DialogueCategory.InnerThought, speaker = "Rasta", text = "Walk good, likkle bredrin. Di ganja work teach patience - Zion nah run from yuh." },
                new DialogueLine { category = DialogueCategory.Normal, speaker = "Rasta", text = "I an' I watch dis field long time. Every plant have him own time, nuh true?" },
            };
            EditorUtility.SetDirty(set);

            var dso = new SerializedObject(npc);
            dso.FindProperty("dialogueSet").objectReferenceValue = set;
            dso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// MINI-059: wires up the plantation theft risk against every real
        /// FarmPlot in the scene (main grid + expansion plots) - see
        /// PlantationTheftController for the actual rules.
        /// </summary>
        private static void BuildPlantationTheft()
        {
            var plots = Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None);
            if (plots.Length == 0)
            {
                Debug.LogWarning("Mini011PhaseBSetup: no FarmPlot found; skipping MINI-059 plantation theft.");
                return;
            }

            var go = new GameObject("PlantationTheftController");
            var controller = go.AddComponent<PlantationTheftController>();
            controller.Configure(_farmCenter, plots);
        }

        /// <summary>
        /// MINI-053: builds (or reuses, on a rebuild) one DialogueSet asset
        /// demonstrating every piece of the new foundation on the ambient
        /// villager - the same 4 lines that were already hardcoded in
        /// TownNPCInteractable.villagerLines (unconditional "Always"
        /// entries, so normal play with low heat/reputation looks
        /// unchanged and still cycles through all 4 the way NextLine did),
        /// plus two new conditional lines that only appear once real game
        /// state crosses a threshold: an InnerThought line gated on high
        /// police heat, and a Faction line gated on high gang reputation.
        /// </summary>
        private static void WireVillagerDialogueSet()
        {
            var villagerGo = GameObject.Find("NPC_Villager");
            if (villagerGo == null)
            {
                Debug.LogWarning("Mini011PhaseBSetup: NPC_Villager not found; skipping MINI-053 DialogueSet demo.");
                return;
            }

            EnsureFolder("Assets/UpIzUpMini/Data/Dialogue");
            const string path = "Assets/UpIzUpMini/Data/Dialogue/VillagerLines.asset";
            var set = AssetDatabase.LoadAssetAtPath<DialogueSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<DialogueSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            set.lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    category = DialogueCategory.InnerThought,
                    text = "Better not be seen talking too long with dem two right now, mn.",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition { type = DialogueConditionType.MinHeat, threshold = 40 }
                    }
                },
                new DialogueLine
                {
                    category = DialogueCategory.Faction,
                    speaker = "Villager",
                    text = "Allu carry weight round here now, nuh. Respect.",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition
                        {
                            type = DialogueConditionType.MinReputation,
                            faction = Progression.Faction.GrandBayGangs,
                            threshold = 25
                        }
                    }
                },
                new DialogueLine { category = DialogueCategory.Normal, speaker = "Villager", text = "Yea wii, the sun hot today mn." },
                new DialogueLine { category = DialogueCategory.Normal, speaker = "Villager", text = "How di val na? I hear it was irie." },
                new DialogueLine { category = DialogueCategory.Normal, speaker = "Villager", text = "Allu doe miss nothing much round here, nuh." },
                new DialogueLine { category = DialogueCategory.Normal, speaker = "Villager", text = "Chhh. Lucky you." },
            };
            EditorUtility.SetDirty(set);

            var npc = villagerGo.GetComponent<TownNPCInteractable>();
            if (npc == null)
            {
                Debug.LogWarning("Mini011PhaseBSetup: NPC_Villager has no TownNPCInteractable; skipping MINI-053 DialogueSet demo.");
                return;
            }
            var so = new SerializedObject(npc);
            so.FindProperty("dialogueSet").objectReferenceValue = set;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// MINI-057: Normy - visually a cop (the same uniform tint applied
        /// to real officers, so he reads as police at a glance), but
        /// mechanically a self-interested individual, not part of
        /// PoliceOfficer/PatrolNPC's patrol/chase/heat-detection systems -
        /// "Normy is not representative of all police" per the brief.
        /// His dialogueSet supplies "information" flavour (vague
        /// foreshadowing only - no faction/group is named, matching
        /// MINI-059's own later instruction to "create suspicion" before
        /// any reveal) for the common case where there's no heat to bribe
        /// him over (see TownNPCInteractable.HandleNormy).
        /// </summary>
        private static void WireNormy()
        {
            var normyGo = GameObject.Find("NPC_Normy");
            if (normyGo == null)
            {
                Debug.LogWarning("Mini011PhaseBSetup: NPC_Normy not found; skipping MINI-057 wiring.");
                return;
            }

            ApplyPoliceUniform(normyGo);

            EnsureFolder("Assets/UpIzUpMini/Data/Dialogue");
            const string path = "Assets/UpIzUpMini/Data/Dialogue/NormyLines.asset";
            var set = AssetDatabase.LoadAssetAtPath<DialogueSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<DialogueSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            set.lines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    category = DialogueCategory.InnerThought,
                    speaker = "Normy",
                    text = "Allu getting real hot, nuh. Some a di real officers - not me - starting to ask questions bout allu.",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition { type = DialogueConditionType.MinHeat, threshold = 50 }
                    }
                },
                new DialogueLine
                {
                    category = DialogueCategory.Faction,
                    speaker = "Normy",
                    text = "Mi hear somebody eyeing up crops dat doe belong to dem, round Highland way. Just... watch yuhself, nuh.",
                    conditions = new List<DialogueCondition>
                    {
                        new DialogueCondition
                        {
                            type = DialogueConditionType.MinReputation,
                            faction = Progression.Faction.GrandBayGangs,
                            threshold = 15
                        }
                    }
                },
                new DialogueLine { category = DialogueCategory.Normal, speaker = "Normy", text = "Nothing for me to look away from right now. Allu lucky today." },
                new DialogueLine { category = DialogueCategory.Normal, speaker = "Normy", text = "Quiet day. Keep it dat way and we won't have no problem, mn." },
            };
            EditorUtility.SetDirty(set);

            var npc = normyGo.GetComponent<TownNPCInteractable>();
            if (npc == null)
            {
                Debug.LogWarning("Mini011PhaseBSetup: NPC_Normy has no TownNPCInteractable; skipping MINI-057 dialogue wiring.");
                return;
            }
            var so = new SerializedObject(npc);
            so.FindProperty("dialogueSet").objectReferenceValue = set;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// MINI-055: Boss C - bigger status than Boss J, offers every
        /// higher-tier strain from one physical NPC via
        /// TownNPCInteractable's new multiBossCrops array (replaces the
        /// old one-NPC-per-strain BossM/BossP/BossQ). Gets the "bigger
        /// boss" visual flourishes the brief asked for - a heavier chain
        /// stack and a couple of black SUVs parked nearby - built as
        /// simple hand-authored placeholder geometry (no vehicle system
        /// exists yet; that's MINI-034) rather than left unaddressed.
        /// </summary>
        private static void BuildBossC(Terrain terrain, List<Vector3> roadPoints,
            CropDefinition[] crops, RuntimeAnimatorController controller,
            int index, float side, string[] cropIds, int[] seedPrices)
        {
            index = Mathf.Clamp(index, 1, roadPoints.Count - 2);
            Vector3 dir = (roadPoints[index + 1] - roadPoints[index - 1]).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 pos = roadPoints[index] + right * side * 6.5f;
            pos.y = SampleHeight(terrain, pos.x, pos.z);
            _bossCPos = pos;

            var go = new GameObject("NPC_BossC");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(-right * side, Vector3.up);
            var visual = InstantiateCharacter("Assets/Floreswa/Models/male02_1.fbx", go.transform, controller, null);
            var bossVisualProfile = AssetDatabase.LoadAssetAtPath<CharacterVisualProfile>(
                "Assets/UpIzUpMini/Data/Character/BossCVisualProfile.asset");
            if (bossVisualProfile != null && bossVisualProfile.useManualScale)
                visual.transform.localScale = bossVisualProfile.localScale;

            var cropRefs = new CropDefinition[cropIds.Length];
            for (int i = 0; i < cropIds.Length; i++)
                cropRefs[i] = System.Array.Find(crops, c => c != null && c.cropId == cropIds[i]);

            var npc = go.AddComponent<TownNPCInteractable>();
            var so = new SerializedObject(npc);
            so.FindProperty("role").enumValueIndex = (int)NpcRole.StrainBoss;
            so.FindProperty("npcName").stringValue = "BossC";
            var cropsProp = so.FindProperty("multiBossCrops");
            cropsProp.arraySize = cropRefs.Length;
            for (int i = 0; i < cropRefs.Length; i++)
                cropsProp.GetArrayElementAtIndex(i).objectReferenceValue = cropRefs[i];
            var pricesProp = so.FindProperty("multiBossSeedPrices");
            pricesProp.arraySize = seedPrices.Length;
            for (int i = 0; i < seedPrices.Length; i++)
                pricesProp.GetArrayElementAtIndex(i).intValue = seedPrices[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // The same cleaned 18k prefab sold to the player. One readable
            // chain only; the prefab path is shared so Boss C cannot drift
            // back to the old generated bead necklace.
            ApplyBossChains(visual, chainCount: 1);
            BuildBlackSuvProps(pos, right, dir, side);
        }

        /// <summary>
        /// A beaded necklace strand (a drooping semicircle of small
        /// spheres across the chest), hand-built the same way
        /// ApplyPoliceUniform decorates officers - no need to route a
        /// decorative NPC prop through the player-only shop/
        /// CharacterEquipment system for this. Parented to the chest bone,
        /// not the neck - a first attempt parented to the neck read as a
        /// flat gold disc floating off the shoulder in a rendered check,
        /// not a chain; a chest-anchored drooping bead arc is what
        /// actually reads as a necklace at gameplay camera distance.
        /// chainCount > 1 reads as "larger/multiple chains" per the brief.
        /// </summary>
        /// <summary>MINI-067: the cleaned, game-ready 18k chain (built by
        /// Mini067GoldChainPrep from the user's 2M-poly source).</summary>
        private static GameObject LoadGoldChainPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UpIzUpMini/Art/Accessories/GoldChain18k.prefab");
            if (prefab == null)
                Debug.LogWarning("MINI-067: GoldChain18k.prefab missing - characters will fall back to the placeholder chain. Run MINI-067/Build Gold Chain Prefab.");
            return prefab;
        }

        private static void ApplyBossChains(GameObject instance, int chainCount)
        {
            var animator = instance.GetComponentInChildren<Animator>();
            var neck = animator != null && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.Neck)
                : null;
            if (neck == null) return;

            // MINI-067: the user's real 18k model, per "i want to put the gold
            // chain on the character's chest and the boss man c chest as well".
            // The placement lessons below (neck bone for position, ROOT for
            // rotation, pushed forward off the collar) were paid for with three
            // failed render checks and still apply - only the geometry changes,
            // so they are reused rather than re-derived.
            var chainPrefab = LoadGoldChainPrefab();
            if (chainPrefab != null)
            {
                // Same anchor and the same two rendered-and-checked numbers the
                // player uses, so the boss and the player wear it identically
                // rather than drifting apart as two hand-tuned placements.
                var chest = animator.GetBoneTransform(HumanBodyBones.Chest) ?? neck;
                var worn = (GameObject)PrefabUtility.InstantiatePrefab(chainPrefab);
                worn.name = "BossChain_18k";
                worn.transform.SetParent(chest, worldPositionStays: true);
                var manual = AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(
                    "Assets/UpIzUpMini/Data/Equipment/BossCChainPlacement.asset");
                if (manual != null && manual.useManualPlacement)
                {
                    worn.transform.localPosition = manual.localPosition;
                    worn.transform.localRotation = Quaternion.Euler(manual.localEulerAngles);
                    worn.transform.localScale = manual.localScale;
                    ApplyAccessoryFittedChildren(worn.transform, manual);
                }
                else
                {
                    worn.transform.rotation = instance.transform.rotation
                        * Quaternion.Euler(CharacterEquipment.BossChainTilt, 0f, 0f);
                    worn.transform.position = chest.position
                        + instance.transform.forward * CharacterEquipment.BossChainForward
                        + instance.transform.up * CharacterEquipment.BossChainUp;
                    // Boss C's chest bone scale differs from the player's, so the
                    // same prefab measured 0.068m on him against 0.308m on Sacat.
                    CharacterEquipment.NormaliseAccessoryScale(
                        worn.transform, chest, CharacterEquipment.ChainWidth);
                }
                foreach (var col in worn.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(col);
                return;
            }

            // Fallback: the original hand-built bead strands, kept so a missing
            // prefab leaves the boss wearing something rather than nothing.
            Material chainMat = GetOrCreateMaterial("BossChainGold", new Color(0.85f, 0.68f, 0.15f));

            for (int c = 0; c < chainCount; c++)
            {
                var strand = new GameObject($"BossChain_{c}");
                // Parented to the neck bone so it follows the character,
                // but its ROTATION is forced to the character root's own
                // rotation rather than inherited from the bone - a first
                // attempt trusted the bone's local axes directly and the
                // result hung down near the character's hip, off to the
                // side, because this rig's neck bone rest orientation
                // isn't simply world-aligned. Anchoring rotation to the
                // root (a known, sane frame - forward is forward) makes
                // the bead-arc math below predictable.
                strand.transform.SetParent(neck, worldPositionStays: true);
                strand.transform.rotation = instance.transform.rotation;
                // Pushed further out in front and less far down than the
                // first two attempts, which sank the beads far enough into
                // the collar/torso mesh to be almost entirely self-occluded
                // (only a stray bead poked out through a gap, visible from
                // the back but not the front). A third attempt (0.10 down)
                // still read as sitting on the stomach once seen at
                // gameplay scale, not the chest - raised again here.
                strand.transform.position = neck.position
                    + instance.transform.forward * 0.22f
                    + instance.transform.up * (0.02f - c * 0.05f);

                const int beadCount = 9;
                float radius = 0.085f + c * 0.015f;
                float sagAmount = 0.025f + c * 0.008f;
                for (int i = 0; i < beadCount; i++)
                {
                    float t = i / (float)(beadCount - 1); // 0..1 across the arc
                    float angle = Mathf.Lerp(-75f, 75f, t) * Mathf.Deg2Rad;
                    float sag = sagAmount * (1f - Mathf.Abs(t - 0.5f) * 2f); // droops most in the middle
                    float x = Mathf.Sin(angle) * radius;
                    float y = -Mathf.Cos(angle) * radius - sag;

                    var bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bead.name = $"Bead_{i}";
                    bead.transform.SetParent(strand.transform, false);
                    bead.transform.localPosition = new Vector3(x, y, 0f);
                    bead.transform.localScale = Vector3.one * 0.024f;
                    Object.DestroyImmediate(bead.GetComponent<Collider>());
                    bead.GetComponent<Renderer>().sharedMaterial = chainMat;
                }
            }
        }

        private static void ApplyAccessoryFittedChildren(Transform root, AccessoryPlacementProfile profile)
        {
            if (profile.fittedChildren == null) return;
            foreach (var pose in profile.fittedChildren)
            {
                if (pose == null || string.IsNullOrEmpty(pose.relativePath)) continue;
                var child = root.Find(pose.relativePath);
                if (child == null)
                {
                    int slash = pose.relativePath.LastIndexOf('/');
                    string parentPath = slash >= 0 ? pose.relativePath.Substring(0, slash) : string.Empty;
                    string name = slash >= 0 ? pose.relativePath.Substring(slash + 1) : pose.relativePath;
                    int suffix = name.LastIndexOf(" (", System.StringComparison.Ordinal);
                    var parent = string.IsNullOrEmpty(parentPath) ? root : root.Find(parentPath);
                    var source = suffix > 0 && parent != null ? parent.Find(name.Substring(0, suffix)) : null;
                    if (source != null)
                    {
                        var copy = Object.Instantiate(source.gameObject, parent, false);
                        copy.name = name;
                        child = copy.transform;
                    }
                }
                if (child == null) continue;
                child.localPosition = pose.localPosition;
                child.localRotation = Quaternion.Euler(pose.localEulerAngles);
                child.localScale = pose.localScale;
            }
        }

        /// <summary>
        /// One simple SUV placeholder near Boss C - "nice black SUVs
        /// nearby" per the brief. Not a real vehicle (no driving/physics) -
        /// that's MINI-033/034's scope - just parked set-dressing
        /// establishing his status. A first pass (plain box + small
        /// hidden-under-the-body wheels) rendered as an unmarked dark
        /// crate in a check render, not a car - this version adds a
        /// narrower "greenhouse" cabin box (distinct from the lower body,
        /// the actual visual cue that reads as a vehicle) and raises the
        /// body so the wheels clear the ground and are actually visible.
        /// Originally placed two, offset in world-space Vector3.forward -
        /// the user found one sitting in the middle of the road, because
        /// world-forward doesn't follow the road's actual bend the way the
        /// road-tangent direction (dir) does. Down to one, offset along
        /// the real road tangent and pushed further out on the perpendicular.
        /// </summary>
        private static void BuildBlackSuvProps(Vector3 bossPos, Vector3 right, Vector3 dir, float side)
        {
            Material bodyMat = GetOrCreateMaterial("BlackSuvBody", new Color(0.04f, 0.04f, 0.045f));
            Material glassMat = GetOrCreateMaterial("BlackSuvGlass", new Color(0.10f, 0.13f, 0.16f));
            Material wheelMat = GetOrCreateMaterial("BlackSuvWheel", new Color(0.01f, 0.01f, 0.01f));

            // bossPos already sits right*side*6.5 off the road centreline
            // (see BuildBossC) - subtracting that same amount here just
            // cancelled it back out and put the SUV on the road centreline
            // itself, which is exactly the bug the user reported. Adding
            // further offset in the SAME direction Boss C is already
            // offset pushes it clear of the road instead.
            Vector3 pos = bossPos + right * side * 3.0f + dir * 1.5f;
            pos.y = bossPos.y;

            var suv = new GameObject("BossC_SUV");
            suv.transform.position = pos;
            // Parked along the road rather than across it. The old placeholder
            // faced `right` while building its body 4.2m along local X, i.e. it
            // was modelled sideways relative to its own forward - a quirk not
            // worth inheriting now that a real car with a real nose is going in.
            suv.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            // MINI-070: the user's real Range Rover replaces the box-and-
            // cylinder stand-in. The prefab is authored nose-along-+Z and
            // origin-at-the-tyres by Mini070RangeRoverPrep, so it just needs
            // parenting - no per-placement height or scale fudge here.
            var rover = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/UpIzUpMini/Art/Vehicles/RangeRover.prefab");
            if (rover != null)
            {
                var placed = (GameObject)PrefabUtility.InstantiatePrefab(rover, suv.transform);
                placed.name = "RangeRover";
                placed.transform.localPosition = Vector3.zero;
                placed.transform.localRotation = Quaternion.identity;
                return;
            }

            Debug.LogWarning("MINI-070: RangeRover.prefab missing - falling back to the placeholder SUV. Run MINI-070/Build Range Rover Prefab.");

            // Cylinder primitives have radius 0.5/height 2 pre-scale;
            // after the 90-degree Z rotation below, a localScale of
            // (2*R, T/2, 2*R) gives an actual visible wheel radius R
            // and axle thickness T - worked out by hand after a first
            // attempt (uniform scale) rendered as barely-visible flat
            // discs hidden under the body.
            const float wheelRadius = 0.42f;
            const float wheelThickness = 0.32f;

            // Lower body sits high enough that the wheels clear the
            // ground and are visible beneath it, not hidden inside it.
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(suv.transform, false);
            body.transform.localPosition = new Vector3(0f, wheelRadius + 0.55f, 0f);
            body.transform.localScale = new Vector3(4.2f, 1.1f, 1.9f);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;

            // Narrower cabin/greenhouse on top - the actual shape cue
            // that reads as a car rather than a shipping container.
            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Cabin";
            cabin.transform.SetParent(suv.transform, false);
            cabin.transform.localPosition = new Vector3(-0.15f, wheelRadius + 1.28f, 0f);
            cabin.transform.localScale = new Vector3(2.6f, 0.9f, 1.7f);
            cabin.GetComponent<Renderer>().sharedMaterial = glassMat;

            float[] wx = { -1.4f, 1.4f };
            float[] wz = { -0.95f, 0.95f };
            foreach (float x in wx)
            {
                foreach (float z in wz)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(suv.transform, false);
                    wheel.transform.localPosition = new Vector3(x, wheelRadius, z);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    wheel.transform.localScale = new Vector3(wheelRadius * 2f, wheelThickness * 0.5f, wheelRadius * 2f);
                    Object.DestroyImmediate(wheel.GetComponent<Collider>());
                    wheel.GetComponent<Renderer>().sharedMaterial = wheelMat;
                }
            }
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
        /// Brakes is the Grand Bay priest requested for the church. He uses
        /// the ordinary dialogue/interactable foundation and a white-clothing
        /// treatment, so no new character asset or paid generation is needed.
        /// The map migration places him at the approved church anchor.
        /// </summary>
        private static void BuildBrakesPriest(
            Terrain terrain, List<Vector3> roadPoints, RuntimeAnimatorController animController)
        {
            BuildNpc(terrain, roadPoints, index: roadPoints.Count - 3, sideMul: 1f,
                goName: "NPC_Brakes", modelPath: "Assets/Floreswa/Models/male03_3.fbx",
                role: NpcRole.Villager, cropsForBuyer: null, animController: animController,
                patrols: false, reactsToHeat: false);

            GameObject brakes = GameObject.Find("NPC_Brakes");
            if (brakes == null) return;
            ApplyPriestWhite(brakes);
            TownNPCInteractable npc = brakes.GetComponent<TownNPCInteractable>();
            if (npc == null) return;
            SerializedObject so = new SerializedObject(npc);
            so.FindProperty("npcName").stringValue = "Brakes";
            SerializedProperty lines = so.FindProperty("villagerLines");
            lines.arraySize = 4;
            lines.GetArrayElementAtIndex(0).stringValue = "Brakes: Blessings, young fellas. Keep a clear head while allu building up allu self.";
            lines.GetArrayElementAtIndex(1).stringValue = "Brakes: Honest work may move slow, but it does leave you able to sleep good, nuh.";
            lines.GetArrayElementAtIndex(2).stringValue = "Brakes: If trouble following allu, come reason by the church before it get worse.";
            lines.GetArrayElementAtIndex(3).stringValue = "Brakes: Grand Bay watching how allu move. Make the community proud, yah wii.";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyPriestWhite(GameObject instance)
        {
            Color white = new Color(0.93f, 0.93f, 0.90f);
            Color darkSkin = new Color(0.20f, 0.10f, 0.065f);
            Color black = new Color(0.025f, 0.025f, 0.025f);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    string materialName = materials[i].name.ToLowerInvariant();
                    Color? colour = null;
                    if (materialName.Contains("cardigan")) colour = white;
                    else if (materialName.Contains("skin")) colour = darkSkin;
                    else if (materialName.Contains("shoe") || materialName.Contains("hair")
                        || materialName.Contains("mustache") || materialName.Contains("beard")
                        || materialName.Contains("goatee") || materialName.Contains("eyebrow")
                        || materialName.Contains("pants")) colour = black;
                    if (!colour.HasValue) continue;
                    Material styled = new Material(materials[i]);
                    styled.color = colour.Value;
                    styled.mainTexture = null;
                    materials[i] = styled;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// Boss J (MINI-055: displayed name, was "Boss K" - see
        /// TownNPCInteractable's PromptLabel note; npcName/targetId stay
        /// "BossK" internally, unrelated to the many mission objectives
        /// already keyed off it) waits near the Montine turnoff, away from
        /// the market - per Docs/STORY.md he watches their deliveries and
        /// offers the higher-paying illegal work. Lower-level than Boss C -
        /// street connections, Bushers only, one chain rather than Boss C's
        /// stacked pair.
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

            var visual = InstantiateCharacter("Assets/Floreswa/Models/male02_3.fbx", go.transform, animController, null);

            CropDefinition bushers = null;
            foreach (var c in allCrops)
            {
                if (c != null && c.isIllegal) { bushers = c; break; }
            }

            var npc = go.AddComponent<TownNPCInteractable>();
            var so = new SerializedObject(npc);
            so.FindProperty("role").enumValueIndex = (int)NpcRole.Boss;
            // Internal identifier, matched by several missions' targetId -
            // left as "BossK" deliberately (see the PromptLabel note).
            so.FindProperty("npcName").stringValue = "BossK";
            so.FindProperty("bossSeedCrop").objectReferenceValue = bushers;
            // MINI-039: first illegal strain, cheapest of the tiers -
            // see the seed-pricing note by the BuildBossC call.
            so.FindProperty("seedPrice").intValue = 150;
            var cropsProp = so.FindProperty("sellableCrops");
            cropsProp.arraySize = allCrops.Length;
            for (int i = 0; i < allCrops.Length; i++)
                cropsProp.GetArrayElementAtIndex(i).objectReferenceValue = allCrops[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // Boss J walks a short roadside beat instead of standing in the
            // vehicle lane. The migration replaces these provisional points
            // with the approved Lalay sidewalk coordinates.
            CharacterController controller = go.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.95f, 0f);
            controller.height = 1.85f;
            controller.radius = 0.32f;
            PatrolNPC patrol = go.AddComponent<PatrolNPC>();
            Vector3 patrolA = pos - dir * 5f;
            Vector3 patrolB = pos + dir * 5f;
            patrolA.y = SampleHeight(terrain, patrolA.x, patrolA.z);
            patrolB.y = SampleHeight(terrain, patrolB.x, patrolB.z);
            patrol.SetWaypoints(new[] { patrolA, patrolB });

            // Boss J no longer wears a chain. The status chain belongs to
            // Boss C and can be purchased individually by Sacat or Franki.
        }

        /// <summary>MINI-108: cheap but readable Paro treatment. The owned
        /// one-piece NPC mesh cannot have garment polygons deleted safely in
        /// the generated scene, so skin-tone patches cover ragged shirt/pants
        /// holes and simple bare feet cover the footwear silhouette.</summary>
        private static void StyleParo(GameObject npcRoot)
        {
            if (npcRoot == null) return;
            Material skin = GetOrCreateMaterial("ParoSkinPatch", new Color(0.28f, 0.13f, 0.075f));

            (Vector3 position, Vector3 scale, string name)[] pieces =
            {
                (new Vector3(-0.16f, 1.28f, 0.18f), new Vector3(0.15f, 0.10f, 0.025f), "ShirtHole_Left"),
                (new Vector3( 0.14f, 1.08f, 0.18f), new Vector3(0.12f, 0.08f, 0.025f), "ShirtHole_Right"),
                (new Vector3(-0.13f, 0.68f, 0.16f), new Vector3(0.11f, 0.10f, 0.025f), "PantsHole_Left"),
                (new Vector3( 0.14f, 0.52f, 0.16f), new Vector3(0.10f, 0.08f, 0.025f), "PantsHole_Right"),
                (new Vector3(-0.12f, 0.07f, 0.08f), new Vector3(0.13f, 0.055f, 0.25f), "BareFoot_Left"),
                (new Vector3( 0.12f, 0.07f, 0.08f), new Vector3(0.13f, 0.055f, 0.25f), "BareFoot_Right"),
            };
            foreach ((Vector3 position, Vector3 scale, string name) in pieces)
            {
                GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                patch.name = name;
                patch.transform.SetParent(npcRoot.transform, false);
                patch.transform.localPosition = position;
                patch.transform.localScale = scale;
                Object.DestroyImmediate(patch.GetComponent<Collider>());
                patch.GetComponent<Renderer>().sharedMaterial = skin;
            }

            foreach (Renderer renderer in npcRoot.GetComponentsInChildren<Renderer>(true))
            {
                string lower = renderer.name.ToLowerInvariant();
                if (lower.Contains("shoe") || lower.Contains("boot") || lower.Contains("sneaker"))
                    renderer.enabled = false;
            }
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
            // The Land and Surveys man stands visibly in front of his stall,
            // between the road and the building, rather than around its side.
            float roadsideOffset = role == NpcRole.LandOffice ? 4.45f : 3.8f;
            pos += right * sideMul * roadsideOffset;
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
                    // MINI-038: hit-reaction stagger + knockdown-and-lie-
                    // down needs the same action layer the player's punch
                    // does, and NpcCombatHealth needs a reference to it.
                    var npcAnimator = npcVisual.GetComponentInChildren<Animator>();
                    var npcAnimationManager = AddHumanoidAnimationManager(npcGo, npcAnimator);

                    var combatHealth = npcGo.AddComponent<NpcCombatHealth>();
                    var ragdoll = npcGo.AddComponent<NpcRagdoll>();
                    var chSo = new SerializedObject(combatHealth);
                    chSo.FindProperty("animationManager").objectReferenceValue = npcAnimationManager;
                    chSo.FindProperty("ragdoll").objectReferenceValue = ragdoll;
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

                // MINI-119 follow-up, user: "if i hit the npc they will
                // behave like ragdoll... you can do it will the police,
                // villagers, gang members" - this generic patrol path is
                // where plain Villager NPCs land (shopkeepers/dealers/
                // mission NPCs don't call this branch with patrols=true
                // for those roles), so give Villagers the same
                // combat/ragdoll setup Police already gets, without
                // touching non-Villager patrol roles.
                if (role == NpcRole.Villager)
                {
                    var villagerAnimator = npcVisual.GetComponentInChildren<Animator>();
                    var villagerAnimationManager = AddHumanoidAnimationManager(npcGo, villagerAnimator);

                    var villagerCombatHealth = npcGo.AddComponent<NpcCombatHealth>();
                    var villagerRagdoll = npcGo.AddComponent<NpcRagdoll>();
                    var vchSo = new SerializedObject(villagerCombatHealth);
                    vchSo.FindProperty("animationManager").objectReferenceValue = villagerAnimationManager;
                    vchSo.FindProperty("ragdoll").objectReferenceValue = villagerRagdoll;
                    vchSo.ApplyModifiedPropertiesWithoutUndo();
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

            // MINI-057: found and fixed a real pre-existing bug while
            // building Normy (who also wears this uniform) - parenting
            // directly to the head bone with `false` (keep local
            // position/rotation) put the cap floating at chest height on
            // EVERY officer, not just Normy, confirmed by rendering a real
            // NPC_Police. Same root cause as MINI-055's necklace: this
            // rig's bone rest orientations aren't world-aligned. Same fix:
            // anchor rotation to the character root instead of the bone.
            // First fix landed the cap across the eyes like a visor, not
            // on top of the head - raised further and pulled back here.
            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.name = "PoliceCap";
            crown.transform.SetParent(head, worldPositionStays: true);
            crown.transform.rotation = instance.transform.rotation;
            crown.transform.position = head.position
                + instance.transform.up * 0.24f;
            crown.transform.localScale = new Vector3(0.2f, 0.12f, 0.2f);
            crown.GetComponent<Renderer>().sharedMaterial = capMat;
            Object.DestroyImmediate(crown.GetComponent<Collider>());

            var peak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            peak.name = "PoliceCapPeak";
            peak.transform.SetParent(head, worldPositionStays: true);
            peak.transform.rotation = instance.transform.rotation;
            peak.transform.position = head.position
                + instance.transform.up * 0.21f
                + instance.transform.forward * 0.10f;
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

        // MINI-083, user: "I want all characters the same size... so
        // swapping shirts etc would be seamless." Measured (not assumed)
        // via a throwaway diagnostic: every Floreswa NPC/boss/gang/police
        // model (male01_1 .. male03_3) is ALREADY exactly this tall,
        // uniformly - only the two hero models diverge from it. Scoped to
        // height only per the user's own choice - this does not touch the
        // deeper bone-SCALE divergence between rig families (see
        // CharacterEquipment's BossChain* constants and
        // NormaliseAccessoryScale, which exist for that separate problem
        // and are unaffected by this).
        private const float TargetCharacterHeightM = 1.85f;
        private const float SacatMeasuredHeightM = 1.9739f;
        private const float FrankiMeasuredHeightM = 1.9337f;

        private static GameObject InstantiateCharacter(
            string fbxPath, Transform parent, RuntimeAnimatorController animController,
            Color? skinTint = null, bool hideFacialHair = false, float uniformScale = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = "Visual";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            // MINI-083: uniform scale only, no bone remapping - safe for the
            // mains' richer 52-bone Humanoid avatar (the sparse-avatar leg
            // distortion this project hit before, per this file's own
            // MINI-016 note near BuildControllableCharacter's call sites,
            // was a retargeting problem, not a scale one).
            if (!Mathf.Approximately(uniformScale, 1f))
            {
                instance.transform.localScale = Vector3.one * uniformScale;
            }

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
            // MINI-119, user: "switch the bluecheese number with the
            // purple black" - Blue Cheese is Rasta's tier-4 base strain,
            // taught BEFORE the tier-5 Purple Sugar hybrid (see MINI-111's
            // mission order), but the plant-selection number keys had them
            // backwards (7=Purple Sugar, 8=Blue Cheese). Reordered here so
            // key 7 is Blue Cheese and key 8 is Purple Sugar, matching the
            // actual unlock order.
            // MINI-048: Blue Cheese - a new base strain (boss-granted, like
            // Black Sugar/Purple), single blue ripe colour.
            ("blue_cheese", "Blue Cheese", 65, true, "1E3550", "2E6BAD", null),
            // MINI-047: Purple Black ("Purple Sugar" player-facing) -
            // interbred from Purple + Black Sugar (see CropBreedingStation).
            // secondaryRipeHex set means ripe fruit alternates orange
            // (Black Sugar's own colour) and purple (Purple's) instead of a
            // single flat colour, so both parent strains show at once.
            ("purple_black", "Purple Black", 90, true, "241A2B", "8C1FAD", "D97314"),
            // Sugar Cheese/Purple Cheese - Blue Cheese's own two hybrids,
            // each alternating blue with their other parent's own colour -
            // Sugar Cheese with Black Sugar's orange, Purple Cheese with
            // Purple's purple - in the order the user asked for them.
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
            ("land_montine",  "Highland Farm Plot",   ShopCategory.Land,     600,  null, 0),
            ("land_hillside", "Hillside Survey Lot",   ShopCategory.Land,     1400, null, 0),
            // MINI-062: renamed from the stale "Montine Safehouse Deed" -
            // this item id (prop_safehouse) has always gated the Lalay
            // House (BuildLalayHouse), never a Montine-side property, so
            // the old name was actively misleading about what a player
            // was buying.
            ("prop_safehouse","Lalay House Deed",ShopCategory.Property, 2200, null, 0),
            // MINI-073: "a two story house with a garage for a vehicle on the
            // lalay". A distinct, pricier property from the simple open
            // shelter above - see BuildLalayEstate.
            ("prop_lalay_estate","Lalay Estate Deed",ShopCategory.Property, 8500, null, 0),
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

        // Vehicles and boats come from a dealer. Deliberately just the one
        // real, driveable vehicle for now - the previous flavour-only
        // "Scrambler Bike"/"Pickup Van"/"Fishing Pirogue" entries were pure
        // economy placeholders with no working model behind them (per the
        // future-extensibility note from MINI-011), and the user explicitly
        // asked for the dealer to offer only the bike that actually works:
        // "the option should be only the bike and no other vehicles."
        // Re-add the placeholders here (not deleted, just removed from the
        // table) once a scrambler/van/boat model actually exists.
        private static readonly (string id, string name, ShopCategory cat, int price, string seedCrop, int qty)[] DealerSpecs =
        {
            // MINI-119 follow-up, user: "make it so you can buy it from
            // the car dealer, it will be the cheapest bike, it will be
            // called a Koss." Spawns the same SuperMotoWRagdoll bike
            // this session already got working end-to-end (mount, ride,
            // wheelie hand/foot tracking, pillion), via
            // VehicleSpawnController.SpawnPurchasedVehicle's "koss"
            // branch - see WireSuperMotoInstance for the shared wiring.
            ("koss", "Koss", ShopCategory.Vehicle, 2500, null, 0),
            // MINI-071: display name only. The ITEM ID stays tmax_560 - renaming it
            // would strip the bike off any existing save, the same reason
            // chain_gold kept its id when it became "Gucci Law".
            ("tmax_560", "TNAX 560", ShopCategory.Vehicle, 6500, null, 0),
            // MINI-071: priced at the real 2026 Range Rover base MSRP
            // ($113,300) per the user's "give it the price of the real latest
            // range". That is deliberately far beyond early-game reach - it is
            // an endgame purchase, roughly 17x the TMAX.
            //
            // Named as a near-miss rather than the trademark, following the
            // user's own standing direction on this list ("Fictional near-miss
            // brand names ... so no real trademark is used"). Say the word if
            // you want the real name on it.
            ("range_rova", "Range Rova", ShopCategory.Vehicle, 113300, null, 0),
        };

        // Separate apparel shopfront - kept distinct from the farm shop.
        private static readonly (string id, string name, ShopCategory cat, int price, string seedCrop, int qty)[] ApparelSpecs =
        {
            ("cap_mike",    "Mike Cap",            ShopCategory.Clothing,  45,  null, 0),
            ("shirt_lacos", "Lacostes Polo",       ShopCategory.Clothing,  80,  null, 0),
            ("shorts_adibas","Adibas Shorts",      ShopCategory.Clothing,  65,  null, 0),
            ("shoes_mike",  "Mike Air Kicks",      ShopCategory.Footwear,  150, null, 0),
            ("shoes_pumba", "Pumba Runners",       ShopCategory.Footwear,  120, null, 0),
            // MINI-067: priced at the user's figure ($6000) now that it is a
            // real 18k model rather than the placeholder ring of spheres. That
            // puts it just under the TMAX (6500), which is the intent - a
            // status buy you work toward, not pocket change.
            // Named "Gucci Law" by the user. The ITEM ID stays chain_gold so
            // existing saves keep the item - a display rename must not silently
            // strip a 6000-dollar purchase off someone's character.
            ("chain_gold",  "Gucci Law",           ShopCategory.Accessory, 6000, null, 0),
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
            // MINI-055: Boss C's single consolidated position, replacing
            // the separate BossM (index 10)/BossP (index 13) marker spots.
            Vector3 bossCPos = _bossCPos != Vector3.zero ? _bossCPos : farmCenter;
            Vector3 rastaPos = _rastaPos != Vector3.zero ? _rastaPos : farmCenter;

            var missions = new List<Mission>
            {
                new Mission
                {
                    missionId = "M1",
                    title = "A Start in Highland",
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
                            instruction = "Follow the dirt track up to the Highland farm",
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
                            instruction = "Go to the LAND AND SURVEYS building and buy the Highland Farm Plot ($600) - the Land and Surveys man is standing in front",
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
                    title = "Things Still Slow",
                    briefing = "The legal farming money moving slow and people doe want to pay for all that hard work. Press [L] to keep farming clean for now, or [K] to check Boss J now.",
                    rewardMoney = 0,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.ChoosePath,
                            instruction = "Choose: [L] keep farming legit for now, or [K] go straight to Boss J",
                            hasMarker = false,
                        },
                    }
                },
                new Mission
                {
                    missionId = "M4L",
                    title = "Try the Clean Way",
                    briefing = "Sacat: Let we try and hold the farming end a little longer first.",
                    requiredPath = CareerPath.LegitimateFarmer,
                    commitToWeedRouteOnComplete = true,
                    rewardMoney = 70,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "tomato", requiredCount = 6, instruction = "Grow and harvest 6 tomato in Highland", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.SellCrop, instruction = "Sell the tomato to the Produce Buyer", markerPosition = marketPos,
                            dialogueBanner = "Franki: Gasah, that frustrating me.\nSacat: Yah, boi lets go and check the bossman." },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "tomato", requiredCount = 6, instruction = "Try one more legal tomato harvest", markerPosition = plotPos,
                            dialogueBanner = "Sacat: Although i hear Mr. does bobol people on paying you know.\nFranki: But man can work for Mr. until we can hold our end." },
                        new MissionObjective { kind = ObjectiveKind.SellCrop, instruction = "Sell the last legal crop, then go check Boss J", markerPosition = marketPos,
                            dialogueBanner = "Sacat: Well lah, we organize." },
                    }
                },
                new Mission
                {
                    missionId = "M4W",
                    title = "The Offer",
                    requiredPath = CareerPath.WeedRoute,
                    briefing = "A man name Boss J been watching allu deliveries. He waiting up by the farm track.",
                    rewardMoney = 0,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BossK", instruction = "Go and hear what Boss J have to say", markerPosition = bossPos },
                        new MissionObjective { kind = ObjectiveKind.PlantCrop, targetId = "bushers", instruction = "Plant the Bushers up at Highland  [ 4 ] to select", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "bushers", requiredCount = 3, instruction = "Water it, let it grow, then harvest the Bushers", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.SellCrop, targetId = "BossK", instruction = "Bring the harvested Bushers back to Boss J  [ E ]", markerPosition = bossPos },
                    }
                },
                new Mission
                {
                    missionId = "M5A",
                    title = "Lay Low",
                    briefing = "Aye, police hot. Go to the Highland safehouse, rest, and let things cool down.",
                    rewardMoney = 120,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.RestAtSafehouse,
                            instruction = "Go to the Highland safehouse and rest in the bed",
                            markerPosition = farmCenter,
                        },
                    }
                },
                new Mission
                {
                    missionId = "M5B",
                    title = "Clean Face",
                    briefing = "Move clean around the regular police so they know your face, then find Normy and give him a little ting.",
                    rewardMoney = 80,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.TalkToCleanPolice,
                            targetId = "Police",
                            requiredCount = 2,
                            instruction = "Carry no weed or weed seeds and talk to 2 regular police officers",
                            markerPosition = policePos,
                        },
                        new MissionObjective { kind = ObjectiveKind.BribeNormy, targetId = "Normy", instruction = "Find Normy and give him $100 to remove 20% heat", markerPosition = roadPoints[Mathf.Clamp(4,1,roadPoints.Count-2)] },
                    }
                },
                new Mission
                {
                    missionId = "M6",
                    title = "Stock Up",
                    briefing = "Keep working the Highland plots. Build enough Bushers stock for the next move.",
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
                    briefing = "Boss J pay best, but a Paro in Lalay buying small amounts with less questions.",
                    rewardMoney = 140,
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective
                        {
                            kind = ObjectiveKind.TalkTo,
                            targetId = "Vagrant",
                            instruction = "Find the Paro along the Lalay road",
                            markerPosition = roadPoints[Mathf.Clamp(9, 1, roadPoints.Count - 2)],
                        },
                        new MissionObjective
                        {
                            kind = ObjectiveKind.SellCrop,
                            targetId = "Vagrant",
                            instruction = "Sell the Bushers to the Paro. Selling weed raises heat.",
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
                    missionId = "M9W", title = "Boss J's Cut", requiredPath = CareerPath.WeedRoute,
                    briefing = "Boss J have another Bushers run ready. Keep production moving and bring it back.",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BossK", instruction = "Return to Boss J for the next job", markerPosition = bossPos },
                        new MissionObjective { kind = ObjectiveKind.AssignFarmhand, targetId = "bushers", instruction = "Select Bushers [4], then assign the other boy to manage three plots", markerPosition = farmCenter },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "bushers", requiredCount = 3, instruction = "Grow and harvest 3 Bushers", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.SellCrop, targetId = "BossK", instruction = "Deliver the Bushers to Boss J", markerPosition = bossPos }
                    }
                },
                new Mission
                {
                    missionId = "M10W", title = "Black Sugar", requiredPath = CareerPath.WeedRoute,
                    // MINI-055: Boss M consolidated into Boss C.
                    briefing = "Boss C will only release Black Sugar after Boss J use allu enough.",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BossC", instruction = "Meet Boss C and unlock Black Sugar [5]", markerPosition = bossCPos },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "black_sugar", requiredCount = 3, instruction = "Grow and harvest Black Sugar [5]", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.SellCrop, targetId = "BossK", instruction = "Deliver the Black Sugar to Boss J", markerPosition = bossPos }
                    }
                },
                new Mission
                {
                    missionId = "M11W", title = "Purple Territory", requiredPath = CareerPath.WeedRoute,
                    // MINI-055: Boss P consolidated into Boss C.
                    briefing = "Your gang reputation open a meeting with Boss C. Purple pays most and brings the most heat.",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BossC", instruction = "Meet Boss C and unlock Purple [6]", markerPosition = bossCPos },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "purple", requiredCount = 3, instruction = "Grow and harvest Purple [6]", markerPosition = plotPos },
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BoatMan", instruction = "The Grand Bay route is established - meet the Boat Man", markerPosition = new Vector3(0f, SeaLevelY, TerrainSize*.5f) }
                    }
                },

                // MINI-073/074: appended tail. All Undecided-required, so they
                // run for BOTH paths regardless of where each finishes its own
                // path-specific content (see MissionSystem's skip-loop, which
                // only ever skips a mission whose requiredPath does not match
                // the player's own). Easy-to-hard: village errands first,
                // then the elders, then the boat man made an explicit stop for
                // BOTH paths (previously only WeedRoute players were ever sent
                // to him), then the gang, then the hardest - the rival gang
                // itself, which is where FactionBrawler's own proximity fight
                // (MINI-069) actually kicks in once DogLifeRevealed is true.
                new Mission
                {
                    missionId = "M12", title = "Round the Village",
                    briefing = "Allu cyar just farm and hide - people need to know your face round Lalay.",
                    rewardMoney = 50,
                    objectives = new List<MissionObjective> {
                        // Real NPC positions (index matches the BuildNpc calls that
                        // place NPC_Normy/NPC_FoodShop/NPC_Pharmacy - policePos above
                        // is a generic patrol spot at a different index, not Normy).
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Normy", instruction = "Say hello to Normy", markerPosition = roadPoints[Mathf.Clamp(4,1,roadPoints.Count-2)] },
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "FoodShop", instruction = "Check what the Food shop selling", markerPosition = roadPoints[Mathf.Clamp(5,1,roadPoints.Count-2)] },
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Pharmacy", instruction = "Check what the Pharmacy selling", markerPosition = roadPoints[Mathf.Clamp(11,1,roadPoints.Count-2)] },
                    }
                },
                new Mission
                {
                    missionId = "M13", title = "Ital and Elders",
                    briefing = "Rasta been watching the plantation long time - go hear what he have to say.",
                    rewardMoney = 60,
                    objectives = new List<MissionObjective> {
                        // MINI-109: was recomputing Rasta's placement
                        // formula here without the farmRight*3f offset
                        // BuildRastaMentor actually applies, landing the
                        // marker 3m off from where he really stands. Now
                        // reads his real built position directly.
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Rasta", instruction = "Go talk to Rasta on the farm track", markerPosition = rastaPos },
                        // MINI-111: "Rasta should stop asking for tomatoes.
                        // His chapter becomes the production/strain
                        // school" - tier 1 of the ladder, Bushers, already
                        // unlocked from the start so it needs no
                        // unlocksCropId.
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "bushers", requiredCount = 3, instruction = "Rasta say bring him proof allu still working - harvest 3 Bushers", markerPosition = plotPos },
                    }
                },
                // MINI-111: Rasta's strain-mentorship ladder, tiers 2-7.
                // Each mission's unlocksCropId fires the moment IT becomes
                // current (see MissionSystem.AdvanceObjective), so the
                // crop is buyable/breedable before its own harvest
                // objective is even reached - never shown/buyable earlier.
                // Placed before M13B/M14 so the Boat Man/Guadeloupe chapter
                // follows strain mastery, not the other way around, per
                // the handoff's own "only after the boys hold all core
                // strains... open the Guadeloupe opportunity."
                new Mission
                {
                    missionId = "M13C2", title = "Black Sugar School",
                    briefing = "Rasta: \"Bushers is just the start, yout. Mi show yuh how fi work Black Sugar now - stronger ting, more careful hand.\"",
                    rewardMoney = 65,
                    unlocksCropId = "black_sugar",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Rasta", instruction = "Go hear Rasta teach Black Sugar", markerPosition = rastaPos },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "black_sugar", requiredCount = 3, instruction = "Grow and harvest 3 Black Sugar", markerPosition = plotPos },
                    }
                },
                new Mission
                {
                    missionId = "M13C3", title = "Purple, Rasta's Way",
                    briefing = "Rasta: \"Allu ready for Purple now. Mind it - dis one bring more heat, but more money too.\"",
                    rewardMoney = 75,
                    unlocksCropId = "purple",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Rasta", instruction = "Go hear Rasta teach Purple", markerPosition = rastaPos },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "purple", requiredCount = 3, instruction = "Grow and harvest 3 Purple", markerPosition = plotPos },
                    }
                },
                new Mission
                {
                    missionId = "M13C4", title = "Blue Cheese Proof",
                    briefing = "Rasta: \"Now a next strain altogether - Blue Cheese. Grow one an' show mi, prove yuh have di hand for it.\"",
                    rewardMoney = 85,
                    unlocksCropId = "blue_cheese",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Rasta", instruction = "Go hear Rasta teach Blue Cheese", markerPosition = rastaPos },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "blue_cheese", requiredCount = 1, instruction = "Grow and harvest 1 Blue Cheese to prove it", markerPosition = plotPos },
                    }
                },
                new Mission
                {
                    // MINI-111: "teach the first mixed strain, Purple Sugar
                    // (if the implementation retains the legacy
                    // purple_black ID, document the player-facing rename)"
                    // - internal crop id stays purple_black; every
                    // player-visible string here says Purple Sugar.
                    missionId = "M13C5", title = "Purple Sugar",
                    briefing = "Rasta: \"Now mi teach yuh di real skill - crossing strains. Purple and Black Sugar together make Purple Sugar. Tek dem to the breeding station.\"",
                    rewardMoney = 95,
                    unlocksCropId = "purple_black",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Rasta", instruction = "Go hear Rasta teach Purple Sugar", markerPosition = rastaPos },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "purple_black", requiredCount = 1, instruction = "Cross Purple and Black Sugar at the breeding station, then grow and harvest 1 Purple Sugar", markerPosition = plotPos },
                    }
                },
                new Mission
                {
                    missionId = "M13C6", title = "Sugar Cheese",
                    briefing = "Rasta: \"Blue Cheese and Black Sugar cross to Sugar Cheese. Yuh close to di top now.\"",
                    rewardMoney = 105,
                    unlocksCropId = "sugar_cheese",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Rasta", instruction = "Go hear Rasta teach Sugar Cheese", markerPosition = rastaPos },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "sugar_cheese", requiredCount = 1, instruction = "Cross Blue Cheese and Black Sugar, then grow and harvest 1 Sugar Cheese", markerPosition = plotPos },
                    }
                },
                new Mission
                {
                    missionId = "M13C7", title = "Purple Cheese",
                    briefing = "Rasta: \"Di last one, Iyah - Purple Cheese. Blue Cheese and Purple together. After dis, allu know every strain in Grand Bay.\"",
                    rewardMoney = 120,
                    unlocksCropId = "purple_cheese",
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Rasta", instruction = "Go hear Rasta teach Purple Cheese", markerPosition = rastaPos },
                        new MissionObjective { kind = ObjectiveKind.HarvestCrop, targetId = "purple_cheese", requiredCount = 1, instruction = "Cross Blue Cheese and Purple, then grow and harvest 1 Purple Cheese", markerPosition = plotPos },
                    }
                },
                new Mission
                {
                    // MINI-110: Normy's item favour, ahead of the Boat
                    // Man/Gardey bridge - "before the Boat Man chapter,
                    // Normy asks the player to bring requested food and
                    // pharmacy items."
                    missionId = "M13B", title = "Small Ting",
                    briefing = "Normy want a little favour before he put allu onto anything else - some food and a little pharmacy ting.",
                    rewardMoney = 50,
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "Normy", instruction = "Go hear what Normy want", markerPosition = roadPoints[Mathf.Clamp(4,1,roadPoints.Count-2)] },
                        new MissionObjective { kind = ObjectiveKind.DeliverItem, targetId = "food_bakes", requiredCount = 1, instruction = "Buy Bakes and Saltfish from the Food shop, then bring it to Normy", markerPosition = roadPoints[Mathf.Clamp(4,1,roadPoints.Count-2)] },
                        new MissionObjective { kind = ObjectiveKind.DeliverItem, targetId = "pill_energy", requiredCount = 1, instruction = "Buy Energy Pills from the Pharmacy, then bring it to Normy", markerPosition = roadPoints[Mathf.Clamp(4,1,roadPoints.Count-2)] },
                    }
                },
                new Mission
                {
                    missionId = "M14", title = "Ason Ki Move",
                    // MINI-110: carries Normy's uncertain street-info hint
                    // from M13B's completion straight into this mission's
                    // own briefing banner - "somebody may be taking the
                    // boys' crop/stuff, he doesn't know who, check the
                    // Boat Man about Gardey Zafeh" - reusing the existing
                    // next-mission-briefing banner instead of new dialogue
                    // plumbing.
                    briefing = "Normy: \"Preciate dat. Listen - I hearing people talking bout somebody taking weh from allu stock and stuff. I doe know who exactly, but if I was allu, I woulda check the Boat Man about a man name Gardey Zafeh, down Gwada way. He does know more than he let on.\"",
                    rewardMoney = 70,
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "BoatMan", instruction = "Meet the Boat Man at the jetty", markerPosition = new Vector3(0f, SeaLevelY, TerrainSize*.5f) },
                    }
                },
                new Mission
                {
                    missionId = "M15", title = "Not Ah Word",
                    briefing = "Time to build up your own crew - go see the recruiter near Boss C's block.",
                    rewardMoney = 80,
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.TalkTo, targetId = "GangRecruiter", instruction = "Talk to the recruiter and take on a member", markerPosition = bossCPos },
                    }
                },
                new Mission
                {
                    missionId = "M16", title = "War Story",
                    briefing = "Dog Life been watching allu plantation too long. Bring your crew and step to their block.",
                    rewardMoney = 150,
                    // MINI-119, user: "i think for the war story you would
                    // have to fight off and kill all dog life members to
                    // win the war story" - previously just ReachArea +
                    // EscapeHeat, no combat requirement at all. Now the
                    // player must actually knock out every pooled Dog Life
                    // member (RivalGangSpawner.AllDefeated) before laying
                    // low, matching a real "war" rather than a walk-in.
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.ReachArea, instruction = "Walk your crew into Dog Life's block on the Lalay road", markerPosition = roadPoints[Mathf.Clamp(1,1,roadPoints.Count-2)] },
                        new MissionObjective { kind = ObjectiveKind.DefeatAllRivals, targetId = "DogLifeSpawner", instruction = "Fight off and knock out every Dog Life member holding the block", markerPosition = roadPoints[Mathf.Clamp(1,1,roadPoints.Count-2)] },
                        new MissionObjective { kind = ObjectiveKind.EscapeHeat, instruction = "Lay low until police heat cools back down", hasMarker = false },
                    }
                },
                // MINI-080: "can you add more missions as i requested from
                // before?" - two more, continuing past M16 (Undecided, so
                // both paths reach them). M17 gives the TNAX a real purchase
                // goal to work toward (it existed and was rideable, but no
                // mission ever pointed the player at buying one). M18 does
                // the same for the Lalay Estate (MINI-073) - it had no
                // mission touchpoint at all before this.
                new Mission
                {
                    missionId = "M17", title = "Wheels of Your Own",
                    briefing = "Allu tired a walk and beg ride. Save up and buy the TNAX from the dealer.",
                    rewardMoney = 100,
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.BuyItem, targetId = "tmax_560", instruction = "Buy the TNAX 560 from the Car Dealer", markerPosition = roadPoints[Mathf.Clamp(15,1,roadPoints.Count-2)] },
                    }
                },
                new Mission
                {
                    missionId = "M18", title = "A Place to Rest",
                    briefing = "Farm safehouse good, but a real house in Lalay show allu made it.",
                    rewardMoney = 200,
                    objectives = new List<MissionObjective> {
                        new MissionObjective { kind = ObjectiveKind.BuyItem, targetId = "prop_lalay_estate", instruction = "Buy the Lalay Estate Deed from the Land Office", markerPosition = roadPoints[Mathf.Clamp(14,1,roadPoints.Count-2)] },
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
                mp.FindPropertyRelative("commitToWeedRouteOnComplete").boolValue = mission.commitToWeedRouteOnComplete;
                mp.FindPropertyRelative("unlocksCropId").stringValue = mission.unlocksCropId ?? string.Empty;

                var objProp = mp.FindPropertyRelative("objectives");
                objProp.arraySize = mission.objectives.Count;
                for (int o = 0; o < mission.objectives.Count; o++)
                {
                    var op = objProp.GetArrayElementAtIndex(o);
                    var obj = mission.objectives[o];
                    op.FindPropertyRelative("kind").enumValueIndex = (int)obj.kind;
                    op.FindPropertyRelative("instruction").stringValue = obj.instruction;
                    op.FindPropertyRelative("dialogueBanner").stringValue = obj.dialogueBanner ?? string.Empty;
                    op.FindPropertyRelative("targetId").stringValue = obj.targetId ?? string.Empty;
                    op.FindPropertyRelative("requiredCount").intValue = Mathf.Max(1, obj.requiredCount);
                    op.FindPropertyRelative("markerPosition").vector3Value = obj.markerPosition;
                    op.FindPropertyRelative("hasMarker").boolValue = obj.hasMarker;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // MINI-054: plays before M1's own briefing (see
            // BuildOpeningConversation - sets firstBriefingDelay on this
            // same MissionSystem to the conversation's total duration).
            BuildOpeningConversation(system);

            BuildObjectiveMarker();
        }

        /// <summary>
        /// MINI-054: Franki and Sacat's opening exchange, per the roadmap
        /// brief's beats - kicked out of school, hungry and struggling,
        /// need money, Franki suggests Zion/Zeb, Sacat is hesitant, they
        /// settle on starting with normal crops (which is exactly what M1
        /// then has them do). Dialect kept to terms confirmed or
        /// high-confidence in Docs/DIALECT-LEXICON.md (Zeb, Zion, and the
        /// already-verified mn/nuh/wii register from
        /// Docs/DIALOGUE-REFERENCE.md) - no unconfirmed term is used.
        /// </summary>
        private static void BuildOpeningConversation(MissionSystem system)
        {
            var go = new GameObject("OpeningConversation");
            var controller = go.AddComponent<OpeningConversationController>();
            var so = new SerializedObject(controller);
            var linesProp = so.FindProperty("lines");

            (string speaker, string text)[] script =
            {
                ("Franki", "Nothing nuh work out for us in dat school, mn. Dey put us out and dat's dat."),
                ("Sacat", "Yea wii. But we still eh have nothing to eat off tonight, Franki."),
                ("Franki", "I hear a man up Zion way does pay big for Zeb. Fast money, fast fast."),
                ("Sacat", "Nah, mn. Not yet. Allu know how dat kind ah ting does end - police, trouble, worse."),
                ("Franki", "So wah we go do then, smart man? Sit down and starve?"),
                ("Sacat", "We start with something clean first - tomato, banana, carrot. Build up slow, den we see."),
                ("Franki", "Alright... but if di crop money slow, I telling you now - I going back to dat Zeb talk."),
                ("Sacat", "Fine. One step at a time, nuh. Let we go see what Highland have for us."),
            };

            linesProp.arraySize = script.Length;
            for (int i = 0; i < script.Length; i++)
            {
                var lp = linesProp.GetArrayElementAtIndex(i);
                lp.FindPropertyRelative("speaker").stringValue = script[i].speaker;
                lp.FindPropertyRelative("category").enumValueIndex = (int)Dialogue.DialogueCategory.Normal;
                lp.FindPropertyRelative("text").stringValue = script[i].text;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            float duration = controller.TotalDuration;
            var missionSo = new SerializedObject(system);
            missionSo.FindProperty("firstBriefingDelay").floatValue = duration;
            missionSo.ApplyModifiedPropertiesWithoutUndo();
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
            BuildSign(parent.transform, terrain, roadPoints[mid], "HIGHLAND", postMat, boardMat);
            BuildSign(parent.transform, terrain,
                farmCenter + (roadPoints[mid] - farmCenter).normalized * 16f,
                "HIGHLAND FARM", postMat, boardMat);
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
            Image reputationFill = CreateMeter(canvasGo.transform, "Street Rep", new Vector2(20f, -110f), new Color(0.30f, 0.55f, 0.95f), font, out Text reputationPct);

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

            Text nameLabel = CreateLabel(canvasGo.transform, "ACTIVE: SACAT", 26, new Vector2(20f, -146f), font);
            nameLabel.rectTransform.anchorMin = nameLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            nameLabel.rectTransform.pivot = new Vector2(0f, 1f);
            nameLabel.rectTransform.sizeDelta = new Vector2(360f, 44f);
            nameLabel.alignment = TextAnchor.MiddleLeft;

            // MINI-110: courier-away return countdown - only visible while
            // GuadeloupeTrade.TripActive, hidden otherwise (HUDController's
            // own Update() toggles Text.enabled).
            Text courierTimerLabel = CreateLabel(canvasGo.transform, string.Empty, 22, new Vector2(20f, -190f), font);
            courierTimerLabel.rectTransform.anchorMin = courierTimerLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            courierTimerLabel.rectTransform.pivot = new Vector2(0f, 1f);
            courierTimerLabel.rectTransform.sizeDelta = new Vector2(420f, 34f);
            courierTimerLabel.alignment = TextAnchor.MiddleLeft;
            courierTimerLabel.color = new Color(0.95f, 0.85f, 0.35f);
            courierTimerLabel.enabled = false;

            var hud = canvasGo.AddComponent<HUDController>();
            var so = new SerializedObject(hud);
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("staminaFill").objectReferenceValue = staminaFill;
            so.FindProperty("heatFill").objectReferenceValue = heatFill;
            so.FindProperty("moneyLabel").objectReferenceValue = moneyLabel;
            so.FindProperty("characterNameLabel").objectReferenceValue = nameLabel;
            so.FindProperty("cropSelectionLabel").objectReferenceValue = cropLabel;
            so.FindProperty("inventoryLabel").objectReferenceValue = inventoryLabel;
            so.FindProperty("reputationFill").objectReferenceValue = reputationFill;
            so.FindProperty("reputationPercent").objectReferenceValue = reputationPct;
            var known = so.FindProperty("knownCrops");
            known.arraySize = crops.Length;
            for (int i = 0; i < crops.Length; i++) known.GetArrayElementAtIndex(i).objectReferenceValue = crops[i];
            so.FindProperty("healthPercent").objectReferenceValue = healthPct;
            so.FindProperty("staminaPercent").objectReferenceValue = staminaPct;
            so.FindProperty("heatPercent").objectReferenceValue = heatPct;
            so.FindProperty("courierTimerLabel").objectReferenceValue = courierTimerLabel;
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildGtaMiniMap(canvasGo.transform, font);
        }

        private static void BuildGtaMiniMap(Transform canvas, Font font)
        {
            GameObject panel = new GameObject("GTA_Minimap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(canvas, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(20f, 20f);
            panelRect.sizeDelta = new Vector2(310f, 220f);
            panel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.03f, 0.72f);

            GameObject mapObject = new GameObject("Map", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            RectTransform mapRect = mapObject.GetComponent<RectTransform>();
            mapRect.SetParent(panelRect, false);
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = Vector2.one;
            mapRect.offsetMin = new Vector2(7f, 24f);
            mapRect.offsetMax = new Vector2(-7f, -28f);
            RawImage mapImage = mapObject.GetComponent<RawImage>();
            mapImage.color = new Color(1f, 1f, 1f, 0.82f);
            mapImage.raycastTarget = false;

            GameObject blipsObject = new GameObject("Blips", typeof(RectTransform));
            RectTransform blips = blipsObject.GetComponent<RectTransform>();
            blips.SetParent(mapRect, false);
            blips.anchorMin = Vector2.zero;
            blips.anchorMax = Vector2.one;
            blips.offsetMin = blips.offsetMax = Vector2.zero;

            GameObject overlayObject = new GameObject("PoliceHeatOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.SetParent(mapRect, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
            Image overlay = overlayObject.GetComponent<Image>();
            overlay.raycastTarget = false;
            overlayObject.SetActive(false);

            Text arrow = CreateLabel(mapRect, "▲", 24, Vector2.zero, font);
            arrow.name = "PlayerDirection";
            arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            arrow.rectTransform.sizeDelta = new Vector2(34f, 34f);
            arrow.alignment = TextAnchor.MiddleCenter;
            arrow.color = Color.white;

            Text wanted = CreateLabel(panelRect, string.Empty, 15, new Vector2(0f, 3f), font);
            wanted.name = "PoliceHeatLabel";
            wanted.rectTransform.anchorMin = wanted.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            wanted.rectTransform.pivot = new Vector2(0.5f, 0f);
            wanted.rectTransform.sizeDelta = new Vector2(280f, 21f);
            wanted.alignment = TextAnchor.MiddleCenter;
            wanted.color = new Color(1f, 0.86f, 0.86f);
            wanted.gameObject.SetActive(false);

            Text missionHint = CreateLabel(panelRect, "YELLOW = CURRENT MISSION", 14, new Vector2(0f, -3f), font);
            missionHint.name = "MissionMarkerHint";
            missionHint.rectTransform.anchorMin = missionHint.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            missionHint.rectTransform.pivot = new Vector2(0.5f, 1f);
            missionHint.rectTransform.sizeDelta = new Vector2(290f, 22f);
            missionHint.alignment = TextAnchor.MiddleCenter;
            missionHint.color = new Color(1f, 0.78f, 0.12f);

            GameObject cameraObject = new GameObject("GTA_MinimapCamera");
            Camera miniCamera = cameraObject.AddComponent<Camera>();
            miniCamera.enabled = true;
            miniCamera.depth = -20f;
            miniCamera.nearClipPlane = 0.5f;
            miniCamera.farClipPlane = 180f;

            GtaMiniMapController controller = panel.AddComponent<GtaMiniMapController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("mapCamera").objectReferenceValue = miniCamera;
            so.FindProperty("mapImage").objectReferenceValue = mapImage;
            so.FindProperty("blipRoot").objectReferenceValue = blips;
            so.FindProperty("wantedOverlay").objectReferenceValue = overlay;
            so.FindProperty("wantedLabel").objectReferenceValue = wanted;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterGrandBayMiniMapMarkers()
        {
            Color shop = new Color(0.35f, 0.95f, 0.45f);
            Color mission = new Color(1f, 0.78f, 0.12f);
            Color police = new Color(0.20f, 0.55f, 1f);
            Color gang = new Color(0.95f, 0.18f, 0.22f);
            Color community = new Color(0.92f, 0.92f, 0.88f);
            Color property = new Color(0.25f, 0.90f, 0.78f);

            foreach (string name in new[] { "Stall_FARM SHOP", "Stall_PRODUCE BUYER", "Stall_FOOD", "Stall_CLOTHES", "Stall_PHARMACY", "Stall_LAND AND SURVEYS", "Stall_CAR DEALER" })
                AddMiniMapMarker(name, MiniMapMarkerKind.Shop, name.Replace("Stall_", string.Empty), shop);
            AddMiniMapMarker("NPC_BossJ", MiniMapMarkerKind.Mission, "Boss J", mission, "M4");
            AddMiniMapMarker("NPC_Vagrant", MiniMapMarkerKind.Person, "Paro", mission, "M7");
            AddMiniMapMarker("NPC_BossC", MiniMapMarkerKind.Mission, "Boss C", mission, "M10W");
            AddMiniMapMarker("NPC_Normy", MiniMapMarkerKind.Person, "Normy", police, "M12");
            AddMiniMapMarker("NPC_Police", MiniMapMarkerKind.Police, "Police", police);
            AddMiniMapMarker("NPC_PoliceShops", MiniMapMarkerKind.Police, "Police", police);
            AddMiniMapMarker("NPC_PoliceEast", MiniMapMarkerKind.Police, "Police", police);
            AddMiniMapMarker("FarmSafehouse_Building", MiniMapMarkerKind.Safehouse, "Highland Safehouse", property);
            AddMiniMapMarker("LalayHouse", MiniMapMarkerKind.Safehouse, "Lalay Safehouse", property, "M12");
            AddMiniMapMarker("FarmPlot_00", MiniMapMarkerKind.Farm, "Highland Farm", shop);
            // MINI-119, user: "point marker for the interbreeding building."
            AddMiniMapMarker("BreedingStation", MiniMapMarkerKind.Farm, "Breeding Station", shop);
            AddMiniMapMarker("GrandBay_Catholic_Church_Graybox", MiniMapMarkerKind.Church, "Church", community);
            AddMiniMapMarker("NPC_Brakes", MiniMapMarkerKind.Person, "Brakes", community);
            AddMiniMapMarker("NPC_BoatMan", MiniMapMarkerKind.Boat, "Boat Man", community, "M11W");
            AddMiniMapMarker("MooredBoat", MiniMapMarkerKind.Boat, "Guadeloupe Boat", community, "M11W");
            AddMiniMapMarker("NPC_GangRecruiter", MiniMapMarkerKind.Gang, "Not Ah Word", gang, "M15");
            for (int i = 0; i < 4; i++) AddMiniMapMarker($"NPC_DogLife_{i}", MiniMapMarkerKind.Gang, "Dog Life", gang);
            AddMiniMapMarker("NotAhWord_Zoomy", MiniMapMarkerKind.Gang, "Not Ah Word", gang, "M10W");
        }

        private static void AddMiniMapMarker(string objectName, MiniMapMarkerKind kind, string label, Color colour, string requiredMissionId = null)
        {
            Transform target = null;
            foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform.name != objectName) continue;
                target = transform;
                break;
            }
            if (target == null) return;
            GtaMiniMapMarker marker = target.GetComponent<GtaMiniMapMarker>();
            if (marker == null) marker = target.gameObject.AddComponent<GtaMiniMapMarker>();
            marker.Configure(kind, label, colour, requiredMissionId);
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
            var bannerBackground = bannerGo.AddComponent<Image>();
            bannerBackground.color = new Color(0f, 0f, 0f, 0f);
            bannerBackground.raycastTarget = false;

            var bannerTextGo = new GameObject("Text");
            bannerTextGo.transform.SetParent(bannerGo.transform, false);
            var bannerTextRect = bannerTextGo.AddComponent<RectTransform>();
            bannerTextRect.anchorMin = Vector2.zero;
            bannerTextRect.anchorMax = Vector2.one;
            bannerTextRect.offsetMin = new Vector2(36f, 22f);
            bannerTextRect.offsetMax = new Vector2(-36f, -22f);
            var banner = bannerTextGo.AddComponent<Text>();
            banner.font = font;
            banner.fontSize = 44;
            banner.fontStyle = FontStyle.Bold;
            banner.alignment = TextAnchor.MiddleCenter;
            banner.color = new Color(1f, 1f, 1f, 0f);
            banner.horizontalOverflow = HorizontalWrapMode.Wrap;
            banner.verticalOverflow = VerticalWrapMode.Overflow;

            var missionHud = canvasGo.AddComponent<MissionHUD>();
            var mhSo = new SerializedObject(missionHud);
            mhSo.FindProperty("objectiveText").objectReferenceValue = objectiveText;
            mhSo.FindProperty("bannerText").objectReferenceValue = banner;
            mhSo.FindProperty("bannerBackground").objectReferenceValue = bannerBackground;
            mhSo.FindProperty("bannerPanelRect").objectReferenceValue = bRect;
            mhSo.FindProperty("objectivePanel").objectReferenceValue = objectivePanel;
            // MINI-073: without this, the card's auto-resize (ResizeObjectiveCard)
            // silently does nothing - it was written and left unwired in the same
            // pass, caught immediately rather than shipped broken.
            mhSo.FindProperty("objectivePanelRect").objectReferenceValue = opRect;
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

            // MINI-119 follow-up, user: "i want highland to start from
            // after the bridge. lalay would be the entire road" then
            // "lalay should end by the car dealer." The bridge is
            // Bridge_03_user_highland_lalay_inroad (~80.93, -146.67 -
            // literally named for this transition and the closest
            // bridge to the farm). NPC_CarDealer sits further out at
            // (149.30, -180.35), past the bridge but on a lower/south
            // road branch distinct from the farm inroad (which climbs
            // toward the safehouse at z~-108 to -134) - so a single big
            // Lalay circle reaching the dealer would sit closer to the
            // farm cluster than Highland's own tight circle
            // (AreaNameDisplay picks whichever zone centre is nearest),
            // wrongly relabeling the farm as Lalay. Lalay is instead TWO
            // circles - west of the bridge (market/Dog Life/LalayHouse)
            // and bridge-to-dealer - both named "Lalay" (name
            // collisions are fine, ResolveArea just returns whichever
            // is nearest). Highland stays a tight circle on the farm/
            // safehouse cluster only, starting at the bridge, never
            // reaching the dealer.
            zonesProp.arraySize = 3;
            var lalayWest = zonesProp.GetArrayElementAtIndex(0);
            lalayWest.FindPropertyRelative("areaName").stringValue = "Lalay";
            lalayWest.FindPropertyRelative("center").vector3Value = new Vector3(-20f, 0f, -140f);
            lalayWest.FindPropertyRelative("radius").floatValue = 85f;

            var lalayEast = zonesProp.GetArrayElementAtIndex(1);
            lalayEast.FindPropertyRelative("areaName").stringValue = "Lalay";
            lalayEast.FindPropertyRelative("center").vector3Value = new Vector3(115f, 0f, -165f);
            lalayEast.FindPropertyRelative("radius").floatValue = 45f;

            var highland = zonesProp.GetArrayElementAtIndex(2);
            highland.FindPropertyRelative("areaName").stringValue = "Highland";
            highland.FindPropertyRelative("center").vector3Value = new Vector3(104f, 0f, -128f);
            highland.FindPropertyRelative("radius").floatValue = 40f;

            aso.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ShopPanelController BuildShopPanel(
            GameObject canvasGo, string panelName, string title, ShopItemDefinition[] stock, Font font,
            bool resaleMode = false)
        {
            // MINI-082, user: "make it smaller or more dynamic." Was a
            // fixed 900x640 block regardless of how few lines a given shop
            // actually has - shrunk to something less overwhelming; the
            // "dynamic" half is the fade in ShopPanelController itself
            // (CanvasGroup added below), not a size that grows/shrinks
            // per-shop, which would need a layout-group rework.
            var panel = CreateModalPanel(canvasGo.transform, panelName, new Vector2(620f, 460f));
            var canvasGroup = panel.AddComponent<CanvasGroup>();
            var text = CreateModalText(panel.transform, font, 23);

            var shop = canvasGo.AddComponent<ShopPanelController>();
            var so = new SerializedObject(shop);
            so.FindProperty("shopTitle").stringValue = title;
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("bodyText").objectReferenceValue = text;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
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
