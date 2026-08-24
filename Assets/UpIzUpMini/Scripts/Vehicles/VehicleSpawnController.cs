using UnityEngine;
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
        [Tooltip("How far from the dealer, along the dealer's own facing direction (which points at the road - see Mini011PhaseBSetup.BuildNpc), the bike appears.")]
        [SerializeField] private float spawnForwardOffset = 5f;
        [SerializeField] private float spawnSideOffset = 1.5f;
        [SerializeField] private string dealerNpcName = "NPC_CarDealer";

        [Tooltip("MINI-068: where the bike lives. Set by Mini011PhaseBSetup to the parking spot outside the farm safehouse - the bike is returned here on every load, so it can never be stranded somewhere unreachable.")]
        [SerializeField] private Transform bikeHome;

        private bool _tmaxSpawned;
        private bool _roverSpawned;
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
        // MINI-119 follow-up, user: "just remove the bike and range rova
        // from the start of the game. it was just for testing." Testing
        // aid turned off - vehicles go back to the normal dealer-purchase
        // flow only.
        private const bool DevSpawnNearPlayerOnStart = false;
        private bool _devSpawnDone;

        private void Awake() => Instance = this;

        private void Update()
        {
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
