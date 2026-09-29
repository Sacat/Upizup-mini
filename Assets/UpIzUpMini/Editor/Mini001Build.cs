using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Builds the current playable Grand Bay scene for Windows.
    /// </summary>
    public static class Mini001Build
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string OutputPath = "Builds/GrandBayProof/UpIzUpMini.exe";

        [MenuItem("Up Iz Up Mini/Build Windows Player (Grand Bay Expansion)")]
        public static void BuildWindowsPlayer()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"MINI-001 BUILD SUCCEEDED: {OutputPath} ({summary.totalSize} bytes, {summary.totalTime}).");
            }
            else
            {
                Debug.LogError($"MINI-001 BUILD FAILED: result={summary.result}, errors={summary.totalErrors}.");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
