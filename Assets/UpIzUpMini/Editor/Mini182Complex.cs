using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-182 stage 6b: layout fix from the satellite and photo review. The satellite view shows only a few sheds around Pierre Charles Secondary
    /// School and one small gated apartment complex of about 4 long red-roofed blocks on the OTHER side of the road, a little lower and further along.
    /// So: thin the scattered apartment blocks (keep only those far from the school, about 1 in 3) and build ONE complex (4 blocks, wall with a gate gap
    /// facing the road, parking slab) across the road from the school. Copy scene only; live scene hash checked; idempotent.
    /// </summary>
    public static class Mini182Complex
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini182";
        const string Export = "Logs/Tasks/MINI-182/Export";
        const string Out = "Logs/Tasks/MINI-182";
        static readonly Vector2 School = new Vector2(107f, 49f);

        static string Sha(string p) { using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p); return BitConverter.ToString(s.ComputeHash(f)).Replace("-", ""); }
        static uint Hash(string s) { uint h = 5381; foreach (char ch in s) h = h * 33 + ch; return h; }

        class MB
        {
            public List<Vector3> v = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
            static Vector2 Uv(int cell) => new Vector2(((cell % 8) + .5f) / 8f, ((cell / 8) + .5f) / 8f);
            public void Box(Vector3 c, Vector3 half, int cell, float yaw)
            {
                var q = Quaternion.Euler(0, yaw, 0); var rx = q * Vector3.right; var fz = q * Vector3.forward; var p = new Vector3[8];
                for (int i = 0; i < 8; i++) { float sx = (i & 1) == 0 ? -1 : 1, sy = (i & 2) == 0 ? -1 : 1, sz = (i & 4) == 0 ? -1 : 1; p[i] = c + rx * (sx * half.x) + Vector3.up * (sy * half.y) + fz * (sz * half.z); }
                int[][] f = { new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 5, 4 }, new[] { 2, 6, 7, 3 }, new[] { 0, 4, 6, 2 }, new[] { 1, 3, 7, 5 } };
                foreach (var face in f)
                {
                    int b = v.Count; foreach (var i in face) { v.Add(p[i]); uv.Add(Uv(cell)); }
                    var n = Vector3.Cross(v[b + 1] - v[b], v[b + 2] - v[b]); var mid = (v[b] + v[b + 2]) * .5f;
                    if (Vector3.Dot(n, mid - c) >= 0) t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }); else t.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                }
            }
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Build Apartment Complex Across From School")]
        public static void Apply()
        {
            try { ApplyInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void ApplyInner()
        {
            string liveHash = Sha(Live); var log = new List<string> { "MINI-182 apartment complex layout", "live sha before=" + liveHash };
            var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            var old = GameObject.Find("MINI182_ApartmentComplex"); if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var cell = new Dictionary<string, int>(); foreach (var l in File.ReadAllLines(Export + "/coast_cells.txt")) { var s = l.Split(' '); cell[s[0]] = int.Parse(s[1]); }
            Physics.SyncTransforms();

            // 1. thin the scattered blocks: keep only blocks at least 75 m from the school, and only about 1 in 3 of those
            var housing = GameObject.Find("MINI182_ExpansionHousing") ?? throw new Exception("MINI182_ExpansionHousing missing (run Mini182Expansion.Apply first)");
            foreach (Transform c in housing.transform) c.gameObject.SetActive(true);
            int kept = 0, hidden = 0;
            foreach (Transform c in housing.transform)
            {
                float d = Vector2.Distance(new Vector2(c.position.x, c.position.z), School);
                bool keep = d >= 75f && Hash(c.name) % 3 == 0;
                c.gameObject.SetActive(keep); if (keep) kept++; else hidden++;
            }
            log.Add($"scattered apartment blocks: kept {kept}, hidden {hidden} (hidden ones stay in the scene, disabled, for rollback)");

            // 2. find the site across the road from the school
            var exp = GameObject.Find("MINI168_Expansion").transform;
            var terrain = exp.Find("ExpansionTerrain").GetComponent<MeshCollider>();
            float G(float x, float z, out bool ok) { RaycastHit h; ok = terrain.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out h, 900); return ok ? h.point.y : 0f; }
            var roadCols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(m => m.enabled && (m.name.StartsWith("ExpansionRoad_") || m.name.StartsWith("Road_") || m.name.StartsWith("ImportTrim_Road") || m.name.Contains("Sidewalk"))).ToList();
            bool OnRoad(Vector2 p) => Physics.RaycastAll(new Vector3(p.x, 500, p.y), Vector3.down, 900, ~0, QueryTriggerInteraction.Ignore).Any(h => roadCols.Contains(h.collider));
            var r4 = exp.Find("ExpansionRoad_4") ?? throw new Exception("ExpansionRoad_4 missing"); var rmesh = r4.GetComponent<MeshFilter>().sharedMesh;
            var rv = rmesh.vertices.Select(v => r4.TransformPoint(v)).ToList(); var cl = new List<Vector2>();
            for (int i = 0; i + 1 < rv.Count; i += 2) cl.Add(new Vector2((rv[i].x + rv[i + 1].x) / 2, (rv[i].z + rv[i + 1].z) / 2));
            int si = 0; float sd = float.MaxValue; for (int i = 0; i < cl.Count; i++) { float d = Vector2.Distance(cl[i], School); if (d < sd) { sd = d; si = i; } }
            log.Add($"road 4 centreline samples={cl.Count}; nearest to school at ({cl[si].x:F0},{cl[si].y:F0}) d={sd:F0} m");
            var others = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && r.bounds.size.y > 1.2f && r.bounds.size.y < 16f && r.bounds.size.x < 30f && r.bounds.size.z < 30f && !r.transform.IsChildOf(housing.transform) && r.transform.root.name != "MINI182_Lalay" && !r.transform.IsChildOf(exp.Find("GenevaField"))).Select(r => r.bounds).ToList();
            bool Free(Vector2 c, float hx, float hz, float yaw)
            {
                var q = Quaternion.Euler(0, yaw, 0); var rx = q * Vector3.right; var fz = q * Vector3.forward;
                for (int i = 0; i <= 6; i++) for (int j = 0; j <= 4; j++)
                {
                    var p = c + new Vector2(rx.x, rx.z) * Mathf.Lerp(-hx, hx, i / 6f) + new Vector2(fz.x, fz.z) * Mathf.Lerp(-hz, hz, j / 4f);
                    if (OnRoad(p)) return false; G(p.x, p.y, out bool ok); if (!ok) return false;
                    foreach (var o in others) if (p.x > o.min.x - .5f && p.x < o.max.x + .5f && p.y > o.min.z - .5f && p.y < o.max.z + .5f) return false;
                }
                return true;
            }
            Vector2 Tan(int i) => (cl[Mathf.Min(i + 3, cl.Count - 1)] - cl[Mathf.Max(i - 3, 0)]).normalized;
            var pf = Directory.GetFiles(Art + "/Prefabs", "HouseApartmentHip2s_*.prefab").OrderBy(f => f).Select(f => AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'))).ToList();
            var root = new GameObject("MINI182_ApartmentComplex"); var mb = new MB(); var results = new List<string>();
            bool found = false; Vector2 anchor = Vector2.zero, along = Vector2.right, away = Vector2.up; int dirSign = 1;
            foreach (int dir in new[] { 1, -1 })
                for (int start = si + dir * 8; start >= 2 && start < cl.Count - 2 && !found; start += dir * 2)
                {
                    var t0 = Tan(start) * dir; var n0 = new Vector2(-t0.y, t0.x); if (Vector2.Dot(n0, cl[start] - School) < 0) n0 = -n0;
                    var a = cl[start] + n0 * 17f; bool ok = true;
                    for (int k = 0; k < 4 && ok; k++) { var c = a + t0 * (k * 17.5f); if (!Free(c, 6.6f, 3.6f, Mathf.Atan2(t0.x, t0.y) * Mathf.Rad2Deg)) ok = false; }
                    if (ok) { anchor = a; along = t0; away = n0; dirSign = dir; found = true; }
                }
            if (!found) throw new Exception("No free site for the complex across the road from the school.");
            float yaw0 = Mathf.Atan2(along.x, along.y) * Mathf.Rad2Deg;
            float faceYaw = Mathf.Atan2(-away.x, -away.y) * Mathf.Rad2Deg;
            var ys = new List<float>();
            for (int k = 0; k < 4; k++)
            {
                var c = anchor + along * (k * 17.5f); var q = Quaternion.Euler(0, faceYaw, 0); float gmin = float.MaxValue;
                foreach (var dx in new[] { -1f, 1f }) foreach (var dz in new[] { -1f, 1f })
                { var p = c + new Vector2((q * Vector3.right).x, (q * Vector3.right).z) * dx * 6.4f + new Vector2((q * Vector3.forward).x, (q * Vector3.forward).z) * dz * 3.4f; float g = G(p.x, p.y, out bool ok); if (ok) gmin = Mathf.Min(gmin, g); }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf[(k + (int)(Hash("cx") % 3)) % pf.Count], root.transform); inst.name = "Apartment_" + k;
                inst.transform.SetPositionAndRotation(new Vector3(c.x, gmin - .02f, c.y), Quaternion.Euler(0, faceYaw, 0)); inst.transform.localScale = new Vector3(1.75f, 1f, 1f);
                ys.Add(gmin); results.Add($"block {k} at ({c.x:F0},{gmin:F1},{c.y:F0})");
            }
            Vector2 W(float u, float w) => anchor + along * u + away * w;
            void Seg(Vector2 a, Vector2 b, string col, float h)
            {
                var mid = (a + b) / 2; float g = G(mid.x, mid.y, out bool ok); if (!ok || OnRoad(mid)) return;
                float len = Vector2.Distance(a, b); float yw = Mathf.Atan2((b - a).x, (b - a).y) * Mathf.Rad2Deg; mb.Box(new Vector3(mid.x, g + h / 2, mid.y), new Vector3(.18f, h / 2, len / 2), cell[col], yw);
            }
            float u0 = -6f, u1 = 3 * 17.5f + 6f, wBack = 9f, wFront = -6.5f, gate = 5f, uMid = (u0 + u1) / 2;
            for (float u = u0; u < u1; u += 3f) Seg(W(u, wBack), W(Mathf.Min(u + 3f, u1), wBack), "school_grey", 1.3f);
            foreach (float uu in new[] { u0, u1 }) for (float w = wFront; w < wBack; w += 3f) Seg(W(uu, w), W(uu, Mathf.Min(w + 3f, wBack)), "school_grey", 1.3f);
            for (float u = u0; u < uMid - gate / 2; u += 3f) Seg(W(u, wFront), W(Mathf.Min(u + 3f, uMid - gate / 2), wFront), "school_white", 1.1f);
            for (float u = uMid + gate / 2; u < u1; u += 3f) Seg(W(u, wFront), W(Mathf.Min(u + 3f, u1), wFront), "school_white", 1.1f);
            foreach (float s in new[] { -1f, 1f }) { var p = W(uMid + s * (gate / 2 + .2f), wFront); float g = G(p.x, p.y, out bool ok); if (ok) mb.Box(new Vector3(p.x, g + 1.1f, p.y), new Vector3(.3f, 1.1f, .3f), cell["school_yellow"], yaw0); }
            { var p = W(uMid, wFront + 6f); float g = G(p.x, p.y, out bool ok); if (ok) mb.Box(new Vector3(p.x, g + .04f, p.y), new Vector3(5f, .04f, uMid - u0 - 4f), cell["court_paving"], yaw0); }
            var mesh = new Mesh { name = "MINI182_ComplexWalls" }; mesh.SetVertices(mb.v); mesh.SetUVs(0, mb.uv); mesh.SetTriangles(mb.t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string mp = Art + "/ComplexWalls.asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(mp) != null) AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
            var vis = new GameObject("ComplexWalls"); vis.transform.SetParent(root.transform, false); vis.isStatic = true; vis.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = vis.AddComponent<MeshRenderer>(); mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Palette_Coast.mat"); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            log.AddRange(results);
            log.Add($"complex anchor ({anchor.x:F0},{anchor.y:F0}) along road dir ({along.x:F2},{along.y:F2}); ground y {ys.Min():F1}..{ys.Max():F1}; school floor y 17.8; direction sign {dirSign}");
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            log.Add("live sha after=" + Sha(Live)); File.WriteAllLines(Out + "/Complex-Report.txt", log); Debug.Log("MINI182COMPLEX " + string.Join(" | ", log));
        }
    }
}
