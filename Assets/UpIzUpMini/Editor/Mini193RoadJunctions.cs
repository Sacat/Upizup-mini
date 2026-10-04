using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-193: smooth road joins in the new-map copy scene. Every road end that dead-ends near another road (a gap, a ragged butt-cut or a tongue
    /// overlapping the wrong way) gets one paved junction mouth: the side road's own edges run straight into the main road's near edge and the
    /// two corners are rounded with fillet curves. The patch follows the real road surface heights (raycast), uses the road's own material and sits 2 cm above.
    /// Idempotent: the MINI193_RoadJunctions root is rebuilt on every run. Never touches the live scene.
    /// </summary>
    public static class Mini193RoadJunctions
    {
        const string RootName = "MINI193_RoadJunctions";

        [MenuItem("Up Iz Up Mini/MINI-193/Apply Road Junctions")]
        public static void Apply()
        {
            try { ApplyInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static Vector2 XZ(Vector3 v) { return new Vector2(v.x, v.z); }

        static void ApplyInner()
        {
            var scene = EditorSceneManager.OpenScene(Mini193RoadAudit.ScenePath, OpenSceneMode.Single);
            var old = GameObject.Find(RootName); if (old != null) Object.DestroyImmediate(old);
            Physics.SyncTransforms();
            var roads = Mini193RoadAudit.ReadRoads().Where(r => r.strip).ToList();
            var root = new GameObject(RootName).transform;
            var log = new List<string>();
            int made = 0, skipped = 0;
            foreach (var a in roads)
                foreach (int endIndex in new[] { 0, a.line.Length - 1 })
                {
                    Vector3 e = a.line[endIndex];
                    // outward direction of road A at this end (average of the last few samples for stability)
                    int inner = endIndex == 0 ? Math.Min(4, a.line.Length - 1) : Math.Max(a.line.Length - 1 - 4, 0);
                    Vector2 d = (XZ(e) - XZ(a.line[inner])); if (d.sqrMagnitude < 1e-4f) continue; d.Normalize();
                    // nearest other road centreline point
                    RoadHit best = default(RoadHit); best.dist = float.MaxValue;
                    foreach (var b in roads) { if (b == a) continue; for (int j = 0; j + 1 < b.line.Length; j++) { var h = Closest(e, b, j); if (h.dist < best.dist) best = h; } }
                    if (best.dist > 9.5f) continue;
                    // continuation of the same road (end-to-end, nearly parallel): not a junction mouth
                    float cont = Vector2.Angle(d, best.tangent); cont = Mathf.Min(cont, 180f - cont);
                    if (cont < 28f) { skipped++; log.Add("skip (continuation, " + cont.ToString("0") + " deg) " + a.name + " end" + endIndex + " -> " + best.road.name); continue; }
                    Vector2 toC = XZ(best.point) - XZ(e);
                    float align = Vector2.Angle(d, toC);
                    if (best.dist > 1.6f && align > 55f) { skipped++; log.Add("skip (not pointing at " + best.road.name + ", " + align.ToString("0") + " deg) " + a.name + " end" + endIndex); continue; }
                    if (BuildPatch(root, a, e, d, best, log)) made++; else skipped++;
                }
            File.WriteAllLines(Mini193RoadAudit.Out + "/JUNCTIONS.txt", log);
            if (made > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            Debug.Log("MINI193_ROAD_JUNCTIONS made=" + made + " skipped=" + skipped);
        }

        struct RoadHit { public Mini193RoadAudit.RoadInfo road; public int seg; public Vector3 point; public Vector2 tangent; public float dist; }

        static RoadHit Closest(Vector3 p, Mini193RoadAudit.RoadInfo b, int j)
        {
            Vector2 pa = XZ(p), aa = XZ(b.line[j]), bb = XZ(b.line[j + 1]); Vector2 ab = bb - aa;
            float t = ab.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(pa - aa, ab) / ab.sqrMagnitude);
            Vector3 pt = Vector3.Lerp(b.line[j], b.line[j + 1], t);
            return new RoadHit { road = b, seg = j, point = pt, tangent = ab.normalized, dist = Vector2.Distance(pa, XZ(pt)) };
        }

        static bool Intersect(Vector2 p, Vector2 r, Vector2 q, Vector2 s, out float t)
        {
            float cross = r.x * s.y - r.y * s.x; t = 0;
            if (Mathf.Abs(cross) < 1e-5f) return false;
            Vector2 qp = q - p; t = (qp.x * s.y - qp.y * s.x) / cross; return true;
        }

        static List<Vector2> Fillet(Vector2 t1, Vector2 corner, Vector2 t2, int n)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= n; i++) { float u = i / (float)n; pts.Add((1 - u) * (1 - u) * t1 + 2 * u * (1 - u) * corner + u * u * t2); }
            return pts;
        }

        static bool BuildPatch(Transform root, Mini193RoadAudit.RoadInfo a, Vector3 e, Vector2 d, RoadHit b, List<string> log)
        {
            float hA = a.half, hB = b.road.half;
            Vector2 c = XZ(b.point), bt = b.tangent, bn = new Vector2(-bt.y, bt.x);
            Vector2 E2 = XZ(e);
            // path: A's own trailing centreline (up to 9 m), then a smooth curve from its end to B's centre line
            var path = new List<Vector2>();
            bool atStart = a.line[0] == e;
            var trail = new List<Vector2>(); float acc = 0f; trail.Add(E2);
            for (int i = 1; i < a.line.Length; i++)
            {
                Vector2 q = XZ(a.line[atStart ? i : a.line.Length - 1 - i]);
                acc += Vector2.Distance(q, trail[trail.Count - 1]); trail.Add(q);
                if (acc >= 9f) break;
            }
            trail.Reverse(); path.AddRange(trail);                      // far -> end
            Vector2 toC = c - E2; float L = toC.magnitude;
            Vector2 dirEnd = L > 0.05f ? Vector2.Lerp(d, toC / L, 0.5f).normalized : d;
            if (L > 0.05f)
            {
                int steps = Mathf.Max(3, Mathf.CeilToInt(L / 0.8f));
                for (int i = 1; i <= steps; i++)
                {
                    float u = i / (float)steps; Vector2 p0 = E2, p1 = E2 + d * L / 3f, p2 = c - dirEnd * L / 3f, p3 = c;
                    path.Add((1 - u) * (1 - u) * (1 - u) * p0 + 3 * (1 - u) * (1 - u) * u * p1 + 3 * (1 - u) * u * u * p2 + u * u * u * p3);
                }
            }
            else
            {
                // A already ends on B's centre line: keep running a few metres into B so the mouth reaches across the whole carriageway
                for (int i = 1; i <= 4; i++) path.Add(E2 + dirEnd * (hB * 0.5f) * i / 4f);
            }
            path.Add(path[path.Count - 1] + dirEnd * 0.5f);
            // arc-length parameter and the arclength where the path crosses B's near edge
            var sArr = new float[path.Count]; for (int i = 1; i < path.Count; i++) sArr[i] = sArr[i - 1] + Vector2.Distance(path[i], path[i - 1]);
            float total = sArr[path.Count - 1];
            float crossing = Mathf.Max(0.35f, Mathf.Abs(Vector2.Dot(dirEnd, bn)));
            float sEdge = Mathf.Max(5f, total - hB / crossing);
            float flare = Mathf.Clamp(hB * 0.8f, 2.0f, 3.2f);
            var verts = new List<Vector3>(); var tris = new List<int>();
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 t = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]); if (t.sqrMagnitude < 1e-6f) t = d; t.Normalize();
                Vector2 n = new Vector2(-t.y, t.x);
                float ramp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sEdge - 5f, sEdge + 0.5f, sArr[i]));
                float hw = hA + flare * ramp;
                // B-side pull so the flared mouth stays inside B beyond its edge (no tongue past the far side)
                verts.Add(new Vector3(path[i].x + n.x * hw, 0, path[i].y + n.y * hw));
                verts.Add(new Vector3(path[i].x - n.x * hw, 0, path[i].y - n.y * hw));
                if (i > 0) { int k = 2 * i; tris.AddRange(new[] { k - 2, k, k - 1, k - 1, k, k + 1 }); }
            }
            var hit = new bool[verts.Count]; float sumY = 0; int nHit = 0;
            for (int i = 0; i < verts.Count; i++) { float y; if (RoadY(verts[i], e.y, out y)) { verts[i] = new Vector3(verts[i].x, y + 0.02f, verts[i].z); hit[i] = true; sumY += y; nHit++; } }
            if (nHit < 4) { log.Add("no road surface under patch for " + a.name); return false; }
            for (int i = 0; i < verts.Count; i++)
                if (!hit[i])
                {
                    // nearest hit along the ribbon (same side first) keeps the grade continuous across the gap
                    int bestJ = -1; float bd = float.MaxValue;
                    for (int j = 0; j < verts.Count; j++) if (hit[j]) { float dd = Mathf.Abs(j / 2 - i / 2) + ((j & 1) == (i & 1) ? 0f : 0.01f); if (dd < bd) { bd = dd; bestJ = j; } }
                    verts[i] = new Vector3(verts[i].x, verts[bestJ].y, verts[i].z);
                }
            for (int i = 0; i < tris.Count; i += 3)
            {
                Vector3 nrm = Vector3.Cross(verts[tris[i + 1]] - verts[tris[i]], verts[tris[i + 2]] - verts[tris[i]]);
                if (nrm.y < 0) { int tmp = tris[i + 1]; tris[i + 1] = tris[i + 2]; tris[i + 2] = tmp; }
            }
            var go = new GameObject("Junction_" + a.name + "_to_" + b.road.name); go.transform.SetParent(root, false);
            var mesh = new Mesh { name = go.name };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, verts.Select(v => new Vector2(v.x * .25f, v.z * .25f)).ToList());
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>(); var src = a.filter.GetComponent<MeshRenderer>(); mr.sharedMaterial = src.sharedMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh;
            log.Add("patch " + a.name + " -> " + b.road.name + " gap=" + b.dist.ToString("0.0") + "m path=" + total.ToString("0.0") + "m flare=" + flare.ToString("0.0"));
            return true;
        }

        static bool RoadY(Vector3 p, float refY, out float y)
        {
            y = 0;
            var hits = Physics.RaycastAll(new Vector3(p.x, refY + 25f, p.z), Vector3.down, 60f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MinValue; bool found = false;
            foreach (var h in hits)
            {
                string n = h.collider.name;
                if (n.StartsWith("Road_") || n.StartsWith("ExpansionRoad_") || n.StartsWith("ImportTrim_Road"))
                {
                    if (Mathf.Abs(h.point.y - refY) > 12f) continue;
                    if (h.point.y > best) { best = h.point.y; found = true; }
                }
            }
            y = best; return found;
        }

        static float SignedArea(List<Vector2> p)
        {
            float s = 0; for (int i = 0; i < p.Count; i++) { var a = p[i]; var b = p[(i + 1) % p.Count]; s += a.x * b.y - b.x * a.y; }
            return s * .5f;
        }

        static List<int> EarClip(List<Vector2> poly)
        {
            var idx = Enumerable.Range(0, poly.Count).ToList(); var tris = new List<int>(); int guard = 0;
            while (idx.Count > 3 && guard++ < 5000)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int i0 = idx[(i + idx.Count - 1) % idx.Count], i1 = idx[i], i2 = idx[(i + 1) % idx.Count];
                    Vector2 a = poly[i0], b = poly[i1], c = poly[i2];
                    if (Cross(b - a, c - b) <= 1e-6f) continue;   // reflex or degenerate (polygon is counter-clockwise)
                    bool inside = false;
                    foreach (int j in idx) { if (j == i0 || j == i1 || j == i2) continue; if (InTri(poly[j], a, b, c)) { inside = true; break; } }
                    if (inside) continue;
                    tris.AddRange(new[] { i0, i1, i2 }); idx.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) return null;
            }
            if (idx.Count == 3) tris.AddRange(new[] { idx[0], idx[1], idx[2] });
            return tris;
        }

        static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(b - a, p - a), d2 = Cross(c - b, p - b), d3 = Cross(a - c, p - c);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0; return !(neg && pos);
        }
    }
}
