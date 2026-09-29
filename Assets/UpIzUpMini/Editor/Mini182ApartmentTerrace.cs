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
    /// MINI-182 stage 6c: apartment terrace placed where the user marked it (red outline drawn on the g1_cx_wide render: the hillside beside the
    /// school road, on the side toward the Geneva field). The outline is un-projected through the SAME fixed camera that made that render onto the
    /// terrain collider, so the polygon is in world coordinates. About 15 apartment units are laid out from the "Housing apartments" photos:
    /// long terraced buildings of 4 attached units (walls in pink / blue / cream / peach panels, maroon roofs, solar heaters), two upper, then one of
    /// 4 and one of 3 lower down, all parallel to the contour and facing downhill, with a white post-and-rail fence. Nothing is placed on a road.
    /// Replaces the earlier 4-block complex (the moved classroom building stays). Copy scene only; live scene hash checked; idempotent.
    /// </summary>
    public static class Mini182ApartmentTerrace
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini182";
        const string Export = "Logs/Tasks/MINI-182/Export";
        const string Out = "Logs/Tasks/MINI-182";
        const string RootName = "MINI182_ApartmentTerrace";
        const float UnitW = 7.6f, Depth = 6.4f;

        // the red outline, in pixels of the user's 2000 x 1295 image of Renders/g1_cx_wide.png
        static readonly Vector2[] Outline = { new Vector2(290, 440), new Vector2(480, 400), new Vector2(560, 450), new Vector2(700, 510), new Vector2(890, 545), new Vector2(905, 600), new Vector2(800, 640), new Vector2(600, 690), new Vector2(520, 690), new Vector2(430, 600), new Vector2(340, 545), new Vector2(260, 480) };

        static string Sha(string p) { using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p); return BitConverter.ToString(s.ComputeHash(f)).Replace("-", ""); }

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

        [MenuItem("Up Iz Up Mini/MINI-182/Build Apartment Terrace In Marked Area")]
        public static void Apply()
        {
            try { ApplyInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static bool Inside(Vector2 p, List<Vector2> poly)
        {
            bool c = false; for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++) if ((poly[i].y > p.y) != (poly[j].y > p.y) && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x) c = !c; return c;
        }

        static void ApplyInner()
        {
            string liveHash = Sha(Live); var log = new List<string> { "MINI-182 apartment terrace (marked area)", "live sha before=" + liveHash };
            var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            var old = GameObject.Find(RootName); if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var cell = new Dictionary<string, int>(); foreach (var l in File.ReadAllLines(Export + "/coast_cells.txt")) { var s = l.Split(' '); cell[s[0]] = int.Parse(s[1]); }
            Physics.SyncTransforms();
            var exp = GameObject.Find("MINI168_Expansion").transform; var terrain = exp.Find("ExpansionTerrain").GetComponent<MeshCollider>();
            float G(float x, float z, out bool ok) { RaycastHit h; ok = terrain.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out h, 900); return ok ? h.point.y : 0f; }

            // 1. un-project the red outline through the fixed camera of Renders/g1_cx_wide.png
            var cgo = new GameObject("UnprojCam"); var cam = cgo.AddComponent<Camera>(); cam.fieldOfView = 55; cam.aspect = 1.6f; cam.transform.position = new Vector3(60f, 70f, 130f); cam.transform.LookAt(new Vector3(120f, 15f, 55f), Vector3.up);
            var poly = new List<Vector2>();
            foreach (var px in Outline)
            {
                var ray = cam.ViewportPointToRay(new Vector3(px.x / 2000f, 1f - px.y / 1295f, 0));
                if (terrain.Raycast(ray, out var hit, 2000f)) poly.Add(new Vector2(hit.point.x, hit.point.z)); else throw new Exception("outline point missed the terrain: " + px);
            }
            UnityEngine.Object.DestroyImmediate(cgo);
            var cen = poly.Aggregate(Vector2.zero, (a, p) => a + p) / poly.Count; float minx = poly.Min(p => p.x), maxx = poly.Max(p => p.x), minz = poly.Min(p => p.y), maxz = poly.Max(p => p.y);
            log.Add($"marked area in world coordinates: x {minx:F0}..{maxx:F0}, z {minz:F0}..{maxz:F0}, centre ({cen.x:F0},{cen.y:F0}); points: " + string.Join(" ", poly.Select(p => $"({p.x:F0},{p.y:F0})")));

            // 2. clear the previous complex (keep the classroom building) and any block inside the marked area
            var oldCx = GameObject.Find("MINI182_ApartmentComplex");
            if (oldCx != null) foreach (Transform c in oldCx.transform.Cast<Transform>().ToList()) if (c.name.StartsWith("Apartment_") || c.name == "ComplexWalls" || c.name == "WallColliders" || c.name == "Plinths") UnityEngine.Object.DestroyImmediate(c.gameObject);
            var expanded = new List<Vector2>(); foreach (var p in poly) expanded.Add(cen + (p - cen) * 1.35f);
            int hid = 0; var housing = GameObject.Find("MINI182_ExpansionHousing");
            if (housing != null) foreach (Transform c in housing.transform) if (c.gameObject.activeSelf && Inside(new Vector2(c.position.x, c.position.z), expanded)) { c.gameObject.SetActive(false); hid++; }
            log.Add($"previous complex blocks removed; scattered apartment blocks hidden inside the area: {hid}");

            // 3. terrain analysis: contour direction from the average slope
            Vector2 grad = Vector2.zero; int n = 0; var hs = new List<float>();
            for (float x = minx; x <= maxx; x += 3f) for (float z = minz; z <= maxz; z += 3f) { var p = new Vector2(x, z); if (!Inside(p, poly)) continue; float g0 = G(x, z, out bool ok); if (!ok) continue; float gx = (G(x + 3, z, out bool o1) - G(x - 3, z, out bool o2)) / 6f, gz = (G(x, z + 3, out bool o3) - G(x, z - 3, out bool o4)) / 6f; if (o1 && o2 && o3 && o4) { grad += new Vector2(gx, gz); n++; hs.Add(g0); } }
            var down = (-grad).normalized; if (down.sqrMagnitude < .01f) down = Vector2.up; float hmin = hs.Min(), hmax = hs.Max(), hmed = hs.OrderBy(h => h).ElementAt(hs.Count / 2);
            log.Add($"terrain in the area: {hmin:F1}..{hmax:F1} m (median {hmed:F1}); mean slope {grad.magnitude / Mathf.Max(1, n):F2}; downhill dir ({down.x:F2},{down.y:F2})");
            var along = new Vector2(-down.y, down.x);   // contour direction = long axis of the buildings
            float faceYaw = Mathf.Atan2(down.x, down.y) * Mathf.Rad2Deg;   // fronts face downhill

            // 4. obstacles
            var roadCols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(m => m.enabled && (m.name.StartsWith("ExpansionRoad_") || m.name.StartsWith("Road_") || m.name.StartsWith("ImportTrim_Road") || m.name.Contains("Sidewalk"))).ToList();
            bool OnRoad(Vector2 p) => Physics.RaycastAll(new Vector3(p.x, 500, p.y), Vector3.down, 900, ~0, QueryTriggerInteraction.Ignore).Any(h => roadCols.Contains(h.collider));
            var others = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && r.gameObject.activeInHierarchy && r.bounds.size.y > 1.2f && r.bounds.size.y < 16f && r.bounds.size.x < 30f && r.bounds.size.z < 30f && r.transform.root.name != RootName && r.transform.root.name != "MINI182_Lalay" && !r.transform.IsChildOf(exp.Find("GenevaField"))).Select(r => r.bounds).ToList();
            Vector2 Ax(float s, float l) => along * l + down * s;   // l along the building, s toward its front (downhill)
            bool Fits(Vector2 c, int units, out float spread)
            {
                spread = 0; float hl = units * UnitW / 2f + 1f, hd = Depth / 2f + 2.5f; float gmin = float.MaxValue, gmax = float.MinValue; int inside = 0, tot = 0;
                for (int i = 0; i <= units * 3; i++) for (int j = 0; j <= 4; j++)
                {
                    var p = c + Ax(Mathf.Lerp(-hd, hd + 2f, j / 4f), Mathf.Lerp(-hl, hl, i / (float)(units * 3)));
                    float g = G(p.x, p.y, out bool ok); if (!ok) return false; if (OnRoad(p)) return false;
                    foreach (var o in others) if (p.x > o.min.x - 1f && p.x < o.max.x + 1f && p.y > o.min.z - 1f && p.y < o.max.z + 1f) return false;
                    gmin = Mathf.Min(gmin, g); gmax = Mathf.Max(gmax, g); tot++; if (Inside(p, expanded)) inside++;
                }
                foreach (var e in new[] { Ax(hd + 4f, 0), Ax(-hd - 3f, 0) }) { if (OnRoad(c + e)) return false; }
                spread = gmax - gmin; return inside >= tot * .62f && spread <= 5.5f;
            }
            var placedC = new List<(Vector2 c, int units)>(); bool Clear(Vector2 c, int units) { foreach (var (pc, pu) in placedC) { var d = c - pc; float dl = Mathf.Abs(Vector2.Dot(d, along)), ds = Mathf.Abs(Vector2.Dot(d, down)); if (dl < (units + pu) * UnitW / 2f + 3f && ds < Depth + 6f) return false; } return true; }
            // slots: two upper buildings of 4 units, then one of 4 and one of 3 lower down
            var slots = new (int units, bool upper)[] { (4, true), (4, true), (4, false), (3, false) };
            var chosen = new List<(Vector2 c, int units)>();
            foreach (var slot in slots)
            {
                Vector2 best = Vector2.zero; float bestScore = float.MinValue; bool ok = false;
                for (int pass = 0; pass < 2 && !ok; pass++) { bool strict = pass == 0;
                for (float x = minx - 8; x <= maxx + 8; x += 2f) for (float z = minz - 8; z <= maxz + 8; z += 2f)
                {
                    var c = new Vector2(x, z); if (!Inside(c, expanded) || !Clear(c, slot.units)) continue; float gc = G(x, z, out bool okg); if (!okg) continue;
                    if (strict && slot.upper != (gc >= hmed)) continue; if (!Fits(c, slot.units, out float spread)) continue;
                    float score = (slot.upper ? gc : -gc) - spread * .3f - Vector2.Distance(c, cen) * .05f; if (score > bestScore) { bestScore = score; best = c; ok = true; }
                } }
                if (!ok) { log.Add($"WARN: no site for a {slot.units}-unit {(slot.upper ? "upper" : "lower")} building"); continue; }
                placedC.Add((best, slot.units)); chosen.Add((best, slot.units));
            }
            // 5. build
            var root = new GameObject(RootName); var pf = Directory.GetFiles(Art + "/Prefabs", "HouseApartmentHip2s_*Maroon.prefab").OrderBy(f => f).Select(f => AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'))).ToList();
            var order = new[] { "Pink_Maroon", "Blue_Maroon", "Cream_Maroon", "Peach_Maroon" }; var pm = new MB(); var fence = new MB(); int units = 0; float maxDrop = 0;
            var pfDict = pf.ToDictionary(p => p.name.Replace("HouseApartmentHip2s_", ""), p => p);
            for (int b = 0; b < chosen.Count; b++)
            {
                var (c, nu) = chosen[b]; var grp = new GameObject($"Building_{b}_{nu}units"); grp.transform.SetParent(root.transform, false);
                for (int u = 0; u < nu; u++)
                {
                    float l = (u - (nu - 1) / 2f) * UnitW; var uc = c + along * l;
                    float gmin = float.MaxValue; var q = Quaternion.Euler(0, faceYaw, 0);
                    for (int i = 0; i <= 4; i++) for (int j = 0; j <= 4; j++) { var p = uc + new Vector2((q * Vector3.right).x, (q * Vector3.right).z) * Mathf.Lerp(-UnitW / 2, UnitW / 2, i / 4f) + new Vector2((q * Vector3.forward).x, (q * Vector3.forward).z) * Mathf.Lerp(-Depth / 2, Depth / 2, j / 4f); float g = G(p.x, p.y, out bool ok); if (ok) gmin = Mathf.Min(gmin, g); }
                    float gcen = G(uc.x, uc.y, out _); float baseY = Mathf.Max(gmin, gcen - 1.1f);   // terraced: base sits on the mid ground so the uphill side is dug in and a plinth fills the downhill side
                    var prefab = pfDict[order[(b * 2 + u) % order.Length]];
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grp.transform); inst.name = $"Unit_{u}_{prefab.name.Replace("HouseApartmentHip2s_", "")}";
                    inst.transform.SetPositionAndRotation(new Vector3(uc.x, baseY, uc.y), Quaternion.Euler(0, faceYaw, 0)); units++;
                    float drop = baseY - gmin; if (drop > 0.05f) { maxDrop = Mathf.Max(maxDrop, drop); pm.Box(new Vector3(uc.x, (baseY + .08f + gmin - .2f) / 2, uc.y), new Vector3(UnitW / 2 + .05f, (baseY + .08f - gmin + .2f) / 2, Depth / 2 + .05f), 3, faceYaw); }
                }
                // white post-and-rail fence along the front, with a 4 m gap in the middle for the steps
                float lenB = nu * UnitW; float sF = Depth / 2 + 2.2f;
                for (float l = -lenB / 2 - 1f; l <= lenB / 2 + 1f; l += 2.4f)
                {
                    if (Mathf.Abs(l) < 2.0f) continue; var p = c + Ax(sF, l); float g = G(p.x, p.y, out bool ok); if (!ok || OnRoad(p)) continue;
                    fence.Box(new Vector3(p.x, g + .5f, p.y), new Vector3(.05f, .5f, .05f), cell["school_white"], faceYaw);
                    var p2 = c + Ax(sF, l + 1.2f); float g2 = G(p2.x, p2.y, out bool ok2); if (!ok2 || OnRoad(p2)) continue;
                    foreach (float hh in new[] { .35f, .85f }) fence.Box(new Vector3(p2.x, (g + g2) / 2 + hh, p2.y), new Vector3(.025f, .04f, 1.25f), cell["school_white"], faceYaw + 90f);
                }
                var bc = new GameObject("Collider"); bc.transform.SetParent(grp.transform, false); bc.transform.SetPositionAndRotation(new Vector3(c.x, G(c.x, c.y, out _) + 2.7f, c.y), Quaternion.Euler(0, faceYaw, 0)); var box = bc.AddComponent<BoxCollider>(); box.size = new Vector3(lenB, 5.5f, Depth);
            }
            Mesh Emit(MB mb, string name) { var m = new Mesh { name = name }; m.SetVertices(mb.v); m.SetUVs(0, mb.uv); m.SetTriangles(mb.t, 0); m.RecalculateNormals(); m.RecalculateBounds(); string mp = Art + "/" + name + ".asset"; if (AssetDatabase.LoadAssetAtPath<Mesh>(mp) != null) AssetDatabase.DeleteAsset(mp); AssetDatabase.CreateAsset(m, mp); return m; }
            if (pm.v.Count > 0) { var g = new GameObject("Plinths"); g.transform.SetParent(root.transform, false); g.isStatic = true; g.AddComponent<MeshFilter>().sharedMesh = Emit(pm, "TerracePlinths"); var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/UpIzUpMini/Art/Environment/Mini142/Palette.mat"); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            if (fence.v.Count > 0) { var g = new GameObject("Fence"); g.transform.SetParent(root.transform, false); g.isStatic = true; g.AddComponent<MeshFilter>().sharedMesh = Emit(fence, "TerraceFence"); var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "/Palette_Coast.mat"); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
            foreach (var (c, nu) in chosen) log.Add($"building of {nu} units at ({c.x:F0}, {G(c.x, c.y, out _):F1}, {c.y:F0})");
            log.Add($"apartment units placed={units} (target 15), plinth drop up to {maxDrop:F2} m; facing downhill yaw {faceYaw:F0}");
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            log.Add("live sha after=" + Sha(Live)); File.WriteAllLines(Out + "/Terrace-Report.txt", log); Debug.Log("MINI182TERRACE " + string.Join(" | ", log));
        }
    }
}
