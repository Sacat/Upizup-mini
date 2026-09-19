using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-171: run AFTER Mini171ImportExpansion.Build. Pushes live houses, the Highland sign and the Montine farm
    /// (plots + brown fences) sideways off the new expansion roads. Idempotent: anything already clear is left alone.</summary>
    public static class Mini171ClearRoads
    {
        const string Import = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        const float Clear = 4f;
        static List<(Vector2 a, Vector2 b, float hw)> segs;

        static float Dist(Vector2 p, out Vector2 away)
        {
            float best = 1e9f; away = Vector2.zero;
            foreach (var s in segs)
            {
                var ab = s.b - s.a; float t = Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-6f));
                var q = s.a + ab * t; float d = (p - q).magnitude - s.hw;
                if (d < best) { best = d; away = (p - q).sqrMagnitude > 1e-6f ? (p - q).normalized : new Vector2(-ab.y, ab.x).normalized; }
            }
            return best;
        }

        static Bounds B(Transform t) { var rs = t.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }

        static List<Bounds> others = new List<Bounds>();
        static float Worst(Renderer[] rs, Vector3 off)
        {
            float worst = 1e9f;
            foreach (var r in rs)
            {
                var b = r.bounds; var pts = new[] { b.center, new Vector3(b.min.x, 0, b.min.z), new Vector3(b.max.x, 0, b.min.z), new Vector3(b.min.x, 0, b.max.z), new Vector3(b.max.x, 0, b.max.z) };
                foreach (var p in pts) { float d = Dist(new Vector2(p.x + off.x, p.z + off.z), out _); if (d < worst) worst = d; }
            }
            return worst;
        }
        static bool HitsOther(Bounds self, Vector3 off)
        {
            var b = self; b.center += off; b.Expand(new Vector3(3f, 100f, 3f));
            foreach (var o in others) if (b.Intersects(o)) return true;
            return false;
        }

        // smallest sideways move (16 directions) that clears every road edge by Clear and touches no other placed object
        static float Push(Transform t, Collider ground, List<string> log)
        {
            var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return 0;
            if (Worst(rs, Vector3.zero) >= Clear) return 0;
            var self = B(t); Vector3 best = Vector3.zero; bool found = false;
            for (float d = 1; d <= 60 && !found; d += 1)
                for (int k = 0; k < 16; k++)
                {
                    float ang = k * Mathf.PI / 8; var off = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * d;
                    if (Worst(rs, off) >= Clear && !HitsOther(self, off)) { best = off; found = true; break; }
                }
            if (!found) { log.Add("NO SPOT FOUND for " + t.name); return 0; }
            t.position += best; float total = best.magnitude; others.Add(B(t));
            if (total > 0)
            {
                Physics.SyncTransforms();
                var c = B(t).center; float y0 = t.position.y;
                float bottom = t.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.y);
                if (ground.Raycast(new Ray(new Vector3(c.x, 400, c.z), Vector3.down), out var h, 900)) { float dy = h.point.y - bottom; t.position += Vector3.up * dy; log.Add($"  {t.name} vertical {dy:F2}m"); }
            }
            return total;
        }


        static List<Collider> roadCols;
        static bool OnRoad(Vector3 p, float radius)
        {
            for (int k = 0; k < 9; k++)
            {
                var q = k == 0 ? p : p + new Vector3(Mathf.Cos(k * Mathf.PI / 4), 0, Mathf.Sin(k * Mathf.PI / 4)) * radius;
                foreach (var c in roadCols) if (c.Raycast(new Ray(new Vector3(q.x, 400, q.z), Vector3.down), out _, 900)) return true;
            }
            return false;
        }
        static List<Vector3> FarmSamples(Transform farm, Vector3 off)
        {
            var pts = new List<Vector3>();
            foreach (var r in farm.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds;
                for (float x = b.min.x; x <= b.max.x + .01f; x += Mathf.Max(1.5f, b.size.x / 8)) for (float z = b.min.z; z <= b.max.z + .01f; z += Mathf.Max(1.5f, b.size.z / 8)) pts.Add(new Vector3(x, 0, z) + off);
                pts.Add(b.center + off);
            }
            return pts;
        }
        static void MoveFarm(GameObject world, Transform exp, Collider ground, List<string> log)
        {
            var farm = GameObject.Find("MontineFarm"); if (farm == null) { log.Add("MontineFarm not found"); return; }
            roadCols = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.enabled && (c.name.StartsWith("ExpansionRoad_") || c.name.StartsWith("ImportTrim_Road_") || (c.name.StartsWith("Road_") && !c.name.Contains("farm_spur")))).Cast<Collider>().ToList();
            var ft = farm.transform; var self = B(ft);
            var basePts = FarmSamples(ft, Vector3.zero);
            int hits0 = basePts.Count(p => OnRoad(p, 1.5f));
            log.Add($"farm samples on/near road before: {hits0}/{basePts.Count} using {roadCols.Count} road colliders");
            if (hits0 == 0) return;
            Vector3 best = Vector3.zero; bool found = false;
            for (float d = 2; d <= 70 && !found; d += 2)
                for (int k = 0; k < 16; k++)
                {
                    float ang = k * Mathf.PI / 8; var off = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * d;
                    if (HitsOther(self, off)) continue;
                    if (!basePts.Any(p => OnRoad(p + off, 1.5f))) { best = off; found = true; break; }
                }
            if (!found) { log.Add("NO SPOT FOUND for farm"); return; }
            ft.position += best; Physics.SyncTransforms();
            float bottom = ft.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.y); var c0 = B(ft).center;
            if (ground.Raycast(new Ray(new Vector3(c0.x, 400, c0.z), Vector3.down), out var h, 900)) ft.position += Vector3.up * (h.point.y - bottom);
            log.Add($"moved MontineFarm {best.magnitude:F0}m by ({best.x:F0},{best.z:F0})");
        }

        [MenuItem("Up Iz Up Mini/MINI-171/Clear Roads Of Houses And Farm")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(Import, OpenSceneMode.Single);
            var exp = GameObject.Find("MINI168_Expansion").transform;
            var ground = exp.Find("ExpansionTerrain").GetComponent<MeshCollider>();
            Physics.SyncTransforms();
            segs = new List<(Vector2, Vector2, float)>();
            foreach (var r in exp.Cast<Transform>().Where(t => t.name.StartsWith("ExpansionRoad_")))
            {
                var v = r.GetComponent<MeshFilter>().sharedMesh.vertices;
                for (int i = 0; i + 3 < v.Length; i += 2)
                {
                    Vector3 l0 = r.TransformPoint(v[i]), r0 = r.TransformPoint(v[i + 1]), l1 = r.TransformPoint(v[i + 2]), r1 = r.TransformPoint(v[i + 3]);
                    segs.Add((new Vector2((l0.x + r0.x) / 2, (l0.z + r0.z) / 2), new Vector2((l1.x + r1.x) / 2, (l1.z + r1.z) / 2), (Vector3.Distance(l0, r0) + Vector3.Distance(l1, r1)) / 4));
                }
            }
            var log = new List<string> { "MINI-171 clear-roads report" };
            var targets = new List<Transform>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (t.IsChildOf(exp)) continue;
                bool house = t.parent != null && t.parent.name == "Highland_Sparse_House_Massing" && t.name.StartsWith("Highland_") && t.GetComponentInChildren<Renderer>() != null;
                if (house || t.name == "Sign_HIGHLAND" ) targets.Add(t);
            }
            others.Clear();
            foreach (var h in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (!h.IsChildOf(exp) && !targets.Contains(h) && h.parent != null && (h.parent.name == "Highland_Sparse_House_Massing" || h.parent.name == "Lalay_Dense_House_Massing") && h.GetComponentInChildren<Renderer>() != null) others.Add(B(h));
            foreach (var e in exp.Cast<Transform>().Where(x => x.name.StartsWith("ExpansionHouse_") || x.name.StartsWith("School"))) if (e.GetComponentInChildren<Renderer>() != null) others.Add(B(e));
            foreach (var t in targets) { var rr = t.GetComponentsInChildren<Renderer>(); if (rr.Length > 0 && Worst(rr, Vector3.zero) >= Clear) others.Add(B(t)); }
            foreach (var t in targets)
            {
                if (t.GetComponentInChildren<Renderer>() == null) continue;
                var before = t.position; float m = Push(t, ground, log);
                if (m > 0) log.Add($"moved {t.name} {m:F1}m from {before} to {t.position}");
            }
            MoveFarm(null, exp, ground, log);
            // report remaining conflicts
            int left = 0;
            foreach (var t in targets)
            {
                if (t.GetComponentInChildren<Renderer>() == null) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>()) if (Dist(new Vector2(r.bounds.center.x, r.bounds.center.z), out _) - Mathf.Max(r.bounds.extents.x, r.bounds.extents.z) < 0) { left++; log.Add("STILL OVERLAPS " + t.name + "/" + r.name); break; }
            }
            log.Add("remainingOverlaps=" + left);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/Tasks/MINI-171"); File.WriteAllLines("Logs/Tasks/MINI-171/CLEAR-ROADS-REPORT.txt", log);
            Debug.Log("MINI171CLEAR " + string.Join(" | ", log));
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
