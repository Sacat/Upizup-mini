using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-203: runs the MINI-201 road smoothing on a COPY of the new-map scene (GrandBayProof_HouseEnhance), so the live scene is never touched.
    /// Env MINI203_SRC / MINI203_DST override the source and copy paths. Re-run Mini193RoadJunctions afterwards so the junction patches follow the new road heights.</summary>
    public static class Mini203RoadsNewMap
    {
        public static void Preview()
        {
            try
            {
                string src = Environment.GetEnvironmentVariable("MINI203_SRC") ?? "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
                string dst = Environment.GetEnvironmentVariable("MINI203_DST") ?? "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance_RoadFix.unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(dst) != null) AssetDatabase.DeleteAsset(dst);
                if (!AssetDatabase.CopyAsset(src, dst)) throw new Exception("copy failed");
                var scene = EditorSceneManager.OpenScene(dst, OpenSceneMode.Single);
                var t = typeof(Mini201RoadSmooth); var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                t.GetMethod("Apply", flags).Invoke(null, null);
                bool ok = (bool)t.GetMethod("Validate", flags).Invoke(null, new object[] { "newmap" });
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                Debug.Log((ok ? "MINI203_PREVIEW_PASS " : "MINI203_PREVIEW_FAIL ") + dst);
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (Exception e) { Debug.LogException(e is TargetInvocationException && e.InnerException != null ? e.InnerException : e); EditorApplication.Exit(1); }
        }
    }
}
