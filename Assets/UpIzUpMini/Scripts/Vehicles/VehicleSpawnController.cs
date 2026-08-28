using System.Reflection;
using UnityEngine;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Missions;
using UpIzUpMini.UI;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-065 (folded forward from MINI-068's "purchase/ownership,
    /// spawn/storage" scope, per the user's explicit insistence while
    /// MINI-064/065 was in progress: "make sure you add buying the bike
    /// and spawn in it, i want to test it dont skip it for a future mini
    /// version"). Deliberately minimal - NOT the full IVehicle/
    /// VehicleDefinition/VehicleOwnership architecture the roadmap
    /// describes for MINI-068 (meant to generalize across every future
    /// vehicle - scrambler, van, boat...). This is just enough to buy the
    /// TMAX once and see it actually appear in the world on a real
    /// purchase flow, rather than only ever meeting it in the isolated
    /// physics test scene. Generalizing this into the real vehicle
    /// architecture remains MINI-068 scope.
    /// </summary>
    public class VehicleSpawnController : MonoBehaviour
    {
        public static VehicleSpawnController Instance { get; private set; }

        [SerializeField] private GameObject tmaxPrefab;
        [Tooltip("MINI-071: the driveable Range Rover. Same one-time spawn treatment as the bike.")]
        [SerializeField] private GameObject roverPrefab;
        [Tooltip("MINI-119, user: \"i want to test this in my actual game scene to get the full gist.\" The new Motorbike Physics Tool-based bike (TMAX_560_SuperMoto.prefab) - purely a dev test spawn alongside the real tmaxPrefab/roverPrefab, never through the real purchase flow, and never touching either of those fields. The Range Rover stays completely untouched by any of this, per the user's own explicit \"the range rova system should be separate as its a car.\"")]
        [SerializeField] private GameObject superMotoTestPrefab;
        [Tooltip("How far from the dealer, along the dealer's own facing direction (which points at the road - see Mini011PhaseBSetup.BuildNpc), the bike appears.")]
        [SerializeField] private float spawnForwardOffset = 5f;
        [SerializeField] private float spawnSideOffset = 1.5f;
        [SerializeField] private string dealerNpcName = "NPC_CarDealer";

        [Tooltip("MINI-068: where the bike lives. Set by Mini011PhaseBSetup to the parking spot outside the farm safehouse - the bike is returned here on every load, so it can never be stranded somewhere unreachable.")]
        [SerializeField] private Transform bikeHome;

        private bool _tmaxSpawned;
        private bool _roverSpawned;
        private bool _superMotoSpawned;
        // MINI-113: distinct from every other GtaMiniMapMarker colour
        // already in use (shop/mission/police/gang/community/property).
        private static readonly Color VehicleMarkerColour = new Color(0.95f, 0.55f, 0.10f);

        // MINI-119 follow-up, user: "you will have to spawn a bike and car
        // close to where i start the game because i am unable to buy the
        // bike and car right now. to test." A temporary testing aid, same
        // on/off-flag convention as Mini065TmaxPhysicsTest.IncludeDevTuner -
        // flip back to false once bike/hill tuning is done and the normal
        // dealer-purchase flow is reachable again. Does not touch the
        // scene file at all (pure runtime code), so it is safe to ship
        // without re-running any scene builder and re-wiping the user's
        // own manual hedge/farm-plot/safehouse placement edits.
        // MINI-119 follow-up, user: "remove the bike and range rova from
        // the start of the game. it was just for testing." Off - vehicles
        // only ever appear via the normal dealer purchase flow now.
        private const bool DevSpawnNearPlayerOnStart = false;

        // MINI-119 follow-up, user: "you may have to temporarily disable
        // the main character and just use the ragdoll and the bike
        // character until testing is successful." Same on/off-flag
        // convention as DevSpawnNearPlayerOnStart - while this is true,
        // the on-foot player character is put straight onto the SuperMoto
        // (its own rigged rider) the instant it spawns, instead of
        // waiting for a walk-up + F press. That means the on-foot
        // character is never simultaneously active and physically near
        // the bike/ragdoll, which is exactly the clash the user was
        // seeing. Flip back to false once the bike is confirmed stable
        // and the walk-up-and-mount flow itself needs testing again.
        private const bool DevAutoPossessSuperMotoOnSpawn = true;

        // MINI-119 follow-up, user: "no your wrong stripping it to near
        // nothing is going to get us back to square 1... let us test the
        // demo controller and physics in my environment first... start
        // from what works which is the demo. lets use the controls of the
        // demo as well." Correct call - my own facade/adapter/upright-
        // torque additions are exactly what broke (the bike spinning
        // uncontrollably in the air), and I'd never actually proven the
        // asset's OWN stock setup even worked in this map before piling
        // changes on top of it. While this is true, EVERYTHING this
        // controller normally spawns (tmaxPrefab, roverPrefab, our
        // TMAX_560_SuperMoto.prefab, TmaxBikeController, GaddInputAdapter,
        // SuperMotoInteractable) is skipped completely, the on-foot
        // character is disabled, and the pack's own untouched
        // SuperMotoWRagdoll.prefab is spawned instead with the asset's
        // OWN stock Input_Manager, KeyBoardShortCuts (R = reload scene,
        // F = flip upright when crashed - the "reset button... just in
        // case something goes wrong" the user asked for, using the
        // asset's existing mechanism rather than inventing a new one) and
        // ThirdPersonCamera, so what gets tested is genuinely the pack's
        // own behaviour, not our integration of it. Flip back to false
        // once this baseline is confirmed solid in this map, then
        // reintroduce our own facade/mapping one small change at a time.
        private const bool StockDemoBikeTestMode = true;
        [Tooltip("MINI-119: only used when StockDemoBikeTestMode is true - the Motorbike Physics Tool's own, completely unmodified Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab. Wired by Mini119WireStockDemoBike.cs, not the normal scene builder.")]
        [SerializeField] private GameObject stockDemoBikePrefab;

        // MINI-119 follow-up, user: "can you do it when i start or press
        // play, sacat is mounted on the bike... i dont want to press f
        // to mount." Same on/off-flag convention as
        // DevSpawnNearPlayerOnStart/DevAutoPossessSuperMotoOnSpawn -
        // while true, the instant the bike finishes spawning/wiring,
        // Sacat is mounted onto it for real (the same live Mount() flow
        // an actual F press triggers), no key press needed. Flip back to
        // false to return to the normal walk-up-and-press-F flow.
        private const bool AutoMountSuperMotoOnSpawn = true;

        private bool _devSpawnDone;

        private void Awake() => Instance = this;

        private void Update()
        {
            if (StockDemoBikeTestMode)
            {
                if (_devSpawnDone) return;
                var activeForStockTest = CharacterSwitchManager.Instance?.Active;
                if (activeForStockTest?.root == null) return;
                _devSpawnDone = true;
                SpawnStockDemoBikeAndDisableOurCharacter(activeForStockTest.root);
                return;
            }

            if (!DevSpawnNearPlayerOnStart || _devSpawnDone) return;

            var active = CharacterSwitchManager.Instance?.Active;
            if (active?.root == null) return; // keep waiting - player may not exist yet (intro/dialogue delay)

            _devSpawnDone = true;
            DevSpawnNearPlayer(active.root.transform);
        }

        /// <summary>
        /// Spawns both vehicles right next to the player's own current
        /// position instead of at the dealer - see DevSpawnNearPlayerOnStart's
        /// own comment. Marks both as already-spawned (same _tmaxSpawned/
        /// _roverSpawned flags SpawnPurchasedVehicle checks), so a later
        /// real purchase attempt at the dealer just says "already have it"
        /// instead of stacking a second copy.
        /// </summary>
        private void DevSpawnNearPlayer(Transform player)
        {
            Vector3 forward = player.forward;
            Vector3 right = player.right;

            if (tmaxPrefab != null && !_tmaxSpawned)
            {
                Vector3 pos = GroundSnap(player.position + forward * 3f - right * 2.5f);
                var bike = Instantiate(tmaxPrefab, pos, Quaternion.LookRotation(forward, Vector3.up));
                bike.name = "PlayerTMAX";
                _tmaxSpawned = true;
                bike.AddComponent<GtaMiniMapMarker>().Configure(MiniMapMarkerKind.Vehicle, "TMAX 560", VehicleMarkerColour);
                Debug.Log("MINI-119 DEV SPAWN: TMAX placed next to the player for testing (DevSpawnNearPlayerOnStart).");
            }

            if (roverPrefab != null && !_roverSpawned)
            {
                Vector3 pos = GroundSnap(player.position + forward * 3f + right * 2.5f);
                var rover = Instantiate(roverPrefab, pos, Quaternion.LookRotation(forward, Vector3.up));
                rover.name = "PlayerRangeRover";
                _roverSpawned = true;
                rover.AddComponent<GtaMiniMapMarker>().Configure(MiniMapMarkerKind.Vehicle, "Range Rover", VehicleMarkerColour);
                Debug.Log("MINI-119 DEV SPAWN: Range Rover placed next to the player for testing (DevSpawnNearPlayerOnStart).");
            }

            if (superMotoTestPrefab != null && !_superMotoSpawned)
            {
                // MINI-119 follow-up, user: "the new bike spawns in the
                // same area [as the old one] so they clash and the new
                // bike falls." tmaxPrefab spawns at forward*3-right*2.5,
                // roverPrefab at forward*3+right*2.5 - both close enough
                // that a bike settling/spawn jolt could reach them. Pushed
                // well clear of both (forward*10+right*4) rather than
                // guessing at a slightly bigger number.
                Vector3 pos = GroundSnap(player.position + forward * 10f + right * 4f);
                var moto = Instantiate(superMotoTestPrefab, pos, Quaternion.LookRotation(forward, Vector3.up));
                moto.name = "TestSuperMoto";
                _superMotoSpawned = true;
                moto.AddComponent<GtaMiniMapMarker>().Configure(MiniMapMarkerKind.Vehicle, "SuperMoto (test)", VehicleMarkerColour);

                if (DevAutoPossessSuperMotoOnSpawn)
                {
                    var interactable = moto.GetComponent<SuperMotoInteractable>();
                    if (interactable != null) interactable.DevForceMount(player.gameObject);
                    Debug.Log("MINI-119 DEV SPAWN: SuperMoto test bike placed and auto-possessed - the on-foot character is disabled, you're straight on the bike's own rider. E to wheelie, Space to brake, R to reset upright if it falls, F to get off.");
                }
                else
                {
                    Debug.Log("MINI-119 DEV SPAWN: SuperMoto test bike placed next to the player - press F to get on, E to wheelie, Space to brake, R to reset upright if it falls.");
                }
            }
        }

        /// <summary>MINI-119 follow-up, user: "temporarily redirect or
        /// disable my character and bike controller and physics first...
        /// use the demo first and see how it works." Disables the on-foot
        /// character entirely (not just SetActive-while-riding, like our
        /// own Possess() does) and spawns the pack's own raw
        /// SuperMotoWRagdoll.prefab with its own stock Input_Manager,
        /// KeyBoardShortCuts and ThirdPersonCamera - none of our facade,
        /// adapter or added torque involved at all.</summary>
        private void SpawnStockDemoBikeAndDisableOurCharacter(GameObject player)
        {
            // MINI-119 follow-up, user: "i want the same bike that is
            // spawning i can mount on." Rather than always instantiating
            // a fresh copy at runtime, this looks for one already placed
            // in the scene BY HAND first (drag SuperMotoWRagdoll.prefab
            // into the Hierarchy, name it "StockDemoSuperMoto") - that
            // becomes the SAME object that's mountable in Play Mode, not
            // a separate runtime copy, so any Animation Rigging setup the
            // user builds on it in Edit Mode (where it actually saves)
            // is exactly what's there when they hit Play. Falls back to
            // the old instantiate-fresh-every-time behaviour if nothing
            // has been placed, so this stays safe with no manual setup
            // too.
            var preplaced = GameObject.Find("StockDemoSuperMoto");
            GameObject instance;
            bool wasPreplaced = preplaced != null;
            Vector3 forward = player.transform.forward;
            Vector3 spawnPos;

            if (wasPreplaced)
            {
                instance = preplaced;
                spawnPos = instance.transform.position;
                // Manual placement is authoritative (same convention
                // this whole task has followed for the seat pose, bike
                // scale, etc.) - position/rotation/scale are exactly
                // whatever the user set in the Editor, never
                // reinterpreted here.

                // MINI-119 follow-up fix, user: "ok revert before that"
                // appeared to do nothing, and wheelie was awkward once
                // before this too - both traced to the SAME real bug:
                // this whole method has no guard against running its
                // wiring a SECOND time against an already-wired preplaced
                // bike (e.g. an Editor tool calling it again via
                // reflection, or any future double-invocation). None of
                // the AddComponent calls below check for an existing
                // component first, so a second run silently doubles up
                // SuperMotoVehicleInteractable/VehicleSeat/
                // TrikeStabilizer/WheelieAssist/etc - two of each
                // independently applying their own torque/logic, and a
                // stale first instance left holding whatever an Editor
                // tool interacted with while a second, freshly-wired one
                // sits unused. Wiring is idempotent from here on: skip
                // the ENTIRE rest of this method if it already has.
                if (instance.GetComponent<SuperMotoVehicleInteractable>() != null)
                {
                    Debug.Log("MINI-119 STOCK DEMO TEST: preplaced bike is already fully wired - skipping re-wiring entirely (prevents duplicate components).");
                    return;
                }
            }
            else
            {
            if (stockDemoBikePrefab == null)
            {
                Debug.LogError("MINI-119 STOCK DEMO TEST: stockDemoBikePrefab not wired - run Mini119WireStockDemoBike.");
                return;
            }

            // MINI-119 follow-up, user (with screenshot): "i want the bike
            // to spawn... this the lalay road inbetween the two shops."
            // Mini011PhaseBSetup.BuildMarketArea places "Stall_FARM SHOP"
            // and "Stall_PRODUCE BUYER" flanking the road at the same road
            // index, one either side - their midpoint IS the road between
            // them. Falls back to the Sign_LALAY position, then the old
            // near-player spawn, if the market stalls aren't found.
            var farmShopStall = GameObject.Find("Stall_FARM SHOP");
            var produceBuyerStall = GameObject.Find("Stall_PRODUCE BUYER");
            if (farmShopStall != null && produceBuyerStall != null)
            {
                Vector3 mid = (farmShopStall.transform.position + produceBuyerStall.transform.position) * 0.5f;
                spawnPos = GroundSnap(mid);
                // Face along the road, not across it - the road runs
                // perpendicular to the line between the two stalls (they
                // flank opposite sides of it).
                Vector3 acrossRoad = produceBuyerStall.transform.position - farmShopStall.transform.position;
                acrossRoad.y = 0f;
                if (acrossRoad.sqrMagnitude > 0.01f)
                    forward = Vector3.Cross(Vector3.up, acrossRoad.normalized);
            }
            else
            {
                var lalaySign = GameObject.Find("Sign_LALAY");
                spawnPos = lalaySign != null
                    ? GroundSnap(lalaySign.transform.position - new Vector3(4.6f, 0f, 0f))
                    : GroundSnap(player.transform.position + forward * 8f);
            }

            // MINI-119 follow-up, user: "i want to be able to walk to the
            // bike and press f to get on the bike." Player keeps walking
            // normally now - nothing about their control is touched at
            // spawn time at all. SuperMotoStockInteractable.Interact
            // (only fires on an actual F press while in range) is what
            // disables control/CharacterController, exactly like
            // BikeInteractable already does for the TMAX.

            // MINI-119 follow-up fix, user: "i must press F to get stable
            // in the beginning because the spawn lands with a crash." Real
            // bug, confirmed independently by the user's own play AND by
            // a direct test against this exact scene/spawn point
            // (Mini119RealSceneWheelieTest showed a genuine -61.7deg pitch
            // reading before the player had touched a single key): this
            // spawn placed the prefab's ROOT TRANSFORM directly at the
            // ground-raycast hit point, with no idea where the wheels
            // actually sit relative to that root - almost certainly
            // burying the wheels partway into the road mesh, so the very
            // first physics step is a violent correction, not a clean
            // landing. The SAME class of bug was already found and fixed
            // for the OTHER (TMAX_560_SuperMoto.prefab) spawn path much
            // earlier in MINI-119 - this stock-demo spawn just never got
            // the same treatment when it was built. Measured here the
            // same way: instantiate at the origin first, read the real
            // wheel-bottom offset from the live WheelColliders, then place
            // the root high enough that the wheels rest ON the ground
            // instead of through it.
            instance = (GameObject)Instantiate(stockDemoBikePrefab, Vector3.zero, Quaternion.LookRotation(forward, Vector3.up));
            instance.name = "StockDemoSuperMoto";

            // MINI-119 follow-up, user: "he smaller than the actual bike...
            // i want the bike scaled because the world would look
            // awkward." Measured, not guessed (Mini119BikeScaleMeasure):
            // the vendor's own rider is 1.924m tall vs Sacat's own
            // established 1.850m (MINI-083) - a 1.040 ratio, so scaling
            // the WHOLE bike down by 1/1.040 makes Sacat sit on it at
            // exactly the same relative proportion the vendor's own rider
            // already did. Applied here at spawn time, not baked into the
            // vendor prefab itself, so it's scoped to this one spawn and
            // easy to retune.
            const float bikeScale = 0.961f;
            instance.transform.localScale = Vector3.one * bikeScale;

            float wheelBottomOffset = 0.4f; // sane fallback if wheelColliders aren't readable yet
            var gaddForOffset = instance.GetComponent<Gadd420.RB_Controller>();
            if (gaddForOffset != null && gaddForOffset.wheelColliders != null && gaddForOffset.wheelColliders.Length >= 2
                && gaddForOffset.wheelColliders[0] != null && gaddForOffset.wheelColliders[1] != null)
            {
                float rearBottom = gaddForOffset.wheelColliders[0].transform.position.y - gaddForOffset.wheelColliders[0].radius;
                float frontBottom = gaddForOffset.wheelColliders[1].transform.position.y - gaddForOffset.wheelColliders[1].radius;
                wheelBottomOffset = -Mathf.Min(rearBottom, frontBottom);
            }
            instance.transform.SetPositionAndRotation(
                spawnPos + Vector3.up * (wheelBottomOffset + 0.1f),
                Quaternion.LookRotation(forward, Vector3.up));
            Physics.SyncTransforms();
            } // end of the "not preplaced" branch - everything below runs either way

            // MINI-119 follow-up, user: "the camera is not smooth research
            // and get it to follow smooth." Real, well-documented Unity
            // cause, not a camera-side tuning problem: the raw
            // SuperMotoWRagdoll prefab ships its Rigidbody with
            // Interpolate = None (confirmed in the prefab's own serialized
            // data), so its visual transform only updates once per physics
            // step (~50Hz) instead of being smoothed to the render
            // framerate - the textbook cause of a jittery follow camera on
            // a Rigidbody target, no amount of camera-side smoothing fully
            // hides a source that's itself moving in discrete jumps. Our
            // OWN TMAX_560_SuperMoto.prefab (Mini119SuperMotoBikeSetup)
            // already sets this correctly - the raw stock-demo spawn here
            // just never got the same treatment.
            var stockDemoRb = instance.GetComponent<Rigidbody>();
            if (stockDemoRb != null)
            {
                stockDemoRb.interpolation = RigidbodyInterpolation.Interpolate;
                stockDemoRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            // MINI-119 follow-up, user: "f respawns the bike only on
            // crash but i want it to respawn anytime." Replaces the
            // stock KeyBoardShortCuts (F gated on isCrashed) entirely -
            // see SuperMotoAnytimeReset's own header.
            var stockShortcuts = instance.GetComponent<Gadd420.KeyBoardShortCuts>();
            if (stockShortcuts != null) DestroyImmediate(stockShortcuts);
            var anytimeReset = instance.AddComponent<SuperMotoAnytimeReset>();

            // MINI-119 follow-up, user: "the E button to wheelie instead
            // of left control and the Q button will be used instead of
            // the left shift." Swapped BEFORE anything else runs a
            // Start() that fetches Input_Manager (RB_Controller's own
            // Start() is the only one that does, and Start() always runs
            // strictly after this synchronous spawn method finishes, not
            // during Instantiate itself) - same safe ordering the earlier
            // GaddInputAdapter swap used at prefab-build time, just done
            // here at runtime instead since this is the raw stock prefab.
            // MINI-119 follow-up fix, user: "the bike doesnt move now."
            // Destroy() is deferred to end-of-frame, so for one whole
            // frame the GameObject genuinely had TWO Input_Manager-typed
            // components (the pending-destroy stock one plus our new
            // subclass) - RB_Controller.Start() (same frame or the next)
            // called GetComponent<Input_Manager>() and could just as
            // easily grab the dead-man-walking stock one instead of the
            // live remap, permanently wiring the bike's input to a
            // component that was about to vanish and never actually get
            // driven. DestroyImmediate removes it synchronously, before
            // AddComponent runs, so there's only ever ONE Input_Manager
            // on this object at any point in time - no ambiguity for
            // GetComponent to resolve.
            // MINI-119 follow-up fix, user: "it never worked... wheelie
            // assist not installed." Real, confirmed root cause, found in
            // the user's own Player.log (not a batch test): "Can't remove
            // Input_Manager (Script) because RB_Controller (Script)
            // depends on it". RB_Controller has [RequireComponent(typeof
            // (Input_Manager))] - at the moment DestroyImmediate used to
            // run here, the stock Input_Manager was the ONLY one on the
            // object, so removing it would leave RB_Controller's
            // requirement unsatisfied and Unity silently BLOCKS the
            // destroy (logs that error, does not throw - the old code
            // never noticed and carried on as if it had worked). The
            // stock Input_Manager therefore never actually left, stayed
            // FIRST in GetComponent's resolution order, and every
            // GetComponent<Input_Manager>() call in this project kept
            // silently resolving to it instead of the remap - wiring the
            // whole wheelie system to LeftCtrl/LeftShift, which nothing
            // ever pressed, while E/Q sat on an orphaned, never-read
            // component. Fix: add the remap FIRST so there are
            // momentarily TWO Input_Managers, THEN destroy the captured
            // stock reference - now the remap alone satisfies
            // RB_Controller's requirement and the destroy actually
            // succeeds.
            var stockInput = instance.GetComponent<Gadd420.Input_Manager>();
            var newInput = instance.AddComponent<SuperMotoWheelieKeyRemap>();
            if (stockInput != null) DestroyImmediate(stockInput);

            // MINI-119 follow-up fix, user: "the bike isnt riding when i
            // mount." Real cause, confirmed directly in RB_Controller.cs:
            // it caches its Input_Manager reference EXACTLY ONCE, in its
            // own Start() (`inputs = GetComponent<Input_Manager>();`).
            // For a bike instantiated fresh this same frame, Start()
            // hasn't run yet (Unity defers Start to before the next
            // Update, well after this whole synchronous spawn method
            // finishes), so it picks up the remap above just fine - the
            // ordering trick the comment above already relies on. But for
            // a PREPLACED bike sitting in the scene since load, Start()
            // already ran, days (in frame terms) before this code ever
            // runs, and cached the STOCK Input_Manager - which the line
            // above just destroyed. Every inputs.HzInput/VInput/etc read
            // in RB_Controller from then on reads a dangling reference to
            // a destroyed object - no throttle, no steering, nothing,
            // silently. RB_Controller's `inputs` field has no modifier
            // (private by default) and no public setter, so this
            // re-points it directly via reflection - fixes both the
            // preplaced case AND is a harmless no-op for the fresh-spawn
            // case (Start() just does the identical GetComponent call a
            // moment later and gets the same answer).
            var rbForInputFix = instance.GetComponent<Gadd420.RB_Controller>();
            if (rbForInputFix != null)
            {
                var inputsField = typeof(Gadd420.RB_Controller).GetField("inputs", BindingFlags.NonPublic | BindingFlags.Instance);
                if (inputsField != null) inputsField.SetValue(rbForInputFix, newInput);
                else Debug.LogError("MINI-119 STOCK DEMO TEST: RB_Controller.inputs field not found via reflection - vendor script changed? Riding will silently not respond to input.");
            }
            // Same exact caching pattern (and same field name) in
            // AutoLeveling - the asset's own upright/wheelie-safety
            // assist - would leave IT reading a dangling reference too on
            // a preplaced bike, silently breaking auto-leveling instead
            // of throttle. Same fix.
            var autoLevelForInputFix = instance.GetComponent<Gadd420.AutoLeveling>();
            if (autoLevelForInputFix != null)
            {
                var inputsField2 = typeof(Gadd420.AutoLeveling).GetField("inputs", BindingFlags.NonPublic | BindingFlags.Instance);
                inputsField2?.SetValue(autoLevelForInputFix, newInput);
            }

            // MINI-119 follow-up, user: "remember i told you to use my
            // trike system that worked originally and you always disabled
            // it... yes use the trike system." Restored, and this time it
            // stays. I removed it twice on the reasoning that the
            // kinematic wheelie makes roll mathematically impossible so
            // outriggers were redundant - but the user has said
            // repeatedly that this system is what worked on their
            // original bike, and their hands-on evidence outranks my
            // reasoning about what should be redundant.
            instance.AddComponent<SuperMotoTrikeStabilizer>();

            instance.AddComponent<SuperMotoWheelieAssist>();

            // MINI-119 follow-up, user: "its a left to right that needs
            // to return to equal angle on both left and right so the
            // bike would be back up right no matter what... a counter
            // force seperate for the superassist to bring it bck to 0
            // degrees lean that it constantly checks for." Continuous
            // roll-correcting spring-damper - see SuperMotoUprightAssist's
            // own header.
            instance.AddComponent<SuperMotoUprightAssist>();

            // MINI-119 follow-up, user: "i didnt see the screen read out if
            // E was held or not." The earlier readout lived inside
            // Mini119StockDemoBikeTuner's toggleable/scrollable panel and
            // was missed entirely. This is a standalone, always-visible
            // overlay (H to hide) showing the same live diagnostic -
            // whether E is registering at all and exactly what is blocking
            // the wheelie - so the user can report back what the game
            // itself says instead of me guessing from batch tests.
            instance.AddComponent<SuperMotoWheelieHud>();

            // MINI-119 follow-up fix, user: "E didnt work for wheelie. i
            // still had to press crtl." A RequireComponent on one of the
            // components just added silently reintroduced a second, stock
            // Input_Manager alongside the remap (see SuperMotoWheelieAssist's
            // own fix comment for the full story) - asserted here as a
            // permanent safety net so the same failure mode can't recur
            // silently if any future component's RequireComponent does
            // the same thing: only the remap may survive.
            var survivingInputMgrs = instance.GetComponents<Gadd420.Input_Manager>();
            foreach (var mgr in survivingInputMgrs)
            {
                if (!(mgr is SuperMotoWheelieKeyRemap)) DestroyImmediate(mgr);
            }

            // MINI-119 follow-up, user: "use my camera follow system...
            // it automatically tracks and follows the character properly."
            // Anchor created here (harmless, just sits idle until
            // mounted) - SuperMotoStockInteractable owns actually
            // pointing the camera at it on mount/dismount, since doing
            // that at SPAWN time would lock the camera onto the parked
            // bike before the player ever interacts with it.
            var camAnchorGo = new GameObject("StockDemoCameraAnchor");
            var camAnchor = camAnchorGo.AddComponent<BikeCameraAnchor>();
            camAnchor.Follow(instance.transform);

            // MINI-119 follow-up, user: "it crashes too easy... it tends
            // to lean on a side while riding sometimes... i like the
            // wheelie but it needs upright assist as well." The asset
            // already HAS real upright-assist (Gadd420.AutoLeveling -
            // autoLevelForce/dotForAutoLevel/antiSpinTorque) and wheelie-
            // specific assist (AutoLeveling.safeWheelies/antiLoopStrength/
            // maxWheelieAngle) - nothing new to invent, just expose what's
            // already there as live sliders. See Mini119StockDemoBikeTuner.
            if (instance.GetComponent<Mini119StockDemoBikeTuner>() == null)
                instance.AddComponent<Mini119StockDemoBikeTuner>();

            // MINI-119 follow-up, user: "i must press F to get stable in
            // the beginning because the spawn lands with a crash" then
            // "so why cant the spawn be [like] the F behaviour." Exactly
            // right - on top of placing the bike correctly above (not
            // instead of it, since a correct placement avoids the jolt in
            // the first place rather than just cleaning up after it),
            // this runs the SAME recovery F itself triggers, once, right
            // now, as a guarantee: whatever tiny settling jolt the very
            // first physics step produces, the player never sees a
            // pre-crashed bike at spawn.
            anytimeReset.ResetUpright();

            // MINI-119 follow-up, user: "i want the bike spawned in the
            // same way, i want to be able to walk to the bike and press
            // f to get on the bike." No more auto-mount - the bike sits
            // parked here, and SuperMotoStockInteractable handles F to
            // mount/dismount, same as the real dealer-purchase flow
            // already does for the TMAX (BikeInteractable). Anchors are
            // found ONCE here (bike geometry doesn't depend on who
            // mounts) and handed to the interactable.
            var vendorRider = FindDeepByName(instance.transform, "Rider 1");
            Vector3 seatLocalPos = new Vector3(0f, 0.62f, -0.05f); // sane fallback if the vendor rider isn't found
            if (vendorRider != null)
            {
                // The vendor rider's own root sits at (roughly) the seat -
                // captured BEFORE hiding it, in the bike's local space, so
                // Sacat lands where the original rider actually sat rather
                // than a guessed offset.
                seatLocalPos = instance.transform.InverseTransformPoint(vendorRider.position);
                foreach (var r in vendorRider.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            }

            Transform rightHandTarget = FindDeepByName(instance.transform, "RightHandPos");
            Transform leftHandTarget = FindDeepByName(instance.transform, "LeftHandPos");
            Transform rightFootTarget = FindDeepByName(instance.transform, "RightFootPos");
            Transform leftFootTarget = FindDeepByName(instance.transform, "LeftFootPos");

            if (rightHandTarget == null || leftHandTarget == null || rightFootTarget == null || leftFootTarget == null)
            {
                Debug.LogError("MINI-119 STOCK DEMO TEST: couldn't find the bike's own hand/foot IK anchors (RightHandPos/LeftHandPos/RightFootPos/LeftFootPos) - no interactable added, bike will spawn but can't be mounted.");
            }
            else
            {
                // MINI-119 follow-up, user: "use the old mounting system
                // you had for the TMAX." VehicleSeat + VehicleRider (the
                // same proven MINI-066 system BikeInteractable already
                // uses) instead of the Animation-Rigging-based
                // SuperMotoKinematicRider/SuperMotoStockInteractable -
                // reuses "MountBike"/"RideBike", already baked into
                // Sacat's shared controller for the TMAX.
                var seatGo = new GameObject("Seat");
                seatGo.transform.SetParent(instance.transform, false);
                seatGo.transform.localPosition = seatLocalPos;
                seatGo.transform.localRotation = Quaternion.identity;

                var seat = instance.AddComponent<VehicleSeat>();
                seat.Configure(seatGo.transform, leftHandTarget, rightHandTarget, leftFootTarget, rightFootTarget, "MountBike", "RideBike");

                // MINI-119 follow-up, user found this by hand, live,
                // while mounted: Sacat's local Position/Rotation
                // relative to the seat (0.03, -0.84, 0.04 / 0,0,0 - the
                // TMAX's own default pitch of 0 already matched, so only
                // the offset needed baking in). This is what "fits
                // properly" on THIS bike specifically - see VehicleSeat's
                // own header for why it lives here and not on
                // VehicleRider directly.
                seat.ConfigureSeatedPose(new Vector3(0.03f, -0.84f, 0.04f), 0f);

                // MINI-119 follow-up fix, user: "the character moves too
                // too high" during a wheelie. Real cause: this seat was
                // still blending toward VehicleSeat's own DEFAULT wheelie
                // offset (0, -0.40, 0.28) - TMAX's own absolute tuned
                // number, calibrated against TMAX's seated offset
                // (0.11, -0.01, 0.32). Applying that SAME absolute number
                // on top of the SuperMoto's very different seated offset
                // (above) produces a huge, wrong vertical jump - the
                // wheelie target was never actually relative to THIS
                // bike's seat at all. Applies TMAX's own relative SHIFT
                // (wheelieOffset - seatedOffset) onto the SuperMoto's own
                // seated baseline instead of the absolute number - a
                // principled starting point, not a guess, but still just
                // a starting point: the user should fine-tune this live
                // via TmaxWheelieTuner (works for whichever VehicleRider
                // is currently mounted, not TMAX-specific despite the
                // name) the same way the seated pose was tuned, and give
                // the resulting numbers back to bake in permanently.
                Vector3 tmaxSeated = new Vector3(0.11f, -0.01f, 0.32f);
                Vector3 tmaxWheelie = new Vector3(0f, -0.40f, 0.28f);
                Vector3 relativeWheelieShift = tmaxWheelie - tmaxSeated;
                seat.ConfigureWheeliePose(new Vector3(0.03f, -0.84f, 0.04f) + relativeWheelieShift, 22f);

                // MINI-119 follow-up, user: "the pillion is on the main
                // character... use the same freeze animation as the main
                // character just rig his hands to his side." Real cause
                // of the pillion floating on top of the driver: this
                // seat was never given its own baked seatedOffset - it
                // was silently using VehicleSeat's shared DEFAULT
                // (calibrated for the TMAX), the exact same class of bug
                // the driver himself had before that offset got baked
                // in. Fixed by baking one here too (reusing the driver's
                // own numbers as a starting point - same bike, similar
                // seated posture, just a different seat anchor) and
                // pushing the seat anchor itself further back so the two
                // riders don't overlap.
                //
                // Also switched off the vendor's "RidePillion" cheer clip
                // entirely per the user's own instruction - reuses
                // "RideBike" (the SAME pose the driver holds) instead.
                // Since that pose's own authored hand position reaches
                // forward as if gripping a handlebar the passenger
                // doesn't have, two simple target Transforms are added
                // at his own sides (children of the seat, so they move
                // with him) and pinned via the same continuous
                // SuperMotoHandFootLock technique already proven for the
                // driver - "rig his hands to his side" done the reliable
                // way, not Mecanim IK.
                var pillionSeatGo = new GameObject("PillionSeat");
                pillionSeatGo.transform.SetParent(instance.transform, false);
                pillionSeatGo.transform.localPosition = seatLocalPos + new Vector3(0f, 0.05f, -0.75f);
                pillionSeatGo.transform.localRotation = Quaternion.identity;

                var pillionLeftHandTarget = new GameObject("PillionLeftHandPos").transform;
                pillionLeftHandTarget.SetParent(pillionSeatGo.transform, false);
                pillionLeftHandTarget.localPosition = new Vector3(-0.2f, -0.15f, 0f);

                var pillionRightHandTarget = new GameObject("PillionRightHandPos").transform;
                pillionRightHandTarget.SetParent(pillionSeatGo.transform, false);
                pillionRightHandTarget.localPosition = new Vector3(0.2f, -0.15f, 0f);

                var pillionSeat = instance.AddComponent<VehicleSeat>();
                pillionSeat.Configure(pillionSeatGo.transform, pillionLeftHandTarget, pillionRightHandTarget, null, null, "MountBike", "RideBike");
                pillionSeat.SetRole(VehicleSeat.SeatRole.Passenger);
                pillionSeat.ConfigureSeatedPose(new Vector3(0.03f, -0.84f, 0.04f), 0f);

                var wheelieAssistForSeat = instance.GetComponent<SuperMotoWheelieAssist>();
                var interactable = instance.AddComponent<SuperMotoVehicleInteractable>();
                interactable.Configure(seat, pillionSeat, wheelieAssistForSeat, camAnchor.transform);

                // MINI-119 follow-up, user: "can you do it when i start
                // or press play, sacat is mounted on the bike... i dont
                // want to press F to mount." Triggers the SAME real,
                // live Mount() flow pressing F does - not an Edit-mode
                // save trick - so the actual "RideBike" animation state
                // and every other runtime-only effect apply correctly,
                // none of the save/reload gaps documented all task apply
                // here at all since this runs mid-Play, in the same
                // continuous session, same as a real F press would.
                // Deliberately NOT TryMountFor - that enforces a range
                // check (built for "another character hops on if
                // nearby"), unsuitable here since the player could be
                // anywhere in Grand Bay when this bike spawns. DevForceMount
                // mirrors the same naming convention already used
                // elsewhere in this project for exactly this "just put
                // them on it, unconditionally" dev/testing case.
                if (AutoMountSuperMotoOnSpawn) interactable.DevForceMount(player);
            }

            // MINI-119 follow-up, user: "where is the bike i am not
            // seeing the super moto." The bike itself was spawning fine
            // (confirmed - "i like how the bike spawns" was said about
            // this exact spot) but it's the only dev-spawned vehicle in
            // this controller that never got a minimap marker (unlike
            // DevSpawnNearPlayer's tmax/rover/test-moto, all of which add
            // one) - with no marker and no way to know it landed on the
            // Lalay road rather than near the player, "not seeing it" most
            // likely means "don't know where to look", not "didn't spawn".
            instance.AddComponent<GtaMiniMapMarker>().Configure(MiniMapMarkerKind.Vehicle, "SuperMoto (demo)", VehicleMarkerColour);

            // Parked: the bike shouldn't drive itself off the player's
            // own WASD before anyone's actually pressed F to mount.
            SetBikeInputEnabled(instance, false);

            Debug.Log($"MINI-119 STOCK DEMO TEST: pack's own SuperMotoWRagdoll spawned and parked at {spawnPos} (Lalay road, between the farm shop and produce buyer stalls), marked on the minimap, our own camera follow attached once mounted, kinematic wheelie (same method as the original bike) active, tuner panel (T) live. Controls: walk up and press F to get on/off, W/S/arrows throttle, A/D/arrows steer, Mouse0/Mouse1 lean, E/Q wheelie (remapped from LeftCtrl/LeftShift), Space brake, F (while riding) resets upright ANY TIME (not just after a crash), R reloads the whole scene.");
        }

        /// <summary>MINI-119 follow-up, user: "my character moves wit
        /// the bike controls same time so when i start the game the bike
        /// automatically starts moving." RB_Controller (and the scripts
        /// that relay input into it) read the keyboard every frame no
        /// matter who's mounted - confirmed directly in RB_Controller.cs's
        /// own Update()/FixedUpdate(), unconditional on any "am I
        /// mounted" state. Switched off here right after spawn (parked)
        /// and only switched on for the duration of an actual ride, by
        /// SuperMotoStockInteractable's own Mount/Dismount. Left alone:
        /// SuperMotoTrikeStabilizer/SuperMotoUprightAssist (passive lean
        /// correction, harmless and actually helpful while parked) and
        /// SuperMotoWheelieHud (a read-only overlay).</summary>
        public static void SetBikeInputEnabled(GameObject bikeInstance, bool enabled)
        {
            var rb = bikeInstance.GetComponent<Gadd420.RB_Controller>();
            if (rb != null) rb.enabled = enabled;
            var anytimeReset = bikeInstance.GetComponent<SuperMotoAnytimeReset>();
            if (anytimeReset != null) anytimeReset.enabled = enabled;
            var wheelieRemap = bikeInstance.GetComponent<SuperMotoWheelieKeyRemap>();
            if (wheelieRemap != null) wheelieRemap.enabled = enabled;
            var wheelieAssist = bikeInstance.GetComponent<SuperMotoWheelieAssist>();
            if (wheelieAssist != null) wheelieAssist.enabled = enabled;

            // MINI-119 follow-up, user: "the character and the bike
            // exploded up." RB_Controller was sitting disabled (parked)
            // for however long the player took to walk over - re-enabling
            // it mid-play, on the very first Update/FixedUpdate after
            // being off, has no guarantee its own internal state
            // (accumulated gear torque, crash-speed trackers other
            // scripts read) still matches the bike's actual current
            // rest state. Zeroing velocity right at the moment input
            // switches on is the same defensive recipe
            // SuperMotoAnytimeReset.ResetUpright already uses elsewhere
            // in this file for exactly this class of one-step spike -
            // just without that method's teleport-up/re-level, since the
            // bike is already sitting correctly parked and shouldn't
            // visibly jump the instant you press F.
            if (enabled)
            {
                var rigidbody = bikeInstance.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.linearVelocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }
                var crash = bikeInstance.GetComponent<Gadd420.CrashController>();
                if (crash != null) { crash.rbSpeed = 0f; crash.lateRbSpeed = 0f; }
            }
        }

        private static Transform FindDeepByName(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var found = FindDeepByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // MINI-119 follow-up: the synchronous version of this used to live
        // here (AttachVendorIK) but hit animator.avatar == null when
        // called in the same frame as spawning - replaced by
        // SacatBikeIKAttacher, which retries every frame until the avatar
        // is genuinely ready before attaching. See that file's own header.

        private static Vector3 GroundSnap(Vector3 pos)
        {
            if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f))
                return hit.point;
            return pos;
        }

        /// <summary>
        /// Puts the player's bike back on its home spot outside the farm
        /// safehouse. Called on load.
        ///
        /// Static and null-tolerant on purpose: SaveLoadSystem runs in scenes
        /// that may have no spawner and no bike at all, and a missing garage
        /// must not break loading a save.
        /// </summary>
        public static void ReturnBikeHome()
        {
            var bike = FindFirstObjectByType<TmaxBikeController>();
            if (bike == null) return;

            Transform home = Instance != null ? Instance.bikeHome : null;
            if (home == null) return;

            var rb = bike.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Zero the velocity, or a bike saved mid-ride keeps its speed
                // and immediately drives itself off the home spot.
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            bike.transform.SetPositionAndRotation(home.position, home.rotation);
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Called by ShopPanelController right after a successful
        /// purchase. No-ops (returns null) for every item id except the
        /// ones this controller knows how to spawn, so the shop's own
        /// purchase message stays the feedback for every other item.
        ///
        /// Bug fix (user report: "you placed the bike in the store after
        /// buying it. place it on the road."): this used to spawn relative
        /// to the BUYER's own facing direction - but the player faces the
        /// dealer NPC while shopping, and the dealer NPC itself always
        /// faces the road (see BuildNpc's `Quaternion.LookRotation(-right *
        /// sideMul, ...)`), so the player's forward direction points the
        /// opposite way, back into the shop/building behind them. Spawning
        /// "in front of the buyer" therefore reliably placed the bike
        /// inside the store. Fixed to use the dealer NPC's own forward
        /// direction instead, which is guaranteed to point at the road
        /// regardless of which way the player happened to be standing.
        /// </summary>
        public string SpawnPurchasedVehicle(string itemId)
        {
            // MINI-071: the Range Rover buys and spawns the same way the bike
            // does. Kept as two explicit branches rather than a generic vehicle
            // table - with exactly two vehicles a table would be indirection for
            // its own sake. The shared layer is worth extracting once the real
            // commonality is visible, not guessed at now.
            if (itemId == "range_rova")
            {
                if (roverPrefab == null) return null;
                if (_roverSpawned) return "Allu already have the Rova - check where you parked it.";

                GetDealerSpawn(out Vector3 roverPos, out Quaternion roverRot);
                var rover = Instantiate(roverPrefab, roverPos, roverRot);
                rover.name = "PlayerRangeRover";
                _roverSpawned = true;
                // MINI-113: "add minimap markers for owned/usable vehicles" -
                // added at spawn time (not build time) since the vehicle
                // GameObject doesn't exist until actually purchased.
                rover.AddComponent<GtaMiniMapMarker>().Configure(MiniMapMarkerKind.Vehicle, "Range Rover", VehicleMarkerColour);

                string roverFeedback = "Parked on the road, keys in it.";
                MissionSystem.Instance?.Alert("RANGE ROVA DELIVERED\n" + roverFeedback);
                return roverFeedback;
            }

            if (itemId != "tmax_560" || tmaxPrefab == null) return null;

            // One-time spawn - a second purchase doesn't stack a second bike.
            if (_tmaxSpawned) return "Allu already have a TNAX - check where you parked it.";

            GetDealerSpawn(out Vector3 spawnPos, out Quaternion spawnRot);

            // MINI-068: if the world already holds a bike (the one parked at the
            // safehouse), that IS your bike - move it rather than instantiating a
            // second one. Two TMAXes would both answer ReturnBikeHome and end up
            // stacked on the same spot.
            var existing = FindFirstObjectByType<TmaxBikeController>();
            GameObject bike;
            if (existing != null)
            {
                bike = existing.gameObject;
                var erb = bike.GetComponent<Rigidbody>();
                if (erb != null)
                {
                    erb.linearVelocity = Vector3.zero;
                    erb.angularVelocity = Vector3.zero;
                }
                bike.transform.SetPositionAndRotation(spawnPos, spawnRot);
                Physics.SyncTransforms();
            }
            else
            {
                bike = Instantiate(tmaxPrefab, spawnPos, spawnRot);
            }
            bike.name = "PlayerTMAX";
            _tmaxSpawned = true;
            if (bike.GetComponent<GtaMiniMapMarker>() == null)
                bike.AddComponent<GtaMiniMapMarker>().Configure(MiniMapMarkerKind.Vehicle, "TMAX 560", VehicleMarkerColour);

            string feedback = "Parked on the road, keys in it.";
            MissionSystem.Instance?.Alert("TNAX 560 DELIVERED\n" + feedback);
            return feedback;
        }

        /// <summary>
        /// Where a bought vehicle appears: in front of the DEALER, not the
        /// buyer. The player faces the dealer while shopping, so "in front of
        /// the buyer" points back into the shop - the original bug that parked
        /// the bike inside the building.
        /// </summary>
        private void GetDealerSpawn(out Vector3 spawnPos, out Quaternion spawnRot)
        {
            var dealerGo = GameObject.Find(dealerNpcName);
            Transform anchor;
            Vector3 facing;
            if (dealerGo != null)
            {
                anchor = dealerGo.transform;
                facing = anchor.forward;   // always points at the road, per BuildNpc.
            }
            else
            {
                var active = CharacterSwitchManager.Instance?.Active;
                anchor = active?.root != null ? active.root.transform : transform;
                facing = anchor.forward;
            }

            spawnPos = anchor.position + facing * spawnForwardOffset + anchor.right * spawnSideOffset;

            // Snap onto the real ground rather than trusting the NPC's stored
            // height, in case the offset moved it over a kerb or slope.
            if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f))
            {
                spawnPos = hit.point;
            }

            spawnRot = Quaternion.LookRotation(facing, Vector3.up);
        }
    }
}
