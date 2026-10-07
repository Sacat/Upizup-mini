using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    public static class Mini198ArmCheck
    {
        public static void Run()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/UpIzUpMini/Art/Characters/Garments/Outfits166/Sacat_Arms.asset");
            var v = mesh.vertices; var w = mesh.boneWeights;
            Debug.Log("MINI198_CHECK bounds=" + mesh.bounds + " verts=" + v.Length + " bindposes=" + mesh.bindposes.Length);
            var bad = Enumerable.Range(0, v.Length).Where(i => w[i].weight0 + w[i].weight1 + w[i].weight2 + w[i].weight3 < .99f || float.IsNaN(v[i].x)).Take(5).ToArray();
            Debug.Log("MINI198_CHECK badWeights=" + bad.Length + " distinctBones=" + string.Join(",", w.SelectMany(b => new[] { b.boneIndex0, b.boneIndex1, b.boneIndex2, b.boneIndex3 }).Distinct().OrderBy(x => x).ToArray()));
            int degenerate = 0; var t = mesh.triangles; for (int i = 0; i < t.Length; i += 3) if (Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).sqrMagnitude < 1e-12f) degenerate++;
            Debug.Log("MINI198_CHECK degenerate=" + degenerate + " longEdges=" + Enumerable.Range(0, t.Length / 3).Count(k => (v[t[3 * k]] - v[t[3 * k + 1]]).magnitude > .1f));
            EditorApplication.Exit(0);
        }
    }
}
