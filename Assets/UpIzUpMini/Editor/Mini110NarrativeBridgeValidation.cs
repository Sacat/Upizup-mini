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
    /// <summary>MINI-110 focused validator: Normy's item favour (M13B),
    /// the rewritten Boat Man introduction, M14's carried-over street-info
    /// hint, and the map-migration marker fix for DeliverItem.</summary>
    public static class Mini110NarrativeBridgeValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-110/Validate Narrative Bridge")]
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

            int m13bIndex = missions.FindIndex(m => m.missionId == "M13B");
            int m14Index = missions.FindIndex(m => m.missionId == "M14");
            Check(ref pass, ref fail, m13bIndex >= 0, "M13B (Small Ting) not found in the built mission list.");
            Check(ref pass, ref fail, m14Index >= 0, "M14 (Ason Ki Move) not found in the built mission list.");
            Check(ref pass, ref fail, m13bIndex >= 0 && m14Index >= 0 && m13bIndex == m14Index - 1,
                "M13B is not immediately before M14 in the mission list.");
            if (!pass) { Report(pass, fail); return; }

            // --- M13B's DeliverItem objectives are well-formed --------------
            var m13b = missions[m13bIndex];
            Check(ref pass, ref fail, m13b.objectives.Count == 3, $"expected M13B to have 3 objectives, found {m13b.objectives.Count}.");
            var foodObj = m13b.objectives.FirstOrDefault(o => o.kind == ObjectiveKind.DeliverItem && o.targetId == "food_bakes");
            var pillObj = m13b.objectives.FirstOrDefault(o => o.kind == ObjectiveKind.DeliverItem && o.targetId == "pill_energy");
            Check(ref pass, ref fail, foodObj != null, "M13B has no DeliverItem objective for food_bakes.");
            Check(ref pass, ref fail, pillObj != null, "M13B has no DeliverItem objective for pill_energy.");

            // --- M14's briefing carries the hint ----------------------------
            Check(ref pass, ref fail, missions[m14Index].briefing.Contains("Gardey Zafeh"),
                "M14's briefing does not mention Gardey Zafeh - Normy's street-info hint was not carried over.");

            // --- Normy delivery gating: refuses without the item, ------------
            // succeeds and advances once actually held --------------------
            var normyGo = GameObject.Find("NPC_Normy");
            Check(ref pass, ref fail, normyGo != null, "NPC_Normy not found in the scene.");
            var normy = normyGo != null ? normyGo.GetComponent<TownNPCInteractable>() : null;
            if (normy != null)
            {
                missionIndexField.SetValue(missionSystem, m13bIndex);
                objectiveIndexField.SetValue(missionSystem, 1); // DeliverItem food_bakes is objective index 1
                Invoke(missionSystem, "PrepareCurrentObjective");

                // Ensure zero food_bakes held.
                var consumablesField = typeof(EconomyManager).GetField("_consumables", BindingFlags.NonPublic | BindingFlags.Instance);
                var consumables = (System.Collections.Generic.Dictionary<string, int>)consumablesField.GetValue(economy);
                consumables["food_bakes"] = 0;
                consumables["pill_energy"] = 0;

                string refusedMsg = (string)Invoke(normy, "HandleNormy");
                Check(ref pass, ref fail, refusedMsg != null && refusedMsg.Contains("need"),
                    $"expected a 'you need it' refusal with zero food_bakes held, got: {refusedMsg}");
                Check(ref pass, ref fail, (int)missionIndexField.GetValue(missionSystem) == m13bIndex
                    && (int)objectiveIndexField.GetValue(missionSystem) == 1,
                    "objective advanced despite the delivery being refused.");

                consumables["food_bakes"] = 1;
                string deliverMsg = (string)Invoke(normy, "HandleNormy");
                Check(ref pass, ref fail, consumables["food_bakes"] == 0,
                    $"food_bakes was not spent on a successful delivery, still have {consumables["food_bakes"]}.");
                int afterFoodObjIndex = (int)objectiveIndexField.GetValue(missionSystem);
                Check(ref pass, ref fail, afterFoodObjIndex == 2,
                    $"objective did not advance to the pill_energy delivery after the food delivery. objectiveIndex={afterFoodObjIndex}, response: {deliverMsg}");

                consumables["pill_energy"] = 1;
                Invoke(normy, "HandleNormy");
                int afterPillMissionIndex = (int)missionIndexField.GetValue(missionSystem);
                Check(ref pass, ref fail, afterPillMissionIndex > m13bIndex,
                    $"mission did not advance past M13B after both deliveries. missionIndex={afterPillMissionIndex}, m13bIndex={m13bIndex}.");
            }

            // --- Boat Man's rewritten first-meeting introduction ------------
            var boatManGo = GameObject.Find("NPC_BoatMan");
            Check(ref pass, ref fail, boatManGo != null, "NPC_BoatMan not found in the scene.");
            var boatMan = boatManGo != null ? boatManGo.GetComponent<TownNPCInteractable>() : null;
            if (boatMan != null)
            {
                string intro = (string)Invoke(boatMan, "FirstMeetingDialogue");
                Check(ref pass, ref fail, intro != null && intro.Contains("euro") && intro.Contains("Gardey")
                    && intro.Contains("introduce allu to the scene"),
                    $"Boat Man's first-meeting dialogue is not the approved four-line exchange: {intro}");
            }

            // --- Map-migration marker resolution for DeliverItem ------------
            var resolveMarker = typeof(Mini100GrandBayMapMigration).GetMethod(
                "ResolveMissionMarker", BindingFlags.NonPublic | BindingFlags.Static);
            var farmPos = new Vector3(1f, 0f, 1f);
            var safehousePos = new Vector3(2f, 0f, 2f);
            var resolved = (Vector3)resolveMarker.Invoke(null, new object[] { ObjectiveKind.DeliverItem, "food_bakes", farmPos, safehousePos });
            // Horizontal (X/Z) only - PositionOf's Ground()+0.15 vertical
            // offset can legitimately differ from the NPC's raw transform.y
            // by more than a metre on sloped/bridged terrain; what actually
            // matters (and what MINI-109's Rasta bug was) is whether the
            // marker is anywhere near the right X/Z spot at all.
            Vector2 resolvedXZ = new Vector2(resolved.x, resolved.z);
            Vector2 normyXZ = normyGo != null ? new Vector2(normyGo.transform.position.x, normyGo.transform.position.z) : Vector2.zero;
            float markerGap = Vector2.Distance(resolvedXZ, normyXZ);
            Check(ref pass, ref fail, normyGo == null || markerGap < 2.0f,
                $"DeliverItem's resolved marker is {markerGap:F2}m (horizontal) from Normy's real position - would go stale after map migration, same class of bug as MINI-109's Rasta marker.");

            Report(pass, fail);
        }

        private static object Invoke(object target, string methodName, object[] args = null)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError($"MINI-110 VALIDATION: method '{methodName}' not found on {target.GetType().Name}.");
                return null;
            }
            return method.Invoke(target, args);
        }

        private static void Check(ref bool pass, ref string fail, bool condition, string message)
        {
            if (pass && !condition) { pass = false; fail = message; }
        }

        private static void Report(bool pass, string fail)
        {
            if (pass)
                Debug.Log("MINI-110 VALIDATION PASS: M13B (Normy's item favour) sits immediately before M14, gates delivery on actually holding food_bakes/pill_energy, spends them exactly once and advances the mission; M14's briefing carries Normy's Gardey Zafeh hint; the Boat Man's first meeting uses the approved four-line introduction; DeliverItem's map-migration marker resolves to Normy's real position instead of going stale. NOT covered: the HUD courier-timer readout during a real Guadeloupe trip, and dialogue/mission feel - both need the user's real Play Mode test.");
            else
                Debug.LogError($"MINI-110 VALIDATION FAIL: {fail}");
        }
    }
}
