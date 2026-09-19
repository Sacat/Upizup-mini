using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-171: run AFTER Build and ClearRoads. Removes expansion-road triangles that lie on top of an existing live road
    /// (coplanar => z-fighting) so the live road shows through, and lifts the few edge vertices left over it by 2cm.</summary>
    public static class Mini171TrimRoadOverlaps
    {
        const string Import = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        const string Art = "Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-expansion-v1/Generated";

        [MenuItem("Up Iz Up Mini/MINI-171/Trim Road Overlaps")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(Import, OpenSceneMode.Single);
            var exp = GameObject.Find("MINI168_Expansion").transform;
            Physics.SyncTransforms();
            var live = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.enabled && !c.transform.IsChildOf(exp) || c.enabled && c.name.StartsWith("ImportTrim_Road_way"))
                .Where(c => c.name.StartsWith("Road_") || c.name.StartsWith("ImportTrim_Road_way")).Cast<Collider>().ToList();
            var earlier = new List<Collider>();
            bool Over(Vector3 p, out float y, bool includeEarlier = false)
            {
                y = 0; bool hit = false;
                foreach (var c in includeEarlier ? live.Concat(earlier) : live) if (c.Raycast(new Ray(new Vector3(p.x, 400, p.z), Vector3.down), out var h, 900)) { if (!hit || h.point.y > y) y = h.point.y; hit = true; }
                return hit;
            }
            var log = new List<string> { "MINI-171 road overlap trim; live road colliders=" + live.Count + " (expansion roads are only lifted over earlier-numbered ones, never cut)" };
            foreach (var road in exp.Cast<Transform>().Where(t => t.name.StartsWith("ExpansionRoad_")).OrderBy(t => int.Parse(t.name.Substring(14))).ToArray())
            {
                var thisCol = road.GetComponent<MeshCollider>();
                var mf = road.GetComponent<MeshFilter>(); var src = mf.sharedMesh;
                var v = src.vertices; var tri = src.triangles; var keep = new List<int>(); int dropped = 0, lifted = 0;
                var lift = new HashSet<int>();
                for (int i = 0; i < tri.Length; i += 3)
                {
                    Vector3 a = road.TransformPoint(v[tri[i]]), b = road.TransformPoint(v[tri[i + 1]]), c = road.TransformPoint(v[tri[i + 2]]);
                    int over = 0; bool ok = true;
                    foreach (var p in new[] { (a + b + c) / 3f, (a + b) / 2f, (b + c) / 2f, (a + c) / 2f })
                    { if (Over(p, out float y) && Mathf.Abs(y - p.y) < .6f) over++; }
                    if (over >= 2) { dropped++; ok = false; }
                    if (ok) foreach (int k in new[] { tri[i], tri[i + 1], tri[i + 2] }) { var wp = road.TransformPoint(v[k]); if (Over(wp, out float oy, true) && Mathf.Abs(oy - wp.y) < .6f) lift.Add(k); }
                    if (ok) keep.AddRange(new[] { tri[i], tri[i + 1], tri[i + 2] });
                }
                if (dropped == 0 && lift.Count == 0) { if (thisCol != null) earlier.Add(thisCol); continue; }
                foreach (int k in lift) { var w = road.TransformPoint(v[k]); w.y += .02f; v[k] = road.InverseTransformPoint(w); lifted++; }
                var m = Object.Instantiate(src); m.name = "Import_" + road.name; m.vertices = v; m.triangles = keep.ToArray(); m.RecalculateNormals(); m.RecalculateBounds();
                string p2 = Art + "/Import_" + road.name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(p2) != null) AssetDatabase.DeleteAsset(p2);
                AssetDatabase.CreateAsset(m, p2); mf.sharedMesh = m;
                var col = road.GetComponent<MeshCollider>(); if (col != null) { col.sharedMesh = null; col.sharedMesh = m; }
                if (col != null) earlier.Add(col);
                log.Add($"{road.name}: dropped {dropped} of {tri.Length / 3} triangles lying on live roads; lifted {lifted} vertices 2cm where over a live/earlier road");
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/Tasks/MINI-171"); File.WriteAllLines("Logs/Tasks/MINI-171/TRIM-OVERLAPS-REPORT.txt", log);
            Debug.Log("MINI171TRIM " + string.Join(" | ", log));
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
