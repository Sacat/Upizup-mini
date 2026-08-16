using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Missions;

namespace UpIzUpMini.Progression
{
    public class LandRiskController : MonoBehaviour
    {
        [SerializeField] CropDefinition[] crops;
        [SerializeField] float checkEverySeconds = 180f;
        float nextCheck;
        void Start() => nextCheck = Time.time + checkEverySeconds;
        void Update()
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + checkEverySeconds;
            var e = EconomyManager.Instance;
            if (e == null || !e.OwnsItem("land_montine")) return;
            var p = ProgressionManager.Instance;
            if (e.Heat >= 50f) { RemoveInventory(true, .5f); MissionSystem.Instance?.Alert("POLICE SEARCH\nPolice seize half the weed stored from your bought land."); }
            else if (p != null && p.GangReputation < 15 && Random.value < .35f) { RemoveInventory(false, .25f); MissionSystem.Instance?.Alert("RIVAL SABOTAGE\nThieves damage stock from your bought farm plots."); }
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
