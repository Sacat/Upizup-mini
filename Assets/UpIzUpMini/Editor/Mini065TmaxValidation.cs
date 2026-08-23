using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-064/065: structural validation against the real built prefab
    /// and test scene. Cannot exercise actual FixedUpdate physics
    /// (WheelCollider forces, Rigidbody integration) since Play Mode does
    /// not tick in this project's batch-mode environment - the same
    /// limitation on record for every timer/coroutine-driven system since
    /// MINI-041. This proves wiring/scale/hierarchy correctness instead:
    /// everything a human would need working before their first Play
    /// Mode test even makes sense to attempt.
    /// </summary>
    public static class Mini065TmaxValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-065/Validate TMAX Prefab + Test Scene")]
        public static void Validate()
        {
            bool pass = true;
            string fail = null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Vehicles/TMAX_560.prefab");
            if (prefab == null)
            {
                Debug.LogError("MINI-065 VALIDATION FAIL: TMAX_560.prefab not found.");
                return;
            }

            // --- Hierarchy shape. ---
            var physics = prefab.transform.Find("Physics");
            var frontWc = physics?.Find("FrontWheelCollider")?.GetComponent<WheelCollider>();
            var rearWc = physics?.Find("RearWheelCollider")?.GetComponent<WheelCollider>();
            var com = prefab.transform.Find("COM");
            var visualLean = prefab.transform.Find("VisualLeanRoot");
            var body = visualLean?.Find("Body");
            var frontSteering = visualLean?.Find("FrontSteering");
            var frontWheelVisual = frontSteering?.Find("FrontWheel");
            var rearWheelVisual = visualLean?.Find("RearWheel");
            var seat = prefab.transform.Find("Seat");
            var handL = prefab.transform.Find("HandlebarLeft");
            var handR = prefab.transform.Find("HandlebarRight");
            var footL = prefab.transform.Find("LeftFootTarget");
            var footR = prefab.transform.Find("RightFootTarget");
            var camTarget = prefab.transform.Find("CameraTarget");

            Check(ref pass, ref fail, physics != null, "expected a 'Physics' child.");
            Check(ref pass, ref fail, frontWc != null, "expected Physics/FrontWheelCollider with a WheelCollider.");
            Check(ref pass, ref fail, rearWc != null, "expected Physics/RearWheelCollider with a WheelCollider.");
            Check(ref pass, ref fail, com != null, "expected a 'COM' child.");
            Check(ref pass, ref fail, visualLean != null, "expected a 'VisualLeanRoot' child.");
            Check(ref pass, ref fail, body != null, "expected VisualLeanRoot/Body (the cleaned mesh).");
            Check(ref pass, ref fail, frontWheelVisual != null, "expected VisualLeanRoot/FrontSteering/FrontWheel.");
            Check(ref pass, ref fail, rearWheelVisual != null, "expected VisualLeanRoot/RearWheel.");
            Check(ref pass, ref fail, seat != null && handL != null && handR != null && footL != null && footR != null && camTarget != null,
                "expected Seat/HandlebarLeft/HandlebarRight/LeftFootTarget/RightFootTarget/CameraTarget anchors.");

            // --- Physics component sanity. ---
            // MINI-118, user: "make the bike heavier a lot heavier... anytime
            // i hit a slight bump... the bike bumps too high." A real-world
            // TMAX (100-400kg) is no longer the intended target - deliberately
            // heavier than reality now (480kg) so ordinary bumps impart a
            // smaller velocity change (Δv = impulse/mass). Range widened to
            // still catch a genuine mistake (e.g. an accidental 0 or a
            // four-figure typo) without re-imposing real-world realism this
            // task deliberately abandoned.
            var rb = prefab.GetComponent<Rigidbody>();
            Check(ref pass, ref fail, rb != null && rb.mass > 100f && rb.mass < 900f,
                $"expected a Rigidbody with a plausible mass (100-900kg, deliberately above real-world TMAX weight per MINI-118), got {(rb == null ? "none" : rb.mass.ToString())}.");

            var bodyCollider = prefab.GetComponent<BoxCollider>();
            Check(ref pass, ref fail, bodyCollider != null, "expected a simple BoxCollider for body collision (not a full mesh collider).");
            Check(ref pass, ref fail, prefab.GetComponent<MeshCollider>() == null,
                "expected NO MeshCollider on the root - collision should be the simple primitive shape, per the brief's 'simple collision representation'.");

            if (frontWc != null && rearWc != null)
            {
                // Deliberately a plausible BAND, not an exact spec match. The
                // wheel positions are now measured off the real scan rather
                // than hardcoded, so demanding exactly 1.575m would either be
                // circular (if the code derived it from spec anyway) or would
                // fail a perfectly good measurement that legitimately differs
                // by a few cm from Yamaha's published figure.
                float wheelbase = Vector3.Distance(frontWc.transform.position, rearWc.transform.position);
                Check(ref pass, ref fail, wheelbase > 1.35f && wheelbase < 1.80f,
                    $"expected a plausible wheelbase (1.35-1.80m; real TMAX 560 spec is 1.575m), got {wheelbase:F3}m.");
                Check(ref pass, ref fail, frontWc.radius > 0.15f && frontWc.radius < 0.4f,
                    $"expected a plausible wheel radius (0.15-0.4m), got {frontWc.radius:F3}m.");

                // The check that actually matters for "does it sit on the
                // road": each wheel's lowest point must be at ground level in
                // the prefab's own local space, since the cleaned mesh's pivot
                // is authored at ground-centre.
                foreach (var wc in new[] { frontWc, rearWc })
                {
                    // A WheelCollider's transform is the TOP of suspension
                    // travel; at rest the wheel centre hangs below it by
                    // (suspensionDistance * targetPosition). The earlier
                    // version of this check ignored that and demanded
                    // (y - radius) == 0, which actively enforced spawning the
                    // bike with its suspension pre-crushed - the cause of the
                    // "bounces 10 feet high" launch. Check the real rest
                    // height instead.
                    float restSag = wc.suspensionDistance * wc.suspensionSpring.targetPosition;
                    float bottomAtRest = wc.transform.localPosition.y - restSag - wc.radius;
                    Check(ref pass, ref fail, Mathf.Abs(bottomAtRest) < 0.06f,
                        $"'{wc.name}' would rest with its tyre bottom at y={bottomAtRest:F3} in prefab space (collider y={wc.transform.localPosition.y:F3}, sag={restSag:F3}, radius={wc.radius:F3}); the mesh pivot is authored at ground level so this should be ~0, otherwise the bike spawns pre-compressed (and launches) or hovering.");
                }

                // The suspension must actually be able to carry the bike at
                // its target ride height, or it either sags to the bump stops
                // or catapults. Derived the same way the prefab builder does.
                var rbForSpring = prefab.GetComponent<Rigidbody>();
                if (rbForSpring != null)
                {
                    float loadN = (rbForSpring.mass / 2f) * 9.81f;
                    float needed = loadN / (frontWc.suspensionSpring.targetPosition * frontWc.suspensionDistance);
                    float actual = frontWc.suspensionSpring.spring;
                    Check(ref pass, ref fail, Mathf.Abs(actual - needed) / needed < 0.35f,
                        $"suspension spring {actual:F0} N/m does not match the {rbForSpring.mass:F0}kg it carries (needs ~{needed:F0} N/m to rest at target height) - too stiff launches the bike, too soft bottoms it out.");
                }

                // And the wheels must sit under the actual bike, not beyond
                // its nose/tail.
                if (body != null)
                {
                    var bodyRend = body.GetComponentInChildren<Renderer>();
                    if (bodyRend != null)
                    {
                        float halfLen = bodyRend.bounds.size.z * 0.5f;
                        Check(ref pass, ref fail,
                            Mathf.Abs(frontWc.transform.localPosition.z) < halfLen &&
                            Mathf.Abs(rearWc.transform.localPosition.z) < halfLen,
                            $"wheel colliders (front z={frontWc.transform.localPosition.z:F3}, rear z={rearWc.transform.localPosition.z:F3}) must sit within the bike's own {halfLen * 2f:F3}m length, not past its nose or tail.");
                    }
                }
            }

            // --- Mesh scale sanity. The cleaned mesh is authored at the real
            // ~2.195m TMAX length, then the whole prefab root is scaled up by
            // Mini064TmaxAssetPrep.BikeScale (the user asked for a visibly
            // bigger bike), so the expected WORLD length is the two
            // multiplied. Deliberately derived rather than hardcoded to the
            // scaled figure: this check exists to catch an accidental scale
            // change, and it can only do that if it tracks the intended
            // scale rather than whatever the asset currently happens to be. ---
            if (body != null)
            {
                var renderer = body.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    const float authoredLengthM = 2.195f;
                    float expected = authoredLengthM * Mini064TmaxAssetPrep.BikeScale;
                    float length = renderer.bounds.size.z;
                    Check(ref pass, ref fail, Mathf.Abs(length - expected) < 0.12f,
                        $"expected the Body mesh's Z extent to be ~{expected:F3}m ({authoredLengthM:F3}m authored x {Mini064TmaxAssetPrep.BikeScale:F2} bike scale), got {length:F3}m.");
                }
            }

            // --- Controller wiring. ---
            var bike = prefab.GetComponent<TmaxBikeControllerCustom>();
            Check(ref pass, ref fail, bike != null, "expected a TmaxBikeControllerCustom on the root.");
            if (bike != null)
            {
                var so = new SerializedObject(bike);
                Check(ref pass, ref fail, so.FindProperty("frontWheel").objectReferenceValue != null, "expected TmaxBikeControllerCustom.frontWheel to be wired.");
                Check(ref pass, ref fail, so.FindProperty("rearWheel").objectReferenceValue != null, "expected TmaxBikeControllerCustom.rearWheel to be wired.");
                Check(ref pass, ref fail, so.FindProperty("centerOfMass").objectReferenceValue != null, "expected TmaxBikeControllerCustom.centerOfMass to be wired.");
                Check(ref pass, ref fail, so.FindProperty("visualLeanRoot").objectReferenceValue != null, "expected TmaxBikeControllerCustom.visualLeanRoot to be wired.");
                Check(ref pass, ref fail, so.FindProperty("enableWheelie").boolValue, "expected the wheelie mechanic to be enabled by default, per the user's explicit repeated request.");
            }

            var visuals = prefab.GetComponent<TmaxWheelVisuals>();
            Check(ref pass, ref fail, visuals != null, "expected a TmaxWheelVisuals on the root.");
            if (visuals != null)
            {
                var vso = new SerializedObject(visuals);
                Check(ref pass, ref fail, vso.FindProperty("frontCollider").objectReferenceValue != null, "expected TmaxWheelVisuals.frontCollider to be wired.");
                Check(ref pass, ref fail, vso.FindProperty("rearCollider").objectReferenceValue != null, "expected TmaxWheelVisuals.rearCollider to be wired.");
                Check(ref pass, ref fail, vso.FindProperty("frontWheelVisual").objectReferenceValue != null, "expected TmaxWheelVisuals.frontWheelVisual to be wired.");
                Check(ref pass, ref fail, vso.FindProperty("rearWheelVisual").objectReferenceValue != null, "expected TmaxWheelVisuals.rearWheelVisual to be wired.");
            }

            // --- API surface: SetInput exists with the expected 3-float shape
            // (compile-time proof, but confirmed here structurally too). ---
            var method = typeof(TmaxBikeControllerCustom).GetMethod("SetInput");
            Check(ref pass, ref fail, method != null && method.GetParameters().Length == 3,
                "expected a public SetInput(float,float,float) method decoupling input from physics.");

            // --- Test scene. ---
            Scene scene = EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/TMAX_Physics_Test.unity", OpenSceneMode.Single);
            var bikeInScene = Object.FindFirstObjectByType<TmaxBikeControllerCustom>();
            Check(ref pass, ref fail, bikeInScene != null, "expected a TmaxBikeControllerCustom instance in TMAX_Physics_Test.unity.");
            Check(ref pass, ref fail, bikeInScene != null && bikeInScene.GetComponent<TmaxTestInput>() != null,
                "expected the test-scene bike to carry TmaxTestInput.");
            Check(ref pass, ref fail, Object.FindFirstObjectByType<Camera>() != null, "expected a camera in the test scene.");
            Check(ref pass, ref fail, GameObject.Find("Slope") != null, "expected a slope test object in the scene.");
            Check(ref pass, ref fail, GameObject.Find("TurnA") != null, "expected turn test geometry in the scene.");
            Check(ref pass, ref fail, GameObject.Find("Wall1") != null && GameObject.Find("Box1") != null,
                "expected collision test obstacles in the scene.");

            // --- Missing-shader check, added after the user reported
            // solid magenta blobs on the bike in Play Mode. Unity renders
            // any material whose shader is null (or fails to compile)
            // using a real, checkable shader named
            // "Hidden/InternalErrorShader" - exactly the pink/magenta
            // "missing shader" look. Checking every renderer on the scene
            // bike for that shader name is a precise, automated stand-in
            // for "does this actually look right" that doesn't depend on
            // the (currently broken) headless renderer at all. ---
            if (bikeInScene != null)
            {
                foreach (var r in bikeInScene.GetComponentsInChildren<Renderer>())
                {
                    foreach (var m in r.sharedMaterials)
                    {
                        bool broken = m == null || m.shader == null || m.shader.name == "Hidden/InternalErrorShader";
                        Check(ref pass, ref fail, !broken,
                            $"renderer '{r.name}' has a broken/missing-shader material ({(m == null ? "null material" : m.shader == null ? "null shader" : m.shader.name)}) - this is exactly what renders as solid magenta in Play Mode.");
                    }
                }
            }

            // --- Ground clearance: a real physics.Raycast check that the
            // bike doesn't start free-floating above (or embedded below)
            // the road, added after the user caught exactly this bug in
            // Play Mode ("spinning in the air like crazy") - this is the
            // one class of error a structural-only check above can't see,
            // so it gets its own explicit numeric proof now. Checks each
            // WheelCollider's actual downward reach (radius +
            // suspensionDistance) against the real distance to the ground
            // collider beneath it. ---
            if (bikeInScene != null)
            {
                Physics.SyncTransforms();
                var wheelCols = bikeInScene.GetComponentsInChildren<WheelCollider>();
                Check(ref pass, ref fail, wheelCols.Length == 2, $"expected exactly 2 WheelColliders on the scene bike, found {wheelCols.Length}.");
                foreach (var wc in wheelCols)
                {
                    float reach = wc.radius + wc.suspensionDistance;
                    bool hit = Physics.Raycast(wc.transform.position, Vector3.down, out RaycastHit groundHit, reach + 0.5f);
                    if (!hit)
                    {
                        Check(ref pass, ref fail, false, $"'{wc.name}': no ground found within 1m below it at all - it's floating in open space.");
                        continue;
                    }
                    Check(ref pass, ref fail, groundHit.distance <= reach,
                        $"'{wc.name}' cannot reach the ground: nearest ground is {groundHit.distance:F3}m below it, but its wheel radius + suspension travel only reaches {reach:F3}m - it will free-fall exactly like the user's reported bug.");
                    Check(ref pass, ref fail, groundHit.distance > 0.02f,
                        $"'{wc.name}' is embedded in/touching the ground at spawn ({groundHit.distance:F3}m) - should start with some suspension travel still available, not already bottomed out.");
                }
            }

            // --- MINI-065's purchase/spawn hook, against the real
            // GrandBayProof scene (a separate integration point from the
            // isolated test scene above). ---
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var spawnerGo = GameObject.Find("VehicleSpawner");
            var spawner = spawnerGo?.GetComponent<VehicleSpawnController>();
            Check(ref pass, ref fail, spawner != null, "expected a VehicleSpawner (VehicleSpawnController) in GrandBayProof.unity.");
            if (spawner != null)
            {
                var spawnerSo = new SerializedObject(spawner);
                Check(ref pass, ref fail, spawnerSo.FindProperty("tmaxPrefab").objectReferenceValue != null,
                    "expected VehicleSpawnController.tmaxPrefab to be wired to the real prefab.");
            }

            var dealer = Object.FindObjectsByType<Interaction.TownNPCInteractable>(FindObjectsSortMode.None);
            bool dealerHasTmax = false;
            int dealerStockCount = -1;
            foreach (var npc in dealer)
            {
                if (npc.Role != Interaction.NpcRole.CarDealer) continue;
                var shopField = new SerializedObject(npc).FindProperty("shop");
                var shop = shopField.objectReferenceValue as UI.ShopPanelController;
                if (shop == null) continue;
                var stockProp = new SerializedObject(shop).FindProperty("stock");
                dealerStockCount = stockProp.arraySize;
                for (int i = 0; i < stockProp.arraySize; i++)
                {
                    var item = stockProp.GetArrayElementAtIndex(i).objectReferenceValue as Economy.ShopItemDefinition;
                    if (item != null && item.itemId == "tmax_560") dealerHasTmax = true;
                }
            }
            Check(ref pass, ref fail, dealerHasTmax, "expected the Car Dealer's stock to include a 'tmax_560' shop item.");
            // User's explicit request: "the option should be only the bike
            // and no other vehicles" - the dealer previously also listed
            // flavour-only Scrambler Bike/Pickup Van/Fishing Pirogue entries.
            // Was "exactly 1" from when the user asked for the bike alone. The
            // Range Rover (MINI-071) is a deliberate second entry, so this now
            // guards the original intent - no flavour-only filler vehicles -
            // rather than a hardcoded count that any real addition breaks.
            Check(ref pass, ref fail, dealerStockCount <= 2,
                $"expected the Car Dealer to stock only real, buyable vehicles (TMAX + Range Rova), found {dealerStockCount} items.");

            // MINI-066 follow-up: the rider's poses are addressed by string id
            // from a runtime script that cannot reference the Editor-only setup
            // class holding the real ids, so the two lists can silently drift
            // and the rider just quietly holds the wrong pose. Check every id
            // BikeRiderAnimation asks for actually exists as a state on the
            // controller, on BOTH full-body layers - the wheelie pose is now
            // blended on FullBodyBlend over the ride pose on FullBodyOverride,
            // so a missing state on the upper layer means the blend slider
            // would appear to do nothing at all.
            var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
                "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller");
            Check(ref pass, ref fail, controller != null,
                "StarterAssetsThirdPerson.controller not found - the rider's pose layers live on it.");
            if (controller != null)
            {
                int fullBody = -1, blend = -1;
                for (int i = 0; i < controller.layers.Length; i++)
                {
                    if (controller.layers[i].name == Character.HumanoidAnimationManager.FullBodyLayerName) fullBody = i;
                    if (controller.layers[i].name == Character.HumanoidAnimationManager.FullBodyBlendLayerName) blend = i;
                }
                Check(ref pass, ref fail, fullBody >= 0, "controller has no FullBodyOverride layer.");
                Check(ref pass, ref fail, blend >= 0, "controller has no FullBodyBlend layer - the partial wheelie pose has nowhere to play.");
                Check(ref pass, ref fail, blend > fullBody,
                    $"FullBodyBlend (index {blend}) must sit ABOVE FullBodyOverride (index {fullBody}) or it cannot blend over the held ride pose.");

                if (fullBody >= 0 && blend >= 0)
                {
                    string[] riderIds =
                    {
                        BikeRiderAnimation.Mini011Ids.Ride,
                        BikeRiderAnimation.Mini011Ids.Stopped,
                        BikeRiderAnimation.Mini011Ids.Wheelie,
                        BikeRiderAnimation.Mini011Ids.LeanLeft,
                        BikeRiderAnimation.Mini011Ids.LeanRight,
                        BikeRiderAnimation.Mini011Ids.StopSettle,
                        BikeRiderAnimation.Mini011Ids.Rest,
                        BikeRiderAnimation.Mini011Ids.PullAway,
                    };
                    foreach (string id in riderIds)
                    {
                        Check(ref pass, ref fail, HasState(controller, fullBody, id),
                            $"rider pose id '{id}' has no state on the FullBodyOverride layer.");
                    }
                    // Only the wheelie pose is used as an overlay today, but the
                    // builder mirrors every full-body pose up here, so check the
                    // one that matters rather than asserting the mirror is total.
                    Check(ref pass, ref fail, HasState(controller, blend, BikeRiderAnimation.Mini011Ids.Wheelie),
                        "the wheelie pose has no state on the FullBodyBlend layer, so 'WHEELIE pose blend' would do nothing.");
                }
            }

            if (pass)
            {
                Debug.Log("MINI-064/065 VALIDATION PASS: TMAX_560.prefab has the full specified hierarchy (Physics/COM/VisualLeanRoot/Body/FrontSteering/FrontWheel/RearWheel/Seat/Handlebars/FootTargets/CameraTarget), a plausible Rigidbody mass, simple BoxCollider collision (no mesh collider), a plausible measured wheelbase with both wheel bottoms at ground level and both wheels within the bike's own length, a Body mesh scaled to ~2.195m length, TmaxBikeControllerCustom + TmaxWheelVisuals fully wired (including the wheelie mechanic enabled by default), a decoupled SetInput(throttle,steer,brake) entry point, TMAX_Physics_Test.unity exists with the bike, a camera, and turn/slope/collision test geometry, AND (added after the user's reported floating/spinning bug) both WheelColliders genuinely reach the real ground at spawn with some suspension travel still in hand, not floating or bottomed out. Actual FixedUpdate physics behaviour (acceleration, lean, wheelie, tip/recovery) is still NOT exercised here - Play Mode doesn't tick in this batch-mode environment, same limitation as every timer-driven system in this project; this needs the user's real Editor Play Mode test.");
            }
            else
            {
                Debug.LogError($"MINI-064/065 VALIDATION FAIL: {fail}");
            }
        }

        private static bool HasState(UnityEditor.Animations.AnimatorController controller, int layerIndex, string stateName)
        {
            foreach (var child in controller.layers[layerIndex].stateMachine.states)
            {
                if (child.state.name == stateName) return true;
            }
            return false;
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }
    }
}
