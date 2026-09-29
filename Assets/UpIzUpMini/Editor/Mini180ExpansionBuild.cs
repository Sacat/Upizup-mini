using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Missions;
using UpIzUpMini.UI;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    public static class Mini180ExpansionBuild
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        private const string OutputPath = "Builds/GrandBayExpansionPreview/UpIzUpMini-GrandBayExpansion.exe";

        [MenuItem("Up Iz Up Mini/MINI-180/Build Grand Bay Expansion Preview")]
        public static void Build()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Require(GameObject.Find("MINI168_Expansion") != null, "Grand Bay expansion root");
            Require(UnityEngine.Object.FindFirstObjectByType<MissionSystem>(FindObjectsInactive.Include) != null, "mission system");
            Require(UnityEngine.Object.FindFirstObjectByType<GtaMiniMapController>(FindObjectsInactive.Include) != null, "minimap and points");
            Require(UnityEngine.Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include) != null, "playable character");

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Grand Bay expansion build failed: " + report.summary.result + ", errors=" + report.summary.totalErrors);

            Debug.Log("MINI180_EXPANSION_BUILD_SUCCEEDED " + OutputPath + " bytes=" + report.summary.totalSize);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Require(bool condition, string item)
        {
            if (!condition) throw new Exception("Grand Bay expansion build missing " + item);
            Debug.Log("MINI180_EXPANSION_CHECK_OK " + item);
        }
    }
}
