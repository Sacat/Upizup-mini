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
    /// MINI-182 stage 5: bay wall, black-sand stone beach and Geneva field dressing in the isolated copy scene.
    /// - Bay seawall (MINI174_BaySeawall): repeating red / yellow / green / teal painted bands laid on the cap of every ACTIVE segment.
    /// - Beach (CoastalBeach): darkened to black sand (instance material, original asset untouched), plus scattered dark/grey stones,
    ///   a wet-sand band and red-brown seaweed lines along the waterline (all in one merged mesh, second palette, one draw call).
    /// - Geneva field: pavilion, boundary hedge, trees and pale worn outfield patches.
    /// Deterministic (seed 185), idempotent (roots rebuilt / paint restored), live scene never touched (hash checked).
    /// </summary>
    public static class Mini182BayCoast
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini182";
        const string OldArt = "Assets/UpIzUpMini/Art/Environment/Mini142";
        const string Export = "Logs/Tasks/MINI-182/Export";
        const string Out = "Logs/Tasks/MINI-182";
        const string PaintRoot = "MINI182_BayWallPaint", BeachRoot = "MINI182_BayBeach", GenevaRoot = "MINI182_GenevaDressing";

        static string Sha(string p) { using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p); return BitConverter.ToString(s.ComputeHash(f)).Replace("-", ""); }
        static float Coast(float z) { return z < -160 ? 250 : z < -60 ? 250 + (z + 160) * .55f : 305 + (z + 60) * .25f; }

        class MB
        {
            public List<Vector3> v = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
            static Vector2 Uv(int cell) => new Vector2(((cell % 8) + .5f) / 8f, ((cell / 8) + .5f) / 8f);
            public void Tri(Vector3 a, Vector3 b, Vector3 c, int cell)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); for (int k = 0; k < 3; k++) uv.Add(Uv(cell));
                if (Vector3.Cross(b - a, c - a).y < 0) t.AddRange(new[] { i, i + 2, i + 1 }); else t.AddRange(new[] { i, i + 1, i + 2 });
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int cell) { Tri(a, b, c, cell); Tri(a, c, d, cell); }
            public void Box(Vector3 c, Vector3 rx, Vector3 uy, Vector3 fz, Vector3 half, int cell, float topScale = 1f)
            {
                var p = new Vector3[8];
                for (int i = 0; i < 8; i++) { float sx = (i & 1) == 0 ? -1 : 1, sy = (i & 2) == 0 ? -1 : 1, sz = (i & 4) == 0 ? -1 : 1; float k = sy > 0 ? topScale : 1f; p[i] = c + rx * (sx * half.x * k) + uy * (sy * half.y) + fz * (sz * half.z * k); }
                int[][] f = { new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 5, 4 }, new[] { 2, 6, 7, 3 }, new[] { 0, 4, 6, 2 }, new[] { 1, 3, 7, 5 } };
                foreach (var q in f)
                {
                    int b = v.Count; foreach (var i in q) { v.Add(p[i]); uv.Add(Uv(cell)); }
                    var n = Vector3.Cross(v[b + 1] - v[b], v[b + 2] - v[b]); var mid = (v[b] + v[b + 2]) * .5f;
                    if (Vector3.Dot(n, mid - c) >= 0) t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }); else t.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                }
            }
            // low-poly stone: squashed, jittered octahedron (8 tris)
            public void Stone(Vector3 c, float r, float squash, float yaw, System.Random rng, int cell)
            {
                float J() => .75f + (float)rng.NextDouble() * .5f; var q = Quaternion.Euler(0, yaw, 0);
                Vector3 px = q * Vector3.right * r * J(), nx = q * Vector3.left * r * J(), pz = q * Vector3.forward * r * J() * .8f, nz = q * Vector3.back * r * J() * .8f;
                Vector3 top = Vector3.up * r * squash * J(), bot = Vector3.down * r * .2f;
                Vector3 X1 = c + px, X2 = c + nx, Z1 = c + pz, Z2 = c + nz, T = c + top, B = c + bot;
                Tri(T, X1, Z1, cell); Tri(T, Z1, X2, cell); Tri(T, X2, Z2, cell); Tri(T, Z2, X1, cell);
                Tri(B, Z1, X1, cell); Tri(B, X2, Z1, cell); Tri(B, Z2, X2, cell); Tri(B, X1, Z2, cell);
            }
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Build Bay Wall Beach And Geneva Dressing")]
        public static void Apply()
        {
            try { ApplyInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static Material CoastMaterial()
        {
            string png = Art + "/palette_coast.png"; File.Copy(Export + "/palette_coast.png", png, true); AssetDatabase.Refresh();
            var ti = (TextureImporter)AssetImporter.GetAtPath(png); ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = false; ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 128; ti.SaveAndReimport();
            string mp = Art + "/Palette_Coast.mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
            if (m == null) { m = new Material(Shader.Find("UpIzUpMini/ApprovedArtDiffuse")); AssetDatabase.CreateAsset(m, mp); }
            m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(png); m.color = Color.white; m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
        }

        static void ApplyInner()
        {
            Directory.CreateDirectory(Out + "/Renders");
            string liveHash = Sha(Live); var log = new List<string> { "MINI-182 bay wall / beach / Geneva", "live sha before=" + liveHash };
            var mat = CoastMaterial();
            var cell = new Dictionary<string, int>(); foreach (var l in File.ReadAllLines(Export + "/coast_cells.txt")) { var s = l.Split(' '); cell[s[0]] = int.Parse(s[1]); }
            var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            foreach (var n in new[] { PaintRoot, BeachRoot, GenevaRoot }) { var o = GameObject.Find(n); if (o != null) UnityEngine.Object.DestroyImmediate(o); }
            Physics.SyncTransforms();
            var rng = new System.Random(185); float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            // ================= 1. painted wall bands =================
            var wall = GameObject.Find("MINI174_BaySeawall") ?? throw new Exception("MINI174_BaySeawall missing");
            var caps = wall.GetComponentsInChildren<Transform>().Where(t => t.name == "Cap" && t.gameObject.activeInHierarchy).OrderBy(t => t.position.z).ToList();
            var bands = new[] { "wall_band_teal", "wall_band_yellow", "wall_band_red", "wall_band_green" };
            var paint = new MB(); int bandCount = 0;
            for (int i = 0; i < caps.Count; i++)
            {
                var c = caps[i]; var sc = c.lossyScale; var up = Vector3.up;
                paint.Box(c.position + up * (sc.y * .5f + .022f), c.right, up, c.forward, new Vector3(sc.x * .5f + .04f, .026f, sc.z * .5f + .012f), cell[bands[(i / 2) % 4]]); bandCount++;
                // painted band also runs down the seaward face for a short skirt so it reads from the beach
                paint.Box(c.position + c.right * (sc.x * .5f + .012f) + up * (-.06f), c.forward, up, c.right, new Vector3(sc.z * .5f + .012f, .10f, .012f), cell[bands[(i / 2) % 4]]);
            }
            log.Add($"wall caps painted={bandCount} (band length ~6 m, order teal, yellow, red, green)");

            // ================= 2. beach =================
            var beachGo = GameObject.Find("CoastalBeach") ?? throw new Exception("CoastalBeach missing");
            var beachR = beachGo.GetComponent<MeshRenderer>(); var beachMc = beachGo.GetComponent<MeshCollider>(); bool tempCol = false;
            if (beachMc == null) { beachMc = beachGo.AddComponent<MeshCollider>(); tempCol = true; }
            Physics.SyncTransforms();
            // black sand: instance material colour (original asset untouched)
            string sandPath = Art + "/BayBlackSand.mat"; var sand = AssetDatabase.LoadAssetAtPath<Material>(sandPath);
            if (sand == null) { sand = new Material(beachR.sharedMaterial) { name = "BayBlackSand" }; AssetDatabase.CreateAsset(sand, sandPath); }
            string prop = sand.HasProperty("_Color") ? "_Color" : sand.HasProperty("_BaseColor") ? "_BaseColor" : null;
            if (prop != null) sand.SetColor(prop, new Color(.07f, .07f, .08f)); else log.Add("WARN: sand material has no colour property");
            EditorUtility.SetDirty(sand); beachR.sharedMaterial = sand;
            log.Add("beach material -> BayBlackSand (color prop " + prop + ")");
            float BG(float x, float z, out bool ok) { RaycastHit h; ok = beachMc.Raycast(new Ray(new Vector3(x, 100, z), Vector3.down), out h, 300); if (!ok) ok = beachMc.Raycast(new Ray(new Vector3(x, -60, z), Vector3.up), out h, 300); ok = ok && h.point.y > -5f; return ok ? h.point.y : 0f; }
            { var bb = beachR.bounds; log.Add($"CoastalBeach bounds centre={bb.center} size={bb.size} meshCol={(beachMc.sharedMesh != null)} readable={(beachMc.sharedMesh != null && beachMc.sharedMesh.isReadable)} tris={(beachMc.sharedMesh != null ? beachMc.sharedMesh.triangles.Length / 3 : 0)}"); { var vv = beachMc.sharedMesh.vertices.Select(q => beachGo.transform.TransformPoint(q)).Where(q => Mathf.Abs(q.z + 100f) < 25f).ToList(); if (vv.Count > 0) log.Add($"mesh verts near z=-100: n={vv.Count} x {vv.Min(q => q.x):F0}..{vv.Max(q => q.x):F0} y {vv.Min(q => q.y):F2}..{vv.Max(q => q.y):F2} samples: " + string.Join(" ", vv.Take(6).Select(q => q.ToString("F1")))); var nn = beachMc.sharedMesh.normals; log.Add("normals sample y: " + (nn.Length > 0 ? nn[0].y.ToString("F2") : "none")); }
            foreach (var tx in new[] { 270f, 300f, 330f }) { BG(tx, -100f, out bool okd); log.Add($"probe x={tx} z=-100 -> ground {BG(tx, -100f, out okd):F2} ok={okd} coastX={Coast(-100f):F0}"); } }
            var beach = new MB(); int stones = 0, weed = 0;
            // The existing CoastalBeach is only a thin strip landward of the wall; build a real black-sand shingle ledge seaward of the wall:
            // starts at wall-base height, falls gently to below sea level (the BayWater plane hides the rest). Rows every 2 m, 7 columns.
            const int Cols = 7; const float Width = 13f; const float Sea = -.2f;
            var rows = new List<(float z, float[] x, float[] y)>();
            for (float z = -158; z <= 34; z += 2f)
            {
                if (z > -78 && z < -20) { rows.Add((z, null, null)); continue; }                   // roundabout beach (MINI-180) has its own surface
                float xa = Coast(z) - 1.1f + .32f; float yw = BG(Coast(z) - 1.1f - .7f, z, out bool okw); if (!okw) yw = .55f; yw = Mathf.Clamp(yw, .3f, 1.2f) + .4f;   // beach starts a little above the land strip so it stays above the sea plane (y=.25) for ~8 m
                var xs = new float[Cols + 1]; var ys = new float[Cols + 1];
                for (int c = 0; c <= Cols; c++) { float t = c / (float)Cols; xs[c] = xa + Width * t; ys[c] = Mathf.Lerp(yw, Sea, t) + (c > 1 && c < Cols ? R(-.05f, .05f) : 0f); }
                rows.Add((z, xs, ys));
            }
            for (int i = 0; i + 1 < rows.Count; i++)
            {
                var r0 = rows[i]; var r1 = rows[i + 1]; if (r0.x == null || r1.x == null) continue;
                for (int c = 0; c < Cols; c++)
                {
                    string col = c < 2 ? "sand_gravel" : c < Cols - 2 ? "sand_black" : "wet_sand";
                    beach.Quad(new Vector3(r0.x[c], r0.y[c], r0.z), new Vector3(r0.x[c + 1], r0.y[c + 1], r0.z), new Vector3(r1.x[c + 1], r1.y[c + 1], r1.z), new Vector3(r1.x[c], r1.y[c], r1.z), cell[col]);
                }
                // stones (bigger near the wall, small gravel near the water) and seaweed at the waterline
                int cwl = 0; for (int c = 0; c <= Cols; c++) if (r0.y[c] > .3f) cwl = c;
                float xwl = r0.x[Mathf.Min(cwl + 1, Cols)];
                int n = Mathf.RoundToInt(R(10f, 15f));
                for (int k = 0; k < n; k++)
                {
                    float u = Mathf.Pow((float)rng.NextDouble(), 1.3f); float xx = Mathf.Lerp(r0.x[0] + .2f, xwl, u), zz = r0.z + R(0, 2f);
                    float t = (xx - r0.x[0]) / Width; float yy = Mathf.Lerp(r0.y[0], Sea, Mathf.Clamp01(t)); if (yy < .3f) continue;
                    float rr = Mathf.Lerp(R(.25f, .6f), R(.10f, .26f), u); string sc = R(0, 1) < .5f ? "stone_dark" : (R(0, 1) < .6f ? "stone_mid" : "stone_light");
                    beach.Stone(new Vector3(xx, yy + .02f, zz), rr, R(.35f, .7f), R(0, 360), rng, cell[sc]); stones++;
                }
                if (R(0, 1) < .8f)
                {
                    float w = R(.9f, 2.4f), xs2 = xwl - R(.2f, 1.2f), len = R(1.6f, 2.4f); string c1 = R(0, 1) < .55f ? "seaweed_brown" : "seaweed_red"; float yw2 = .32f;
                    beach.Quad(new Vector3(xs2 - w / 2, yw2, r0.z), new Vector3(xs2 + w / 2, yw2 - .04f, r0.z), new Vector3(xs2 + w / 2 + R(-.3f, .3f), yw2 - .04f, r0.z + len), new Vector3(xs2 - w / 2 + R(-.3f, .3f), yw2, r0.z + len), cell[c1]); weed++;
                }
            }
            // ---- the older Lalay bay beach (Bay_Sand_Patch, pale yellow) -> black sand + stones + seaweed ----
            var oldSand = GameObject.Find("Bay_Sand_Patch");
            if (oldSand != null)
            {
                var osr = oldSand.GetComponent<MeshRenderer>(); osr.sharedMaterial = sand;
                var omc = oldSand.GetComponent<MeshCollider>(); bool otemp = false; if (omc == null) { omc = oldSand.AddComponent<MeshCollider>(); otemp = true; }
                Physics.SyncTransforms();
                float OG(float x, float z, out bool ok) { RaycastHit h; ok = omc.Raycast(new Ray(new Vector3(x, 100, z), Vector3.down), out h, 300); if (!ok) ok = omc.Raycast(new Ray(new Vector3(x, -60, z), Vector3.up), out h, 300); return ok ? h.point.y : 0f; }
                var ob = osr.bounds; int os = 0, ow = 0;
                for (float x = ob.min.x + 1; x < ob.max.x - 1; x += 1.7f)
                    for (float z = ob.min.z + 1; z < ob.max.z - 1; z += 1.7f)
                    {
                        float xx = x + R(-.8f, .8f), zz = z + R(-.8f, .8f); float g = OG(xx, zz, out bool ok); if (!ok || g < .3f) continue;
                        bool nearWater = g < .75f;
                        if (R(0, 1) < (nearWater ? .55f : .42f))
                        { float rr = nearWater ? R(.10f, .28f) : R(.18f, .6f); string sc = R(0, 1) < .5f ? "stone_dark" : (R(0, 1) < .6f ? "stone_mid" : "stone_light"); beach.Stone(new Vector3(xx, g + .02f, zz), rr, R(.35f, .7f), R(0, 360), rng, cell[sc]); os++; }
                        if (nearWater && g > .3f && g < .5f && R(0, 1) < .3f)
                        { float w = R(1.0f, 2.6f), len = R(1.6f, 3f); string c1 = R(0, 1) < .55f ? "seaweed_brown" : "seaweed_red"; float ya = OG(xx, zz, out _) + .06f; beach.Quad(new Vector3(xx - w / 2, ya, zz), new Vector3(xx + w / 2, ya, zz), new Vector3(xx + w / 2 + R(-.4f, .4f), ya, zz + len), new Vector3(xx - w / 2 + R(-.4f, .4f), ya, zz + len), cell[c1]); ow++; }
                    }
                if (otemp) UnityEngine.Object.DestroyImmediate(omc);
                log.Add($"old bay beach Bay_Sand_Patch recoloured; stones={os} seaweed={ow}"); stones += os; weed += ow;
            }
            else log.Add("WARN: Bay_Sand_Patch not found");
            log.Add($"beach stones={stones} seaweed strips={weed}");
            if (tempCol) UnityEngine.Object.DestroyImmediate(beachMc);

            GameObject Emit(string name, MB mb, string assetName)
            {
                var mesh = new Mesh { name = assetName }; if (mb.v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(mb.v); mesh.SetUVs(0, mb.uv); mesh.SetTriangles(mb.t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var nr = mesh.normals; for (int i = 0; i < nr.Length; i++) { } mesh.normals = nr;
                string mp = Art + "/" + assetName + ".asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(mp) != null) AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
                var go = new GameObject(name); go.isStatic = true; go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                log.Add($"{name}: verts={mesh.vertexCount} tris={mesh.triangles.Length / 3}"); return go;
            }
            Emit(PaintRoot, paint, "BayWallPaint"); Emit(BeachRoot, beach, "BayBeachStones");

            // ================= 3. Geneva field dressing =================
            var field = GameObject.Find("GenevaField") ?? throw new Exception("GenevaField missing");
            var fr = field.GetComponentsInChildren<Renderer>(); var fb = fr[0].bounds; foreach (var r in fr) fb.Encapsulate(r.bounds);
            var gen = new MB(); var fieldCols = field.GetComponentsInChildren<Collider>();
            float SlabY(Vector2 p, out bool ok)
            {
                float best = float.NegativeInfinity; foreach (var h in Physics.RaycastAll(new Vector3(p.x, 300, p.y), Vector3.down, 600, ~0, QueryTriggerInteraction.Ignore)) if (fieldCols.Contains(h.collider)) best = Mathf.Max(best, h.point.y);
                ok = !float.IsNegativeInfinity(best); return best;
            }
            var groundCols = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.enabled && c.name.Contains("Terrain")).ToList();
            float TG(float x, float z, out bool ok) { float best = float.NegativeInfinity; foreach (var t in groundCols) if (t.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out var h, 900)) best = Mathf.Max(best, h.point.y); ok = !float.IsNegativeInfinity(best); return best; }
            var roadCols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(m => m.enabled && (m.name.StartsWith("Road_") || m.name.StartsWith("ExpansionRoad_") || m.name.StartsWith("ImportTrim_Road") || m.name.Contains("Sidewalk"))).ToList();
            bool OnRoad(Vector2 p) => Physics.RaycastAll(new Vector3(p.x, 500, p.y), Vector3.down, 900, ~0, QueryTriggerInteraction.Ignore).Any(h => roadCols.Contains(h.collider));
            var others = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && !r.transform.IsChildOf(field.transform) && r.bounds.size.y > 1f && r.bounds.size.y < 14f && r.bounds.size.x < 25f).Select(r => r.bounds).ToList();
            bool FreeSpot(Vector2 p, float r) { foreach (var b in others) { float dx = Mathf.Max(b.min.x - p.x, 0, p.x - b.max.x), dz = Mathf.Max(b.min.z - p.y, 0, p.y - b.max.z); if (dx * dx + dz * dz < r * r) return false; } for (int a = 0; a < 8; a++) if (OnRoad(p + new Vector2(Mathf.Cos(a * .785f), Mathf.Sin(a * .785f)) * r)) return false; return !OnRoad(p); }

            var fc = new Vector2(fb.center.x, fb.center.z); float fr0 = Mathf.Max(fb.extents.x, fb.extents.z);
            // user request: the field stays ONE solid colour, so no outfield patches
            log.Add("outfield patches=0 (removed by request)");

            // pavilion beside the field: first free spot on a ring around the slab, preferring the road (west) side
            int pav = 0; Vector2 pavPos = Vector2.zero; float pavY = 0;
            foreach (var ang in new[] { 180f, 200f, 160f, 220f, 140f, 240f, 120f, 260f, 100f, 280f, 20f, 340f })
                foreach (var rad in new[] { fr0 * .78f, fr0 * .88f, fr0 * 1.0f, fr0 * 1.12f })
                {
                    var p = fc + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * rad; float g = TG(p.x, p.y, out bool ok); if (!ok || g < 2f) continue;
                    if (SlabY(p, out bool onSlab) > 0 && onSlab) continue;
                    if (!FreeSpot(p, 6.5f)) continue;
                    float g2 = TG(p.x + 4, p.y, out bool o2), g3 = TG(p.x - 4, p.y, out bool o3), g4 = TG(p.x, p.y + 4, out bool o4), g5 = TG(p.x, p.y - 4, out bool o5); if (!(o2 && o3 && o4 && o5) || Mathf.Max(g2, g3, g4, g5) - Mathf.Min(g2, g3, g4, g5) > 1.6f) continue;
                    pavPos = p; pavY = Mathf.Max(g, g2, g3, g4, g5); pav = 1; goto placed;
                }
            placed:
            if (pav == 1)
            {
                var toField = (fc - pavPos).normalized; var f3 = new Vector3(toField.x, 0, toField.y); var r3 = Vector3.Cross(Vector3.up, f3).normalized; var up = Vector3.up; var b0 = new Vector3(pavPos.x, pavY, pavPos.y);
                gen.Box(b0 + up * .2f, r3, up, f3, new Vector3(4.6f, .2f, 3.2f), cell["pav_wall"]);                                   // plinth
                gen.Box(b0 + up * 2.6f, r3, up, f3, new Vector3(4.2f, 2.2f, 2.8f), cell["pav_wall"]);                                  // body (two storeys)
                gen.Box(b0 + up * 4.95f, r3, up, f3, new Vector3(4.6f, .18f, 3.2f), cell["pav_roof"]);                                 // roof slab
                gen.Box(b0 + up * 3.25f + f3 * 2.85f, r3, up, f3, new Vector3(4.2f, .09f, .55f), cell["pav_trim"]);                    // balcony floor toward field
                gen.Box(b0 + up * 3.85f + f3 * 3.3f, r3, up, f3, new Vector3(4.2f, .45f, .04f), cell["pav_trim"]);                     // balcony rail band
                foreach (float s in new[] { -2.6f, -.9f, .9f, 2.6f }) { gen.Box(b0 + up * 1.7f + f3 * 2.82f + r3 * s, r3, up, f3, new Vector3(.62f, .5f, .03f), cell["pav_window"]); gen.Box(b0 + up * 4.05f + f3 * 2.82f + r3 * s, r3, up, f3, new Vector3(.62f, .5f, .03f), cell["pav_window"]); }
                gen.Box(b0 + up * 1.1f + f3 * 2.82f, r3, up, f3, new Vector3(.55f, 1.0f, .04f), cell["pav_trim"]);                     // door
                var col = new GameObject("PavilionCollider"); col.transform.SetParent(null); col.name = "MINI182_PavilionCollider"; var bc = col.AddComponent<BoxCollider>(); bc.center = b0 + up * 2.6f; bc.size = new Vector3(8.4f, 5f, 5.6f);
                col.transform.rotation = Quaternion.identity; bc.size = new Vector3(Mathf.Abs(r3.x) * 8.4f + Mathf.Abs(f3.x) * 5.6f, 5f, Mathf.Abs(r3.z) * 8.4f + Mathf.Abs(f3.z) * 5.6f);
                col.transform.SetParent(null); log.Add($"pavilion at ({pavPos.x:F0},{pavY:F1},{pavPos.y:F0}) facing the field");
            }
            else log.Add("WARN: no free spot for the pavilion");
            var gg = Emit(GenevaRoot, gen, "GenevaDressing");
            var pc = GameObject.Find("MINI182_PavilionCollider"); if (pc != null) pc.transform.SetParent(gg.transform, true);

            // hedge + trees along the outer edge on the road/west and north sides (existing tree prefabs)
            var hedge = new MB(); int hedges = 0; var treeRoot = new GameObject("Trees"); treeRoot.transform.SetParent(gg.transform);
            var pfs = Directory.GetFiles(Art + "/Prefabs/Scenery", "SceneryBroadleaf*.prefab").Concat(Directory.GetFiles(Art + "/Prefabs/Scenery", "SceneryCoconut*.prefab")).Select(f => AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'))).ToList();
            int trees = 0;
            for (float ang = 90; ang <= 270; ang += 3.2f)
            {
                var d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)); var p = fc + d * (fr0 * 1.0f + R(1f, 3f)); float g = TG(p.x, p.y, out bool ok); if (!ok || g < 1.5f) continue;
                if ((SlabY(p, out bool os) > 0 && os) || !FreeSpot(p, 1.6f)) continue;
                if (R(0, 1) < .55f) { var t = new Vector3(-d.y, 0, d.x); hedge.Box(new Vector3(p.x, g + .45f, p.y), t, Vector3.up, new Vector3(d.x, 0, d.y), new Vector3(.4f, .5f, 1.2f), cell["hedge_green"], .85f); hedges++; }
                else if (pfs.Count > 0 && R(0, 1) < .5f && trees < 14 && FreeSpot(p, 3f)) { var inst = (GameObject)PrefabUtility.InstantiatePrefab(pfs[rng.Next(pfs.Count)], treeRoot.transform); inst.transform.SetPositionAndRotation(new Vector3(p.x, g - .05f, p.y), Quaternion.Euler(0, R(0, 360), 0)); inst.transform.localScale = Vector3.one * R(.9f, 1.2f); trees++; }
            }
            if (hedge.v.Count > 0) { var hm = Emit(GenevaRoot + "_Hedge", hedge, "GenevaHedge"); hm.transform.SetParent(gg.transform, true); }
            log.Add($"geneva hedge sections={hedges} trees={trees}");

            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            log.Add("live sha after=" + Sha(Live)); log.Add("copy in build settings=" + EditorBuildSettings.scenes.Any(s => s.enabled && s.path == Copy));
            File.WriteAllLines(Out + "/BayCoast-Report.txt", log); Debug.Log("MINI182COAST_APPLY " + string.Join(" | ", log));
        }
    }
}
