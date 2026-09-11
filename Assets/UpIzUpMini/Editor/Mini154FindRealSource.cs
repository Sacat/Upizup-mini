using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-154 diagnostic: the actual playable Sacat/Franki in
    /// GrandBayProof.unity may not be Mainchar.fbx/Strong.fbx at all
    /// (Mini016CharacterImport.cs's comment claiming that mapping may be
    /// stale). Find the real GameObjects and report their actual
    /// SkinnedMeshRenderer source mesh asset paths before any more garment
    /// work targets the wrong source.</summary>
    public static class Mini154FindRealSource
    {
        [MenuItem("Up Iz Up Mini/MINI-154/Find Real Playable Source")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            foreach (var rootName in new[] { "Sacat", "Franki" })
            {
                var go = GameObject.Find(rootName);
                if (go == null) { Debug.Log($"MINI154FIND: no root named '{rootName}'"); continue; }
                Debug.Log($"MINI154FIND: ==== {rootName} at {go.transform.position} ====");
                foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh = r.sharedMesh;
                    var meshPath = mesh != null ? AssetDatabase.GetAssetPath(mesh) : "null";
                    Debug.Log($"MINI154FIND:  renderer '{r.name}' path={GetPath(r.transform, go.transform)} mesh='{(mesh != null ? mesh.name : "null")}' sourceAsset='{meshPath}'");
                }
                var animator = go.GetComponentInChildren<Animator>(true);
                Debug.Log($"MINI154FIND:  Animator avatar={(animator != null && animator.avatar != null ? animator.avatar.name : "null")}");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static string GetPath(Transform t, Transform root)
        {
            if (t == root) return t.name;
            var parts = new System.Collections.Generic.List<string>();
            while (t != null && t != root)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
