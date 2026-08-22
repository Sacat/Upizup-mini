using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Missions;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Focused static gate for MINI-108's early mission/navigation polish.</summary>
    public static class Mini108EarlyMissionValidation
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";

        [MenuItem("Up Iz Up Mini/MINI-108/Validate Early Mission Polish")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            List<string> problems = new List<string>();

            MissionSystem missionSystem = UnityEngine.Object.FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include);
            Require(missionSystem != null, "MissionSystem missing", problems);
            if (missionSystem != null)
            {
                SerializedProperty missions = new SerializedObject(missionSystem).FindProperty("missions");
                string[] requiredIds = { "M4", "M4L", "M4W", "M5A", "M5B", "M6" };
                foreach (string id in requiredIds) Require(FindMission(missions, id) != null, $"mission {id} missing", problems);
                Require(FindMission(missions, "M8") == null, "obsolete second path choice M8 still exists", problems);
                SerializedProperty m4l = FindMission(missions, "M4L");
                Require(m4l != null && m4l.FindPropertyRelative("commitToWeedRouteOnComplete").boolValue,
                    "legitimate frustration branch does not rejoin the Boss J route", problems);
                SerializedProperty m5b = FindMission(missions, "M5B");
                Require(HasObjective(m5b, ObjectiveKind.TalkToCleanPolice), "clean-police objective missing", problems);
                Require(HasObjective(m5b, ObjectiveKind.BribeNormy), "$100 Normy objective missing", problems);
                SerializedProperty m6 = FindMission(missions, "M6");
                Require(m6 != null && m6.FindPropertyRelative("title").stringValue == "Stock Up", "M6 was not renamed Stock Up", problems);
            }

            MissionHUD hud = UnityEngine.Object.FindFirstObjectByType<MissionHUD>(FindObjectsInactive.Include);
            Require(hud != null, "MissionHUD missing", problems);
            if (hud != null)
            {
                SerializedObject so = new SerializedObject(hud);
                Require(so.FindProperty("bannerBackground").objectReferenceValue is Image, "transparent black mission-dialogue background is not wired", problems);
                Require(so.FindProperty("bannerPanelRect").objectReferenceValue is RectTransform, "dynamic mission banner panel is not wired", problems);
            }
            Require(UnityEngine.Object.FindFirstObjectByType<GtaMiniMapController>(FindObjectsInactive.Include) != null,
                "GTA minimap controller missing", problems);
            Require(UnityEngine.Object.FindFirstObjectByType<ObjectiveMarker>(FindObjectsInactive.Include) != null,
                "world objective marker missing", problems);

            GameObject paro = Find("NPC_Vagrant");
            Require(paro != null, "Paro missing", problems);
            if (paro != null)
            {
                Require(paro.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("ShirtHole_")), "Paro shirt holes missing", problems);
                Require(paro.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("BareFoot_")) == 2, "Paro barefoot treatment missing", problems);
            }

            Transform banana = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == "BananaVisual");
            Require(banana != null, "banana crop visual missing", problems);
            if (banana != null)
            {
                Require(banana.GetComponentsInChildren<Renderer>(true).Count(r => r.name.StartsWith("Banana_")) >= 15,
                    "banana hand does not contain the curved segmented fruit set", problems);
                Require(banana.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Banana_BrownStem"),
                    "banana bunch brown hanging stem is missing", problems);
            }

            CheckBridgeClearance(problems);

            if (problems.Count == 0)
                Debug.Log("MINI-108 VALIDATION PASS: early route branch, safehouse/police/Normy missions, universal waypoint UI, Paro and banana visuals, and bridge clearance are wired.");
            else
                Debug.LogError("MINI-108 VALIDATION FAIL:\n- " + string.Join("\n- ", problems));
            if (Application.isBatchMode) EditorApplication.Exit(problems.Count == 0 ? 0 : 1);
        }

        private static SerializedProperty FindMission(SerializedProperty missions, string id)
        {
            if (missions == null) return null;
            for (int i = 0; i < missions.arraySize; i++)
            {
                SerializedProperty mission = missions.GetArrayElementAtIndex(i);
                if (mission.FindPropertyRelative("missionId").stringValue == id) return mission;
            }
            return null;
        }

        private static bool HasObjective(SerializedProperty mission, ObjectiveKind kind)
        {
            if (mission == null) return false;
            SerializedProperty objectives = mission.FindPropertyRelative("objectives");
            for (int i = 0; i < objectives.arraySize; i++)
                if (objectives.GetArrayElementAtIndex(i).FindPropertyRelative("kind").enumValueIndex == (int)kind) return true;
            return false;
        }

        private static void CheckBridgeClearance(List<string> problems)
        {
            GameObject bridges = Find("Bridges");
            GameObject houses = Find("Lalay_Dense_House_Massing");
            if (bridges == null || houses == null) { problems.Add("bridge or Lalay house root missing"); return; }
            foreach (Transform house in houses.transform.Cast<Transform>())
            {
                Renderer[] houseRenderers = house.GetComponentsInChildren<Renderer>(true);
                if (houseRenderers.Length == 0) continue;
                Bounds houseBounds = houseRenderers[0].bounds;
                foreach (Renderer renderer in houseRenderers.Skip(1)) houseBounds.Encapsulate(renderer.bounds);
                foreach (Renderer bridgeRenderer in bridges.GetComponentsInChildren<Renderer>(true))
                {
                    Bounds bridgeBounds = bridgeRenderer.bounds;
                    bridgeBounds.Expand(new Vector3(2f, 0f, 2f));
                    bool overlap = houseBounds.min.x < bridgeBounds.max.x && houseBounds.max.x > bridgeBounds.min.x
                                && houseBounds.min.z < bridgeBounds.max.z && houseBounds.max.z > bridgeBounds.min.z;
                    if (overlap) { problems.Add($"house {house.name} obstructs a bridge"); return; }
                }
            }
        }

        private static GameObject Find(string name) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(t => t.name == name)?.gameObject;
        private static void Require(bool condition, string problem, List<string> problems) { if (!condition) problems.Add(problem); }
    }
}
