using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-171 read-only: list live objects (houses, rails/fences/farm) lying on or beside the new expansion roads.</summary>
    public static class Mini171SurveyConflicts
    {
        [MenuItem("Up Iz Up Mini/MINI-171/Survey Road Conflicts")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity", OpenSceneMode.Single);
            var exp = GameObject.Find("MINI168_Expansion").transform;
            var segs = new List<(Vector2 a, Vector2 b, float hw, string n)>();
            foreach (var r in exp.Cast<Transform>().Where(t => t.name.StartsWith("ExpansionRoad_")))
            {
                var v = r.GetComponent<MeshFilter>().sharedMesh.vertices;
                for (int i = 0; i + 3 < v.Length; i += 2)
                {
                    Vector3 l0 = r.TransformPoint(v[i]), r0 = r.TransformPoint(v[i + 1]), l1 = r.TransformPoint(v[i + 2]), r1 = r.TransformPoint(v[i + 3]);
                    segs.Add((new Vector2((l0.x + r0.x) / 2, (l0.z + r0.z) / 2), new Vector2((l1.x + r1.x) / 2, (l1.z + r1.z) / 2), (Vector3.Distance(l0, r0) + Vector3.Distance(l1, r1)) / 4, r.name));
                }
            }
            float Dist(Vector2 p, out string n)
            {
                float best = 1e9f; n = "";
                foreach (var s in segs)
                {
                    var ab = s.b - s.a; float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                    float d = (p - (s.a + ab * t)).magnitude - s.hw; if (d < best) { best = d; n = s.n; }
                }
                return best;
            }
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.IsChildOf(exp)) continue;
                string nm = t.name.ToLower();
                bool rail = nm.Contains("wood") || nm.Contains("crop") || nm.Contains("garden") || nm.Contains("plot") || nm.Contains("rail") || nm.Contains("fence") || nm.Contains("farm") || nm.Contains("pen") || nm.Contains("post");
                bool house = t.name == "ApprovedHouse_MINI142" || (t.parent != null && (t.parent.name == "Lalay_Dense_House_Massing" || t.parent.name == "Highland_Sparse_House_Massing"));
                if (!rail && !house) continue;
                var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float d = Dist(new Vector2(b.center.x, b.center.z), out string rn);
                float ext = Mathf.Max(b.extents.x, b.extents.z);
                if (d - ext < 6f && (house || t.GetComponent<MeshFilter>() != null))
                    Debug.Log($"MINI171CONF {(house ? "HOUSE" : "RAIL?")} '{t.name}' parent='{t.parent?.name}' centre=({b.center.x:F1},{b.center.z:F1}) ext={ext:F1} roadEdgeDist={d:F1} clear={d - ext:F1} road={rn} mats={string.Join(",", rs.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.name).Distinct().Take(3))}");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
