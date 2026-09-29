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
    /// MINI-182 stage 6 (isolated copy scene GrandBayProof_HouseEnhance.unity only; live scene hash checked):
    ///  A) Geneva field: level the field with the ground (terrain regraded on a COPY mesh asset, whole field group moved to the mean ground height),
    ///  B) expansion housing: the 52 grey ExpansionHouse_* boxes become red-roofed cream/peach apartment blocks (originals disabled),
    ///  C) Pierre Charles Secondary School rebuilt from the aerial photos around a basketball court, one merged mesh + box colliders (originals disabled),
    ///  D) old world-edge blockers (PhaseOne_Boundaries) disabled and replaced by a far outer boundary so the character can roam the whole map.
    /// Deterministic, idempotent, never touches the live scene.
    /// </summary>
    public static class Mini182Expansion
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini182";
        const string Gen = "Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-expansion-v1/Generated";
        const string Prefabs = Art + "/Prefabs";
        const string Export = "Logs/Tasks/MINI-182/Export";
        const string Out = "Logs/Tasks/MINI-182";
        const float OrigFieldTop = 12.93f;

        static string Sha(string p) { using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p); return BitConverter.ToString(s.ComputeHash(f)).Replace("-", ""); }
        static uint Hash(string s) { uint h = 5381; foreach (char ch in s) h = h * 33 + ch; return h; }
        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        class MB
        {
            public List<Vector3> v = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
            static Vector2 Uv(int cell) => new Vector2(((cell % 8) + .5f) / 8f, ((cell / 8) + .5f) / 8f);
            public void Box(Vector3 c, Vector3 half, int cell, float yaw = 0)
            {
                var q = Quaternion.Euler(0, yaw, 0); var rx = q * Vector3.right; var fz = q * Vector3.forward; var uy = Vector3.up; var p = new Vector3[8];
                for (int i = 0; i < 8; i++) { float sx = (i & 1) == 0 ? -1 : 1, sy = (i & 2) == 0 ? -1 : 1, sz = (i & 4) == 0 ? -1 : 1; p[i] = c + rx * (sx * half.x) + uy * (sy * half.y) + fz * (sz * half.z); }
                int[][] f = { new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 }, new[] { 0, 1, 5, 4 }, new[] { 2, 6, 7, 3 }, new[] { 0, 4, 6, 2 }, new[] { 1, 3, 7, 5 } };
                foreach (var face in f)
                {
                    int b = v.Count; foreach (var i in face) { v.Add(p[i]); uv.Add(Uv(cell)); }
                    var n = Vector3.Cross(v[b + 1] - v[b], v[b + 2] - v[b]); var mid = (v[b] + v[b + 2]) * .5f;
                    if (Vector3.Dot(n, mid - c) >= 0) t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 }); else t.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                }
            }
            public void Flat(Vector3 c, float hx, float hz, int cell, float yaw = 0) => Box(c, new Vector3(hx, .01f, hz), cell, yaw);
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Apply Field Housing School Boundaries")]
        public static void Apply()
        {
            try { ApplyInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static Material CoastMat()
        {
            string png = Art + "/palette_coast.png"; File.Copy(Export + "/palette_coast.png", png, true); AssetDatabase.Refresh();
            var ti = (TextureImporter)AssetImporter.GetAtPath(png); ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = false; ti.filterMode = FilterMode.Point; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 128; ti.SaveAndReimport();
            var m = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Palette_Coast.mat"); if (m == null) { m = new Material(Shader.Find("UpIzUpMini/ApprovedArtDiffuse")); AssetDatabase.CreateAsset(m, Art + "/Palette_Coast.mat"); }
            m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(png); m.color = Color.white; m.enableInstancing = true; EditorUtility.SetDirty(m); return m;
        }

        static void ApplyInner()
        {
            Directory.CreateDirectory(Out + "/Renders");
            string liveHash = Sha(Live); var log = new List<string> { "MINI-182 stage 6 (field, housing, school, boundaries)", "live sha before=" + liveHash };
            var cell = new Dictionary<string, int>(); foreach (var l in File.ReadAllLines(Export + "/coast_cells.txt")) { var s = l.Split(' '); cell[s[0]] = int.Parse(s[1]); }
            var coastMat = CoastMat();
            var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            foreach (var n in new[] { "MINI182_School", "MINI182_ExpansionHousing", "MINI182_OuterBoundary" }) { var o = GameObject.Find(n); if (o != null) UnityEngine.Object.DestroyImmediate(o); }
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (t != null && (t.name.StartsWith("SchoolCourtyard") || t.name.StartsWith("SchoolMetalRoof") || t.name.StartsWith("SchoolRear") || t.name.StartsWith("SchoolTeachingWing") || t.name == "SchoolWindow" || t.name.StartsWith("ExpansionHouse_")) && t.parent != null && t.parent.name == "MINI168_Expansion") t.gameObject.SetActive(true);
            Physics.SyncTransforms();
            var rng = new System.Random(186); float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var exp = GameObject.Find("MINI168_Expansion").transform;

            // ================= A. Geneva field level with the ground =================
            var field = exp.Find("GenevaField") ?? throw new Exception("GenevaField missing");
            var terrainTr = exp.Find("ExpansionTerrain") ?? throw new Exception("ExpansionTerrain missing");
            var tmf = terrainTr.GetComponent<MeshFilter>(); var tmc = terrainTr.GetComponent<MeshCollider>();
            string origPath = Gen + "/Import_ExpansionTerrain.asset"; var orig = AssetDatabase.LoadAssetAtPath<Mesh>(origPath) ?? throw new Exception("Import_ExpansionTerrain asset missing");
            var ov = orig.vertices; var wv = ov.Select(p => terrainTr.TransformPoint(p)).ToArray();
            field.position = new Vector3(field.position.x, 0, field.position.z);   // undo any earlier shift (root y is absolute below)
            var surf = field.Find("GenevaPlayingSurface") ?? throw new Exception("GenevaPlayingSurface missing");
            var sb = surf.GetComponent<Renderer>().bounds; float x0 = sb.min.x, x1 = sb.max.x, z0 = sb.min.z, z1 = sb.max.z;
            var inside = wv.Where(p => p.x > x0 && p.x < x1 && p.z > z0 && p.z < z1).ToList();
            float mean = inside.Count > 0 ? inside.Average(p => p.y) : OrigFieldTop; float mn = inside.Count > 0 ? inside.Min(p => p.y) : 0, mx = inside.Count > 0 ? inside.Max(p => p.y) : 0;
            float top = mean;   // level the field to the mean ground height (least cut and fill)
            float dy = top - OrigFieldTop; field.position = new Vector3(field.position.x, dy, field.position.z);
            log.Add($"field slab x {x0:F0}..{x1:F0} z {z0:F0}..{z1:F0}; ground under it min {mn:F2} mean {mean:F2} max {mx:F2}; original slab top {OrigFieldTop}; field moved by {dy:F2} m");
            var roadPts = new List<Vector2>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.enabled && (c.name.StartsWith("ExpansionRoad_") || c.name.StartsWith("Road_") || c.name.StartsWith("ImportTrim_Road"))))
            { var me = r.sharedMesh; if (me == null) continue; foreach (var v in me.vertices) { var w = r.transform.TransformPoint(v); roadPts.Add(new Vector2(w.x, w.z)); } }
            float RoadDist(Vector2 p) { float d = float.MaxValue; foreach (var q in roadPts) { float dd = (q - p).sqrMagnitude; if (dd < d) d = dd; } return Mathf.Sqrt(d); }
            var nv = (Vector3[])ov.Clone(); int changed = 0; float blend = 12f;
            for (int i = 0; i < wv.Length; i++)
            {
                var p = wv[i]; float dx = Mathf.Max(x0 - p.x, 0, p.x - x1), dz = Mathf.Max(z0 - p.z, 0, p.z - z1); float d = Mathf.Sqrt(dx * dx + dz * dz); if (d >= blend) continue;
                float rd = RoadDist(new Vector2(p.x, p.z)); if (rd < 7f) continue;                               // never bury or lift a road
                float w = d <= 0 ? 1f : 1f - Smooth(d / blend); float y = Mathf.Lerp(p.y, top - .04f, w);
                if (Mathf.Abs(y - p.y) < .001f) continue; var np = p; np.y = y; nv[i] = terrainTr.InverseTransformPoint(np); changed++;
            }
            var nm = UnityEngine.Object.Instantiate(orig); nm.name = "HouseEnhance_ExpansionTerrain"; nm.vertices = nv; nm.RecalculateNormals(); nm.RecalculateBounds();
            string np2 = Art + "/HouseEnhance_ExpansionTerrain.asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(np2) != null) AssetDatabase.DeleteAsset(np2); AssetDatabase.CreateAsset(nm, np2);
            tmf.sharedMesh = nm; tmc.sharedMesh = null; tmc.sharedMesh = nm; Physics.SyncTransforms();
            log.Add($"terrain vertices regraded={changed} (12 m blend, kept 7 m clear of roads); copy mesh asset HouseEnhance_ExpansionTerrain");
            // solid single-colour field: make sure the slab material is the plain grass and nothing else is layered on it
            float FG(float x, float z, out bool ok) { RaycastHit h; ok = tmc.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out h, 900); return ok ? h.point.y : 0f; }
            { int bad = 0; float maxGap = 0; for (float x = x0 + 2; x < x1 - 2; x += 6) for (float z = z0 + 2; z < z1 - 2; z += 6) { float g = FG(x, z, out bool ok); if (!ok) continue; float gap = (top) - g; maxGap = Mathf.Max(maxGap, Mathf.Abs(gap)); if (Mathf.Abs(gap) > .35f) bad++; } log.Add($"field level check: samples off by >0.35 m = {bad}, max |slab - ground| = {maxGap:F2} m"); }

            // ================= D. blockers =================
            var pob = GameObject.Find("PhaseOne_Boundaries"); if (pob != null) { pob.SetActive(false); log.Add("PhaseOne_Boundaries disabled (old world-edge blockers incl. FutureExitBarrier_*)"); }
            var tb = terrainTr.GetComponent<MeshCollider>().bounds; var ob = new GameObject("MINI182_OuterBoundary");
            void Wall(string n, Vector3 c, Vector3 size) { var g = new GameObject(n); g.transform.SetParent(ob.transform, false); var bc = g.AddComponent<BoxCollider>(); bc.center = c; bc.size = size; }
            Wall("West", new Vector3(tb.min.x - 1, tb.center.y, tb.center.z), new Vector3(2, 120, tb.size.z + 8)); Wall("East", new Vector3(tb.max.x + 1, tb.center.y, tb.center.z), new Vector3(2, 120, tb.size.z + 8));
            Wall("South", new Vector3(tb.center.x, tb.center.y, tb.min.z - 1), new Vector3(tb.size.x + 8, 120, 2)); Wall("North", new Vector3(tb.center.x, tb.center.y, tb.max.z + 1), new Vector3(tb.size.x + 8, 120, 2));
            log.Add($"outer boundary at terrain edge x {tb.min.x:F0}..{tb.max.x:F0}, z {tb.min.z:F0}..{tb.max.z:F0} (invisible, 4 walls)");

            // helpers shared by B and C
            var groundCols = new List<Collider> { tmc };
            float G(float x, float z, out bool ok) { float best = float.NegativeInfinity; foreach (var c in groundCols) if (c.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out var h, 900)) best = Mathf.Max(best, h.point.y); ok = !float.IsNegativeInfinity(best); return best; }

            // ================= B. expansion housing -> apartment complexes =================
            var pf = Directory.GetFiles(Prefabs, "HouseApartmentHip2s_*.prefab").OrderBy(f => f).Select(f => AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'))).ToList();
            if (pf.Count == 0) throw new Exception("apartment prefabs missing - import the house kit first");
            var housing = new GameObject("MINI182_ExpansionHousing"); int placed = 0, kept = 0;
            var roadCols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(m => m.enabled && (m.name.StartsWith("ExpansionRoad_") || m.name.StartsWith("Road_") || m.name.StartsWith("ImportTrim_Road") || m.name.Contains("Sidewalk"))).ToList();
            bool OnRoad(Vector2 p) => Physics.RaycastAll(new Vector3(p.x, 500, p.y), Vector3.down, 900, ~0, QueryTriggerInteraction.Ignore).Any(h => roadCols.Contains(h.collider));
            var boxes = new List<(Vector2 c, Vector2 f, Vector2 r, float hf, float hb, float hr)>();
            bool Ov((Vector2 c, Vector2 f, Vector2 r, float hf, float hb, float hr) a, (Vector2 c, Vector2 f, Vector2 r, float hf, float hb, float hr) b)
            {
                Vector2[] Cs((Vector2 c, Vector2 f, Vector2 r, float hf, float hb, float hr) o) => new[] { o.c + o.f * o.hf + o.r * o.hr, o.c + o.f * o.hf - o.r * o.hr, o.c - o.f * o.hb - o.r * o.hr, o.c - o.f * o.hb + o.r * o.hr };
                var ca = Cs(a); var cb = Cs(b);
                foreach (var ax in new[] { a.f, a.r, b.f, b.r }) { float amin = 1e9f, amax = -1e9f, bmin = 1e9f, bmax = -1e9f; foreach (var p in ca) { float d = Vector2.Dot(p, ax); amin = Mathf.Min(amin, d); amax = Mathf.Max(amax, d); } foreach (var p in cb) { float d = Vector2.Dot(p, ax); bmin = Mathf.Min(bmin, d); bmax = Mathf.Max(bmax, d); } if (amax < bmin || bmax < amin) return false; }
                return true;
            }
            var others = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && r.bounds.size.y > 1.2f && r.bounds.size.y < 16f && r.bounds.size.x < 30f && r.bounds.size.z < 30f && !(r.transform.parent != null && r.transform.parent.name == "MINI168_Expansion" && r.name.StartsWith("ExpansionHouse_")) && !r.name.StartsWith("HouseFoundation_") && !r.transform.IsChildOf(field) && !r.name.StartsWith("School") && r.transform.root.name != "MINI182_Lalay").Select(r => r.bounds).ToList();
            var toRoad = roadPts.Where(p => p.x > -130).ToList();
            var eh = exp.Cast<Transform>().Where(t => t.name.StartsWith("ExpansionHouse_")).OrderBy(t => t.name).ToList();
            foreach (var t in eh)
            {
                var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue; var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var c2 = new Vector2(b.center.x, b.center.z); Vector2 nr = Vector2.up; float dn = float.MaxValue; foreach (var q in toRoad) { float dd = (q - c2).sqrMagnitude; if (dd < dn) { dn = dd; nr = (q - c2); } }
                var f = nr.sqrMagnitude > .01f ? nr.normalized : Vector2.up; var r2 = new Vector2(f.y, -f.x);
                var prefab = pf[(int)(Hash(t.name) % (uint)pf.Count)]; bool okp = false; Vector2 pos = c2; float sc = 1f;
                foreach (var s in new[] { 1f, .92f, .85f })
                {
                    var me = (c: c2, f, r: r2, hf: 3.9f * s + .9f, hb: 3.9f * s + .3f, hr: 4.0f * s + .3f);
                    bool bad = false; foreach (var o in others) { var ob2 = (c: new Vector2(o.center.x, o.center.z), f: Vector2.up, r: Vector2.right, hf: o.extents.z, hb: o.extents.z, hr: o.extents.x); if (Ov(me, ob2)) { bad = true; break; } }
                    if (!bad) foreach (var pb in boxes) if (Ov(me, pb)) { bad = true; break; }
                    if (!bad) for (int i = 0; i <= 6 && !bad; i++) for (int j = 0; j <= 6; j++) { var p = c2 + f * Mathf.Lerp(-me.hb, me.hf, i / 6f) + r2 * Mathf.Lerp(-me.hr, me.hr, j / 6f); if (OnRoad(p)) { bad = true; break; } }
                    if (bad) continue; okp = true; sc = s; boxes.Add(me); break;
                }
                if (!okp) { kept++; continue; }
                float gmin = float.MaxValue; foreach (var dx in new[] { -1f, 1f }) foreach (var dz in new[] { -1f, 1f }) { var p = c2 + r2 * dx * 3.8f * sc + f * dz * 3.2f * sc; float g = G(p.x, p.y, out bool ok); if (ok) gmin = Mathf.Min(gmin, g); }
                if (gmin > 1e8f) { kept++; continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, housing.transform); inst.name = prefab.name + "__for__" + t.name;
                float yaw = Quaternion.LookRotation(new Vector3(-f.x, 0, -f.y), Vector3.up).eulerAngles.y;
                inst.transform.SetPositionAndRotation(new Vector3(c2.x, gmin - .02f, c2.y), Quaternion.Euler(0, yaw, 0)); inst.transform.localScale = new Vector3(sc, 1f, sc);
                t.gameObject.SetActive(false); var fnd = exp.Find("HouseFoundation_" + t.name.Substring("ExpansionHouse_".Length)); if (fnd != null) fnd.gameObject.SetActive(false);
                placed++;
            }
            log.Add($"apartment blocks placed={placed} kept original massing={kept} of {eh.Count}");

            // ================= C. school =================
            var cy = exp.Find("SchoolCourtyard"); var wing0 = exp.Find("SchoolTeachingWing_0"); var wing1 = exp.Find("SchoolTeachingWing_1"); var rear = exp.Find("SchoolRearWing");
            if (cy == null || wing0 == null || wing1 == null || rear == null) throw new Exception("school pieces missing");
            Bounds Bd(Transform t) { var r = t.GetComponent<Renderer>(); return r.bounds; }
            var bw0 = Bd(wing0); var bw1 = Bd(wing1); var br = Bd(rear); var bc0 = Bd(cy);
            float yb = bc0.min.y;   // courtyard slab base = school floor level
            foreach (var t in exp.Cast<Transform>().Where(t => t.name.StartsWith("SchoolCourtyard") || t.name.StartsWith("SchoolMetalRoof") || t.name.StartsWith("SchoolRear") || t.name.StartsWith("SchoolTeachingWing") || t.name == "SchoolWindow").ToList()) t.gameObject.SetActive(false);
            var sm = new MB(); var school = new GameObject("MINI182_School"); var cols = new List<(Vector3 c, Vector3 size)>();
            void Col(Vector3 c, Vector3 size) => cols.Add((c, size));
            // a two-storey block: white ground floor + window band, orange upper wall + window band, dark roof, walkway slab and rail on the courtyard side
            void Block(Bounds b, int side /*+1 = courtyard on +x, -1 = courtyard on -x, 0 = courtyard on +z (rear)*/)
            {
                float h1 = 3.0f, h2 = 3.0f; var c = b.center; float hx = b.extents.x, hz = b.extents.z; float y0 = yb;
                sm.Box(new Vector3(c.x, y0 + h1 / 2, c.z), new Vector3(hx, h1 / 2, hz), cell["school_white"]);
                sm.Box(new Vector3(c.x, y0 + h1 + h2 / 2, c.z), new Vector3(hx - .04f, h2 / 2, hz - .04f), cell["school_orange"]);
                sm.Box(new Vector3(c.x, y0 + h1 + .1f, c.z), new Vector3(hx + .08f, .1f, hz + .08f), cell["school_grey"]);                    // floor slab band
                sm.Box(new Vector3(c.x, y0 + h1 + h2 + .32f, c.z), new Vector3(hx + .7f, .16f, hz + .7f), cell["school_roof"]);               // roof, overhanging
                sm.Box(new Vector3(c.x, y0 + h1 + h2 + .75f, c.z), new Vector3(hx * .55f, .32f, hz + .35f), cell["school_roof"]);             // low ridge lift
                float len = side == 0 ? hx : hz; int n = Mathf.Max(3, Mathf.RoundToInt(len * 2f / 2.6f));
                for (int k = 0; k < n; k++)
                {
                    float u = Mathf.Lerp(-len + 1.2f, len - 1.2f, (k + .5f) / n);
                    foreach (var (yy, hh, col) in new[] { (y0 + 1.7f, .6f, "school_glass"), (y0 + h1 + 1.9f, .55f, "school_glass") })
                    {
                        if (side == 0) { sm.Box(new Vector3(c.x + u, yy, b.min.z - .02f), new Vector3(.8f, hh, .04f), cell[col]); sm.Box(new Vector3(c.x + u, yy, b.max.z + .02f), new Vector3(.8f, hh, .04f), cell[col]); }
                        else { sm.Box(new Vector3(b.min.x - .02f, yy, c.z + u), new Vector3(.04f, hh, .8f), cell[col]); sm.Box(new Vector3(b.max.x + .02f, yy, c.z + u), new Vector3(.04f, hh, .8f), cell[col]); }
                    }
                }
                // walkway slab + rail on the courtyard face (upper floor)
                if (side != 0)
                {
                    float fx = side > 0 ? b.max.x : b.min.x; float wx = fx + side * .8f;
                    sm.Box(new Vector3(wx, y0 + h1 + .15f, c.z), new Vector3(.8f, .12f, hz), cell["school_grey"]);
                    sm.Box(new Vector3(fx + side * 1.55f, y0 + h1 + .95f, c.z), new Vector3(.04f, .45f, hz), cell["school_rail"]);
                    for (float z = c.z - hz + 1f; z < c.z + hz; z += 3.5f) sm.Box(new Vector3(fx + side * 1.5f, y0 + (h1) / 2, z), new Vector3(.1f, h1 / 2, .1f), cell["school_grey"]);   // posts holding the walkway
                }
                else
                {
                    float fz = b.max.z; sm.Box(new Vector3(c.x, y0 + h1 + .15f, fz + .8f), new Vector3(hx, .12f, .8f), cell["school_grey"]); sm.Box(new Vector3(c.x, y0 + h1 + .95f, fz + 1.55f), new Vector3(hx, .45f, .04f), cell["school_rail"]);
                }
                Col(new Vector3(c.x, y0 + (h1 + h2) / 2, c.z), new Vector3(hx * 2, h1 + h2, hz * 2));
            }
            Block(bw0, +1); Block(bw1, -1); Block(br, 0);
            // basketball court in the middle of the campus
            float cx = (bw0.max.x + bw1.min.x) / 2, czMid = (br.max.z + Mathf.Max(bw0.max.z, bw1.max.z)) / 2 - 1f; float cw = Mathf.Min(bw1.min.x - bw0.max.x - 3.2f, 13f), cl = Mathf.Min(Mathf.Max(bw0.max.z, bw1.max.z) - br.max.z - 3.5f, 20f);
            sm.Box(new Vector3(bc0.center.x, yb + .03f, bc0.center.z), new Vector3(bc0.extents.x, .03f, bc0.extents.z), cell["school_grey"]);   // paved yard
            float cyv = yb + .075f;
            sm.Box(new Vector3(cx, cyv, czMid), new Vector3(cw / 2, .03f, cl / 2), cell["court_paving"]);
            float lw = .06f;
            sm.Box(new Vector3(cx, cyv + .03f, czMid - cl / 2), new Vector3(cw / 2, .008f, lw), cell["court_line"]); sm.Box(new Vector3(cx, cyv + .03f, czMid + cl / 2), new Vector3(cw / 2, .008f, lw), cell["court_line"]);
            sm.Box(new Vector3(cx - cw / 2, cyv + .03f, czMid), new Vector3(lw, .008f, cl / 2), cell["court_line"]); sm.Box(new Vector3(cx + cw / 2, cyv + .03f, czMid), new Vector3(lw, .008f, cl / 2), cell["court_line"]);
            sm.Box(new Vector3(cx, cyv + .03f, czMid), new Vector3(cw / 2, .008f, lw), cell["court_line"]);
            for (int i = 0; i < 24; i++) { float a0 = i * Mathf.PI * 2 / 24, a1 = (i + 1) * Mathf.PI * 2 / 24; var m = new Vector3(cx + Mathf.Cos(a0 + .13f) * 1.9f, cyv + .03f, czMid + Mathf.Sin(a0 + .13f) * 1.9f); sm.Box(m, new Vector3(.3f, .008f, lw), cell["court_line"], -(a0 + .13f + Mathf.PI / 2) * Mathf.Rad2Deg); }
            foreach (float sg in new[] { -1f, 1f })
            {
                float zE = czMid + sg * cl / 2; sm.Box(new Vector3(cx, cyv + .03f, zE - sg * 5.6f), new Vector3(2.4f, .008f, lw), cell["court_line"]); sm.Box(new Vector3(cx - 2.4f, cyv + .03f, zE - sg * 2.8f), new Vector3(lw, .008f, 2.8f), cell["court_line"]); sm.Box(new Vector3(cx + 2.4f, cyv + .03f, zE - sg * 2.8f), new Vector3(lw, .008f, 2.8f), cell["court_line"]);
                sm.Box(new Vector3(cx, yb + 1.6f, zE + sg * .9f), new Vector3(.07f, 1.6f, .07f), cell["school_dark"]); sm.Box(new Vector3(cx, yb + 3.05f, zE + sg * .5f), new Vector3(.7f, .45f, .04f), cell["court_line"]);       // pole + backboard
                sm.Box(new Vector3(cx, yb + 2.85f, zE + sg * .28f), new Vector3(.22f, .025f, .22f), cell["school_orange"]);                                                                                                              // hoop
            }
            Col(new Vector3(cx, yb + 1.6f, czMid - cl / 2 - .9f), new Vector3(.2f, 3.2f, .2f)); Col(new Vector3(cx, yb + 1.6f, czMid + cl / 2 + .9f), new Vector3(.2f, 3.2f, .2f));
            // front wall + gate at the open (entrance) side
            float zf = Mathf.Max(bw0.max.z, bw1.max.z) + 1.2f; float xL = bw0.min.x, xR = bw1.max.x, gate = 3.2f;
            foreach (var (a, b) in new[] { (xL, cx - gate), (cx + gate, xR) })
            { sm.Box(new Vector3((a + b) / 2, yb + .55f, zf), new Vector3((b - a) / 2, .55f, .12f), cell["school_white"]); sm.Box(new Vector3((a + b) / 2, yb + 1.22f, zf), new Vector3((b - a) / 2, .06f, .05f), cell["school_rail"]); Col(new Vector3((a + b) / 2, yb + .7f, zf), new Vector3(b - a, 1.4f, .3f)); }
            foreach (float sg in new[] { -1f, 1f }) sm.Box(new Vector3(cx + sg * gate, yb + 1.0f, zf), new Vector3(.22f, 1.0f, .22f), cell["school_yellow"]);
            sm.Box(new Vector3(cx, yb + 2.15f, zf), new Vector3(gate + .3f, .25f, .08f), cell["school_yellow"]);   // sign board over the gate
            // (the side classroom that used to sit on the road beside the school moved across the road, higher up - see Mini182Complex)
            // export mesh
            var mesh = new Mesh { name = "MINI182_School" }; if (sm.v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(sm.v); mesh.SetUVs(0, sm.uv); mesh.SetTriangles(sm.t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string mp = Art + "/School.asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(mp) != null) AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
            var vis = new GameObject("SchoolMesh"); vis.transform.SetParent(school.transform, false); vis.isStatic = true; vis.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = vis.AddComponent<MeshRenderer>(); mr.sharedMaterial = coastMat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var cg = new GameObject("SchoolColliders"); cg.transform.SetParent(school.transform, false); foreach (var (c, size) in cols) { var g = new GameObject("Box"); g.transform.SetParent(cg.transform, false); var bxc = g.AddComponent<BoxCollider>(); bxc.center = c; bxc.size = size; }
            log.Add($"school rebuilt: wings {bw0.center}/{bw1.center}, rear {br.center}, court {cw:F0}x{cl:F0} at ({cx:F0},{czMid:F0}), floor y {yb:F2}; mesh tris={mesh.triangles.Length / 3}, colliders={cols.Count}");

            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            log.Add("live sha after=" + Sha(Live)); log.Add("copy in build settings=" + EditorBuildSettings.scenes.Any(s => s.enabled && s.path == Copy));
            File.WriteAllLines(Out + "/Expansion-Report.txt", log); Debug.Log("MINI182EXP_APPLY " + string.Join(" | ", log));
        }
    }
}
