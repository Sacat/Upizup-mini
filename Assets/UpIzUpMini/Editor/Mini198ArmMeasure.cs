using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-198 read-only: Sacat's arm bone rest positions and the original Ch06 arm cross-sections along the arm (bind pose).</summary>
    public static class Mini198ArmMeasure
    {
        [MenuItem("Up Iz Up Mini/MINI-198/Measure Sacat Arms")]
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
                var root = GameObject.Find("Sacat"); var smr = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch06");
                var mesh = smr.sharedMesh; var m = root.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                var sb = new StringBuilder();
                foreach (var suffix in new[] { "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightArm", "RightForeArm", "RightHand", "Neck", "Spine2" })
                {
                    int i = Array.FindIndex(smr.bones, b => b && b.name.EndsWith(":" + suffix)); if (i < 0) { sb.AppendLine(suffix + " missing"); continue; }
                    sb.AppendLine(suffix + " rest=" + m.MultiplyPoint3x4(mesh.bindposes[i].inverse.MultiplyPoint3x4(Vector3.zero)).ToString("F3"));
                }
                var vs = mesh.vertices.Select(v => m.MultiplyPoint3x4(v)).ToArray();
                foreach (int sign in new[] { 1, -1 })
                {
                    sb.AppendLine("== arm side " + sign + " (x station: center y,z | half extents y,z)");
                    for (float x = .22f; x <= .90f; x += .04f)
                    {
                        var band = vs.Where(v => Mathf.Abs(v.x * sign - x) < .012f && v.y > 1.05f && v.y < 1.65f).ToArray();
                        if (band.Length < 6) { sb.AppendLine(x.ToString("0.00") + " -"); continue; }
                        sb.AppendLine(string.Format("x={0:0.00} c=({1:0.000},{2:0.000}) h=({3:0.000},{4:0.000}) n={5}", x, (band.Max(v => v.y) + band.Min(v => v.y)) / 2, (band.Max(v => v.z) + band.Min(v => v.z)) / 2, (band.Max(v => v.y) - band.Min(v => v.y)) / 2, (band.Max(v => v.z) - band.Min(v => v.z)) / 2, band.Length));
                    }
                }
                Directory.CreateDirectory("Logs/Tasks/MINI-198"); File.WriteAllText("Logs/Tasks/MINI-198/arm_measure.txt", sb.ToString());
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
