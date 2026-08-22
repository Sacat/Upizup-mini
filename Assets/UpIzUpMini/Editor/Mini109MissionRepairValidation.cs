using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.Missions;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-109 focused validator: reproduces, then proves the fix for,
    /// the three reported stuck-mission defects plus the new retrospective-
    /// completion rule. Play Mode does not tick in this batch environment,
    /// so MonoBehaviour lifecycle calls (Awake/Start) are invoked directly
    /// via reflection - the same technique used by every prior mission/
    /// economy validator this project has (e.g. the MINI-080 chain-purchase
    /// simulation), not a new pattern.
    /// </summary>
    public static class Mini109MissionRepairValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-109/Validate Mission Repairs")]
        public static void Validate()
        {
            bool pass = true;
            string fail = null;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var economy = Object.FindFirstObjectByType<EconomyManager>();
            var missionSystem = Object.FindFirstObjectByType<MissionSystem>();
            Check(ref pass, ref fail, economy != null, "no EconomyManager in the scene.");
            Check(ref pass, ref fail, missionSystem != null, "no MissionSystem in the scene.");
            if (!pass) { Report(pass, fail); return; }

            Invoke(economy, "Awake");
            Invoke(missionSystem, "Awake");

            var missionsField = typeof(MissionSystem).GetField("missions", BindingFlags.NonPublic | BindingFlags.Instance);
            var missionIndexField = typeof(MissionSystem).GetField("_missionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var objectiveIndexField = typeof(MissionSystem).GetField("_objectiveIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var missions = (System.Collections.Generic.List<Mission>)missionsField.GetValue(missionSystem);

            int m5bIndex = missions.FindIndex(m => m.missionId == "M5B");
            int m10wIndex = missions.FindIndex(m => m.missionId == "M10W");
            int m13Index = missions.FindIndex(m => m.missionId == "M13");
            int m17Index = missions.FindIndex(m => m.missionId == "M17");
            Check(ref pass, ref fail, m5bIndex >= 0, "M5B (Clean Face) not found in the built mission list.");
            Check(ref pass, ref fail, m10wIndex >= 0, "M10W (Black Sugar) not found in the built mission list.");
            Check(ref pass, ref fail, m13Index >= 0, "M13 (Ital and Elders) not found in the built mission list.");
            Check(ref pass, ref fail, m17Index >= 0, "M17 (Wheels of Your Own) not found in the built mission list.");
            if (!pass) { Report(pass, fail); return; }

            // --- Bug 1: Clean Face / Normy idempotent payment -----------
            var normyGo = GameObject.Find("NPC_Normy");
            Check(ref pass, ref fail, normyGo != null, "NPC_Normy not found in the scene.");
            var normy = normyGo != null ? normyGo.GetComponent<TownNPCInteractable>() : null;
            if (normy != null)
            {
                missionIndexField.SetValue(missionSystem, m5bIndex);
                objectiveIndexField.SetValue(missionSystem, 1); // BribeNormy is M5B's 2nd objective (index 1)
                Invoke(missionSystem, "PrepareCurrentObjective");

                SetMoney(economy, 0);
                SetHeat(economy, 0f);

                string shortMsg = (string)Invoke(normy, "HandleNormy");
                Check(ref pass, ref fail, shortMsg != null && shortMsg.Contains("short"),
                    $"expected a 'you're short' message when Normy's mission payment can't be afforded, got: {shortMsg}");
                Check(ref pass, ref fail, economy.Money == 0, "Normy charged money even though the payment was refused.");
                Check(ref pass, ref fail, (int)missionIndexField.GetValue(missionSystem) == m5bIndex,
                    "mission advanced past M5B despite the payment being refused.");

                SetMoney(economy, 100);
                string payMsg = (string)Invoke(normy, "HandleNormy");
                Check(ref pass, ref fail, payMsg != null && payMsg.Contains("$100"),
                    $"expected the mission-payment confirmation line, got: {payMsg}");
                // BribeNormy is M5B's LAST objective, so paying it also
                // completes the whole mission and pays out its own
                // rewardMoney (80) in the same call - 100 - 100 + 80 = 80,
                // not 0.
                int expectedMoney = 100 - 100 + missions[m5bIndex].rewardMoney;
                Check(ref pass, ref fail, economy.Money == expectedMoney,
                    $"expected Money=={expectedMoney} after the $100 mission payment plus the M5B completion reward, got {economy.Money}.");
                int afterPayMissionIndex = (int)missionIndexField.GetValue(missionSystem);
                Check(ref pass, ref fail, afterPayMissionIndex > m5bIndex,
                    $"MISSION DID NOT ADVANCE past M5B (Clean Face) after a valid Normy payment - the exact reported bug. missionIndex={afterPayMissionIndex}, m5bIndex={m5bIndex}.");

                // Duplicate-interaction / idempotency: re-interacting must
                // NOT charge again, since IsCurrentObjective(BribeNormy) is
                // now false - it must fall through to the ambient service,
                // which itself is a no-op at zero heat.
                SetMoney(economy, 100);
                string secondMsg = (string)Invoke(normy, "HandleNormy");
                Check(ref pass, ref fail, economy.Money == 100,
                    $"Normy charged money AGAIN on a second interaction after the mission already advanced - not idempotent. Money={economy.Money}, response: {secondMsg}");
            }

            // --- Bug 2: Black Sugar delivery to Boss J -------------------
            var bossJGo = GameObject.Find("NPC_BossJ");
            Check(ref pass, ref fail, bossJGo != null, "NPC_BossJ not found in the scene.");
            var bossJ = bossJGo != null ? bossJGo.GetComponent<TownNPCInteractable>() : null;
            if (bossJ != null)
            {
                economy.AddCrop("bushers", -economy.GetCount("bushers")); // ensure 0 bushers held
                economy.AddCrop("black_sugar", 3); // simulate 3 harvested Black Sugar, nothing else

                bool anyHeld = (bool)Invoke(bossJ, "HasAnySellableIllegalStock");
                Check(ref pass, ref fail, anyHeld,
                    "HasAnySellableIllegalStock() is false while holding 3 Black Sugar and 0 Bushers - the exact reported bug (the old gate only checked Bushers).");

                missionIndexField.SetValue(missionSystem, m10wIndex);
                objectiveIndexField.SetValue(missionSystem, 2); // SellCrop/BossK is M10W's 3rd objective (index 2)
                Invoke(missionSystem, "PrepareCurrentObjective");

                SetMoney(economy, 0);
                Invoke(bossJ, "Interact", new object[] { bossJGo });

                Check(ref pass, ref fail, economy.GetCount("black_sugar") == 0,
                    $"expected Black Sugar inventory to be sold/cleared, found {economy.GetCount("black_sugar")} remaining.");
                int afterSellMissionIndex = (int)missionIndexField.GetValue(missionSystem);
                Check(ref pass, ref fail, afterSellMissionIndex > m10wIndex,
                    $"MISSION DID NOT ADVANCE past M10W after delivering Black Sugar to Boss J with 0 Bushers held - the exact reported bug. missionIndex={afterSellMissionIndex}, m10wIndex={m10wIndex}.");
            }

            // --- Bug 3: Rasta's stale marker -----------------------------
            var rastaGo = GameObject.Find("NPC_RastaMentor");
            Check(ref pass, ref fail, rastaGo != null, "NPC_RastaMentor not found in the scene.");
            if (rastaGo != null && m13Index >= 0)
            {
                var m13Marker = missions[m13Index].objectives[0].markerPosition;
                float d = Vector3.Distance(m13Marker, rastaGo.transform.position);
                // Markers intentionally float ~0.15m above the NPC for
                // readability (see PositionOf's own +Vector3.up*0.15f) -
                // a wide-but-bounded tolerance catches real staleness
                // (the pre-fix gap measured 312m) without failing on that
                // deliberate offset or minor terrain-resample variance.
                Check(ref pass, ref fail, d < 1.0f,
                    $"M13's TalkTo-Rasta marker is {d:F2}m from Rasta's real built position - still stale.");
            }

            // --- Bug 4: retrospective BuyItem completion -----------------
            if (m17Index >= 0)
            {
                var ownedField = typeof(EconomyManager).GetField("_owned", BindingFlags.NonPublic | BindingFlags.Instance);
                var owned = (System.Collections.Generic.HashSet<string>)ownedField.GetValue(economy);
                owned.Add("tmax_560"); // simulate an earlier, already-completed purchase

                missionIndexField.SetValue(missionSystem, m17Index);
                objectiveIndexField.SetValue(missionSystem, 0);
                Invoke(missionSystem, "PrepareCurrentObjective");

                int afterRetroMissionIndex = (int)missionIndexField.GetValue(missionSystem);
                Check(ref pass, ref fail, afterRetroMissionIndex > m17Index,
                    $"M17 (BuyItem tmax_560) did not retrospectively credit an already-owned item - the player would be forced to repeat a purchase EconomyManager.TryPurchase would refuse anyway. missionIndex={afterRetroMissionIndex}, m17Index={m17Index}.");

                owned.Remove("tmax_560");
            }

            // --- Save-compatible IDs unchanged ----------------------------
            Check(ref pass, ref fail, bossJGo != null && bossJ != null
                && (string)GetPrivateField(bossJ, "npcName") == "BossK",
                "Boss J's internal npcName is no longer 'BossK' - would break save compatibility.");

            Report(pass, fail);
        }

        private static void SetMoney(EconomyManager economy, int target)
        {
            economy.AddMoney(target - economy.Money);
        }

        private static void SetHeat(EconomyManager economy, float target)
        {
            economy.AddHeat(target - economy.Heat);
        }

        private static object Invoke(object target, string methodName, object[] args = null)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError($"MINI-109 VALIDATION: method '{methodName}' not found on {target.GetType().Name}.");
                return null;
            }
            return method.Invoke(target, args);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(target);
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }

        private static void Report(bool pass, string fail)
        {
            if (pass)
                Debug.Log("MINI-109 VALIDATION PASS: Normy's Clean Face payment is a one-time, non-double-chargeable transaction independent of the ambient heat-service gate; Boss J correctly detects and sells held Black Sugar even with zero Bushers, advancing M10W; Rasta's M13 marker matches his real built position; an already-owned item (M17) is retrospectively credited instead of demanding a repeat purchase; save-facing IDs (BossK) are unchanged. NOT covered: full end-to-end mission-chain playthrough from M1, and mission/dialogue feel - both need the user's real Play Mode test.");
            else
                Debug.LogError($"MINI-109 VALIDATION FAIL: {fail}");
        }
    }
}
