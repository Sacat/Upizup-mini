using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.Missions;

namespace UpIzUpMini.Progression
{
    public class LandRiskController : MonoBehaviour
    {
        [SerializeField] CropDefinition[] crops;
        [SerializeField] float checkEverySeconds = 180f;
        float nextCheck;

        // MINI-039: "if police is close to your plantation with weed it
        // can be confiscated" - immediate and location-driven, unlike the
        // periodic heat-threshold check above (which only fires on the
        // bought land_montine expansion). This applies to the whole
        // Montine plantation - the starting plots included - since the
        // user's ask wasn't scoped to the purchased expansion specifically.
        [Header("Proximity confiscation")]
        [SerializeField] Vector3 plantationCenter;
        [SerializeField] float plantationRadius = 15f;
        [SerializeField] float proximityCooldownSeconds = 25f;
        float nextProximityCheck;

        void Start() => nextCheck = Time.time + checkEverySeconds;

        void Update()
        {
            CheckPeriodicRisk();
            CheckPoliceProximity();
        }

        void CheckPeriodicRisk()
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + checkEverySeconds;
            var e = EconomyManager.Instance;
            if (e == null || !e.OwnsItem("land_montine")) return;
            var p = ProgressionManager.Instance;
            if (e.Heat >= 50f) { RemoveInventory(true, .5f); MissionSystem.Instance?.Alert("POLICE SEARCH\nPolice seize half di zeb stored from your bought land."); }
            else if (p != null && p.GangReputation < 15 && Random.value < .35f) { RemoveInventory(false, .25f); MissionSystem.Instance?.Alert("RIVAL SABOTAGE\nThieves damage stock from your bought farm plots."); }
        }

        /// <summary>
        /// Confiscates a portion of illegal stock the instant an officer
        /// wanders within plantationRadius of the farm while the player
        /// holds any illegal crop/seed - a GTA-style "they'll find it if
        /// they get close enough" risk, rather than the periodic check's
        /// blind timer. Cooldown-gated so a lingering officer doesn't
        /// strip the whole stash frame by frame.
        /// </summary>
        void CheckPoliceProximity()
        {
            if (Time.time < nextProximityCheck) return;
            if (!HasIllegalStock()) return;

            foreach (var npc in Object.FindObjectsByType<TownNPCInteractable>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (npc.Role != NpcRole.Police) continue;
                if (Vector3.Distance(npc.transform.position, plantationCenter) > plantationRadius) continue;

                nextProximityCheck = Time.time + proximityCooldownSeconds;
                RemoveInventory(true, .4f);
                MissionSystem.Instance?.Alert("POLICE NEAR DI PLANTATION\nAn officer get close enough to spot di zeb - some of it gone.");
                return;
            }
        }

        bool HasIllegalStock()
        {
            if (crops == null || EconomyManager.Instance == null) return false;
            foreach (var crop in crops)
            {
                if (crop == null || !crop.isIllegal) continue;
                if (EconomyManager.Instance.GetCount(crop.cropId) > 0) return true;
                if (EconomyManager.Instance.GetSeeds(crop.cropId) > 0) return true;
            }
            return false;
        }

        void RemoveInventory(bool illegalOnly, float fraction)
        {
            if (crops == null || EconomyManager.Instance == null) return;
            foreach (var crop in crops) {
                if (crop == null || (illegalOnly && !crop.isIllegal)) continue;
                int loss = Mathf.FloorToInt(EconomyManager.Instance.GetCount(crop.cropId) * fraction);
                if (loss > 0) EconomyManager.Instance.AddCrop(crop.cropId, -loss);
            }
        }
    }
}
