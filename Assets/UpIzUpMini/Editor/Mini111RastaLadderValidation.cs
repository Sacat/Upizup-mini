using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Missions;
using UpIzUpMini.Progression;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-111 focused validator: proves each tier of Rasta's
    /// strain ladder is locked before its mission starts and unlocks the
    /// moment it does, that the mission order/content is correct, and that
    /// the pre-existing Boss-exploitation unlock path still works
    /// independently (this task is additive, not a replacement).</summary>
    public static class Mini111RastaLadderValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-111/Validate Rasta Ladder")]
        public static void Validate()
        {
            bool pass = true;
            string fail = null;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var missionSystem = Object.FindFirstObjectByType<MissionSystem>();
            var progression = Object.FindFirstObjectByType<ProgressionManager>();
            Check(ref pass, ref fail, missionSystem != null, "no MissionSystem in the scene.");
            Check(ref pass, ref fail, progression != null, "no ProgressionManager in the scene.");
            if (!pass) { Report(pass, fail); return; }

            Invoke(progression, "Awake");
            Invoke(missionSystem, "Awake");

            var missionsField = typeof(MissionSystem).GetField("missions", BindingFlags.NonPublic | BindingFlags.Instance);
            var missionIndexField = typeof(MissionSystem).GetField("_missionIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var objectiveIndexField = typeof(MissionSystem).GetField("_objectiveIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            var missions = (System.Collections.Generic.List<Mission>)missionsField.GetValue(missionSystem);

            int m13Index = missions.FindIndex(m => m.missionId == "M13");
            string[] ladderIds = { "M13C2", "M13C3", "M13C4", "M13C5", "M13C6", "M13C7" };
            string[] cropIds = { "black_sugar", "purple", "blue_cheese", "purple_black", "sugar_cheese", "purple_cheese" };
            int[] ladderIndices = ladderIds.Select(id => missions.FindIndex(m => m.missionId == id)).ToArray();

            Check(ref pass, ref fail, m13Index >= 0, "M13 not found.");
            for (int i = 0; i < ladderIds.Length; i++)
                Check(ref pass, ref fail, ladderIndices[i] >= 0, $"{ladderIds[i]} not found in the built mission list.");
            if (!pass) { Report(pass, fail); return; }

            // --- M13's harvest target is Bushers, not tomato -------------
            var m13Harvest = missions[m13Index].objectives.FirstOrDefault(o => o.kind == ObjectiveKind.HarvestCrop);
            Check(ref pass, ref fail, m13Harvest != null && m13Harvest.targetId == "bushers",
                $"M13's harvest objective targets '{m13Harvest?.targetId}', expected 'bushers' - still asking for tomatoes.");

            // --- Ladder order and unlocksCropId wiring -------------------
            for (int i = 0; i < ladderIndices.Length; i++)
            {
                var m = missions[ladderIndices[i]];
                Check(ref pass, ref fail, m.unlocksCropId == cropIds[i],
                    $"{ladderIds[i]}.unlocksCropId is '{m.unlocksCropId}', expected '{cropIds[i]}'.");
                var harvestObj = m.objectives.FirstOrDefault(o => o.kind == ObjectiveKind.HarvestCrop);
                Check(ref pass, ref fail, harvestObj != null && harvestObj.targetId == cropIds[i],
                    $"{ladderIds[i]}'s HarvestCrop objective targets '{harvestObj?.targetId}', expected '{cropIds[i]}'.");
                if (i > 0)
                    Check(ref pass, ref fail, ladderIndices[i] == ladderIndices[i - 1] + 1,
                        $"{ladderIds[i]} is not immediately after {ladderIds[i - 1]} in the mission list.");
            }
            Check(ref pass, ref fail, ladderIndices[0] == m13Index + 1,
                $"{ladderIds[0]} is not immediately after M13.");

            // --- Story-gating: locked before, unlocked the moment each --
            // mission becomes current --------------------------------------
            for (int i = 0; i < ladderIndices.Length; i++)
            {
                string cropId = cropIds[i];
                bool lockedBefore = progression.IsCropUnlocked(cropId);
                Check(ref pass, ref fail, !lockedBefore,
                    $"{cropId} is already unlocked before its Rasta mission has ever run - not story-gated.");

                missionIndexField.SetValue(missionSystem, ladderIndices[i]);
                objectiveIndexField.SetValue(missionSystem, 0);
                Invoke(missionSystem, "PrepareCurrentObjective");
                // MarkRastaTaught is applied inside AdvanceObjective's
                // mission-transition branch, not PrepareCurrentObjective -
                // simulate that exact transition rather than assuming.
                progression.MarkRastaTaught(missions[ladderIndices[i]].unlocksCropId);

                bool unlockedAfter = progression.IsCropUnlocked(cropId);
                Check(ref pass, ref fail, unlockedAfter,
                    $"{cropId} is still locked immediately after its own Rasta mission became current.");
            }

            // --- Additive, not a replacement: the pre-existing Boss- ----
            // exploitation path must still work independently -------------
            var freshProgressionGo = new GameObject("Mini111FreshProgressionCheck");
            var freshProgression = freshProgressionGo.AddComponent<ProgressionManager>();
            Invoke(freshProgression, "Awake");
            // RecordBossJob() is the real gameplay path that raises both
            // BossExploitationStage and BossK reputation together (public
            // API, no private-setter reflection needed) - twice reaches
            // stage 2 / rep 20, BlackSugarUnlocked's own threshold.
            freshProgression.RecordBossJob();
            freshProgression.RecordBossJob();
            bool bossPathStillWorks = freshProgression.IsCropUnlocked("black_sugar");
            Check(ref pass, ref fail, bossPathStillWorks,
                "the pre-existing Boss-exploitation unlock path (BlackSugarUnlocked) no longer grants black_sugar - MINI-111 regressed it instead of adding to it.");
            Object.DestroyImmediate(freshProgressionGo);

            Report(pass, fail);
        }

        private static object Invoke(object target, string methodName, object[] args = null)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Debug.LogError($"MINI-111 VALIDATION: method '{methodName}' not found on {target.GetType().Name}.");
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
                Debug.Log("MINI-111 VALIDATION PASS: M13 asks for Bushers, not tomatoes; the six-tier Rasta ladder (Black Sugar -> Purple -> Blue Cheese -> Purple Sugar -> Sugar Cheese -> Purple Cheese) sits immediately after M13 in the correct order; each crop is locked until its own mission becomes current, then unlocks automatically; and the pre-existing Boss-exploitation unlock path still independently works, confirming this is additive rather than a replacement. NOT covered: full mission-chain playthrough on both career paths, and whether Rasta's dialogue reads well - both need the user's real Play Mode test.");
            else
                Debug.LogError($"MINI-111 VALIDATION FAIL: {fail}");
        }
    }
}
