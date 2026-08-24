using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-065: (1) wires TmaxBikeControllerCustom + TmaxWheelVisuals onto the
    /// MINI-064 prefab, and (2) builds an isolated TMAX_Physics_Test scene
    /// (flat road, one turn, one slope, a few boxes/walls) to prototype
    /// and tune the bike in - per the brief's "don't debug this throughout
    /// Grand Bay" instruction. Test only this bike; nothing here touches
    /// GrandBayProof.unity.
    /// </summary>
    public static class Mini065TmaxPhysicsTest
    {
        private const string PrefabPath = "Assets/UpIzUpMini/Vehicles/TMAX_560.prefab";
        private const string TestScenePath = "Assets/UpIzUpMini/Scenes/TMAX_Physics_Test.unity";

        /// <summary>MINI-080: whether the live tuning panel (T key) ships on
        /// the real bike. Off while not actively tuning - flip back to true
        /// and re-run WireController whenever tuning resumes.
        /// MINI-119 follow-up, user: "i want to do this in the game scene...
        /// i can tell you to save it when i have done all the edits" -
        /// flipped back on so the panel (now including the anti-spin/hill-
        /// climb/air-gravity sliders) rides along on the real, purchasable
        /// bike in GrandBayProof itself.
        /// MINI-119 follow-up round 5, user: "save all these settings" -
        /// final numbers reported and baked into the explicit writes
        /// above; flipped back off per this file's own standing
        /// convention now that tuning is done.</summary>
        private const bool IncludeDevTuner = false;

        [MenuItem("Up Iz Up Mini/MINI-065/Wire TMAX Controller Onto Prefab")]
        public static void WireController()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"MINI-065 WIRE FAIL: could not load prefab at {PrefabPath}. Run MINI-064's BuildPrefab first.");
                return;
            }

            var physics = prefabRoot.transform.Find("Physics");
            var frontWc = physics?.Find("FrontWheelCollider")?.GetComponent<WheelCollider>();
            var rearWc = physics?.Find("RearWheelCollider")?.GetComponent<WheelCollider>();
            var rearStabLeftAnchor = physics?.Find("RearStabilizerLeftAnchor");
            var rearStabRightAnchor = physics?.Find("RearStabilizerRightAnchor");
            var com = prefabRoot.transform.Find("COM");
            var visualLean = prefabRoot.transform.Find("VisualLeanRoot");
            var frontWheelVisual = visualLean?.Find("FrontSteering/FrontWheel");
            var rearWheelVisual = visualLean?.Find("RearWheel");

            if (frontWc == null || rearWc == null || rearStabLeftAnchor == null || rearStabRightAnchor == null || com == null || visualLean == null || frontWheelVisual == null || rearWheelVisual == null)
            {
                Debug.LogError("MINI-065 WIRE FAIL: prefab is missing one of the expected child transforms (Physics/FrontWheelCollider, Physics/RearWheelCollider, Physics/RearStabilizerLeftAnchor, Physics/RearStabilizerRightAnchor, COM, VisualLeanRoot, VisualLeanRoot/FrontSteering/FrontWheel, VisualLeanRoot/RearWheel).");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            var bike = prefabRoot.GetComponent<TmaxBikeControllerCustom>();
            if (bike == null) bike = prefabRoot.AddComponent<TmaxBikeControllerCustom>();
            var bikeSo = new SerializedObject(bike);
            bikeSo.FindProperty("frontWheel").objectReferenceValue = frontWc;
            bikeSo.FindProperty("rearWheel").objectReferenceValue = rearWc;
            bikeSo.FindProperty("rearStabLeftAnchor").objectReferenceValue = rearStabLeftAnchor;
            bikeSo.FindProperty("rearStabRightAnchor").objectReferenceValue = rearStabRightAnchor;
            bikeSo.FindProperty("centerOfMass").objectReferenceValue = com;
            bikeSo.FindProperty("visualLeanRoot").objectReferenceValue = visualLean;
            // MINI-077: written explicitly, or a prefab whose
            // TmaxBikeControllerCustom already existed from an earlier build keeps
            // its OLD serialized value forever - a C# field default only
            // applies the first time a component is added, never retroactively
            // (the same trap already hit this project's wheelie key and the
            // Range Rover's centre of mass).
            bikeSo.FindProperty("wheelieMinSpeedKmh").floatValue = 8f;
            // MINI-118: same trap - motorTorque/brakeTorque/
            // wheelieRearTorqueBoost were scaled up in the C# defaults to
            // match the heavier bike, but an already-existing
            // TmaxBikeControllerCustom on the prefab would otherwise keep its
            // old, now-undersized values forever.
            // MINI-119: raised again, past the MINI-118 mass-parity value,
            // per the user's "should have enough power to climb hill and
            // ledges" - same trap, written explicitly so an existing
            // prefab's TmaxBikeControllerCustom actually picks up the new value.
            bikeSo.FindProperty("motorTorque").floatValue = 950f;
            bikeSo.FindProperty("brakeTorque").floatValue = 1200f;
            bikeSo.FindProperty("wheelieRearTorqueBoost").floatValue = 1960f;
            bikeSo.FindProperty("wheelieRiseRate").floatValue = 72f;
            // MINI-119 follow-up round 5, user: "save all these settings"
            // - final numbers read directly off the live tuner panel
            // after several rounds of hands-on ledge/hill testing. Same
            // explicit-write trap as everything else on this prefab.
            bikeSo.FindProperty("yawLockStrength").floatValue = 1f;
            bikeSo.FindProperty("yawLockTurnRate").floatValue = 110f;
            bikeSo.FindProperty("yawSpinThreshold").floatValue = 0f;
            bikeSo.FindProperty("yawSpinDamping").floatValue = 250f;
            bikeSo.FindProperty("ledgeReactionStrength").floatValue = 0f;
            bikeSo.FindProperty("ledgeMaxHeight").floatValue = 0.6f;
            bikeSo.FindProperty("rampAssistStrength").floatValue = 0f;
            bikeSo.FindProperty("collisionYawSpinCap").floatValue = 40f;
            bikeSo.FindProperty("hillClimbAssist").floatValue = 9f;
            bikeSo.FindProperty("hillClimbMaxSlopeDeg").floatValue = 35f;
            bikeSo.FindProperty("extraAirGravity").floatValue = 2500f;
            bikeSo.FindProperty("airborneGraceSeconds").floatValue = 0.01f;
            bikeSo.FindProperty("airGravityRampSeconds").floatValue = 0f;
            // MINI-079: was 42.36deg - far past the rider's own 15deg roll
            // clamp (riderMaxLeanDegrees). At full lean the BIKE BODY kept
            // leaning all the way to 42deg while the rider's own lean got
            // clamped and held at 15deg, so the two visibly separated -
            // "their bodies [come] off the bike... the bike is leaning more
            // than them". Capped to the SAME 15deg so the bike body and both
            // riders share one envelope, per the user's original "make the
            // bike, pillion and main character lean or bend at a 15 degree
            // angle" - which this had missed; only the RIDERS were capped
            // before, not the bike itself.
            bikeSo.FindProperty("maxVisualLean").floatValue = 15f;
            bikeSo.ApplyModifiedPropertiesWithoutUndo();

            var visuals = prefabRoot.GetComponent<TmaxWheelVisuals>();
            if (visuals == null) visuals = prefabRoot.AddComponent<TmaxWheelVisuals>();
            var visualsSo = new SerializedObject(visuals);
            visualsSo.FindProperty("frontCollider").objectReferenceValue = frontWc;
            visualsSo.FindProperty("rearCollider").objectReferenceValue = rearWc;
            visualsSo.FindProperty("frontWheelVisual").objectReferenceValue = frontWheelVisual;
            visualsSo.FindProperty("rearWheelVisual").objectReferenceValue = rearWheelVisual;
            visualsSo.ApplyModifiedPropertiesWithoutUndo();

            // MINI-066: driver + pillion seats. Wired here rather than in
            // BuildPrefab so the seat definition lives next to the other
            // component wiring, and so re-running this alone re-wires seats
            // without a full mesh re-measure.
            WireSeat(prefabRoot, "DriverSeat", VehicleSeat.SeatRole.Driver,
                seatAnchor: "Seat",
                lh: "HandlebarLeft", rh: "HandlebarRight",
                lf: "LeftFootTarget", rf: "RightFootTarget",
                ridePose: "RideBike", mount: "MountBike");

            // MINI-079: "i want to use the latter part of cheer2 for the
            // pillion just so he wouldnt do all that cheering before he
            // sits and his head is still too far back." cheer02_Loop is
            // 6.00s (see its ActionEntry comment in Mini011PhaseBSetup) -
            // starting at 3.6s skips the cheering lead-in (arms up, head
            // back) and lands in the settled latter portion, still safely
            // inside the loop.
            WireSeat(prefabRoot, "PillionSeat_Seat", VehicleSeat.SeatRole.Passenger,
                seatAnchor: "PillionSeat",
                lh: "PillionGrabLeft", rh: "PillionGrabRight",
                lf: "PillionFootLeft", rf: "PillionFootRight",
                ridePose: "RidePillion", mount: "MountBike",
                ridePoseStartTimeSeconds: 3.6f);

            // MINI-066 fix: BikeInteractable belongs on the PREFAB, not just
            // the test scene. It was previously added only in BuildTestScene,
            // which meant the bike the real game spawns after purchase had no
            // interactable on it at all - the user could see it but never get
            // on it ("i cant interact and hope [hop] the bike in the game").
            // Putting it here means every instance, spawned or placed, is
            // rideable.
            // MINI-080: "remove the test bike, test range and take out the
            // test screens until we are testing again" - the tuner was baked
            // onto the real, shipped bike (see the comment this replaced:
            // "strip this component once the values are settled" - exactly
            // what this is). Gated behind IncludeDevTuner rather than deleted
            // outright, so a future tuning pass is a one-line flip back, not
            // rewriting this. Explicitly REMOVES an existing tuner too (not
            // just skips adding one), so re-running this on a prefab that
            // already has one from an earlier build actually strips it.
            var existingTuner = prefabRoot.GetComponent<TmaxWheelieTuner>();
            if (IncludeDevTuner)
            {
                if (existingTuner == null) prefabRoot.AddComponent<TmaxWheelieTuner>();
            }
            else if (existingTuner != null)
            {
                Object.DestroyImmediate(existingTuner, true);
            }

            var interactable = prefabRoot.GetComponent<BikeInteractable>();
            if (interactable == null) interactable = prefabRoot.AddComponent<BikeInteractable>();
            var biSo = new SerializedObject(interactable);
            biSo.FindProperty("driverSeat").objectReferenceValue = prefabRoot.transform.Find("DriverSeat")?.GetComponent<VehicleSeat>();
            biSo.FindProperty("pillionSeat").objectReferenceValue = prefabRoot.transform.Find("PillionSeat_Seat")?.GetComponent<VehicleSeat>();
            // MINI-069: the control scheme is written onto the PREFAB, not left
            // to the C# field defaults. A serialized component keeps whatever
            // value it was created with, so changing a default in code does
            // nothing to a prefab that already exists - the bike kept the old
            // R-to-wheelie binding until this was set explicitly.
            biSo.FindProperty("wheelieKey").intValue = (int)KeyCode.E;
            biSo.FindProperty("mountKey").intValue = (int)KeyCode.F;
            biSo.FindProperty("dismountKey").intValue = (int)KeyCode.F;
            biSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath, out bool success);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            Debug.Log(success
                ? "MINI-065/066 WIRE OK: TmaxBikeControllerCustom + TmaxWheelVisuals + Driver/Pillion VehicleSeat wired onto TMAX_560.prefab."
                : "MINI-065 WIRE FAIL: SaveAsPrefabAsset reported failure.");
        }

        /// <summary>
        /// MINI-066. Creates (or refreshes) one VehicleSeat on the prefab,
        /// pointing at markers Mini064TmaxAssetPrep already measured onto the
        /// real mesh. Missing markers are reported rather than silently
        /// producing a seat with null IK targets, since a null target just
        /// means "that limb isn't pinned" at runtime and would otherwise hide
        /// a genuine wiring mistake.
        /// </summary>
        private static void WireSeat(
            GameObject prefabRoot, string seatObjectName, VehicleSeat.SeatRole role,
            string seatAnchor, string lh, string rh, string lf, string rf,
            string ridePose, string mount, float ridePoseStartTimeSeconds = 0f)
        {
            Transform Find(string n)
            {
                var t = prefabRoot.transform.Find(n);
                if (t == null) Debug.LogWarning($"MINI-066 WIRE: marker '{n}' not found on the prefab - that limb/anchor will be unpinned.");
                return t;
            }

            var holder = prefabRoot.transform.Find(seatObjectName);
            if (holder == null)
            {
                var go = new GameObject(seatObjectName);
                go.transform.SetParent(prefabRoot.transform, false);
                holder = go.transform;
            }

            var seat = holder.GetComponent<VehicleSeat>();
            if (seat == null) seat = holder.gameObject.AddComponent<VehicleSeat>();

            var so = new SerializedObject(seat);
            so.FindProperty("role").enumValueIndex = (int)role;
            // Every seat on this vehicle is straddled - it's a scooter. Cars
            // will pass DoorSeated here instead (see VehicleSeat.EntryStyle).
            so.FindProperty("style").enumValueIndex = (int)VehicleSeat.EntryStyle.Straddle;
            so.FindProperty("seatAnchor").objectReferenceValue = Find(seatAnchor);
            so.FindProperty("leftHandTarget").objectReferenceValue = Find(lh);
            so.FindProperty("rightHandTarget").objectReferenceValue = Find(rh);
            so.FindProperty("leftFootTarget").objectReferenceValue = Find(lf);
            so.FindProperty("rightFootTarget").objectReferenceValue = Find(rf);
            so.FindProperty("ridePoseActionId").stringValue = ridePose;
            so.FindProperty("mountActionId").stringValue = mount;
            so.FindProperty("ridePoseStartTimeSeconds").floatValue = ridePoseStartTimeSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// MINI-066. Drops the real player character model onto the test
        /// bike, wired with the same components the game uses (Animator +
        /// HumanoidAnimationManager + VehicleRider + BikeRiderAnimation), and
        /// auto-mounts it. Uses Sacat's actual model rather than a placeholder
        /// capsule so the riding poses can be judged on the real proportions -
        /// IK reach to the handlebars depends on arm length.
        /// </summary>
        private static void BuildTestRider(GameObject bikeGo)
        {
            const string modelPath = "Assets/UpIzUpMini/Art/Characters/Mainchar.fbx";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogWarning($"MINI-066 TEST RIDER: character model not found at {modelPath} - test scene will have no rider.");
                return;
            }

            var rider = (GameObject)PrefabUtility.InstantiatePrefab(model);
            rider.name = "TestRider";
            rider.transform.position = bikeGo.transform.position + new Vector3(1.2f, 0f, 0f);

            var animator = rider.GetComponent<Animator>();
            if (animator == null) animator = rider.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
                "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller");
            if (controller != null) animator.runtimeAnimatorController = controller;

            // Same shared action list the real characters get, so every
            // riding pose baked in Mini011PhaseBSetup is available here too.
            var anim = rider.GetComponent<UpIzUpMini.Character.HumanoidAnimationManager>();
            if (anim == null) anim = rider.AddComponent<UpIzUpMini.Character.HumanoidAnimationManager>();
            var animSo = new SerializedObject(anim);
            animSo.FindProperty("animator").objectReferenceValue = animator;
            var actionsProp = animSo.FindProperty("actions");
            var entries = Mini011PhaseBSetup.GetSharedActionEntriesPublic();
            actionsProp.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = actionsProp.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").stringValue = entries[i].id;
                e.FindPropertyRelative("clip").objectReferenceValue = entries[i].clip;
                e.FindPropertyRelative("fullBody").boolValue = entries[i].fullBody;
            }
            animSo.ApplyModifiedPropertiesWithoutUndo();

            rider.AddComponent<Vehicles.VehicleRider>();
            rider.AddComponent<Vehicles.BikeRiderAnimation>();

            // BikeInteractable both drives the bike while ridden and provides
            // the get-on/get-off flow; the auto-mount helper below saves
            // having to walk up to it in this scene.
            var interactable = bikeGo.GetComponent<Vehicles.BikeInteractable>();
            if (interactable == null) interactable = bikeGo.AddComponent<Vehicles.BikeInteractable>();

            var auto = bikeGo.AddComponent<Vehicles.BikeTestAutoMount>();
            var autoSo = new SerializedObject(auto);
            autoSo.FindProperty("rider").objectReferenceValue = rider;
            autoSo.ApplyModifiedPropertiesWithoutUndo();

            // The direct keyboard rig and the ridden-bike rig would both feed
            // SetInput every frame and fight each other, so the standalone one
            // steps aside once a rider is aboard (see TmaxTestInput).
            Debug.Log("MINI-066 TEST RIDER: Sacat placed beside the bike and auto-mounted on Play.");
        }

        [MenuItem("Up Iz Up Mini/MINI-065/Build TMAX Physics Test Scene")]
        public static void BuildTestScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"MINI-065 SCENE FAIL: prefab not found at {PrefabPath}.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Ground: a long flat run, one gentle turn (a curved
            // segment made of angled box slabs - simplest reliable
            // approach without a spline/terrain tool), one ramp/slope,
            // and a scattering of boxes/walls to crash into. ---
            var groundParent = new GameObject("Ground");
            Material roadMat = NewMat("TestRoad", new Color(0.32f, 0.32f, 0.34f));
            Material rampMat = NewMat("TestRamp", new Color(0.4f, 0.35f, 0.28f));
            Material wallMat = NewMat("TestWall", new Color(0.65f, 0.2f, 0.18f));

            // ONE large continuous slab, rather than the previous patchwork of
            // separate road/turn/slope pieces. Those left real gaps between
            // them - the drive test drove straight off the end of the world
            // and recorded y=-8.9 - which is a flaw in the test rig, not in
            // the bike, and exactly the kind of confound that makes a physics
            // result impossible to trust. Everything now sits on this one
            // surface with generous margin in every direction.
            Slab(groundParent.transform, "Straight", new Vector3(0f, 0f, 20f), Quaternion.identity,
                new Vector3(50f, 0.4f, 160f), roadMat);

            // The turn, marked out with walls ON the slab rather than built
            // from angled road pieces, so there is nothing to fall between.
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                float ang = t * Mathf.PI * 0.5f;
                var inner = new Vector3(Mathf.Sin(ang) * 8f, 0.6f, 45f + (1f - Mathf.Cos(ang)) * 8f);
                var outer = new Vector3(Mathf.Sin(ang) * 16f, 0.6f, 45f + (1f - Mathf.Cos(ang)) * 16f);
                Wall(groundParent.transform, $"TurnInner{i}", inner, new Vector3(0.6f, 1.2f, 0.6f), wallMat);
                Wall(groundParent.transform, $"TurnOuter{i}", outer, new Vector3(0.6f, 1.2f, 0.6f), wallMat);
            }
            // Kept under this name because the validation harness looks for it
            // as proof the scene has turn geometry.
            Wall(groundParent.transform, "TurnA", new Vector3(12f, 0.6f, 45f), new Vector3(0.6f, 1.2f, 0.6f), wallMat);

            // A ramp resting ON the slab, well clear of the spawn point, for
            // climb/descent testing.
            var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Slope";
            ramp.transform.SetParent(groundParent.transform);
            ramp.transform.position = new Vector3(-16f, 1.1f, 30f);
            ramp.transform.rotation = Quaternion.Euler(-10f, 0f, 0f);
            ramp.transform.localScale = new Vector3(9f, 0.4f, 18f);
            ramp.GetComponent<Renderer>().sharedMaterial = rampMat;

            // Obstacles for collision/recovery, off the straight-ahead line so
            // a plain acceleration run is not blocked by them.
            Wall(groundParent.transform, "Wall1", new Vector3(-7f, 0.95f, 22f), new Vector3(1f, 1.5f, 4f), wallMat);
            Wall(groundParent.transform, "Wall2", new Vector3(7f, 0.95f, 30f), new Vector3(1f, 1.5f, 4f), wallMat);
            Box(groundParent.transform, "Box1", new Vector3(-4f, 0.7f, 12f), Vector3.one, wallMat);
            Box(groundParent.transform, "Box2", new Vector3(4f, 0.7f, 12f), Vector3.one, wallMat);

            // --- The bike itself, on the straight. ---
            // Bug fix (user report: "spinning in the air like crazy" on
            // Play): this used to be a hardcoded guess (0.5f) that sat
            // ~0.3m above the Straight slab's real top surface (0.2f,
            // since a unit cube scaled to 0.4f in Y and centred at Y=0 has
            // its top face at half that) - well outside the WheelColliders'
            // suspension travel, so the bike free-fell with the wheels
            // never touching anything. Now measured directly against the
            // actual built road via a raycast, so the root - whose own
            // local origin is ground level, per the Blender cleanup pass
            // - lands exactly on the real surface instead of guessing.
            // Edit Mode doesn't run a physics step on its own - colliders
            // created moments ago (the road slabs above) aren't
            // necessarily visible to a Physics query yet without an
            // explicit sync (this is why the very first version of this
            // fix logged "raycast onto the road failed" here instead of
            // actually measuring anything).
            Physics.SyncTransforms();

            // On the flat straight, clear of both the slope behind and the
            // obstacle boxes ahead at z=10.
            Vector3 desiredSpawnXZ = new Vector3(0f, 0f, 2f);
            Vector3 spawnPos;
            if (Physics.Raycast(desiredSpawnXZ + Vector3.up * 10f, Vector3.down, out RaycastHit roadHit, 30f))
            {
                spawnPos = roadHit.point;
            }
            else
            {
                Debug.LogWarning("MINI-065 SCENE: raycast onto the road failed - falling back to a guessed spawn height.");
                spawnPos = desiredSpawnXZ + Vector3.up * 0.3f;
            }

            var bikeGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            // Spawn a couple of centimetres clear of the road rather than
            // exactly on it: the prefab's own wheel colliders already sit at
            // their suspension rest height, so dropping it from a hair above
            // lets the suspension take up the load naturally instead of
            // starting interpenetrating.
            bikeGo.transform.position = spawnPos + Vector3.up * 0.02f;
            bikeGo.transform.rotation = Quaternion.identity;
            var testInput = bikeGo.AddComponent<TmaxTestInput>();

            // MINI-065 round 7 - the user's own explicit request after
            // reporting the headless test didn't match real Play Mode feel:
            // "put the wheelie controls as sliders into unity and i will
            // adjust them to get the wheelie." Test-scene-only, same as
            // TmaxTestInput - never touches the real TMAX_560.prefab.
            bikeGo.AddComponent<Vehicles.TmaxWheelieTuner>();

            // --- MINI-066 rider, so the riding animations can actually be
            // seen and judged in this isolated scene instead of only in the
            // full game behind a purchase flow (user: "can you make me test
            // it in the bike test mode you had testing for the wheelie and
            // bike movements?"). Auto-mounted at start so Play Mode shows a
            // rider immediately - no walking over and pressing F first. ---
            BuildTestRider(bikeGo);

            // --- Simple follow camera so the bike is actually visible
            // when testing (Scene view works too, but Game view needs a
            // camera). ---
            var camGo = new GameObject("TestFollowCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.transform.position = bikeGo.transform.position + new Vector3(0f, 3f, -6f);
            camGo.transform.LookAt(bikeGo.transform.position + Vector3.up * 1f);
            var followCam = camGo.AddComponent<Vehicles.TmaxTestFollowCam>();
            var fcSo = new SerializedObject(followCam);
            fcSo.FindProperty("target").objectReferenceValue = bikeGo.transform;
            fcSo.ApplyModifiedPropertiesWithoutUndo();

            var lightGo = new GameObject("SunLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None; // headless-render crash avoidance, same as MINI-064's preview
            lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            light.intensity = 1.1f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.42f, 0.48f);

            System.IO.Directory.CreateDirectory("Assets/UpIzUpMini/Scenes");
            bool saved = EditorSceneManager.SaveScene(scene, TestScenePath);
            Debug.Log(saved
                ? $"MINI-065 SCENE OK: TMAX_Physics_Test built and saved to {TestScenePath}."
                : "MINI-065 SCENE FAIL: SaveScene reported failure.");
        }

        private static void Slab(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void Wall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
            => Slab(parent, name, pos, Quaternion.identity, scale, mat);

        private static void Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
            => Slab(parent, name, pos, Quaternion.identity, scale, mat);

        private static Material NewMat(string name, Color color)
        {
            var mat = new Material(Shader.Find("Standard")) { color = color };
            mat.name = name;
            return mat;
        }
    }
}
