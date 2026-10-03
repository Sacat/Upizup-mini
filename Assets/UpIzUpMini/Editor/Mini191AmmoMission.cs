using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Interaction;
using UpIzUpMini.Missions;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-191: inserts the "Ammunition" mission (M12A) right after M12T in the given scene.
    /// Targeted (never runs the whole-world builder). Scene path from env MINI191_SCENE, default the new-map scene.</summary>
    public static class Mini191AmmoMission
    {
        const string DefaultScene = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        public const string AmmoItemId = "sidearm_ammo_12";

        public static Mission MakeMission(Vector3 traderPosition) => new Mission
        {
            missionId = "M12A",
            title = "Ammunition",
            briefing = "A tool is nothing without rounds. Go back to the Lalay trader for a box of ammunition, then learn to reload before you ever need to.",
            rewardMoney = 50,
            objectives = new List<MissionObjective>
            {
                new MissionObjective
                {
                    kind = ObjectiveKind.BuyItem, targetId = AmmoItemId,
                    instruction = "Buy a box of rounds from the Lalay trader  [ 2 ]  ($70)",
                    dialogueBanner = "Trader: Twelve rounds, no questions. Count them, and do not waste them.",
                    markerPosition = traderPosition
                },
                new MissionObjective
                {
                    kind = ObjectiveKind.ReloadTool,
                    instruction = "Aim with right click and press R to load a fresh magazine",
                    dialogueBanner = "Reload with R. Keep your head up while you do it.",
                    hasMarker = false
                }
            }
        };

        [MenuItem("Up Iz Up Mini/MINI-191/Apply Ammunition Mission")]
        public static void Apply()
        {
            try { ApplyInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static string ScenePath { get { return Environment.GetEnvironmentVariable("MINI191_SCENE") ?? DefaultScene; } }

        static List<Mission> Missions(MissionSystem system)
        {
            return (List<Mission>)typeof(MissionSystem).GetField("missions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(system);
        }

        static void ApplyInner()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var system = UnityEngine.Object.FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include);
            TownNPCInteractable trader = null;
            foreach (var npc in UnityEngine.Object.FindObjectsByType<TownNPCInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (npc.name == "NPC_BlackMarket" && npc.Role == NpcRole.BlackMarket) trader = npc;
            if (system == null || trader == null) throw new InvalidOperationException("MissionSystem or NPC_BlackMarket missing; scene not saved.");
            var missions = Missions(system);
            int tool = missions.FindIndex(m => m.missionId == "M12T"); int existing = missions.FindIndex(m => m.missionId == "M12A");
            if (tool < 0 || tool + 1 >= missions.Count) throw new InvalidOperationException("M12T missing or last; scene not saved.");
            var mission = MakeMission(trader.transform.position);
            if (existing >= 0) missions[existing] = mission; else missions.Insert(tool + 1, mission);
            int ammo = missions.FindIndex(m => m.missionId == "M12A");
            if (ammo != tool + 1 || missions.FindAll(m => m.missionId == "M12A").Count != 1) throw new InvalidOperationException("Order check failed; scene not saved.");
            var ids = new List<string>(); for (int i = Math.Max(0, tool - 1); i < Math.Min(missions.Count, tool + 4); i++) ids.Add(missions[i].missionId);
            Debug.Log("MINI191 mission order: " + string.Join(" -> ", ids.ToArray()));
            EditorUtility.SetDirty(system); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("MINI191_AMMO_MISSION_APPLY_PASS " + ScenePath);
        }

        [MenuItem("Up Iz Up Mini/MINI-191/Exercise Ammunition Mission")]
        public static void Exercise()
        {
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var system = UnityEngine.Object.FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include);
                typeof(MissionSystem).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(system, null);
                var missions = Missions(system);
                int idx = missions.FindIndex(m => m.missionId == "M12A");
                if (idx < 1 || missions[idx - 1].missionId != "M12T" || missions[idx + 1].missionId != "M13") throw new Exception("order M12T -> M12A -> M13 failed");
                system.LoadState(idx, 0, new[] { 0, 0 });
                if (system.CurrentObjective == null || system.CurrentObjective.kind != ObjectiveKind.BuyItem) throw new Exception("buy objective failed");
                system.Notify(ObjectiveKind.BuyItem, AmmoItemId);
                if (system.CurrentObjective == null || system.CurrentObjective.kind != ObjectiveKind.ReloadTool) throw new Exception("reload objective failed");
                system.Notify(ObjectiveKind.ReloadTool);
                if (system.CurrentMissionId != "M13") throw new Exception("did not advance to M13: " + system.CurrentMissionId);
                if (system.ResolveSavedMissionIndex("M13", 0, 2) != idx + 1 || system.ResolveSavedMissionIndex("M12A", 0, 2) != idx) throw new Exception("id save resolve failed");
                int t = missions.FindIndex(m => m.missionId == "M12T");
                if (system.ResolveSavedMissionIndex(null, t, 0) != t + 2) throw new Exception("legacy index save should skip both inserted missions");
                Debug.Log("MINI191_AMMO_MISSION_EXERCISE_PASS order, objectives, advance to M13, id and legacy save resolve");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
