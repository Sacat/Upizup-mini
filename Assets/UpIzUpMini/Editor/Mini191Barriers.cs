using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-191: survey/remove invisible blockers in the new-map scene, and build that scene to its own folder.</summary>
    public static class Mini191Barriers
    {
        const string SceneDefault = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        static string ScenePath { get { return Environment.GetEnvironmentVariable("MINI191_SCENE") ?? SceneDefault; } }

        static bool IsInvisibleBlocker(Collider c)
        {
            if (c == null || !c.enabled || c.isTrigger || !c.gameObject.activeInHierarchy) return false;
            if (c is TerrainCollider) return false;
            if (c.transform.root.name == "MINI173_LalayHomes" || c.transform.root.name == "MINI182_School" || c.transform.root.name == "MINI182_LalayProps" || c.transform.root.name == "MINI182_ApartmentTerrace") return false;
            if (c.GetComponentInParent<CharacterController>() != null) return false;
            if (c.GetComponentInParent<Rigidbody>() != null) return false;
            foreach (var r in c.GetComponentsInParent<Renderer>()) if (r.enabled) return false;
            foreach (var r in c.GetComponentsInChildren<Renderer>()) if (r.enabled) return false;
            return true;
        }

        static string Path(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }

        [MenuItem("Up Iz Up Mini/MINI-191/Survey Invisible Barriers")]
        public static void Survey() { Run(false); }

        [MenuItem("Up Iz Up Mini/MINI-191/Remove Invisible Barriers")]
        public static void Remove() { Run(true); }

        static void Run(bool apply)
        {
            try
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var sb = new StringBuilder();
                var groups = new SortedDictionary<string, int>();
                int disabled = 0, kept = 0;
                foreach (var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (!IsInvisibleBlocker(c)) continue;
                    var b = c.bounds;
                    string key = c.transform.root.name + " :: " + (c.transform.parent != null ? c.transform.parent.name : "-") + " :: " + c.GetType().Name;
                    groups[key] = groups.ContainsKey(key) ? groups[key] + 1 : 1;
                    // the terrain-edge fall protection is deliberately kept
                    bool keep = c.gameObject.name.StartsWith("MINI182_OuterBoundary") || (c.transform.parent != null && c.transform.parent.name.StartsWith("MINI182_OuterBoundary"));
                    sb.AppendLine((keep ? "KEEP  " : apply ? "OFF   " : "FOUND ") + Path(c.transform) + " center=" + b.center + " size=" + b.size);
                    if (keep) { kept++; continue; }
                    if (apply) { c.enabled = false; EditorUtility.SetDirty(c); disabled++; }
                }
                foreach (var kv in groups) sb.AppendLine("GROUP " + kv.Value + " x " + kv.Key);
                Debug.Log("MINI191_BARRIERS " + (apply ? "removed" : "survey") + " disabled=" + disabled + " kept=" + kept + "\n" + sb);
                if (apply && disabled > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        [MenuItem("Up Iz Up Mini/MINI-191/Build New Map Windows Player")]
        public static void BuildNewMap()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/GrandBayHouses/UpIzUpMini.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var summary = BuildPipeline.BuildPlayer(options).summary;
            Debug.Log("MINI191_BUILD " + summary.result + " size=" + summary.totalSize + " time=" + summary.totalTime);
            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
