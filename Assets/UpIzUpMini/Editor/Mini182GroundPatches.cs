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
    /// MINI-182 stage 4 step 3: ground colour variety for Lalay in the isolated copy scene. Thin, terrain-conforming patch meshes
    /// (dirt, worn paths, garden soil, dry grass, forest floor) each drawn as a coloured core over a soft-coloured rim, merged
    /// into ONE mesh using the shared palette material (one draw call). Deterministic (seed 183), idempotent (root rebuilt),
    /// live scene never touched (hash checked). Nothing is placed over roads/sidewalks, buildings or streams.
    /// </summary>
    public static class Mini182GroundPatches
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string OldArt = "Assets/UpIzUpMini/Art/Environment/Mini142";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini182";
        const string Export = "Logs/Tasks/MINI-182/Export";
        const string Out = "Logs/Tasks/MINI-182";
        const string RootName = "MINI182_LalayGround";
        const float XMin = -110f, XMax = 160f, ZMin = -215f, ZMax = -95f;

        static string Sha(string p) { using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p); return BitConverter.ToString(s.ComputeHash(f)).Replace("-", ""); }

        class Ctx
        {
            public List<Bounds> buildings = new List<Bounds>();
            public List<Collider> roads = new List<Collider>();
            public List<Vector2> roadPts = new List<Vector2>();
            public List<Vector2> streamPts = new List<Vector2>();
            public List<MeshCollider> terrain = new List<MeshCollider>();
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
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var b = r.bounds; if (b.size.y < .9f || b.size.y > 18f || b.size.x > 25f || b.size.z > 25f) continue;
                bool skip = false; for (var p = r.transform; p != null; p = p.parent) { string pn = p.name; if (pn.StartsWith("ApprovedGrass") || pn.Contains("Grass") || pn.StartsWith("MINI182_LalayTrees") || pn.StartsWith(RootName) || pn.StartsWith("NPC_") || pn.Contains("Waterway")) { skip = true; break; } }
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
        static bool PointOk(Ctx c, Vector2 p, float bldGap, float streamGap)
        {
            Ground(c, p.x, p.y, out bool ok); if (!ok) return false;
            foreach (var b in c.buildings) if (DistToBox(p, b) < bldGap) return false;
            if (OnRoad(c, p)) return false;
            if (streamGap > 0) foreach (var s in c.streamPts) if ((s - p).sqrMagnitude < streamGap * streamGap) return false;
            return true;
        }

        class MB
        {
            public List<Vector3> v = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
            public Vector2 Uv(int cell) => new Vector2(((cell % 8) + .5f) / 8f, ((cell / 8) + .5f) / 8f);
            public int Add(Vector3 p, int cell) { v.Add(p); uv.Add(Uv(cell)); return v.Count - 1; }
        }

        static Vector3 Lift(Ctx c, Vector2 p, float layer)
        {
            float g0 = Ground(c, p.x, p.y, out _); float s = 0;
            foreach (var d in new[] { new Vector2(1.4f, 0), new Vector2(-1.4f, 0), new Vector2(0, 1.4f), new Vector2(0, -1.4f) }) { float g1 = Ground(c, p.x + d.x, p.y + d.y, out bool ok); if (ok) s = Mathf.Max(s, Mathf.Abs(g1 - g0) / 1.4f); }
            return new Vector3(p.x, g0 + .05f + layer + s * .14f, p.y);
        }

        static Func<float, float> Shape(System.Random rng)
        {
            float p1 = (float)rng.NextDouble() * 6.28f, p2 = (float)rng.NextDouble() * 6.28f, p3 = (float)rng.NextDouble() * 6.28f;
            float a1 = .12f + (float)rng.NextDouble() * .10f, a2 = .08f + (float)rng.NextDouble() * .08f, a3 = .04f + (float)rng.NextDouble() * .05f;
            return th => 1f + a1 * Mathf.Sin(2 * th + p1) + a2 * Mathf.Sin(3 * th + p2) + a3 * Mathf.Sin(5 * th + p3);
        }

        // blob polygon points at scale k (rings share the same shape so layers nest)
        static Vector2[] Ring(Vector2 c, float R, float stretch, float rot, Func<float, float> shape, int n, float k)
        {
            var pts = new Vector2[n]; var ax = new Vector2(Mathf.Cos(rot), Mathf.Sin(rot)); var ay = new Vector2(-ax.y, ax.x);
            for (int i = 0; i < n; i++) { float th = i * Mathf.PI * 2 / n; float r = R * k * shape(th); pts[i] = c + ax * (Mathf.Cos(th) * r * stretch) + ay * (Mathf.Sin(th) * r / stretch); }
            return pts;
        }

        static void AddDisc(Ctx c, MB mb, Vector2 centre, Vector2[] outer, float layer, int cell, int rings, Func<float, float> shape, float R, float stretch, float rot)
        {
            int n = outer.Length; int ci = mb.Add(Lift(c, centre, layer), cell); var prev = new int[n];
            for (int i = 0; i < n; i++) prev[i] = ci;
            for (int r = 1; r <= rings; r++)
            {
                float k = r / (float)rings; var pts = Ring(centre, R, stretch, rot, shape, n, k * (outer.Length > 0 ? 1f : 1f) * 1f);
                var cur = new int[n]; for (int i = 0; i < n; i++) cur[i] = mb.Add(Lift(c, pts[i], layer), cell);
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    if (r == 1) mb.t.AddRange(new[] { ci, cur[j], cur[i] });
                    else { mb.t.AddRange(new[] { prev[i], prev[j], cur[j] }); mb.t.AddRange(new[] { prev[i], cur[j], cur[i] }); }
                }
                prev = cur;
            }
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Apply Ground Patches Around Lalay")]
        public static void Apply()
        {
            try { ApplyInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void ImportPalette(List<string> log)
        {
            string oldPng = OldArt + "/palette.png", newPng = Export + "/palette.png";
            var a = new Texture2D(2, 2); a.LoadImage(File.ReadAllBytes(oldPng)); var b = new Texture2D(2, 2); b.LoadImage(File.ReadAllBytes(newPng));
            for (int c = 0; c < 42; c++)
            {
                int px = c % 8 * 16 + 8, py = (c / 8) * 16 + 8; Color32 ca = a.GetPixel(px, py), cb = b.GetPixel(px, py);
                if (ca.r != cb.r || ca.g != cb.g || ca.b != cb.b) throw new Exception("SAFETY STOP: palette cell " + c + " changed; not overwriting.");
            }
            log.Add("palette cells 0..41 verified identical");
            if (File.ReadAllBytes(oldPng).SequenceEqual(File.ReadAllBytes(newPng))) { log.Add("palette already up to date"); return; }
            File.Copy(newPng, oldPng, true); AssetDatabase.Refresh();
            var ti = (TextureImporter)AssetImporter.GetAtPath(oldPng);
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = false; ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 128; ti.SaveAndReimport();
            log.Add("palette.png replaced by superset (appended cells only)");
        }

        static void ApplyInner()
        {
            Directory.CreateDirectory(Out + "/Renders");
            string liveHash = Sha(Live); var log = new List<string> { "MINI-182 Lalay ground patches", "live sha before=" + liveHash };
            ImportPalette(log);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OldArt + "/Palette.mat"); mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(OldArt + "/palette.png");
            var cell = new Dictionary<string, int>(); foreach (var l in File.ReadAllLines(Export + "/ground_cells.txt")) { var s = l.Split(' '); cell[s[0]] = int.Parse(s[1]); }

            var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            var old = GameObject.Find(RootName); if (old != null) UnityEngine.Object.DestroyImmediate(old);
            Physics.SyncTransforms(); var ctx = Build();
            log.Add($"buildings={ctx.buildings.Count} roads={ctx.roads.Count} streamPts={ctx.streamPts.Count}");
            var rng = new System.Random(183); float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            int patchIx = 0; var mb = new MB(); var counts = new Dictionary<string, int>(); var rejected = 0;

            bool Blob(string kind, Vector2 c, float radius, string main, string edge, float layer, float bldGap, float streamGap, float stretchMax = 1.35f)
            {
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    float Rr = radius * Mathf.Pow(.85f, attempt); var cJ = attempt == 0 ? c : c + new Vector2(R(-1.5f, 1.5f), R(-1.5f, 1.5f)); var shape = Shape(rng); float stretch = 1f + (float)rng.NextDouble() * (stretchMax - 1f), rot = R(0, 6.28f);
                    var rim = Ring(cJ, Rr, stretch, rot, shape, 12, 1.25f); bool ok = PointOk(ctx, cJ, bldGap, streamGap);
                    if (ok) foreach (var p in rim) if (!PointOk(ctx, p, bldGap, streamGap)) { ok = false; break; }
                    if (!ok) continue;
                    float jit = (patchIx++ % 8) * .0015f; var outer = rim; AddDisc(ctx, mb, cJ, outer, layer + jit, cell[edge], 3, shape, Rr * 1.25f, stretch, rot);
                    AddDisc(ctx, mb, cJ, outer, layer + jit + .0012f, cell[main], 3, shape, Rr, stretch, rot);
                    counts[kind] = counts.TryGetValue(kind, out var n) ? n + 1 : 1; return true;
                }
                rejected++; return false;
            }
            void Path(Vector2 start, Vector2 dir, float length, float width)
            {
                var pts = new List<Vector2>(); var p = start; var d = dir.normalized;
                for (float s = 0; s <= length; s += 2f)
                {
                    pts.Add(p); d = (d + new Vector2(-d.y, d.x) * R(-.22f, .22f)).normalized; p += d * 2f;
                }
                foreach (var q in pts) if (!PointOk(ctx, q, .5f, 1f)) { rejected++; return; }
                int prevL = -1, prevR = -1;
                for (int i = 0; i < pts.Count; i++)
                {
                    var dd = i < pts.Count - 1 ? (pts[i + 1] - pts[i]).normalized : (pts[i] - pts[i - 1]).normalized; var nrm = new Vector2(-dd.y, dd.x);
                    float taper = Mathf.Clamp01(Mathf.Min(i, pts.Count - 1 - i) / 2f + .35f); float hw = width * .5f * taper;
                    int a = mb.Add(Lift(ctx, pts[i] + nrm * hw, .036f), cell["ground_path"]), b = mb.Add(Lift(ctx, pts[i] - nrm * hw, .036f), cell["ground_path"]);
                    if (prevL >= 0) { mb.t.AddRange(new[] { prevL, a, b }); mb.t.AddRange(new[] { prevL, b, prevR }); }
                    prevL = a; prevR = b;
                }
                counts["path"] = counts.TryGetValue("path", out var n) ? n + 1 : 1;
            }
            void Garden(Vector2 c, Vector2 f, Vector2 r)
            {
                float w = R(1.4f, 2.2f), d = R(1f, 1.6f); var corners = new[] { c - r * w - f * d, c + r * w - f * d, c + r * w + f * d, c - r * w + f * d };
                foreach (var q in corners) if (!PointOk(ctx, q, .7f, 1.5f)) { rejected++; return; }
                var idx = corners.Select(q => mb.Add(Lift(ctx, q, .03f), cell["ground_garden"])).ToArray();
                var mid = mb.Add(Lift(ctx, c, .03f), cell["ground_garden"]);
                for (int i = 0; i < 4; i++) mb.t.AddRange(new[] { mid, idx[i], idx[(i + 1) % 4] }.Reverse());
                counts["garden"] = counts.TryGetValue("garden", out var n) ? n + 1 : 1;
            }

            // houses in the strip
            var houses = new List<Bounds>();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                bool kit = t.parent != null && t.parent.name == "MINI182_Lalay"; bool orig = t.name.StartsWith("Lalay_House_") || t.name.StartsWith("Lalay_Home_");
                if (!(kit || orig) || !t.gameObject.activeInHierarchy) continue;
                var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue; var b = rs[0].bounds; foreach (var rr in rs) b.Encapsulate(rr.bounds);
                if (b.center.x < XMin || b.center.x > XMax || b.center.z < ZMin || b.center.z > ZMax) continue; houses.Add(b);
            }
            Vector2 ToRoad(Vector2 c2) { Vector2 best = Vector2.up; float d = float.MaxValue; foreach (var q in ctx.roadPts) { float dd = (q - c2).sqrMagnitude; if (dd < d) { d = dd; best = q - c2; } } return best.normalized; }

            // 1) forest floor first (lowest layer): under broadleaf trees and big hillside blobs
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(t => t.name.StartsWith("SceneryBroadleaf")))
                if (R(0, 1) < .45f) Blob("forest", new Vector2(t.position.x, t.position.z), R(4f, 6.5f), "ground_forest", "ground_forest_edge", .0f, .3f, 1.2f, 1.3f);
            for (int i = 0; i < 26; i++) Blob("forest", new Vector2(R(XMin, XMax), R(ZMax + 5, ZMax + 50)), R(9f, 16f), "ground_forest", "ground_forest_edge", .0f, .3f, 1.2f, 1.5f);
            // 2) dry grass on open ground
            for (int i = 0; i < 110; i++) Blob("dry grass", new Vector2(R(XMin, XMax), R(ZMin - 12, ZMax + 45)), R(5f, 11f), "ground_dry_grass", "ground_dry_edge", .012f, .3f, 1.5f, 1.6f);
            // 3) dirt near houses, paths, gardens
            foreach (var h in houses.OrderBy(b => b.center.x))
            {
                var c2 = new Vector2(h.center.x, h.center.z); var f = ToRoad(c2); var r = new Vector2(f.y, -f.x);
                float depth = Mathf.Max(h.extents.x, h.extents.z), width = Mathf.Min(h.extents.x, h.extents.z);
                if (R(0, 1) < .6f) Blob("dirt", c2 - f * (depth + R(.8f, 3f)) + r * R(-width - 1f, width + 1f), R(1.4f, 3.2f), "ground_dirt", "ground_dirt_edge", .024f, .25f, 1.5f, 1.5f);
                if (R(0, 1) < .3f) Blob("dirt", c2 + r * (R(0, 1) < .5f ? -1 : 1) * (width + R(1.2f, 2.5f)) - f * R(0f, depth), R(1f, 2.2f), "ground_dirt", "ground_dirt_edge", .024f, .25f, 1.5f, 1.5f);
                if (R(0, 1) < .3f) for (int pa = 0; pa < 3; pa++) { int before = counts.TryGetValue("path", out var pc) ? pc : 0; Path(c2 - f * (depth + 1.2f) + r * R(-width, width), (-f + r * R(-.8f, .8f)), R(8f, 22f), R(.9f, 1.4f)); if ((counts.TryGetValue("path", out var pc2) ? pc2 : 0) > before) break; }
                if (R(0, 1) < .22f) Garden(c2 - f * (depth + R(3f, 6f)) + r * R(-width, width), f, r);
            }
            var mesh = new Mesh { name = "MINI182_LalayGround" };
            if (mb.v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(mb.v); mesh.SetUVs(0, mb.uv); mesh.SetTriangles(mb.t, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            for (int i = 0; i < mesh.normals.Length; i++) { }   // normals from RecalculateNormals; flip any pointing down
            var nrms = mesh.normals; for (int i = 0; i < nrms.Length; i++) if (nrms[i].y < 0) nrms[i] = -nrms[i]; mesh.normals = nrms;
            string mp = Art + "/GroundPatches.asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(mp) != null) AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(mesh, mp);
            var go = new GameObject(RootName); go.isStatic = true;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = true;
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            log.Add($"mesh verts={mesh.vertexCount} tris={mesh.triangles.Length / 3}"); foreach (var kv in counts) log.Add($"  {kv.Key}: {kv.Value}"); log.Add("rejected attempts=" + rejected);
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            log.Add("live sha after=" + Sha(Live)); log.Add("copy in build settings=" + EditorBuildSettings.scenes.Any(s => s.enabled && s.path == Copy));
            File.WriteAllLines(Out + "/LalayGround-Report.txt", log); Debug.Log("MINI182GROUND_APPLY " + string.Join(" | ", log));
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Validate And Render Ground Patches")]
        public static void ValidateAndRender()
        {
            try
            {
                EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single); Physics.SyncTransforms();
                var root = GameObject.Find(RootName); if (root == null) throw new Exception("no ground root");
                var ctx = Build(); var mesh = root.GetComponent<MeshFilter>().sharedMesh; var vs = mesh.vertices;
                int onRoad = 0, inBld = 0, floating = 0, buried = 0;
                foreach (var v in vs)
                {
                    var p = new Vector2(v.x, v.z); float g = Ground(ctx, p.x, p.y, out bool ok); if (!ok) continue;
                    if (OnRoad(ctx, p)) onRoad++;
                    foreach (var b in ctx.buildings) if (DistToBox(p, b) < .01f) { inBld++; break; }
                    float dy = v.y - g; if (dy > .6f) floating++; if (dy < .0f) buried++;
                }
                var lines = new List<string> { "MINI-182 Lalay ground validation (fresh reload)", $"vertices={vs.Length} onRoad={onRoad} insideBuildingBounds={inBld} floating>0.6m={floating} belowGround={buried}" };
                File.WriteAllLines(Out + "/LalayGroundValidation.txt", lines); foreach (var l in lines) Debug.Log("MINI182GROUND_VALIDATE " + l);
                var go = new GameObject("GroundCam"); var cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f); cam.farClipPlane = 2000; cam.fieldOfView = 55;
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
                Shot("g1_lalay_top", new Vector3(c0.x, 400, c0.z), c0, true, 73.5f);
                Shot("g2_lalay_oblique", new Vector3(c0.x - 5, G(c0.x, c0.z) + 55, c0.z - 60), new Vector3(c0.x + 10, G(c0.x + 10, c0.z), c0.z + 5));
                Shot("g3_street_west", new Vector3(c0.x - 40, G(c0.x - 40, c0.z) + 6, c0.z), new Vector3(c0.x + 20, G(c0.x + 20, c0.z) + 3, c0.z));
                Shot("g4_backyards", new Vector3(c0.x + 10, G(c0.x + 10, c0.z - 40) + 14, c0.z - 62), new Vector3(c0.x + 10, G(c0.x + 10, c0.z - 20) + 2, c0.z - 25));
                Shot("g5_hillside", new Vector3(c0.x - 30, G(c0.x - 30, c0.z + 60) + 22, c0.z + 25), new Vector3(c0.x + 5, G(c0.x + 5, c0.z + 60) + 4, c0.z + 60));
                Shot("g6_top_close", new Vector3(c0.x + 20, 400, c0.z + 25), new Vector3(c0.x + 20, 0, c0.z + 25), true, 32f);
                Debug.Log("MINI182GROUND_RENDER_DONE");
                if (Application.isBatchMode) EditorApplication.Exit(onRoad + inBld + floating == 0 ? 0 : 2);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
