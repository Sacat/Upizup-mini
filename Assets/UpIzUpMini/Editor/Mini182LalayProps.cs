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
    /// MINI-182 stage 4 step 4: utility poles with sagging wires, yard fences and hedges around Lalay, in the isolated copy scene.
    /// Everything is generated as ONE merged mesh (shared Palette.mat, one draw call, no shadows); poles also get small box colliders
    /// on a child object. Deterministic (seed 184), idempotent (root MINI182_LalayProps rebuilt), live scene never touched (hash checked).
    /// Every post/pole/hedge point must clear buildings, roads/sidewalks, streams and tree trunks.
    /// </summary>
    public static class Mini182LalayProps
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string OldArt = "Assets/UpIzUpMini/Art/Environment/Mini142";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini182";
        const string Export = "Logs/Tasks/MINI-182/Export";
        const string Out = "Logs/Tasks/MINI-182";
        const string RootName = "MINI182_LalayProps";
        const float XMin = -110f, XMax = 160f, ZMin = -215f, ZMax = -95f;

        static string Sha(string p) { using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p); return BitConverter.ToString(s.ComputeHash(f)).Replace("-", ""); }

        class Ctx
        {
            public List<Bounds> buildings = new List<Bounds>(); public List<Collider> roads = new List<Collider>(); public List<Vector2> roadPts = new List<Vector2>();
            public List<Vector2> streamPts = new List<Vector2>(); public List<MeshCollider> terrain = new List<MeshCollider>(); public List<Vector2> trunks = new List<Vector2>();
        }

        static Ctx Build()
        {
            var c = new Ctx();
            c.terrain = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(m => m.enabled && m.name.Contains("Terrain")).ToList();
            c.roads = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(m => m.enabled && (m.name.StartsWith("Road_") || m.name.StartsWith("ExpansionRoad_") || m.name.StartsWith("ImportTrim_Road") || m.name.Contains("Sidewalk") || m.name.Contains("KerbRamp") || m.name.Contains("BridgeDeck"))).ToList();
            foreach (var r in c.roads.OfType<MeshCollider>().Where(m => m.name.StartsWith("Road_") || m.name.StartsWith("ImportTrim_Road")))
            { var me = r.sharedMesh; if (me == null) continue; foreach (var v in me.vertices) { var w = r.transform.TransformPoint(v); c.roadPts.Add(new Vector2(w.x, w.z)); } }
            foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!mf.gameObject.activeInHierarchy || mf.sharedMesh == null) continue;
                string n = mf.name; if (!(n.Contains("Waterway") || n.Contains("Stream") || n.Contains("River"))) continue;
                foreach (var v in mf.sharedMesh.vertices) { var w = mf.transform.TransformPoint(v); c.streamPts.Add(new Vector2(w.x, w.z)); }
            }
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (t.parent != null && t.parent.name == "MINI182_LalayTrees") c.trunks.Add(new Vector2(t.position.x, t.position.z));
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var b = r.bounds; if (b.size.y < .9f || b.size.y > 18f || b.size.x > 25f || b.size.z > 25f) continue;
                bool skip = false; for (var p = r.transform; p != null; p = p.parent) { string pn = p.name; if (pn.StartsWith("ApprovedGrass") || pn.Contains("Grass") || pn.StartsWith("MINI182_LalayTrees") || pn.StartsWith("MINI182_LalayGround") || pn.StartsWith(RootName) || pn.StartsWith("NPC_") || pn.Contains("Waterway")) { skip = true; break; } }
                if (!skip) c.buildings.Add(b);
            }
            return c;
        }

        static float Ground(Ctx c, float x, float z, out bool ok)
        {
            float best = float.NegativeInfinity;
            foreach (var t in c.terrain) if (t.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out var h, 900)) best = Mathf.Max(best, h.point.y);
            ok = !float.IsNegativeInfinity(best); return best;
        }
        static bool OnRoad(Ctx c, Vector2 p) { foreach (var h in Physics.RaycastAll(new Vector3(p.x, 500, p.y), Vector3.down, 900, ~0, QueryTriggerInteraction.Ignore)) if (c.roads.Contains(h.collider)) return true; return false; }
        static float DistToBox(Vector2 p, Bounds b) { float dx = Mathf.Max(b.min.x - p.x, 0f, p.x - b.max.x), dz = Mathf.Max(b.min.z - p.y, 0f, p.y - b.max.z); return Mathf.Sqrt(dx * dx + dz * dz); }
        static bool Clear(Ctx c, Vector2 p, float bldGap, float roadGap, float streamGap, float trunkGap)
        {
            Ground(c, p.x, p.y, out bool ok); if (!ok) return false;
            foreach (var b in c.buildings) if (DistToBox(p, b) < bldGap) return false;
            if (OnRoad(c, p)) return false;
            if (roadGap > 0) for (int a = 0; a < 8; a++) { float ang = a * Mathf.PI / 4; if (OnRoad(c, p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * roadGap)) return false; }
            foreach (var s in c.streamPts) if ((s - p).sqrMagnitude < streamGap * streamGap) return false;
            foreach (var t in c.trunks) if ((t - p).sqrMagnitude < trunkGap * trunkGap) return false;
            return true;
        }

        class MB
        {
            public List<Vector3> v = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
            static Vector2 Uv(int cell) => new Vector2(((cell % 8) + .5f) / 8f, ((cell / 8) + .5f) / 8f);
            // oriented box: centre, right/up/forward axes, half sizes; top scale for a tapered (frustum) top
            public void Box(Vector3 c, Vector3 rx, Vector3 uy, Vector3 fz, Vector3 half, int cell, float topScale = 1f)
            {
                var p = new Vector3[8];
                for (int i = 0; i < 8; i++)
                {
                    float sx = (i & 1) == 0 ? -1 : 1, sy = (i & 2) == 0 ? -1 : 1, sz = (i & 4) == 0 ? -1 : 1; float k = sy > 0 ? topScale : 1f;
                    p[i] = c + rx * (sx * half.x * k) + uy * (sy * half.y) + fz * (sz * half.z * k);
                }
                int[][] f = { new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 5, 4 }, new[] { 2, 6, 7, 3 }, new[] { 0, 4, 6, 2 }, new[] { 1, 3, 7, 5 } };
                foreach (var q in f)
                {
                    int b = v.Count; foreach (var i in q) { v.Add(p[i]); uv.Add(Uv(cell)); }
                    t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                }
                // outward winding fix: ensure each face normal points away from the centre
                for (int fi = 0; fi < 6; fi++)
                {
                    int b = v.Count - 24 + fi * 4; var n = Vector3.Cross(v[b + 1] - v[b], v[b + 2] - v[b]); var mid = (v[b] + v[b + 2]) * .5f;
                    if (Vector3.Dot(n, mid - c) < 0) { int ti = t.Count - 36 + fi * 6; (t[ti + 1], t[ti + 2]) = (t[ti + 2], t[ti + 1]); (t[ti + 4], t[ti + 5]) = (t[ti + 5], t[ti + 4]); }
                }
            }
            // thin strip between a and b (two quads, double sided)
            public void Wire(Vector3 a, Vector3 b, float w, int cell)
            {
                var d = (b - a).normalized; var side = Vector3.Cross(d, Vector3.up).normalized * w;
                int i = v.Count; v.Add(a - side); v.Add(a + side); v.Add(b + side); v.Add(b - side); for (int k = 0; k < 4; k++) uv.Add(Uv(cell));
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i, i + 2, i + 1, i, i + 3, i + 2 });
            }
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Place Poles Fences Hedges Around Lalay")]
        public static void Apply()
        {
            try { ApplyInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void ImportPalette(List<string> log)
        {
            string oldPng = OldArt + "/palette.png", newPng = Export + "/palette.png";
            var a = new Texture2D(2, 2); a.LoadImage(File.ReadAllBytes(oldPng)); var b = new Texture2D(2, 2); b.LoadImage(File.ReadAllBytes(newPng));
            for (int c = 0; c < 50; c++)
            { int px = c % 8 * 16 + 8, py = (c / 8) * 16 + 8; Color32 ca = a.GetPixel(px, py), cb = b.GetPixel(px, py); if (ca.r != cb.r || ca.g != cb.g || ca.b != cb.b) throw new Exception("SAFETY STOP: palette cell " + c + " changed."); }
            log.Add("palette cells 0..49 verified identical");
            if (File.ReadAllBytes(oldPng).SequenceEqual(File.ReadAllBytes(newPng))) return;
            File.Copy(newPng, oldPng, true); AssetDatabase.Refresh();
            var ti = (TextureImporter)AssetImporter.GetAtPath(oldPng); ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = false; ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 128; ti.SaveAndReimport(); log.Add("palette.png replaced by superset");
        }

        static void ApplyInner()
        {
            Directory.CreateDirectory(Out + "/Renders");
            string liveHash = Sha(Live); var log = new List<string> { "MINI-182 Lalay poles/fences/hedges", "live sha before=" + liveHash };
            ImportPalette(log);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OldArt + "/Palette.mat"); mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(OldArt + "/palette.png");
            var cell = new Dictionary<string, int>(); foreach (var l in File.ReadAllLines(Export + "/props_cells.txt")) { var s = l.Split(' '); cell[s[0]] = int.Parse(s[1]); }
            var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            var old = GameObject.Find(RootName); if (old != null) UnityEngine.Object.DestroyImmediate(old);
            Physics.SyncTransforms(); var ctx = Build();
            log.Add($"buildings={ctx.buildings.Count} roads={ctx.roads.Count} trunks={ctx.trunks.Count}");
            var rng = new System.Random(184); float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var mb = new MB(); var counts = new Dictionary<string, int>(); void Cnt(string k, int n = 1) => counts[k] = counts.TryGetValue(k, out var v) ? v + n : n;
            Vector3 P(Vector2 p, float dy = 0) { float g = Ground(ctx, p.x, p.y, out _); return new Vector3(p.x, g + dy, p.y); }

            // ---- utility poles along the main road (north side), every ~26-30 m ----
            var main = ctx.roadPts.Where(p => p.x >= XMin && p.x <= XMax && p.y > ZMin && p.y < ZMax).ToList();
            var poles = new List<(Vector3 pos, Vector3 along, Vector3 toRoad)>();
            float xs = XMin + 8;
            while (xs < XMax)
            {
                var near = main.Where(p => Mathf.Abs(p.x - xs) < 3f).ToList(); if (near.Count < 2) { xs += 6; continue; }
                var c0 = near.Aggregate(Vector2.zero, (a, p) => a + p) / near.Count;
                var ahead = main.Where(p => Mathf.Abs(p.x - (xs + 6)) < 3f).ToList(); var behind = main.Where(p => Mathf.Abs(p.x - (xs - 6)) < 3f).ToList();
                Vector2 tan = Vector2.right; if (ahead.Count > 0 && behind.Count > 0) tan = (ahead.Aggregate(Vector2.zero, (a, p) => a + p) / ahead.Count - behind.Aggregate(Vector2.zero, (a, p) => a + p) / behind.Count).normalized;
                var nrm = new Vector2(-tan.y, tan.x); bool placed = false;
                for (float off = 4.6f; off <= 13f && !placed; off += .4f)
                {
                    var p = c0 + nrm * off;   // one side only (north): poles stand on the same side of the road, like real utility lines
                    if (Clear(ctx, p, .7f, .7f, 2f, 1.0f)) { poles.Add((P(p), new Vector3(tan.x, 0, tan.y), new Vector3(-nrm.x, 0, -nrm.y))); placed = true; }
                }
                xs += placed ? R(24f, 30f) : 3f;
            }
            var polesGo = new GameObject("Poles"); float H = 7.6f;
            foreach (var (pos, along, toRoad) in poles)
            {
                var right = Vector3.Cross(Vector3.up, along).normalized; var fwd = along.normalized;
                mb.Box(pos + Vector3.up * (H / 2), right, Vector3.up, fwd, new Vector3(.12f, H / 2, .12f), cell["pole_wood"], .8f);
                mb.Box(pos + Vector3.up * (H - .35f), fwd, Vector3.up, right, new Vector3(.08f, .07f, .85f), cell["pole_wood"]);   // crossarm across the road direction
                foreach (float s in new[] { -.7f, 0f, .7f }) mb.Box(pos + Vector3.up * (H - .18f) + right * s, right, Vector3.up, fwd, new Vector3(.05f, .09f, .05f), cell["insulator"]);
                // street light: curved arm reaching over the road, lamp head + glowing lens underneath
                var arm = toRoad.normalized; var armUp = Vector3.up; var armRight = Vector3.Cross(Vector3.up, arm).normalized;
                mb.Box(pos + Vector3.up * (H - .25f) + arm * .5f, armRight, armUp, arm, new Vector3(.05f, .05f, .55f), cell["lamp_arm"]);
                mb.Box(pos + Vector3.up * (H + .05f) + arm * 1.35f, armRight, armUp, arm, new Vector3(.05f, .05f, .5f), cell["lamp_arm"]);
                mb.Box(pos + Vector3.up * (H - .1f) + arm * 1.75f, armRight, armUp, arm, new Vector3(.18f, .07f, .36f), cell["lamp_head"], .85f);
                mb.Box(pos + Vector3.up * (H - .2f) + arm * 1.75f, armRight, armUp, arm, new Vector3(.13f, .03f, .27f), cell["lamp_glow"]);
                var col = new GameObject("PoleCollider"); col.transform.SetParent(polesGo.transform, false); var bc = col.AddComponent<BoxCollider>(); bc.center = pos + Vector3.up * 1.5f; bc.size = new Vector3(.3f, 3f, .3f);
                Cnt("pole");
            }
            for (int i = 0; i + 1 < poles.Count; i++)   // three sagging wires between neighbours (no wire across gaps longer than 45 m)
                if ((poles[i + 1].pos - poles[i].pos).magnitude < 45f)
                foreach (float s in new[] { -.7f, 0f, .7f })
                {
                    var a = poles[i].pos + Vector3.up * (H - .18f) + Vector3.Cross(Vector3.up, poles[i].along).normalized * s; var b = poles[i + 1].pos + Vector3.up * (H - .18f) + Vector3.Cross(Vector3.up, poles[i + 1].along).normalized * s;
                    float sag = Mathf.Min(1.1f, (b - a).magnitude * .035f); Vector3 prev = a;
                    for (int k = 1; k <= 6; k++) { float u = k / 6f; var q = Vector3.Lerp(a, b, u) + Vector3.down * (sag * 4f * u * (1 - u)); mb.Wire(prev, q, .025f, cell["wire_dark"]); prev = q; }
                }
            log.Add($"poles={poles.Count}");

            // ---- houses in the strip ----
            var houses = new List<(Bounds b, bool front)>();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                bool kit = t.parent != null && t.parent.name == "MINI182_Lalay"; bool orig = t.name.StartsWith("Lalay_House_") || t.name.StartsWith("Lalay_Home_");
                if (!(kit || orig) || !t.gameObject.activeInHierarchy) continue;
                var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; var b = rs[0].bounds; foreach (var rr in rs) b.Encapsulate(rr.bounds);
                if (b.center.x < XMin || b.center.x > XMax || b.center.z < ZMin || b.center.z > ZMax) continue; houses.Add((b, true));
            }
            Vector2 ToRoad(Vector2 c2) { Vector2 best = Vector2.up; float d = float.MaxValue; foreach (var q in ctx.roadPts) { float dd = (q - c2).sqrMagnitude; if (dd < d) { d = dd; best = q - c2; } } return best.normalized; }

            void FenceLine(Vector2 a, Vector2 b, bool picket)
            {
                float len = Vector2.Distance(a, b); int n = Mathf.Max(2, Mathf.RoundToInt(len / 2f)); var d = (b - a).normalized; var dir3 = new Vector3(d.x, 0, d.y); var right = Vector3.Cross(Vector3.up, dir3).normalized;
                var pts = new Vector2[n + 1]; var ok = new bool[n + 1];
                for (int i = 0; i <= n; i++) { pts[i] = Vector2.Lerp(a, b, i / (float)n); ok[i] = Clear(ctx, pts[i], .35f, .6f, 1.2f, .9f); }
                int cellW = cell[picket ? "fence_wood_light" : "fence_wood"]; float hgt = picket ? 1.05f : 1.15f;
                for (int i = 0; i <= n; i++) if (ok[i]) { var p = P(pts[i]); mb.Box(p + Vector3.up * (hgt / 2 - .05f), right, Vector3.up, dir3, new Vector3(.06f, hgt / 2, .06f), cellW); Cnt("fence post"); }
                for (int i = 0; i < n; i++)
                {
                    if (!ok[i] || !ok[i + 1]) continue; var p0 = P(pts[i]); var p1 = P(pts[i + 1]); var mid = (p0 + p1) / 2; var seg = (p1 - p0); var f = seg.normalized; var rgt = Vector3.Cross(Vector3.up, f).normalized; float half = seg.magnitude / 2;
                    foreach (float hh in new[] { .38f, .88f }) mb.Box(mid + Vector3.up * hh, rgt, Vector3.up, f, new Vector3(.025f, .04f, half), cellW);
                    if (picket) { int np = Mathf.Max(2, Mathf.RoundToInt(seg.magnitude / .3f)); for (int k = 0; k < np; k++) { var q = Vector3.Lerp(p0, p1, (k + .5f) / np); mb.Box(q + Vector3.up * .55f, rgt, Vector3.up, f, new Vector3(.03f, .5f, .05f), cellW); } }
                    Cnt("fence section");
                }
            }
            void HedgeLine(Vector2 a, Vector2 b)
            {
                float len = Vector2.Distance(a, b); int n = Mathf.Max(2, Mathf.RoundToInt(len / 1.3f)); var d = (b - a).normalized; var dir3 = new Vector3(d.x, 0, d.y); var right = Vector3.Cross(Vector3.up, dir3).normalized;
                for (int i = 0; i < n; i++)
                {
                    var p = Vector2.Lerp(a, b, (i + .5f) / n); if (!Clear(ctx, p, .5f, 1.0f, 1.5f, 1.1f)) continue;
                    float h = R(.75f, 1.15f), w = R(.32f, .42f); mb.Box(P(p) + Vector3.up * (h / 2 - .05f), right, Vector3.up, dir3, new Vector3(w, h / 2, len / n / 2 + .08f), cell[i % 2 == 0 ? "hedge_dark" : "hedge_light"], .82f); Cnt("hedge section");
                }
            }
            foreach (var (h, _) in houses.OrderBy(x => x.b.center.x))
            {
                var c2 = new Vector2(h.center.x, h.center.z); var f = ToRoad(c2); var r = new Vector2(f.y, -f.x);
                float depth = Mathf.Max(h.extents.x, h.extents.z), width = Mathf.Min(h.extents.x, h.extents.z);
                bool pick = R(0, 1) < .5f; float roll = R(0, 1);
                if (roll < .34f)   // back boundary + one side
                {
                    var b0 = c2 - f * (depth + R(4f, 7f)); float wd = width + R(2f, 3.5f); FenceLine(b0 - r * wd, b0 + r * wd, pick);
                    if (R(0, 1) < .6f) FenceLine(b0 + r * wd, c2 + r * wd - f * (depth * .3f), pick);
                }
                else if (roll < .58f)   // front-of-yard hedge beside the frontage (kept off the sidewalk by the clearance test)
                {
                    var fr = c2 + f * (depth + R(1.4f, 2.2f)); float wd = width + R(.6f, 1.8f);
                    HedgeLine(fr - r * wd, fr - r * .9f); HedgeLine(fr + r * .9f, fr + r * wd);
                }
                else if (roll < .72f)   // side hedge between neighbours
                {
                    var sd = c2 + r * (R(0, 1) < .5f ? -1 : 1) * (width + R(1.6f, 2.4f)); HedgeLine(sd - f * (depth * .6f), sd + f * (depth * .6f) - f * 2f);
                }
            }
            // farm/back-lot long fence on the north hillside edge
            for (int i = 0; i < 3; i++) { var s0 = new Vector2(R(XMin + 20, XMax - 40), R(ZMax - 10, ZMax + 25)); var dd = new Vector2(1, R(-.3f, .3f)).normalized; FenceLine(s0, s0 + dd * R(14f, 26f), false); }

            var mesh = new Mesh { name = "MINI182_LalayProps" };
            if (mb.v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(mb.v); mesh.SetUVs(0, mb.uv); mesh.SetTriangles(mb.t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string mp = Art + "/LalayProps.asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(mp) != null) AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
            var root = new GameObject(RootName); polesGo.transform.SetParent(root.transform, false);
            var vis = new GameObject("PropsMesh"); vis.transform.SetParent(root.transform, false); vis.isStatic = true;
            vis.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = vis.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            log.Add($"mesh verts={mesh.vertexCount} tris={mesh.triangles.Length / 3}"); foreach (var kv in counts) log.Add($"  {kv.Key}: {kv.Value}");
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            log.Add("live sha after=" + Sha(Live)); log.Add("copy in build settings=" + EditorBuildSettings.scenes.Any(s => s.enabled && s.path == Copy));
            File.WriteAllLines(Out + "/LalayProps-Report.txt", log); Debug.Log("MINI182PROPS_APPLY " + string.Join(" | ", log));
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Validate And Render Props")]
        public static void ValidateAndRender()
        {
            try
            {
                EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single); Physics.SyncTransforms();
                var root = GameObject.Find(RootName); if (root == null) throw new Exception("no props root");
                var ctx = Build(); var mesh = root.transform.Find("PropsMesh").GetComponent<MeshFilter>().sharedMesh; var vs = mesh.vertices;
                int onRoad = 0, inBld = 0, floating = 0, buried = 0; var seen = new HashSet<Vector2Int>();
                foreach (var v in vs)
                {
                    var key = new Vector2Int(Mathf.RoundToInt(v.x * 2), Mathf.RoundToInt(v.z * 2)); if (!seen.Add(key)) continue;
                    var p = new Vector2(v.x, v.z); float g = Ground(ctx, p.x, p.y, out bool ok); if (!ok) continue;
                    if (v.y > g + .15f && v.y < g + 1.6f)   // ground-level parts only (posts, hedges); wires/crossarms are high up
                    {
                        if (OnRoad(ctx, p)) onRoad++;
                        foreach (var b in ctx.buildings) if (DistToBox(p, b) < .01f) { inBld++; break; }
                    }
                    float dy = v.y - g; if (dy < -.12f) buried++;
                }
                var lines = new List<string> { "MINI-182 Lalay props validation (fresh reload)", $"unique xz samples={seen.Count} groundLevelOnRoad={onRoad} groundLevelInsideBuildingBounds={inBld} belowGround={buried}" };
                var polesGo = root.transform.Find("Poles"); lines.Add("pole colliders=" + polesGo.childCount);
                File.WriteAllLines(Out + "/LalayPropsValidation.txt", lines); foreach (var l in lines) Debug.Log("MINI182PROPS_VALIDATE " + l);
                var go = new GameObject("PropsCam"); var cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f); cam.farClipPlane = 2000; cam.fieldOfView = 55;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white * .85f;
                var rt = new RenderTexture(1600, 1000, 24); cam.targetTexture = rt;
                void Shot(string n, Vector3 pos, Vector3 look, bool ortho = false, float size = 30)
                {
                    cam.orthographic = ortho; cam.orthographicSize = size; cam.transform.position = pos; cam.transform.LookAt(look, ortho ? Vector3.forward : Vector3.up); cam.Render();
                    var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply();
                    File.WriteAllBytes(Out + "/Renders/" + n + ".png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
                }
                float G(float x, float z) { return Physics.Raycast(new Vector3(x, 500, z), Vector3.down, out var h, 900) ? h.point.y : 0f; }
                var c0 = new Vector3(48, 0, -151);
                Shot("h1_lalay_top", new Vector3(c0.x, 400, c0.z), c0, true, 73.5f);
                Shot("h2_lalay_oblique", new Vector3(c0.x - 5, G(c0.x, c0.z) + 55, c0.z - 60), new Vector3(c0.x + 10, G(c0.x + 10, c0.z), c0.z + 5));
                Shot("h3_street_west", new Vector3(c0.x - 40, G(c0.x - 40, c0.z) + 6, c0.z), new Vector3(c0.x + 20, G(c0.x + 20, c0.z) + 3, c0.z));
                Shot("h4_street_east", new Vector3(c0.x + 40, G(c0.x + 40, c0.z) + 6, c0.z), new Vector3(c0.x - 20, G(c0.x - 20, c0.z) + 3, c0.z));
                Shot("h5_backyards", new Vector3(c0.x + 10, G(c0.x + 10, c0.z - 40) + 14, c0.z - 62), new Vector3(c0.x + 10, G(c0.x + 10, c0.z - 20) + 2, c0.z - 25));
                Shot("h6_top_close", new Vector3(c0.x - 20, 400, c0.z), new Vector3(c0.x - 20, 0, c0.z), true, 32f);
                Debug.Log("MINI182PROPS_RENDER_DONE");
                if (Application.isBatchMode) EditorApplication.Exit(onRoad + inBld == 0 ? 0 : 2);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
