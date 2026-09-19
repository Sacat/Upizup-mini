using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-171 verification of the import scene. Read-only (saves
    /// nothing): compares old vs new ground height over the live Lalay/
    /// Highland footprint and at every gameplay anchor, checks the bay seam,
    /// checks where new roads end on existing roads, and finds any new house
    /// that sits on an existing one.</summary>
    public static class Mini171VerifyImport
    {
        const string Import = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        const string Out = "Logs/Tasks/MINI-171";

        [MenuItem("Up Iz Up Mini/MINI-171/Verify Expansion Import Scene")]
        public static void Run()
        {
            var lines = new List<string>();
            int fails = 0;
            void Log(string s) { lines.Add(s); Debug.Log("MINI171VERIFY: " + s); }
            try
            {
                Directory.CreateDirectory(Out);
                EditorSceneManager.OpenScene(Import, OpenSceneMode.Single);
                var exp = GameObject.Find("MINI168_Expansion").transform;
                var world = GameObject.Find("GrandBayPhase1_ApprovedWorld_VA005").transform;
                var liveT = world.GetComponentsInChildren<Transform>(true).First(t => t.name == "Copernicus_GLO30_Terrain");
                var newT = exp.Find("ExpansionTerrain");

                // ---- 1. bay seam ----
                var road = exp.Find("ImportTrim_Road_way_22917921");
                var seamRoad = road.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v => road.TransformPoint(v)).Where(p => Mathf.Abs(p.x - 250f) < .001f).OrderBy(p => p.z).ToArray();
                var app = exp.Find("ExpansionRoad_10");
                var seamApp = app.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v => app.TransformPoint(v)).Where(p => Mathf.Abs(p.x - 250f) < .001f).OrderBy(p => p.z).ToArray();
                if (seamRoad.Length < 2 || seamApp.Length < 2) { Log("FAIL seam vertices missing road=" + seamRoad.Length + " approach=" + seamApp.Length); fails++; }
                else
                {
                    float d0 = Vector3.Distance(seamRoad.First(), seamApp.First()), d1 = Vector3.Distance(seamRoad.Last(), seamApp.Last());
                    Log($"seam road {seamRoad.First()} -> {seamRoad.Last()} | approach {seamApp.First()} -> {seamApp.Last()} | endpoint gaps {d0:F3}m, {d1:F3}m");
                    if (Mathf.Max(d0, d1) > .05f) { Log("FAIL seam gap over 0.05m"); fails++; }
                }

                // ---- 2. ground height old vs new over the live footprint ----
                Physics.SyncTransforms();
                var pts = new List<Vector2>();
                for (float x = -100; x <= 290; x += 10) for (float z = -225; z <= -45; z += 10) pts.Add(new Vector2(x, z));
                float?[] hOld = Sample(liveT.gameObject, newT.gameObject, pts, world);
                float?[] hNew = Sample(newT.gameObject, liveT.gameObject, pts, world);
                var diffs = new List<(Vector2 p, float d)>();
                for (int i = 0; i < pts.Count; i++) if (hOld[i].HasValue && hNew[i].HasValue) diffs.Add((pts[i], hNew[i].Value - hOld[i].Value));
                Log($"ground grid: {diffs.Count} cells compared of {pts.Count}; max |diff| {diffs.Max(d => Mathf.Abs(d.d)):F2}m, mean |diff| {diffs.Average(d => Mathf.Abs(d.d)):F3}m, cells >0.25m: {diffs.Count(d => Mathf.Abs(d.d) > .25f)}, >1m: {diffs.Count(d => Mathf.Abs(d.d) > 1f)}");
                var big = diffs.Where(d => Mathf.Abs(d.d) > .25f).OrderByDescending(d => Mathf.Abs(d.d)).Take(12);
                foreach (var b in big) Log($"  ground diff {b.d:+0.00;-0.00}m at x={b.p.x:F0} z={b.p.y:F0}");

                // ---- 3. every gameplay anchor / house: old vs new ground under it ----
                var anchors = new List<(string n, Vector3 p)>();
                foreach (var n in new[] { "FarmSafehouse_Rest", "LalayHouse_Rest", "LalayEstate_Rest", "Sacat", "Franki" })
                { var g = GameObject.Find(n); if (g != null) anchors.Add((n, g.transform.position)); }
                foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                    if (t.name == "ApprovedHouse_MINI142" && !t.IsChildOf(exp)) anchors.Add((t.name, t.position));
                var ap = anchors.Select(a => new Vector2(a.p.x, a.p.z)).ToList();
                float?[] aOld = Sample(liveT.gameObject, newT.gameObject, ap, world);
                float?[] aNew = Sample(newT.gameObject, liveT.gameObject, ap, world);
                int worse = 0; float worst = 0;
                for (int i = 0; i < anchors.Count; i++)
                {
                    if (!aOld[i].HasValue || !aNew[i].HasValue) { Log($"  anchor {anchors[i].n} at ({anchors[i].p.x:F0},{anchors[i].p.z:F0}): no ground hit old={aOld[i].HasValue} new={aNew[i].HasValue}"); continue; }
                    float d = aNew[i].Value - aOld[i].Value;
                    if (Mathf.Abs(d) > worst) worst = Mathf.Abs(d);
                    if (Mathf.Abs(d) > .15f) { worse++; Log($"  anchor {anchors[i].n} at ({anchors[i].p.x:F0},{anchors[i].p.z:F0}): ground moved {d:+0.00;-0.00}m"); }
                }
                Log($"anchors checked {anchors.Count} (houses+safehouses+players); worst ground shift {worst:F2}m; anchors shifted >0.15m: {worse}");

                // ---- 4. where new roads end: is there an existing road under them, and how far apart vertically ----
                foreach (Transform r in exp)
                {
                    if (!r.name.StartsWith("ExpansionRoad_")) continue;
                    var mf = r.GetComponent<MeshFilter>(); if (mf == null) continue;
                    var v = mf.sharedMesh.vertices; if (v.Length < 4) continue;
                    foreach (var (label, p) in new[] { ("start", r.TransformPoint((v[0] + v[1]) * .5f)), ("end", r.TransformPoint((v[v.Length - 2] + v[v.Length - 1]) * .5f)) })
                    {
                        var hit = Physics.RaycastAll(new Vector3(p.x, 300, p.z), Vector3.down, 600f)
                            .Where(h => !h.collider.transform.IsChildOf(exp) && h.collider.name.StartsWith("Road_")).OrderBy(h => h.distance).FirstOrDefault();
                        Log(hit.collider != null
                            ? $"  {r.name} {label} ({p.x:F0},{p.z:F0}) sits on live road '{hit.collider.name}': vertical gap {(p.y - hit.point.y):+0.00;-0.00}m"
                            : $"  {r.name} {label} ({p.x:F0},{p.z:F0}) no live road underneath (free end or joins new road)");
                    }
                }

                // ---- 5. new houses sitting on existing houses ----
                var liveHouses = new List<Vector3>();
                foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                {
                    if (t.IsChildOf(exp) || !t.gameObject.activeInHierarchy) continue;
                    if (t.name == "ApprovedHouse_MINI142" || (t.parent != null && (t.parent.name == "Lalay_Dense_House_Massing" || t.parent.name == "Highland_Sparse_House_Massing"))) liveHouses.Add(t.position);
                }
                int overlaps = 0;
                foreach (Transform h in exp)
                {
                    if (!h.name.StartsWith("ExpansionHouse_")) continue;
                    float best = liveHouses.Count == 0 ? 999f : liveHouses.Min(p => Vector2.Distance(new Vector2(p.x, p.z), new Vector2(h.position.x, h.position.z)));
                    if (best < 8f) { overlaps++; Log($"  {h.name} at ({h.position.x:F0},{h.position.z:F0}) is {best:F1}m from an existing house"); }
                }
                Log($"house overlap check: {liveHouses.Count} live houses vs new houses; new houses within 8m of an existing one: {overlaps}");
                if (overlaps > 0) fails++;

                // ---- 6. new ROADS running through/next to existing houses (footprint ~5m + half road 3.1m => want >8m) ----
                var centre = new List<(string road, Vector2 p, float y)>();
                foreach (Transform r in exp)
                {
                    if (!r.name.StartsWith("ExpansionRoad_") || r.GetComponent<MeshFilter>() == null) continue;
                    var v = r.GetComponent<MeshFilter>().sharedMesh.vertices;
                    for (int i = 0; i + 1 < v.Length; i += 2) { var m = r.TransformPoint((v[i] + v[i + 1]) * .5f); centre.Add((r.name, new Vector2(m.x, m.z), m.y)); }
                }
                int conflicts = 0;
                foreach (var hp in liveHouses)
                {
                    var q = new Vector2(hp.x, hp.z);
                    var near = centre.OrderBy(c => Vector2.Distance(c.p, q)).First();
                    float d = Vector2.Distance(near.p, q);
                    if (d < 8f) { conflicts++; if (conflicts <= 25) Log($"  live house at ({hp.x:F0},{hp.z:F0}) is only {d:F1}m from {near.road}"); }
                }
                Log($"road-vs-house check: live houses within 8m of a NEW road centreline: {conflicts}");

                // ---- 7. every road end: nearest live road within 6m (rings), and vertical gap ----
                foreach (Transform r in exp)
                {
                    if (!r.name.StartsWith("ExpansionRoad_") || r.GetComponent<MeshFilter>() == null) continue;
                    var v = r.GetComponent<MeshFilter>().sharedMesh.vertices; if (v.Length < 4) continue;
                    foreach (var (label, p) in new[] { ("start", r.TransformPoint((v[0] + v[1]) * .5f)), ("end", r.TransformPoint((v[v.Length - 2] + v[v.Length - 1]) * .5f)) })
                    {
                        RaycastHit best = default; float bestR = -1;
                        foreach (float rad in new[] { 0f, 2f, 4f, 6f })
                        {
                            for (int k = 0; k < (rad == 0 ? 1 : 12); k++)
                            {
                                float a = k * Mathf.PI * 2 / 12;
                                var o = new Vector3(p.x + Mathf.Cos(a) * rad, 300, p.z + Mathf.Sin(a) * rad);
                                var hit = Physics.RaycastAll(o, Vector3.down, 600f).Where(h => !h.collider.transform.IsChildOf(exp) && h.collider.name.StartsWith("Road_")).OrderBy(h => h.distance).FirstOrDefault();
                                if (hit.collider != null) { best = hit; bestR = rad; break; }
                            }
                            if (bestR >= 0) break;
                        }
                        if (bestR >= 0) Log($"  END {r.name} {label} ({p.x:F0},{p.z:F0},y{p.y:F2}) meets live road '{best.collider.name}' within {bestR:F0}m: vertical gap {(p.y - best.point.y):+0.00;-0.00}m");
                    }
                }
            }
            catch (Exception e) { Debug.LogException(e); Log("FAIL exception: " + e.Message); fails++; }
            Log(fails == 0 ? "VERIFY RESULT: all hard checks passed (see warnings above)" : "VERIFY RESULT: " + fails + " hard check(s) failed");
            File.WriteAllLines(Out + "/VERIFY-REPORT.txt", lines);
            if (Application.isBatchMode) EditorApplication.Exit(fails == 0 ? 0 : 1);
        }

        // Height of `target` under each point; `target` active, `other` inactive, then restore.
        static float?[] Sample(GameObject target, GameObject other, List<Vector2> pts, Transform world)
        {
            bool tA = target.activeSelf, oA = other.activeSelf;
            target.SetActive(true); other.SetActive(false); Physics.SyncTransforms();
            var col = target.GetComponent<Collider>();
            var res = new float?[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                var hits = Physics.RaycastAll(new Vector3(pts[i].x, 400, pts[i].y), Vector3.down, 800f).Where(h => h.collider == col).ToArray();
                if (hits.Length > 0) res[i] = hits.OrderByDescending(h => h.point.y).First().point.y;
            }
            target.SetActive(tA); other.SetActive(oA); Physics.SyncTransforms();
            return res;
        }
    }
}
