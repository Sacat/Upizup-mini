using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Missions;

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

        private void Awake() => Instance = this;

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
