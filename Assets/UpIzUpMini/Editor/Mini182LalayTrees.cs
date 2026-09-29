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
    /// MINI-182 stage 4 step 2: place the approved tree kit (coconut palm, broadleaf, banana clump) around Lalay in the
    /// isolated copy scene GrandBayProof_HouseEnhance.unity. Deterministic (seed 182), idempotent (root MINI182_LalayTrees is rebuilt),
    /// never touches the live scene (hash checked). A tree is only placed if its trunk is clear of every building, road/sidewalk,
    /// stream, other tree and steep ground, with per-type clearances.
    /// </summary>
    public static class Mini182LalayTrees
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string Prefabs = "Assets/UpIzUpMini/Art/Environment/Mini182/Prefabs/Scenery";
        const string RootName = "MINI182_LalayTrees";
        const string Out = "Logs/Tasks/MINI-182";
        const float XMin = -110f, XMax = 160f, ZMin = -215f, ZMax = -95f;
        const int Cap = 170;

        class Kind { public string name; public string prefix; public float bldMargin, roadR, spacing, scaleLo, scaleHi; }
        static readonly Kind Palm = new Kind { name = "palm", prefix = "SceneryCoconutPalm_", bldMargin = 2.0f, roadR = 2.2f, spacing = 4.5f, scaleLo = .85f, scaleHi = 1.15f };
        static readonly Kind Broad = new Kind { name = "broadleaf", prefix = "SceneryBroadleafTree_", bldMargin = 3.2f, roadR = 6.0f, spacing = 6.0f, scaleLo = .8f, scaleHi = 1.2f };
        static readonly Kind Banana = new Kind { name = "banana", prefix = "SceneryBananaClump_", bldMargin = 1.3f, roadR = 2.5f, spacing = 2.8f, scaleLo = .9f, scaleHi = 1.3f };

        static string Sha(string p) { using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p); return BitConverter.ToString(s.ComputeHash(f)).Replace("-", ""); }

        static float DistToBox(Vector2 p, Bounds b)
        {
            float dx = Mathf.Max(b.min.x - p.x, 0f, p.x - b.max.x), dz = Mathf.Max(b.min.z - p.y, 0f, p.y - b.max.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        class Ctx
        {
            public List<Bounds> buildings = new List<Bounds>();
            public List<Collider> roads = new List<Collider>();
            public List<Vector2> roadPts = new List<Vector2>();
            public List<Vector2> streamPts = new List<Vector2>();
            public List<MeshCollider> terrain = new List<MeshCollider>();
            public List<(Vector2 p, float r)> trees = new List<(Vector2, float)>();
        }

        static Ctx Build(bool excludeTrees)
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
                string n = mf.name; bool water = n.Contains("Waterway") || n.Contains("Stream") || n.Contains("River");
                if (!water) continue;
                foreach (var v in mf.sharedMesh.vertices) { var w = mf.transform.TransformPoint(v); c.streamPts.Add(new Vector2(w.x, w.z)); }
            }
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var b = r.bounds; if (b.size.y < .9f || b.size.y > 18f || b.size.x > 25f || b.size.z > 25f) continue;
                bool skip = false; for (var p = r.transform; p != null; p = p.parent) { string pn = p.name; if (pn.StartsWith("ApprovedGrass") || pn.Contains("Grass") || pn.StartsWith(RootName) || pn.StartsWith("NPC_") || pn.Contains("Waterway")) { skip = true; break; } }
                if (skip) continue;
                if (excludeTrees && r.transform.name.StartsWith("Tree_")) continue;
                c.buildings.Add(b);
            }
            return c;
        }

        static float Ground(Ctx c, float x, float z, out bool ok)
        {
            float best = float.NegativeInfinity;
            foreach (var t in c.terrain) if (t.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out var h, 900)) best = Mathf.Max(best, h.point.y);
            ok = !float.IsNegativeInfinity(best); return best;
        }

        static bool OnRoad(Ctx c, Vector2 p)
        {
            foreach (var h in Physics.RaycastAll(new Vector3(p.x, 500, p.y), Vector3.down, 900, ~0, QueryTriggerInteraction.Ignore)) if (c.roads.Contains(h.collider)) return true;
            return false;
        }

        static string Check(Ctx c, Kind k, Vector2 p, out float gy)
        {
            gy = 0;
            if (p.x < XMin - 15 || p.x > XMax + 5 || p.y < ZMin - 10 || p.y > ZMax + 40) return "outside area";
            float g0 = Ground(c, p.x, p.y, out bool ok); if (!ok) return "no ground";
            float slope = 0; foreach (var d in new[] { new Vector2(1.5f, 0), new Vector2(-1.5f, 0), new Vector2(0, 1.5f), new Vector2(0, -1.5f) }) { float g1 = Ground(c, p.x + d.x, p.y + d.y, out bool o2); if (!o2) return "no ground"; slope = Mathf.Max(slope, Mathf.Abs(g1 - g0) / 1.5f); }
            if (slope > .75f) return "too steep"; gy = g0;
            foreach (var b in c.buildings) if (DistToBox(p, b) < k.bldMargin) return "building";
            if (OnRoad(c, p)) return "road";
            for (int a = 0; a < 8; a++) { float ang = a * Mathf.PI / 4; if (OnRoad(c, p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * k.roadR)) return "near road"; }
            foreach (var s in c.streamPts) if ((s - p).sqrMagnitude < 2.6f * 2.6f) return "stream";
            foreach (var t in c.trees) if ((t.p - p).magnitude < Mathf.Max(k.spacing, t.r)) return "tree spacing";
            return null;
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Place Trees Around Lalay")]
        public static void Apply()
        {
            try { ApplyInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void ApplyInner()
        {
            Directory.CreateDirectory(Out + "/Renders");
            string liveHash = Sha(Live);
            var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            var old = GameObject.Find(RootName); if (old != null) UnityEngine.Object.DestroyImmediate(old);
            Physics.SyncTransforms();
            var log = new List<string> { "MINI-182 Lalay trees", "live sha before=" + liveHash };
            var ctx = Build(false);
            log.Add($"buildings={ctx.buildings.Count} roads={ctx.roads.Count} roadPts={ctx.roadPts.Count} streamPts={ctx.streamPts.Count}");
            var pf = new Dictionary<string, List<GameObject>>();
            foreach (var k in new[] { Palm, Broad, Banana }) pf[k.name] = Directory.GetFiles(Prefabs, k.prefix + "*.prefab").OrderBy(f => f).Select(f => AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'))).ToList();
            var rng = new System.Random(182);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var root = new GameObject(RootName);
            var counts = new Dictionary<string, int>(); var rejects = new Dictionary<string, int>(); var rows = new List<string> { "kind,prefab,x,y,z,scale,yaw,source" };
            int placed = 0;
            bool TryPlace(Kind k, Vector2 p, string source)
            {
                if (placed >= Cap) return false;
                string why = Check(ctx, k, p, out float gy);
                if (why != null) { rejects[why] = rejects.TryGetValue(why, out var n) ? n + 1 : 1; return false; }
                var list = pf[k.name]; var prefab = list[rng.Next(list.Count)];
                float sc = R(k.scaleLo, k.scaleHi), yaw = R(0, 360);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                inst.transform.SetPositionAndRotation(new Vector3(p.x, gy - .05f, p.y), Quaternion.Euler(0, yaw, 0)); inst.transform.localScale = Vector3.one * sc;
                float crown = k == Palm ? 5f : k == Broad ? 4.2f : 1.6f; ctx.trees.Add((p, k.spacing));
                counts[k.name] = counts.TryGetValue(k.name, out var m) ? m + 1 : 1; placed++;
                rows.Add($"{k.name},{prefab.name},{p.x:F1},{gy:F1},{p.y:F1},{sc:F2},{yaw:F0},{source}"); return true;
            }

            // houses in the strip (active only; kit houses and remaining originals)
            var houses = new List<Bounds>();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                bool kit = t.parent != null && t.parent.name == "MINI182_Lalay"; bool orig = t.name.StartsWith("Lalay_House_") || t.name.StartsWith("Lalay_Home_");
                if (!(kit || orig) || !t.gameObject.activeInHierarchy) continue;
                var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var rr in rs) b.Encapsulate(rr.bounds);
                if (b.center.x < XMin || b.center.x > XMax || b.center.z < ZMin || b.center.z > ZMax) continue;
                houses.Add(b);
            }
            log.Add("houses in strip=" + houses.Count);
            Vector2 ToRoad(Vector2 c2) { Vector2 best = Vector2.up; float d = float.MaxValue; foreach (var q in ctx.roadPts) { float dd = (q - c2).sqrMagnitude; if (dd < d) { d = dd; best = q - c2; } } return best.normalized; }

            // a) back yards: banana clumps, palms and the odd broadleaf behind and beside houses
            foreach (var h in houses.OrderBy(b => b.center.x))
            {
                var c2 = new Vector2(h.center.x, h.center.z); var f = ToRoad(c2); var r = new Vector2(f.y, -f.x);
                float depth = Mathf.Max(h.extents.x, h.extents.z), width = Mathf.Min(h.extents.x, h.extents.z);
                for (int i = 0; i < 2; i++)
                {
                    float roll = R(0, 1); var k = roll < .5f ? Banana : roll < .78f ? Palm : Broad;
                    if (R(0, 1) > (i == 0 ? .75f : .35f)) continue;
                    for (int attempt = 0; attempt < 5; attempt++)
                    { var p = c2 - f * (depth + R(1.5f, 6f)) + r * R(-width - 2.5f, width + 2.5f); if (TryPlace(k, p, "backyard")) break; }
                }
                // gap palm between houses along the frontage
                if (R(0, 1) < .22f) { float side = R(0, 1) < .5f ? -1 : 1; TryPlace(Palm, c2 + r * side * (width + R(2.2f, 4f)) + f * R(-1f, 1f), "gap"); }
            }
            // b) stream banks
            for (int i = 0; i < ctx.streamPts.Count; i += 9)
            {
                var s = ctx.streamPts[i]; if (s.x < XMin || s.x > XMax || s.y < ZMin - 10 || s.y > ZMax + 40) continue;
                if (R(0, 1) < .45f) TryPlace(R(0, 1) < .5f ? Banana : Palm, s + new Vector2(R(-1f, 1f), R(-1f, 1f)).normalized * R(3f, 5f), "stream");
            }
            // c) hillside clusters: broadleaf mostly, thinning away from the houses
            for (int attempt = 0; attempt < 900 && placed < Cap; attempt++)
            {
                var p = new Vector2(R(XMin, XMax), R(ZMin - 6, ZMax + 40));
                var k = R(0, 1) < .72f ? Broad : (R(0, 1) < .5f ? Palm : Banana);
                TryPlace(k, p, "hillside");
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            log.Add($"trees placed={placed} (cap {Cap})"); foreach (var kv in counts) log.Add($"  {kv.Key}: {kv.Value}");
            log.Add("rejected by: " + string.Join(", ", rejects.OrderByDescending(k => k.Value).Select(k => k.Key + "=" + k.Value)));
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            log.Add("live sha after=" + Sha(Live)); log.Add("copy in build settings=" + EditorBuildSettings.scenes.Any(s => s.enabled && s.path == Copy));
            File.WriteAllLines(Out + "/LalayTrees-Report.txt", log); File.WriteAllLines(Out + "/TreeAssignments.csv", rows);
            Debug.Log("MINI182TREES_APPLY " + string.Join(" | ", log));
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Validate And Render Lalay Trees")]
        public static void ValidateAndRender()
        {
            try
            {
                EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single); Physics.SyncTransforms();
                var root = GameObject.Find(RootName); if (root == null) throw new Exception("no tree root");
                var ctx = Build(true);
                var trees = root.GetComponentsInChildren<Transform>().Where(t => t.parent == root.transform).ToList();
                var lines = new List<string> { "MINI-182 Lalay trees validation (fresh reload)" }; int bad = 0;
                var placedSoFar = new List<(Vector2 p, float r)>();
                foreach (var t in trees)
                {
                    var k = t.name.StartsWith("SceneryCoconut") ? Palm : t.name.StartsWith("SceneryBroadleaf") ? Broad : Banana;
                    var p = new Vector2(t.position.x, t.position.z); ctx.trees = placedSoFar.ToList();
                    string why = Check(ctx, k, p, out _); if (why != null && why != "outside area") { bad++; lines.Add($"  {t.name} at {p}: {why}"); }
                    placedSoFar.Add((p, k.spacing));
                }
                lines.Add($"trees={trees.Count} violations={bad}");
                var byKind = trees.GroupBy(t => t.name.Split('_')[0]).Select(g => g.Key + "=" + g.Count());
                lines.Add("by type: " + string.Join(", ", byKind));
                File.WriteAllLines(Out + "/LalayTreesValidation.txt", lines); foreach (var l in lines) Debug.Log("MINI182TREES_VALIDATE " + l);

                var go = new GameObject("TreeCam"); var cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f); cam.farClipPlane = 2000; cam.fieldOfView = 55;
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
                Shot("s1_lalay_top", new Vector3(c0.x, 400, c0.z), c0, true, 73.5f);
                Shot("s2_lalay_oblique", new Vector3(c0.x - 5, G(c0.x, c0.z) + 55, c0.z - 60), new Vector3(c0.x + 10, G(c0.x + 10, c0.z), c0.z + 5));
                Shot("s3_street_west", new Vector3(c0.x - 40, G(c0.x - 40, c0.z) + 6, c0.z), new Vector3(c0.x + 20, G(c0.x + 20, c0.z) + 3, c0.z));
                Shot("s4_street_east", new Vector3(c0.x + 40, G(c0.x + 40, c0.z) + 6, c0.z), new Vector3(c0.x - 20, G(c0.x - 20, c0.z) + 3, c0.z));
                Shot("s5_backyards", new Vector3(c0.x + 10, G(c0.x + 10, c0.z - 40) + 14, c0.z - 62), new Vector3(c0.x + 10, G(c0.x + 10, c0.z - 20) + 2, c0.z - 25));
                Shot("s6_hillside", new Vector3(c0.x - 30, G(c0.x - 30, c0.z + 60) + 22, c0.z + 25), new Vector3(c0.x + 5, G(c0.x + 5, c0.z + 60) + 4, c0.z + 60));
                Debug.Log("MINI182TREES_RENDER_DONE");
                if (Application.isBatchMode) EditorApplication.Exit(bad == 0 ? 0 : 2);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
