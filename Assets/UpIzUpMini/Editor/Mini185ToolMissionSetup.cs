using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Combat;
using UpIzUpMini.Economy;
using UpIzUpMini.Interaction;
using UpIzUpMini.Missions;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Targeted mission insertion; never invokes the historical world builder.</summary>
    public static class Mini185ToolMissionSetup
    {
        const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string ToolPath = "Assets/UpIzUpMini/Data/Shop/Mini183/LalaySidearm.asset";

        [MenuItem("Up Iz Up Mini/MINI-185/Apply Tool Mission")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var system = UnityEngine.Object.FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include);
            var trader = FindTrader();
            if (system == null || trader == null) throw new InvalidOperationException("MissionSystem or NPC_BlackMarket missing; scene not saved.");
            var missions = Missions(system);
            int m12 = missions.FindIndex(m => m.missionId == "M12");
            int m13 = missions.FindIndex(m => m.missionId == "M13");
            int existing = missions.FindIndex(m => m.missionId == "M12T");
            if (m12 < 0 || m13 < 0 || (existing < 0 && m13 != m12 + 1)
                || (existing >= 0 && (existing != m12 + 1 || m13 != existing + 1)))
                throw new InvalidOperationException("Expected M12 -> [M12T] -> M13; scene not saved.");

            var toolMission = MakeMission(trader.transform.position);
            if (existing >= 0) missions[existing] = toolMission;
            else missions.Insert(m12 + 1, toolMission);
            EditorUtility.SetDirty(system);

            var item = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(ToolPath);
            if (item == null || item.itemId != FirearmController.SidearmId)
                throw new InvalidOperationException("Lalay sidearm asset missing; scene not saved.");
            item.displayName = "Lalay Tool";
            EditorUtility.SetDirty(item);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("MINI-185 APPLY PASS: M12 -> M12T Get Your Tool -> M13, trader marker and shop label saved.");
        }

        [MenuItem("Up Iz Up Mini/MINI-185/Verify Tool Mission")]
        public static void Verify()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var system = UnityEngine.Object.FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include);
            var trader = FindTrader();
            if (system == null || trader == null) throw new InvalidOperationException("Mission system or trader missing.");
            var missions = Missions(system);
            int index = missions.FindIndex(m => m.missionId == "M12T");
            if (index < 1 || index + 1 >= missions.Count || missions[index - 1].missionId != "M12"
                || missions[index + 1].missionId != "M13" || missions.FindAll(m => m.missionId == "M12T").Count != 1)
                throw new InvalidOperationException("Tool mission order/uniqueness failed.");
            var objectives = missions[index].objectives;
            if (objectives.Count != 3 || objectives[0].kind != ObjectiveKind.TalkTo || objectives[0].targetId != "BlackMarket"
                || objectives[1].kind != ObjectiveKind.BuyItem || objectives[1].targetId != FirearmController.SidearmId
                || objectives[2].kind != ObjectiveKind.FireTool || objectives[2].hasMarker
                || Vector3.Distance(objectives[0].markerPosition, trader.transform.position) > .01f)
                throw new InvalidOperationException("Tool mission objectives/marker failed.");
            if (system.ResolveSavedMissionIndex(null, index, 0) != index + 1
                || system.ResolveSavedMissionIndex("M13", 0, 2) != index + 1
                || system.ResolveSavedMissionIndex("M12T", 0, 2) != index)
                throw new InvalidOperationException("Mission save migration failed.");
            var item = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(ToolPath);
            if (item == null || item.displayName != "Lalay Tool") throw new InvalidOperationException("Tool shop label failed.");
            Debug.Log("MINI-185 VERIFY PASS: order, objectives, trader marker, save migration and tool label.");
        }

        [MenuItem("Up Iz Up Mini/MINI-185/Exercise Tool Mission")]
        public static void Exercise()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var system = UnityEngine.Object.FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include);
            var economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>(FindObjectsInactive.Include);
            if (system == null || economy == null) throw new InvalidOperationException("Mission or economy manager missing.");
            typeof(MissionSystem).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(system, null);
            typeof(EconomyManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(economy, null);
            int index = Missions(system).FindIndex(m => m.missionId == "M12T");
            system.LoadState(index, 0, new[] { 0, 0, 0 });
            if (system.CurrentObjective?.kind != ObjectiveKind.TalkTo) throw new InvalidOperationException("Talk objective failed.");
            system.Notify(ObjectiveKind.TalkTo, "BlackMarket");
            if (system.CurrentObjective?.kind != ObjectiveKind.BuyItem) throw new InvalidOperationException("Buy objective failed.");
            var item = AssetDatabase.LoadAssetAtPath<ShopItemDefinition>(ToolPath);
            economy.AddMoney(1000);
            if (!economy.TryPurchase(item, out string purchase)) throw new InvalidOperationException("Tool purchase failed: " + purchase);
            system.Notify(ObjectiveKind.BuyItem, item.itemId);
            if (system.CurrentObjective?.kind != ObjectiveKind.FireTool) throw new InvalidOperationException("Fire objective failed.");
            system.Notify(ObjectiveKind.FireTool);
            if (system.CurrentMissionId != "M13") throw new InvalidOperationException("Tool mission did not transition to M13.");

            system.LoadState(index, 1, new[] { 1, 0, 0 });
            if (system.CurrentObjective?.kind != ObjectiveKind.FireTool)
                throw new InvalidOperationException("Previously owned tool did not receive retrospective purchase credit.");
            if (system.ResolveSavedMissionIndex(null, index, 0) != index + 1)
                throw new InvalidOperationException("Legacy save transition failed.");
            system.LoadState(index, 0, new[] { 0, 0, 0 });
            system.SetResumeAfterTool("M13C2", 1, new[] { 1, 0 });
            system.Notify(ObjectiveKind.TalkTo, "BlackMarket");
            if (system.CurrentObjective?.kind != ObjectiveKind.FireTool)
                throw new InvalidOperationException("Deferred old-save mission did not credit the owned tool.");
            system.Notify(ObjectiveKind.FireTool);
            if (system.CurrentMissionId != "M13C2" || system.CurrentObjective?.kind != ObjectiveKind.HarvestCrop)
                throw new InvalidOperationException("Old-save mission/progress was not resumed after the tool mission.");
            if (system.ResolveSavedMissionIndex(MissionSystem.CompletedMissionSentinel, 0, 2) != Missions(system).Count)
                throw new InvalidOperationException("Completed legacy save sentinel failed.");
            Debug.Log("MINI-185 EXERCISE PASS: talk -> purchase -> test shot -> M13, prior ownership, legacy index, deferred mission resume.");
        }

        public static Mission MakeMission(Vector3 traderPosition) => new Mission
        {
            missionId = "M12T",
            title = "Get Your Tool",
            briefing = "The boys need a tool — a gun. The Lalay trader can set them up quietly. Earn the money first if you need it.",
            rewardMoney = 100,
            objectives = new List<MissionObjective>
            {
                new MissionObjective
                {
                    kind = ObjectiveKind.TalkTo, targetId = "BlackMarket",
                    instruction = "Find the Lalay black-market trader and ask about a tool  [ E ]",
                    markerPosition = traderPosition
                },
                new MissionObjective
                {
                    kind = ObjectiveKind.BuyItem, targetId = FirearmController.SidearmId,
                    instruction = "Buy the tool from the trader  [ 1 ]  ($750)",
                    dialogueBanner = "Trader: I have a tool under the counter. Keep it quiet, and mind how you use it.",
                    markerPosition = traderPosition
                },
                new MissionObjective
                {
                    kind = ObjectiveKind.FireTool,
                    instruction = "Close the shop, aim with right click and fire one test shot with left click",
                    dialogueBanner = "Test the tool once. Keep clear of people.",
                    hasMarker = false
                }
            }
        };

        static TownNPCInteractable FindTrader()
        {
            foreach (var npc in UnityEngine.Object.FindObjectsByType<TownNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (npc.name == "NPC_BlackMarket" && npc.Role == NpcRole.BlackMarket) return npc;
            return null;
        }

        static List<Mission> Missions(MissionSystem system) =>
            (List<Mission>)typeof(MissionSystem).GetField("missions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(system);
    }
}
