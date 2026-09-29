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
    /// MINI-182 stage 3: restyle Lalay main-road houses with the approved kit (in place, in the isolated copy scene
    /// GrandBayProof_HouseEnhance.unity). Originals are DISABLED (never deleted). A house is only replaced if the kit
    /// footprint (with balcony/canopy allowance) overlaps no other building, no other kit house and no road/sidewalk
    /// collider; otherwise it is retried at smaller horizontal scale (never height), else the original is kept.
    /// Idempotent: re-running re-enables the originals first. Live scene is never saved (hash checked).
    /// </summary>
    public static class Mini182LalayPlace
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string Prefabs = "Assets/UpIzUpMini/Art/Environment/Mini182/Prefabs";
        const string RootName = "MINI182_Lalay";
        const string Out = "Logs/Tasks/MINI-182";
        const float XMin = -110f, XMax = 160f, ZMin = -215f, ZMax = -100f;   // Lalay main-road strip (not the bay side)

        struct Obb { public Vector2 c; public Vector2 f; public Vector2 r; public float hf, hb, hr; }

        static string Sha(string p) { using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p); return BitConverter.ToString(s.ComputeHash(f)).Replace("-", ""); }
        static uint Hash(string s) { uint h = 5381; foreach (char ch in s) h = h * 33 + ch; return h; }

        static Vector2[] Corners(Obb o) => new[]
        {
            o.c + o.f * o.hf + o.r * o.hr, o.c + o.f * o.hf - o.r * o.hr, o.c - o.f * o.hb - o.r * o.hr, o.c - o.f * o.hb + o.r * o.hr
        };
        static bool Overlap(Obb a, Obb b, float pad = 0f)
        {
            var ca = Corners(a); var cb = Corners(b);
            foreach (var axis in new[] { a.f, a.r, b.f, b.r })
            {
                float amin = float.MaxValue, amax = float.MinValue, bmin = float.MaxValue, bmax = float.MinValue;
                foreach (var p in ca) { float d = Vector2.Dot(p, axis); amin = Mathf.Min(amin, d); amax = Mathf.Max(amax, d); }
                foreach (var p in cb) { float d = Vector2.Dot(p, axis); bmin = Mathf.Min(bmin, d); bmax = Mathf.Max(bmax, d); }
                if (amax + pad < bmin || bmax + pad < amin) return false;
            }
            return true;
        }
        static Obb FromBounds(Bounds b) => new Obb { c = new Vector2(b.center.x, b.center.z), f = Vector2.up, r = Vector2.right, hf = b.extents.z, hb = b.extents.z, hr = b.extents.x };

        [MenuItem("Up Iz Up Mini/MINI-182/Place Kit Houses Along Lalay")]
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
            var log = new List<string> { "MINI-182 Lalay placement", "live sha before=" + liveHash };

            // undo any previous run + remove the bay-side/test rows
            var oldRoot = GameObject.Find(RootName);
            var previousNames = new HashSet<string>();
            if (oldRoot != null) { foreach (Transform c in oldRoot.transform) { int i = c.name.IndexOf("__for__"); if (i >= 0) previousNames.Add(c.name.Substring(i + 7)); } UnityEngine.Object.DestroyImmediate(oldRoot); }
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray())
            {
                if (t == null) continue;
                if (t.name.StartsWith("MINI182_KitTest")) { log.Add("removed test row " + t.name); UnityEngine.Object.DestroyImmediate(t.gameObject); continue; }
            }
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (previousNames.Contains(t.name) && !t.gameObject.activeSelf) t.gameObject.SetActive(true);
            Physics.SyncTransforms();

            // colliders
            var terrain = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.enabled && c.name.Contains("Terrain")).ToList();
            var roadCols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.enabled && (c.name.StartsWith("Road_") || c.name.StartsWith("ExpansionRoad_") || c.name.StartsWith("ImportTrim_Road") || c.name.Contains("Sidewalk") || c.name.Contains("KerbRamp") || c.name.Contains("BridgeDeck"))).ToList();
            var roadPts = new List<Vector2>();
            foreach (var r in roadCols.OfType<MeshCollider>().Where(c => c.name.StartsWith("Road_") || c.name.StartsWith("ImportTrim_Road")))
            { var me = r.sharedMesh; if (me == null) continue; foreach (var v in me.vertices) { var w = r.transform.TransformPoint(v); roadPts.Add(new Vector2(w.x, w.z)); } }
            log.Add($"terrainColliders={terrain.Count} roadLikeColliders={roadCols.Count} roadPoints={roadPts.Count}");

            // candidates
            var cand = new List<Transform>();
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!(t.name.StartsWith("Lalay_House_") || t.name.StartsWith("Lalay_Home_"))) continue;
                if (t.GetComponentsInChildren<MonoBehaviour>(true).Any(m => m != null && !(m is Unity.AI.Navigation.NavMeshModifier))) continue;
                var rs = t.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.center.x < XMin || b.center.x > XMax || b.center.z < ZMin || b.center.z > ZMax) continue;
                cand.Add(t);
            }
            cand = cand.OrderBy(t => t.position.x).ToList();
            var candSet = new HashSet<Transform>(cand);

            // fixed obstacles: every other building-sized renderer (not candidates, not tiny/huge/flat)
            var obstacles = new List<Obb>(); var obsName = new List<string>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                var b = r.bounds; if (b.size.y < 1.2f || b.size.y > 16f || b.size.x > 25f || b.size.z > 25f) continue;
                var tr = r.transform; bool mine = false; for (var p = tr; p != null; p = p.parent) if (candSet.Contains(p)) { mine = true; break; }
                if (mine) continue;
                bool decor = false; for (var p = tr; p != null; p = p.parent) if (p.name.StartsWith("ApprovedGrass") || p.name.Contains("Grass") || p.name == "world") { decor = true; break; }
                if (decor) continue;
                obstacles.Add(FromBounds(b)); obsName.Add(r.transform.parent != null ? r.transform.parent.name + "/" + r.name : r.name);
            }
            // candidate originals stay obstacles until they are actually replaced (kept/skipped ones remain in the scene)
            var candObb = new Dictionary<Transform, Obb>();
            foreach (var t in cand) { var rs2 = t.GetComponentsInChildren<Renderer>(); var b2 = rs2[0].bounds; foreach (var rr in rs2) b2.Encapsulate(rr.bounds); candObb[t] = FromBounds(b2); }
            log.Add("fixed obstacles=" + obstacles.Count + " candidate originals=" + candObb.Count);

            // prefabs
            var prefabs = Directory.GetFiles(Prefabs, "*.prefab").Select(f => AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'))).Where(p => p != null).ToList();
            var groups = new (string prefix, int weight)[] { ("HouseCorrugatedGable1s_", 30), ("HouseHipRed2s_", 26), ("HouseFlatConcrete2s_", 26), ("HouseShopfront2s_", 10) };
            var byGroup = groups.ToDictionary(g => g.prefix, g => prefabs.Where(p => p.name.StartsWith(g.prefix)).OrderBy(p => p.name).ToList());

            Vector2 Tangent(Vector2 c, out bool ok, out Vector2 toRoad)
            {
                var near = roadPts.Where(p => (p - c).sqrMagnitude < 18f * 18f).ToList();
                ok = near.Count >= 6; toRoad = Vector2.up;
                if (roadPts.Count == 0) { ok = false; return Vector2.right; }
                var nearest = roadPts.OrderBy(p => (p - c).sqrMagnitude).First(); toRoad = (nearest - c).normalized;
                if (!ok) return new Vector2(-toRoad.y, toRoad.x);
                var mean = near.Aggregate(Vector2.zero, (a, p) => a + p) / near.Count;
                float sxx = 0, sxy = 0, syy = 0; foreach (var p in near) { var d = p - mean; sxx += d.x * d.x; sxy += d.x * d.y; syy += d.y * d.y; }
                float ang = .5f * Mathf.Atan2(2 * sxy, sxx - syy); return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            }
            float Ground(float x, float z, out bool ok) { float best = float.NegativeInfinity; foreach (var t in terrain) if (t.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out var h, 900)) best = Mathf.Max(best, h.point.y); ok = !float.IsNegativeInfinity(best); return best; }
            bool OnRoad(Vector2 p)
            {
                foreach (var h in Physics.RaycastAll(new Vector3(p.x, 500, p.y), Vector3.down, 900, ~0, QueryTriggerInteraction.Ignore))
                    if (roadCols.Contains(h.collider)) return true;
                return false;
            }
            bool RoadConflict(Obb o)
            {
                var e = new Obb { c = o.c, f = o.f, r = o.r, hf = o.hf + .3f, hb = o.hb + .3f, hr = o.hr + .3f };
                for (int i = 0; i <= 8; i++) for (int j = 0; j <= 8; j++)
                    {
                        float a = Mathf.Lerp(-e.hb, e.hf, i / 8f), s = Mathf.Lerp(-e.hr, e.hr, j / 8f);
                        if (OnRoad(e.c + e.f * a + e.r * s)) return true;
                    }
                return false;
            }

            var root = new GameObject(RootName); var replacedSet = new HashSet<Transform>();
            var placed = new List<Obb>(); var report = new List<string> { "original,category,variant,scale,yaw,x,y,z,result" };
            int replaced = 0, kept = 0, skipped = 0; var perVariant = new Dictionary<string, int>();
            string prevSideAVariant = "", prevSideBVariant = ""; float lastShopX = -999f;
            foreach (var t in cand)
            {
                var rs = t.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var rr in rs) b.Encapsulate(rr.bounds);
                bool home = t.name.StartsWith("Lalay_Home_"); uint h = Hash(t.name);
                if (!home && (h % 100) >= 45) { kept++; report.Add($"{t.name},approved-art,-,-,-,{b.center.x:F1},-,{b.center.z:F1},kept (style mix)"); continue; }
                // pick group (weighted), avoid repeating the previous house's group on the same side, limit shops
                bool sideA = t.name.Contains("SideA"); string prev = sideA ? prevSideAVariant : prevSideBVariant;
                string pick = null;
                for (int attempt = 0; attempt < 8 && pick == null; attempt++)
                {
                    uint hv = Hash(t.name + "#" + attempt); int roll = (int)(hv % 92), acc = 0; string g = groups[0].prefix;
                    foreach (var gr in groups) { acc += gr.weight; if (roll < acc) { g = gr.prefix; break; } }
                    if (g == prev) continue;
                    if (g == "HouseShopfront2s_" && b.center.x - lastShopX < 30f) continue;
                    pick = g;
                }
                if (pick == null) pick = groups[(int)(h % 3)].prefix;
                var list = byGroup[pick]; var prefab = list[(int)((h / 7) % (uint)list.Count)];
                var c2 = new Vector2(b.center.x, b.center.z);
                var tan = Tangent(c2, out bool okTan, out var toRoad).normalized;
                var f = new Vector2(-tan.y, tan.x); if (Vector2.Dot(f, toRoad) < 0) f = -f;   // front faces the road
                var r = new Vector2(f.y, -f.x);
                var box = prefab.GetComponent<BoxCollider>();
                float fw = 6.2f, fd = 5.2f; var bcs = prefab.GetComponents<BoxCollider>(); if (bcs.Length > 0) { fw = bcs[0].size.x; fd = bcs[0].size.z; }
                Obb chosen = default; float chosenS = 0, chosenZ = 0; Vector2 chosenC = c2; bool ok = false; float ny = 0;
                string why = "";
                foreach (var off in new[] { 0f, 1.2f, -1.2f, 2.4f, -2.4f })
                {
                foreach (var sc in new[] { (1f, 1f), (.94f, .94f), (.88f, .9f), (.8f, .88f), (.72f, .85f), (.65f, .8f) })
                {
                    var c3 = c2 + r * off;
                    float sx = sc.Item1, sz = sc.Item2; float s = sx;
                    var o = new Obb { c = c3, f = f, r = r, hf = fd * sz * .5f + .95f, hb = fd * sz * .5f + .4f, hr = fw * sx * .5f + .35f };
                    
                    bool bad = false;
                    for (int oi = 0; oi < obstacles.Count; oi++) if (Overlap(o, obstacles[oi], .25f)) { bad = true; why = "building:" + obsName[oi]; break; }
                    if (!bad) foreach (var kv in candObb) if (kv.Key != t && !replacedSet.Contains(kv.Key) && Overlap(o, kv.Value, .25f)) { bad = true; why = "neighbour house"; break; }
                    if (!bad) foreach (var pl in placed) if (Overlap(o, pl, .2f)) { bad = true; why = "kit neighbour"; break; }
                    if (!bad && RoadConflict(o)) { bad = true; why = "road/sidewalk"; }
                    if (bad) continue;
                    float g0 = float.MaxValue; bool gok = true;
                    foreach (var dx in new[] { -.5f, .5f }) foreach (var dz in new[] { -.5f, .5f })
                        { var p = c3 + r * (dx * fw * sx) + f * (dz * fd * sz); float gy = Ground(p.x, p.y, out bool okg); if (!okg) gok = false; else g0 = Mathf.Min(g0, gy); }
                    float gc = Ground(c3.x, c3.y, out bool okc); if (!okc || !gok) continue;
                    ny = Mathf.Min(g0, gc) + .02f; chosen = o; chosenS = sx; chosenZ = sz; chosenC = c3; ok = true; break;
                }
                if (ok) break;
                }
                if (!ok) { skipped++; report.Add($"{t.name},{(home ? "procedural" : "approved-art")},{prefab.name},-,-,{b.center.x:F1},-,{b.center.z:F1},SKIPPED ({why}) - original kept"); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                inst.name = prefab.name + "__for__" + t.name;
                float yaw = Quaternion.LookRotation(new Vector3(-f.x, 0, -f.y), Vector3.up).eulerAngles.y;
                inst.transform.SetPositionAndRotation(new Vector3(chosenC.x, ny, chosenC.y), Quaternion.Euler(0, yaw, 0));
                inst.transform.localScale = new Vector3(chosenS, 1f, chosenZ);
                t.gameObject.SetActive(false); replacedSet.Add(t);
                placed.Add(chosen); replaced++; perVariant[pick] = perVariant.TryGetValue(pick, out var n) ? n + 1 : 1;
                if (sideA) prevSideAVariant = pick; else prevSideBVariant = pick; if (pick == "HouseShopfront2s_") lastShopX = b.center.x;
                report.Add($"{t.name},{(home ? "procedural" : "approved-art")},{prefab.name},{chosenS:F2}x{chosenZ:F2},{yaw:F0},{chosenC.x:F1},{ny:F1},{chosenC.y:F1},replaced");
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            log.Add($"candidates={cand.Count} replaced={replaced} keptOriginalStyle={kept} skippedNoRoom={skipped}");
            foreach (var kv in perVariant) log.Add($"  {kv.Key}: {kv.Value}");
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            log.Add("live sha after=" + Sha(Live)); log.Add("copy in build settings=" + EditorBuildSettings.scenes.Any(s => s.enabled && s.path == Copy));
            File.WriteAllLines(Out + "/LalayPlacement-Report.txt", log); File.WriteAllLines(Out + "/HouseAssignments.csv", report);
            Debug.Log("MINI182LALAY_APPLY " + string.Join(" | ", log));
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Validate And Render Lalay")]
        public static void ValidateAndRender()
        {
            try
            {
                var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single); Physics.SyncTransforms();
                var root = GameObject.Find(RootName); if (root == null) throw new Exception("no placement root");
                var kit = root.GetComponentsInChildren<Transform>().Where(t => t.parent == root.transform).ToList();
                var roadCols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.enabled && (c.name.StartsWith("Road_") || c.name.StartsWith("ExpansionRoad_") || c.name.StartsWith("ImportTrim_Road") || c.name.Contains("Sidewalk") || c.name.Contains("KerbRamp") || c.name.Contains("BridgeDeck"))).ToList();
                var lines = new List<string> { "MINI-182 Lalay validation (fresh reload)" };
                // 1) kit house vs every other active renderer bounds (that is not part of the same kit house), 2) kit vs kit, 3) roads
                int overlapsOther = 0, overlapsKit = 0, onRoad = 0;
                var otherR = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && !r.transform.IsChildOf(root.transform) && r.bounds.size.y > 1.2f && r.bounds.size.y < 16f && r.bounds.size.x < 25f && r.bounds.size.z < 25f).ToList();
                string PathName(Transform t) { var n = t.name; for (var p = t.parent; p != null && n.Length < 90; p = p.parent) n = p.name + "/" + n; return n; }
                var others = new List<Bounds>(); var otherNames = new List<string>();
                foreach (var r in otherR) { string pn = PathName(r.transform); if (pn.Contains("Grass") || pn.Contains("Sidewalk") || pn.Contains("Road") || pn.Contains("Kerb") || pn.Contains("Frontage") || pn.Contains("Terrain") || pn.Contains("Water") || pn.StartsWith("world")) continue; others.Add(r.bounds); otherNames.Add(pn); }
                                Obb KitObb(Transform k)
                {
                    var bc = k.GetComponents<BoxCollider>().First(); var wc = k.TransformPoint(bc.center); var fv = -k.forward; var f2 = new Vector2(fv.x, fv.z).normalized; var r2 = new Vector2(f2.y, -f2.x);
                    return new Obb { c = new Vector2(wc.x, wc.z), f = f2, r = r2, hf = bc.size.z * k.lossyScale.z * .5f + .5f, hb = bc.size.z * k.lossyScale.z * .5f + .2f, hr = bc.size.x * k.lossyScale.x * .5f + .25f };
                }
                var kobb = kit.Select(KitObb).ToList();
                for (int i = 0; i < kit.Count; i++)
                {
                    for (int oi = 0; oi < others.Count; oi++)
                    {
                        if (otherNames[oi].Contains("NPC_")) continue;
                        if (Overlap(kobb[i], FromBounds(others[oi]), -.05f)) { overlapsOther++; lines.Add("  overlaps: " + kit[i].name + " with " + otherNames[oi] + " box size " + others[oi].size); break; }
                    }
                    for (int j = i + 1; j < kit.Count; j++) if (Overlap(kobb[i], kobb[j], -.05f)) { overlapsKit++; lines.Add("  kit-kit overlap: " + kit[i].name + " / " + kit[j].name); }
                    var col = kit[i].GetComponents<BoxCollider>().First();
                    for (int a = 0; a <= 6; a++) for (int c = 0; c <= 6; c++)
                        {
                            var p = kit[i].TransformPoint(new Vector3(Mathf.Lerp(-col.size.x / 2, col.size.x / 2, a / 6f), 0, Mathf.Lerp(-col.size.z / 2 - 1.2f, col.size.z / 2, c / 6f)));
                            if (Physics.RaycastAll(new Vector3(p.x, 500, p.z), Vector3.down, 900, ~0, QueryTriggerInteraction.Ignore).Any(h => roadCols.Contains(h.collider))) { onRoad++; lines.Add("  road hit under footprint of " + kit[i].name); a = 99; break; }
                        }
                }
                lines.Add($"kit houses={kit.Count} overlapsWithOtherBuildings={overlapsOther} kitKitOverlaps={overlapsKit} roadHits={onRoad}");
                File.WriteAllLines(Out + "/LalayValidation.txt", lines);
                foreach (var l in lines) Debug.Log("MINI182LALAY_VALIDATE " + l);

                var go = new GameObject("PlaceCam"); var cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f); cam.farClipPlane = 2000; cam.fieldOfView = 55;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white * .85f;
                var rt = new RenderTexture(1600, 1000, 24); cam.targetTexture = rt;
                void Shot(string n, Vector3 pos, Vector3 look, bool ortho = false, float size = 30)
                {
                    cam.orthographic = ortho; cam.orthographicSize = size; cam.transform.position = pos; cam.transform.LookAt(look, ortho ? Vector3.forward : Vector3.up); cam.Render();
                    var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply();
                    File.WriteAllBytes(Out + "/Renders/" + n + ".png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
                }
                float G(float x, float z) { return Physics.Raycast(new Vector3(x, 500, z), Vector3.down, out var h, 900) ? h.point.y : 0f; }
                var c0 = new Vector3(48, 0, -151);   // same centre/cameras as the stage 1 audit renders (a1, a3, a4) for before/after
                Shot("p1_lalay_top", new Vector3(c0.x, 400, c0.z), c0, true, 73.5f);
                Shot("p2_lalay_top_close", new Vector3(c0.x, 400, c0.z), c0, true, 40f);
                Shot("p3_lalay_oblique", new Vector3(c0.x - 5, G(c0.x, c0.z) + 55, c0.z - 60), new Vector3(c0.x + 10, G(c0.x + 10, c0.z), c0.z + 5));
                Shot("p4_lalay_street", new Vector3(c0.x - 40, G(c0.x - 40, c0.z) + 6, c0.z), new Vector3(c0.x + 20, G(c0.x + 20, c0.z) + 3, c0.z));
                Shot("p5_street_east", new Vector3(c0.x + 40, G(c0.x + 40, c0.z) + 6, c0.z), new Vector3(c0.x - 20, G(c0.x - 20, c0.z) + 3, c0.z));
                Shot("p6_street_mid", new Vector3(c0.x, G(c0.x, c0.z) + 5, c0.z + 8), new Vector3(c0.x + 30, G(c0.x + 30, c0.z) + 3, c0.z - 4));
                Debug.Log("MINI182LALAY_RENDER_DONE");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
