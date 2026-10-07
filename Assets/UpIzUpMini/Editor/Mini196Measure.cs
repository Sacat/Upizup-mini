using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-196 read-only: torso/arm slice widths of the original Ch06 mesh vs the wardrobe meshes (all in character-local bind space).</summary>
    public static class Mini196Measure
    {
        [MenuItem("Up Iz Up Mini/MINI-196/Measure Sacat Silhouette")]
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
                string name = Environment.GetEnvironmentVariable("MINI196_NAME") ?? "Sacat";
                var root = GameObject.Find(name); var sb = new StringBuilder();
                var parts = new[] { "Ch06", "Ch28_Hoody", "WardrobeBody", "WardrobeShirt", "WardrobePants" };
                foreach (var pn in parts)
                {
                    var smr = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == pn); if (smr == null || smr.sharedMesh == null) continue;
                    var m = root.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                    var vs = smr.sharedMesh.vertices.Select(v => m.MultiplyPoint3x4(v)).ToArray();
                    sb.AppendLine("== " + pn + " verts=" + vs.Length + " (y: torso width |x|<.30 [min,max,width] depth z-range | arm: outer x max)");
                    for (float y = .70f; y <= 1.62f; y += .06f)
                    {
                        var band = vs.Where(v => Mathf.Abs(v.y - y) < .03f).ToArray(); if (band.Length == 0) continue;
                        var torso = band.Where(v => Mathf.Abs(v.x) < .30f).ToArray();
                        string t = torso.Length > 0 ? string.Format("x[{0:0.000},{1:0.000}] w={2:0.000} z[{3:0.000},{4:0.000}] d={5:0.000}", torso.Min(v => v.x), torso.Max(v => v.x), torso.Max(v => v.x) - torso.Min(v => v.x), torso.Min(v => v.z), torso.Max(v => v.z), torso.Max(v => v.z) - torso.Min(v => v.z)) : "-";
                        var arm = band.Where(v => v.x > .15f).ToArray();
                        string a = arm.Length > 0 ? string.Format("armX max={0:0.000} zspan={1:0.000}", arm.Max(v => v.x), arm.Max(v => v.z) - arm.Min(v => v.z)) : "";
                        sb.AppendLine(string.Format("y={0:0.00} {1} | {2}", y, t, a));
                    }
                }
                Directory.CreateDirectory("Logs/Tasks/MINI-196"); File.WriteAllText("Logs/Tasks/MINI-196/measure_" + name + ".txt", sb.ToString());
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
