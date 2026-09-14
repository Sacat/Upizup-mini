using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    // Starts MINI-168 from the accepted spline proof without saving either protected scene.
    public static class Mini168GrandBayExpansionCopy
    {
        const string Source = "Assets/UpIzUpMini/Scenes/MapLab_MBRoad_LalayHighlandProof.unity";
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/MapLab_GrandBayExpansionCopy.unity";
        const string Evidence = "Logs/Tasks/MINI-168";
        const string Baseline = "Docs/Maps/dm-dom-grand-bay-expansion-v1/Evidence/protected-scene-hashes.txt";

        [MenuItem("Up Iz Up Mini/MINI-168/Create Grand Bay Expansion Copy")]
        public static void Build()
        {
            Need(File.Exists(Source) && File.Exists(Live), "Protected scene source is missing.");
            Directory.CreateDirectory(Evidence);
            string sourceHash = Hash(Source), liveHash = Hash(Live);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Copy) != null)
            {
                Validate();
                Debug.Log("MINI-168 COPY EXISTS: retained all current expansion work.");
                return;
            }
            Need(AssetDatabase.CopyAsset(Source, Copy), "Could not copy accepted spline proof.");
            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "MapLab_LalayHighland");
            Need(root != null, "Accepted map root is missing from copy.");
            root.name = "MapLab_GrandBayExpansionCopy";
            Transform old = Find(root.transform, "MINI168_EXPANSION_PENDING_MAP_TRUTH");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            new GameObject("MINI168_EXPANSION_PENDING_MAP_TRUTH").transform.SetParent(root.transform, false);
            EditorSceneManager.MarkSceneDirty(scene);
            Need(EditorSceneManager.SaveScene(scene, Copy), "Could not save expansion copy.");
            AssetDatabase.SaveAssets();
            Need(sourceHash == Hash(Source), "Accepted spline proof changed while copying.");
            Need(liveHash == Hash(Live), "Live gameplay scene changed while copying.");
            Need(!InBuild(Copy), "Expansion copy is enabled in build settings.");
            File.WriteAllLines(Baseline, new[] {
                "sourceSha256=" + sourceHash, "liveSha256=" + liveHash,
                "copySha256=" + Hash(Copy), "copyInBuildSettings=false" });
            Debug.Log("MINI-168 COPY PASS: isolated copy created; protected scenes unchanged; copy excluded from builds.");
        }

        [MenuItem("Up Iz Up Mini/MINI-168/Validate Grand Bay Expansion Copy")]
        public static void Validate()
        {
            Need(File.Exists(Source) && File.Exists(Live) && File.Exists(Copy), "Scene set is incomplete.");
            Need(File.Exists(Baseline), "Protected hash baseline is missing.");
            Need(!InBuild(Copy), "Expansion copy must remain excluded from builds.");
            string[] lines = File.ReadAllLines(Baseline);
            Need(Value(lines, "sourceSha256=") == Hash(Source), "Accepted spline proof changed.");
            Need(Value(lines, "liveSha256=") == Hash(Live), "Live gameplay scene changed.");
            Scene scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "MapLab_GrandBayExpansionCopy");
            Need(root != null && Find(root.transform, "MINI168_EXPANSION_PENDING_MAP_TRUTH") != null,
                "Expansion root or map-truth marker is missing.");
            Debug.Log("MINI-168 COPY VALIDATION PASS: copy present, protected hashes match, build exclusion confirmed.");
        }

        static bool InBuild(string path) => EditorBuildSettings.scenes.Any(x => x.enabled && x.path == path);
        static string Value(string[] lines, string prefix)
        {
            string line = lines.FirstOrDefault(x => x.StartsWith(prefix, StringComparison.Ordinal));
            Need(line != null, "Baseline entry missing: " + prefix);
            return line.Substring(prefix.Length);
        }
        static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root) { Transform hit = Find(child, name); if (hit != null) return hit; }
            return null;
        }
        static string Hash(string path)
        {
            using SHA256 sha = SHA256.Create(); using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        static void Need(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    }
}
