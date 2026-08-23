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
        private const bool DevSpawnNearPlayerOnStart = true;

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
            if (stockDemoBikePrefab == null)
            {
                Debug.LogError("MINI-119 STOCK DEMO TEST: stockDemoBikePrefab not wired - run Mini119WireStockDemoBike.");
                return;
            }

            // MINI-119 follow-up, user: "can you spawn on the lalay
            // road." The "Sign_LALAY" street sign (Mini011PhaseBSetup.
            // BuildSign) is built 4.6m sideways (world +X) from the actual
            // Lalay road centerline point it marks - subtracting that same
            // offset back out lands right on the road itself, not beside
            // it. Falls back to the old near-player spawn if the sign
            // can't be found (e.g. a scene without Mini011's world build).
            Vector3 forward = player.transform.forward;
            Vector3 spawnPos;
            var lalaySign = GameObject.Find("Sign_LALAY");
            if (lalaySign != null)
            {
                spawnPos = GroundSnap(lalaySign.transform.position - new Vector3(4.6f, 0f, 0f));
            }
            else
            {
                spawnPos = GroundSnap(player.transform.position + forward * 8f);
            }

            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.IsControlled = false;
            player.SetActive(false);

            var instance = (GameObject)Instantiate(stockDemoBikePrefab, spawnPos, Quaternion.LookRotation(forward, Vector3.up));
            instance.name = "StockDemoSuperMoto";

            var shortcuts = instance.GetComponent<Gadd420.KeyBoardShortCuts>();
            if (shortcuts == null) shortcuts = instance.AddComponent<Gadd420.KeyBoardShortCuts>();
            shortcuts.currentBike = instance.transform;

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
            var stockInput = instance.GetComponent<Gadd420.Input_Manager>();
            if (stockInput != null) DestroyImmediate(stockInput);
            instance.AddComponent<SuperMotoWheelieKeyRemap>();

            // MINI-119 follow-up, user: "i prefer my wheelie system with
            // the collider system activated on wheelie." Ported outrigger
            // stabilizer - see SuperMotoTrikeStabilizer's own header.
            instance.AddComponent<SuperMotoTrikeStabilizer>();

            // MINI-119 follow-up, user: "use my camera follow system, from
            // my bike system because for this one i must keep turning the
            // mouse to it can keep track but with my old system it
            // automatically tracks and follows the character properly."
            // Same anchor + SetTarget/OrbitLocked pattern SuperMotoInteractable.
            // Possess() already uses for the mapped bike - our OWN camera,
            // no mouse-orbit required, reused as-is rather than reinvented.
            var cam = FindFirstObjectByType<ThirdPersonFollowCamera>();
            if (cam != null)
            {
                var anchorGo = new GameObject("StockDemoCameraAnchor");
                var anchor = anchorGo.AddComponent<BikeCameraAnchor>();
                anchor.Follow(instance.transform);
                cam.SetTarget(anchor.transform);
                cam.OrbitLocked = true;
            }

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

            Debug.Log("MINI-119 STOCK DEMO TEST: pack's own SuperMotoWRagdoll spawned, on-foot character disabled, our own camera follow attached, trike stabilizer active on wheelie, tuner panel (T) live. Controls: W/S throttle, A/D steer, Mouse0/Mouse1 lean, E/Q wheelie (remapped from LeftCtrl/LeftShift), Space brake. R reloads the whole scene (hard reset), F flips the bike upright when it's flagged as crashed (soft reset). No mount/dismount key - you start already on it.");
        }

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
