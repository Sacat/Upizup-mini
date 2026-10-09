using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    public static class Mini205Dump
    {
        public static void Run()
        {
            var sb = new StringBuilder();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Art/Characters/Modular/Bare/SacatBare.prefab");
            var rs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            sb.AppendLine("new rig renderers:");
            foreach (var r in rs) sb.AppendLine("  " + r.name + " bones=" + r.bones.Length + " root=" + (r.rootBone != null ? r.rootBone.name : "null") + " first=" + r.bones[0].name + " tris=" + r.sharedMesh.triangles.Length / 3 + " bindposesMatchFirst=" + (r.sharedMesh.bindposes.Length == rs[0].sharedMesh.bindposes.Length));
            bool same = rs.All(r => r.bones.Length == rs[0].bones.Length && r.bones.Select(b => b.name).SequenceEqual(rs[0].bones.Select(b => b.name)));
            sb.AppendLine("all renderers share identical bone list: " + same);
            sb.AppendLine("new bones: " + string.Join(",", rs[0].bones.Select(b => b.name).ToArray()));
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
            var old = GameObject.Find("Sacat").GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch06");
            sb.AppendLine("old bones (" + old.bones.Length + "): " + string.Join(",", old.bones.Select(b => b == null ? "null" : b.name).ToArray()));
            File.WriteAllText("Logs/Tasks/MINI-205/dump2.txt", sb.ToString());
            EditorApplication.Exit(0);
        }
    }
}
