using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-171: bring the MINI-168 Grand Bay expansion (roads, terrain,
    /// houses, PCSS school, Geneva field, roundabout) into a NEW copy of the
    /// LIVE scene. The live scene is never saved; the expansion copy is never
    /// saved; both hashes are checked before and after.
    ///
    /// Why not just use the expansion copy as the game: it was seeded from
    /// the Aug-29 spline proof, so it has none of the 86 MINI-142 house-art
    /// houses, the safehouse work, or the Lalay House (measured by scene text
    /// counts, not assumed). So only the NEW content is moved across.
    ///
    /// Codex's GenevaTrim_* / GenevaRetainedBayRoad objects are NOT moved:
    /// they clip the copy's MB-spline road, which the live scene does not
    /// have. Instead the LIVE bay road and its sidewalk/kerb/frontage pieces
    /// are clipped at the same world X=250 cut, so the live junction repairs
    /// baked into that mesh are kept.
    /// </summary>
    public static class Mini171ImportExpansion
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/MapLab_GrandBayExpansionCopy.unity";
        const string Import = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        const string Art = "Assets/UpIzUpMini/Maps/Regions/dm-dom-grand-bay-expansion-v1/Generated";
        const string Out = "Logs/Tasks/MINI-171";
        const float Cut = 250f;

        [MenuItem("Up Iz Up Mini/MINI-171/Build Expansion Import Scene")]
        public static void Build()
        {
            try { BuildInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void BuildInner()
        {
            Directory.CreateDirectory(Out);
            string liveHash = Sha(Live), copyHash = Sha(Copy);
            var report = new List<string> { "MINI-171 import report", "liveSha256(before)=" + liveHash, "copySha256(before)=" + copyHash };

            var liveScene = EditorSceneManager.OpenScene(Live, OpenSceneMode.Single);
            var world = Find(liveScene, "GrandBayPhase1_ApprovedWorld_VA005");
            if (world == null) throw new Exception("Live map root GrandBayPhase1_ApprovedWorld_VA005 not found.");
            RequireIdentity(world.transform, "live map root");
            if (!EditorSceneManager.SaveScene(liveScene, Import, false)) throw new Exception("Could not save new import scene.");
            var importScene = SceneManager.GetActiveScene();
            if (importScene.path != Import) throw new Exception("Active scene is not the import scene after save-as: " + importScene.path);
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed while creating the import scene.");
            report.Add("importScene=" + Import);

            // ---- bring the new expansion root across (copy scene stays unsaved) ----
            var copyScene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Additive);
            var copyMap = Find(copyScene, "MapLab_GrandBayExpansionCopy");
            if (copyMap == null) throw new Exception("Expansion copy map root not found.");
            RequireIdentity(copyMap.transform, "copy map root");
            var exp = copyMap.transform.Find("MINI168_Expansion");
            if (exp == null) throw new Exception("MINI168_Expansion not found in the copy.");
            exp.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(exp.gameObject, importScene);
            SceneManager.SetActiveScene(importScene);
            EditorSceneManager.CloseScene(copyScene, true);

            int removedTrims = 0;
            foreach (var c in exp.Cast<Transform>().ToArray())
                if (c.name.StartsWith("GenevaTrim_") || c.name == "GenevaRetainedBayRoad") { UnityEngine.Object.DestroyImmediate(c.gameObject); removedTrims++; }
            report.Add("expansionChildrenAfterExclusions=" + exp.childCount + " (excluded copy-only Geneva trims: " + removedTrims + ")");

            // ---- clip the LIVE bay road + roadside pieces at X=250 ----
            var seam = new List<Vector3>();
            int clipped = 0;
            foreach (var f in world.GetComponentsInChildren<MeshFilter>(false).ToArray())
            {
                if (!f.name.Contains("way_22917921") || f.sharedMesh == null) continue;
                bool isRoad = f.name == "Road_way_22917921";
                bool isEdge = f.name.Contains("Sidewalk") || f.name.Contains("KerbRamp") || f.name.Contains("Frontage");
                if (!isRoad && !isEdge) continue;
                if (!f.sharedMesh.vertices.Any(p => f.transform.TransformPoint(p).x > Cut)) continue;
                ClipToNewObject(f, exp, isRoad ? seam : null);
                clipped++;
            }
            report.Add("liveObjectsClippedAtX250=" + clipped);
            if (seam.Count < 2) throw new Exception("Live bay road produced no cross-section at X=250.");
            var lo = seam.OrderBy(p => p.z).First(); var hi = seam.OrderBy(p => p.z).Last();
            report.Add("liveBaySeam=" + lo + " -> " + hi + " centre=" + (lo + hi) * .5f);

            // ---- snap the bay approach to the LIVE seam (Codex built it against the copy's MB mesh,
            // which sits ~12cm/3cm off the live road at X=250). Done on a separate mesh asset so the
            // expansion copy's own road mesh is never edited. ----
            var approach = exp.Find("ExpansionRoad_10");
            if (approach == null) throw new Exception("ExpansionRoad_10 (bay approach) not found.");
            var amf = approach.GetComponent<MeshFilter>();
            var am = UnityEngine.Object.Instantiate(amf.sharedMesh); am.name = "Import_ExpansionRoad_10";
            var av = am.vertices;
            var idx = Enumerable.Range(0, av.Length).Where(i => Mathf.Abs(approach.TransformPoint(av[i]).x - Cut) < .05f).OrderBy(i => approach.TransformPoint(av[i]).z).ToArray();
            if (idx.Length != 2) throw new Exception("Expected 2 seam vertices on the bay approach, found " + idx.Length);
            report.Add("approachSeamBefore=" + approach.TransformPoint(av[idx[0]]) + " -> " + approach.TransformPoint(av[idx[1]]));
            av[idx[0]] = approach.InverseTransformPoint(lo); av[idx[1]] = approach.InverseTransformPoint(hi);
            am.vertices = av; am.RecalculateNormals(); am.RecalculateBounds();
            string apath = Art + "/Import_ExpansionRoad_10.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(apath) != null) AssetDatabase.DeleteAsset(apath);
            AssetDatabase.CreateAsset(am, apath);
            amf.sharedMesh = am;
            var amc = approach.GetComponent<MeshCollider>(); if (amc != null) { amc.sharedMesh = null; amc.sharedMesh = am; }
            report.Add("approachSeamAfter=" + approach.TransformPoint(am.vertices[idx[0]]) + " -> " + approach.TransformPoint(am.vertices[idx[1]]));

            // ---- ground + roads + houses: hybrid terrain, roads flush with the ground, live-road joins,
            // conflicting live houses nudged aside ----
            var liveTerrain = world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Copernicus_GLO30_Terrain");
            if (liveTerrain == null) throw new Exception("Live Copernicus_GLO30_Terrain not found.");
            var newTerrain = exp.Find("ExpansionTerrain");
            if (newTerrain == null || newTerrain.GetComponent<MeshCollider>() == null) throw new Exception("ExpansionTerrain missing or has no MeshCollider.");
            RegradeAndFit(world, exp, liveTerrain, newTerrain, lo, hi, report);
            liveTerrain.gameObject.SetActive(false); report.Add("liveTerrainDisabled=" + liveTerrain.name + " (kept in scene, inactive, for rollback)");

            // ---- world edge barriers cover only the old footprint; report, do not silently drop ----
            var barriers = world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "PhaseOne_Boundaries");
            report.Add("liveBoundaries=" + (barriers != null ? barriers.name + " active=" + barriers.gameObject.activeSelf + " (left unchanged; new outer edge has no barrier yet)" : "not found"));

            EditorSceneManager.MarkSceneDirty(importScene);
            if (!EditorSceneManager.SaveScene(importScene, Import)) throw new Exception("Could not save import scene.");
            AssetDatabase.SaveAssets();

            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed during import.");
            if (Sha(Copy) != copyHash) throw new Exception("SAFETY STOP: expansion copy scene changed during import.");
            if (EditorBuildSettings.scenes.Any(s => s.enabled && s.path == Import)) throw new Exception("SAFETY STOP: import scene is enabled in build settings.");
            report.Add("liveSha256(after)=" + Sha(Live));
            report.Add("copySha256(after)=" + Sha(Copy));
            report.Add("importInBuildSettings=false");
            File.WriteAllLines(Out + "/IMPORT-REPORT.txt", report);
            Debug.Log("MINI171_IMPORT_PASS " + string.Join(" | ", report));
        }

        // Clip a live mesh object at world X=Cut into a NEW world-space object under `parent`;
        // the original is disabled (kept for rollback). UVs are interpolated, layer/tag/static flags copied.
        static void ClipToNewObject(MeshFilter f, Transform parent, List<Vector3> seamOut)
        {
            var src = f.sharedMesh;
            if (src.subMeshCount != 1) throw new Exception(f.name + " has " + src.subMeshCount + " submeshes; clip supports 1.");
            var sv = src.vertices; var suv = src.uv; var st = src.triangles;
            bool hasUv = suv != null && suv.Length == sv.Length;
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            for (int i = 0; i < st.Length; i += 3)
            {
                var poly = new List<(Vector3 p, Vector2 uv)>();
                for (int j = 0; j < 3; j++) poly.Add((f.transform.TransformPoint(sv[st[i + j]]), hasUv ? suv[st[i + j]] : Vector2.zero));
                var outp = new List<(Vector3 p, Vector2 uv)>();
                for (int j = 0; j < 3; j++)
                {
                    var a = poly[j]; var b = poly[(j + 1) % 3];
                    bool ia = a.p.x <= Cut, ib = b.p.x <= Cut;
                    if (ia) outp.Add(a);
                    if (ia != ib)
                    {
                        float t = (Cut - a.p.x) / (b.p.x - a.p.x);
                        var q = (Vector3.Lerp(a.p, b.p, t), Vector2.Lerp(a.uv, b.uv, t));
                        outp.Add(q); seamOut?.Add(q.Item1);
                    }
                }
                for (int j = 1; j + 1 < outp.Count; j++)
                {
                    int k = verts.Count;
                    verts.AddRange(new[] { outp[0].p, outp[j].p, outp[j + 1].p });
                    uvs.AddRange(new[] { outp[0].uv, outp[j].uv, outp[j + 1].uv });
                    tris.AddRange(new[] { k, k + 1, k + 2 });
                }
            }
            var mesh = new Mesh { name = "Import_" + f.name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts); if (hasUv) mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            string path = Art + "/Import_" + f.name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            var go = new GameObject("ImportTrim_" + f.name) { layer = f.gameObject.layer };
            go.tag = f.gameObject.tag;
            go.transform.SetParent(parent, false);
            GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(f.gameObject));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = f.GetComponent<Renderer>().sharedMaterials;
            var oldCol = f.GetComponent<MeshCollider>();
            if (oldCol != null) { var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.sharedMaterial = oldCol.sharedMaterial; }
            f.gameObject.SetActive(false);
        }

        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        static bool Drop(Collider c, float x, float z, out float y)
        {
            if (c.Raycast(new Ray(new Vector3(x, 400f, z), Vector3.down), out var h, 900f)) { y = h.point.y; return true; }
            y = 0; return false;
        }

        // Hybrid ground: LIVE heights wherever the live terrain exists (so Lalay/Highland/safehouses do not
        // move), the expansion heights outside it, blended over 40m. Expansion roads are then re-seated onto
        // that ground (ends blended to any live road they meet) and the ground is flattened under each road
        // (road width + 3m shoulder, 12m smoothstep back to natural) so there is no lip between road and grass.
        // Houses/school/field are moved by the local ground change. Live houses are never moved.
        static void RegradeAndFit(GameObject world, Transform exp, Transform liveTerrain, Transform newTerrain, Vector3 liveSeamLo, Vector3 liveSeamHi, List<string> report)
        {
            const float Shoulder = 3f, RoadBlend = 12f, JoinBlend = 40f, RoadDrop = 0.03f, EndBlend = 45f;
            var liveCol = liveTerrain.GetComponent<MeshCollider>();
            if (liveCol == null) throw new Exception("Live terrain has no MeshCollider.");
            var newCol = newTerrain.GetComponent<MeshCollider>();
            Physics.SyncTransforms();
            var rect = liveCol.bounds;
            var nmf = newTerrain.GetComponent<MeshFilter>();
            var nm = UnityEngine.Object.Instantiate(nmf.sharedMesh); nm.name = "Import_ExpansionTerrain";
            var nv = nm.vertices;
            var wv = new Vector3[nv.Length];
            for (int i = 0; i < nv.Length; i++) wv[i] = newTerrain.TransformPoint(nv[i]);

            // H0: hybrid base ground per vertex
            var h0 = new float[nv.Length]; int liveUsed = 0, blended = 0, newOnly = 0;
            var origNew = new float[nv.Length];
            for (int i = 0; i < nv.Length; i++)
            {
                float x = wv[i].x, z = wv[i].z; origNew[i] = wv[i].y;
                float cx = Mathf.Clamp(x, rect.min.x + .5f, rect.max.x - .5f), cz = Mathf.Clamp(z, rect.min.z + .5f, rect.max.z - .5f);
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
                if (Drop(liveCol, cx, cz, out float ly))
                {
                    float w = Smooth(dist / JoinBlend);
                    h0[i] = Mathf.Lerp(ly, wv[i].y, w);
                    if (w <= 0) liveUsed++; else if (w < 1) blended++; else newOnly++;
                }
                else { h0[i] = wv[i].y; newOnly++; }
            }
            report.Add($"terrain vertices={nv.Length} liveHeight={liveUsed} blended={blended} expansionHeight={newOnly}");

            // Hybrid sampler over the base grid (nearest-vertex lookup via spatial hash)
            var cell = new Dictionary<long, List<int>>();
            long Key(float x, float z) => ((long)Mathf.FloorToInt(x / 4f) << 32) ^ (uint)Mathf.FloorToInt(z / 4f);
            for (int i = 0; i < nv.Length; i++) { long k = Key(wv[i].x, wv[i].z); if (!cell.TryGetValue(k, out var l)) cell[k] = l = new List<int>(); l.Add(i); }
            float Base(float x, float z)
            {
                float bx = Mathf.FloorToInt(x / 4f), bz = Mathf.FloorToInt(z / 4f), sum = 0, ws = 0;
                for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                    if (cell.TryGetValue(((long)(bx + dx) << 32) ^ (uint)(bz + dz), out var l))
                        foreach (int i in l) { float d = (wv[i].x - x) * (wv[i].x - x) + (wv[i].z - z) * (wv[i].z - z); float w = 1f / (d + .25f); sum += h0[i] * w; ws += w; }
                return ws > 0 ? sum / ws : 0f;
            }

            // live roads (for road-end joins)
            Physics.SyncTransforms();
            var liveRoads = world.GetComponentsInChildren<MeshCollider>(false).Where(c => c.name.StartsWith("Road_")).ToList();
            liveRoads.AddRange(exp.GetComponentsInChildren<MeshCollider>(false).Where(c => c.name.StartsWith("ImportTrim_Road_way")));
            bool LiveRoadY(Vector3 p, out float y)
            {
                float best = float.MaxValue; y = 0;
                for (int r = 0; r <= 6; r++)
                    for (int a = 0; a < (r == 0 ? 1 : 8); a++)
                    {
                        float ang = a * Mathf.PI / 4f; float x = p.x + Mathf.Cos(ang) * r, z = p.z + Mathf.Sin(ang) * r;
                        foreach (var c in liveRoads) if (Drop(c, x, z, out float hy) && r < best) { best = r; y = hy; }
                        if (best < float.MaxValue) return true;
                    }
                return false;
            }

            bool LiveRoadNear(Vector3 p, float radius, out float y)
            {
                y = float.MinValue; bool any = false;
                for (int a = 0; a < 9; a++)
                {
                    float x = a == 0 ? p.x : p.x + Mathf.Cos(a * Mathf.PI / 4) * radius, z = a == 0 ? p.z : p.z + Mathf.Sin(a * Mathf.PI / 4) * radius;
                    foreach (var c in liveRoads) if (Drop(c, x, z, out float hy)) { any = true; if (a == 0 || true) y = Mathf.Max(y, a == 0 ? hy : hy); }
                }
                return any;
            }

            // roads: re-seat centre-line heights on the hybrid ground
            var segs = new List<(Vector2 a, Vector2 b, float ya, float yb, float hw)>();
            int roadsFixed = 0;
            foreach (var road in exp.Cast<Transform>().Where(t => t.name.StartsWith("ExpansionRoad_")).ToArray())
            {
                var rmf = road.GetComponent<MeshFilter>(); var rm0 = rmf.sharedMesh;
                var rv = rm0.vertices; int n = rv.Length;
                if (n % 2 != 0 || n < 4) { report.Add("WARN " + road.name + " vertex count " + n + " not paired; left as is"); continue; }
                var wl = new Vector3[n / 2]; var wr = new Vector3[n / 2];
                for (int i = 0; i < n / 2; i++) { wl[i] = road.TransformPoint(rv[2 * i]); wr[i] = road.TransformPoint(rv[2 * i + 1]); }
                int m = n / 2; var ry = new float[m];
                for (int i = 0; i < m; i++) { var c = (wl[i] + wr[i]) * .5f; ry[i] = Base(c.x, c.z); }
                for (int pass = 0; pass < 3; pass++)
                {
                    var t = (float[])ry.Clone();
                    for (int i = 0; i < m; i++) { float s = 0; int cnt = 0; for (int k = -4; k <= 4; k++) { int j = i + k; if (j >= 0 && j < m) { s += ry[j]; cnt++; } } t[i] = s / cnt; }
                    ry = t;
                }
                // join to live roads at either end
                var cum = new float[m]; for (int i = 1; i < m; i++) cum[i] = cum[i - 1] + Vector3.Distance((wl[i] + wr[i]) * .5f, (wl[i - 1] + wr[i - 1]) * .5f);
                foreach (bool startEnd in new[] { true, false })
                {
                    int e = startEnd ? 0 : m - 1; var cp = (wl[e] + wr[e]) * .5f;
                    if (!LiveRoadY(cp, out float ly)) continue;
                    float off = ly - ry[e];
                    for (int i = 0; i < m; i++) { float d = startEnd ? cum[i] : cum[m - 1] - cum[i]; ry[i] += off * (1f - Smooth(d / EndBlend)); }
                    report.Add($"{road.name} {(startEnd ? "start" : "end")} joined to live road, offset {off:F2}m blended over {EndBlend}m");
                }
                for (int i = 0; i < m; i++) { wl[i].y = ry[i]; wr[i].y = ry[i];
                    if (road.name == "ExpansionRoad_10" && i == 0)
                    {   // keep the exact live seam cross-section (its two edges differ by 13cm)
                        if (Mathf.Abs(wl[0].z - liveSeamLo.z) < Mathf.Abs(wr[0].z - liveSeamLo.z)) { wl[0] = liveSeamLo; wr[0] = liveSeamHi; }
                        else { wr[0] = liveSeamLo; wl[0] = liveSeamHi; }
                    } rv[2 * i] = road.InverseTransformPoint(wl[i]); rv[2 * i + 1] = road.InverseTransformPoint(wr[i]); }
                for (int i = 0; i + 1 < m; i++)
                    segs.Add((new Vector2((wl[i].x + wr[i].x) * .5f, (wl[i].z + wr[i].z) * .5f), new Vector2((wl[i + 1].x + wr[i + 1].x) * .5f, (wl[i + 1].z + wr[i + 1].z) * .5f), ry[i], ry[i + 1],
                        (Vector3.Distance(wl[i], wr[i]) + Vector3.Distance(wl[i + 1], wr[i + 1])) * .25f));
                var rm = UnityEngine.Object.Instantiate(rm0); rm.name = "Import_" + road.name;
                rm.vertices = rv; rm.RecalculateNormals(); rm.RecalculateBounds();
                string rp = Art + "/Import_" + road.name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Mesh>(rp) != null) AssetDatabase.DeleteAsset(rp);
                AssetDatabase.CreateAsset(rm, rp);
                rmf.sharedMesh = rm; var rc = road.GetComponent<MeshCollider>(); if (rc != null) { rc.sharedMesh = null; rc.sharedMesh = rm; }
                roadsFixed++;
            }
            report.Add("expansionRoadsReseated=" + roadsFixed + " centrelineSegments=" + segs.Count);

            // ground: flat pad under each road, smooth blend to natural ground
            int padded = 0, underLive = 0;
            for (int i = 0; i < nv.Length; i++)
            {
                var p = new Vector2(wv[i].x, wv[i].z); float bestD = float.MaxValue, bestY = 0;
                foreach (var s in segs)
                {
                    var ab = s.b - s.a; float len2 = ab.sqrMagnitude; float t = len2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - s.a, ab) / len2) : 0f;
                    float d = (p - (s.a + ab * t)).magnitude - s.hw;
                    if (d < bestD) { bestD = d; bestY = Mathf.Lerp(s.ya, s.yb, t) - RoadDrop; }
                }
                float y = h0[i];
                if (bestD < Shoulder) { y = bestY; padded++; }
                else if (bestD < Shoulder + RoadBlend) { y = Mathf.Lerp(bestY, h0[i], Smooth((bestD - Shoulder) / RoadBlend)); padded++; }
                if (bestD < Shoulder + RoadBlend && LiveRoadNear(new Vector3(wv[i].x, 0, wv[i].z), 2f, out float lry) && y > lry - .05f) { y = lry - .05f; underLive++; }
                wv[i].y = y; nv[i] = newTerrain.InverseTransformPoint(wv[i]);
            }
            nm.vertices = nv; nm.RecalculateNormals(); nm.RecalculateBounds();
            string tp = Art + "/Import_ExpansionTerrain.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(tp) != null) AssetDatabase.DeleteAsset(tp);
            AssetDatabase.CreateAsset(nm, tp);
            nmf.sharedMesh = nm; newCol.sharedMesh = null; newCol.sharedMesh = nm;
            Physics.SyncTransforms();
            report.Add("terrainVerticesShapedByRoads=" + padded + " keptBelowLiveRoad=" + underLive);

            // reseat houses/school/field/island by the ground change at their footprint centre
            var finalAt = new Func<float, float, float>((x, z) => Drop(newCol, x, z, out float y) ? y : float.NaN);
            var origMesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GetAssetPath(newTerrain.GetComponent<MeshFilter>().sharedMesh));
            float maxShift = 0; int moved = 0;
            foreach (var t in exp.Cast<Transform>().ToArray())
            {
                bool reseat = t.name.StartsWith("ExpansionHouse_") || t.name.StartsWith("HouseFoundation_") || t.name.StartsWith("School") || t.name.StartsWith("Geneva");
                if (!reseat) continue;
                var rs = t.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float after = finalAt(b.center.x, b.center.z);
                float before = Base(b.center.x, b.center.z) + 0f;
                // "before" = the ground the object was authored against (original expansion terrain)
                float orig = OrigNewHeight(nv.Length, wv, origNew, cell, b.center.x, b.center.z);
                if (float.IsNaN(after) || float.IsNaN(orig)) continue;
                float dy = after - orig;
                if (Mathf.Abs(dy) < .005f) continue;
                t.position += Vector3.up * dy; moved++; maxShift = Mathf.Max(maxShift, Mathf.Abs(dy));
                if (Mathf.Abs(dy) > 1f) report.Add($"reseat {t.name} by {dy:F2}m (large)");
            }
            report.Add($"expansionObjectsReseated={moved} maxShift={maxShift:F2}m");
        }

        static float OrigNewHeight(int n, Vector3[] wv, float[] origNew, Dictionary<long, List<int>> cell, float x, float z)
        {
            float bx = Mathf.FloorToInt(x / 4f), bz = Mathf.FloorToInt(z / 4f), sum = 0, ws = 0;
            for (int dx = -1; dx <= 1; dx++) for (int dz = -1; dz <= 1; dz++)
                if (cell.TryGetValue(((long)(bx + dx) << 32) ^ (uint)(bz + dz), out var l))
                    foreach (int i in l) { float d = (wv[i].x - x) * (wv[i].x - x) + (wv[i].z - z) * (wv[i].z - z); float w = 1f / (d + .25f); sum += origNew[i] * w; ws += w; }
            return ws > 0 ? sum / ws : float.NaN;
        }

        static GameObject Find(Scene s, string name) => s.GetRootGameObjects().FirstOrDefault(g => g.name == name);

        static void RequireIdentity(Transform t, string label)
        {
            if (t.position.sqrMagnitude > 1e-6f || Quaternion.Angle(t.rotation, Quaternion.identity) > .01f || (t.lossyScale - Vector3.one).sqrMagnitude > 1e-6f)
                throw new Exception(label + " is not at identity transform (" + t.position + ", " + t.eulerAngles + ", " + t.lossyScale + "); coordinates would not line up.");
        }

        static string Sha(string path)
        {
            using var sha = SHA256.Create(); using var s = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "").ToLowerInvariant();
        }
    }
}
