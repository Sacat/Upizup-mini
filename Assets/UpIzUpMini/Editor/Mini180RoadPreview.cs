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
    /// <summary>Read-only road crowding audit and render for MINI-180. It never
    /// saves the scene. Preview geometry is temporary and exits with the editor.</summary>
    public static class Mini180RoadPreview
    {
        const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        const string Out = "Logs/Tasks/MINI-180";

        struct Road { public string name; public MeshFilter filter; public Vector3[] line; public float half; }
        struct Issue { public Road a, b; public int ai, bi; public Vector3 point; public float angle; public float separation; }

        [MenuItem("Up Iz Up Mini/MINI-180/Audit And Render Road Preview")]
        public static void Run()
        {
            try
            {
                Directory.CreateDirectory(Out + "/Renders");
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var root = GameObject.Find("MINI168_Expansion")?.transform ?? throw new Exception("Expansion root missing.");
                var roads = ReadRoads().ToArray();
                var issues = FindIssues(roads).OrderByDescending(x => Score(x)).ToList();
                WriteAudit(roads, issues);
                Render("before-network", new Vector3(105, 225, -12), new Vector3(105, 0, -12), true, 190);
                foreach (var issue in issues.Take(3)) Render("before-" + Safe(issue.a.name) + "-" + Safe(issue.b.name), issue.point + Vector3.up * 55, issue.point, true, 26);
                AddPreviewPatches(root, issues.Take(3).ToArray());
                Render("preview-network", new Vector3(105, 225, -12), new Vector3(105, 0, -12), true, 190);
                foreach (var issue in issues.Take(3)) Render("preview-" + Safe(issue.a.name) + "-" + Safe(issue.b.name), issue.point + Vector3.up * 55, issue.point, true, 26);
                Debug.Log("MINI180_AUDIT_PREVIEW_PASS roads=" + roads.Length + " issues=" + issues.Count);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static IEnumerable<Road> ReadRoads()
        {
            foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(x => x.name.IndexOf("road", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var v = f.sharedMesh?.vertices;
                if (v == null || v.Length < 4 || v.Length % 2 != 0) continue;
                var line = Enumerable.Range(0, v.Length / 2).Select(i => f.transform.TransformPoint((v[2 * i] + v[2 * i + 1]) * .5f)).ToArray();
                float half = Vector3.Distance(f.transform.TransformPoint(v[0]), f.transform.TransformPoint(v[1])) * .5f;
                yield return new Road { name = f.name, filter = f, line = line, half = half };
            }
        }

        static List<Issue> FindIssues(Road[] roads)
        {
            var output = new List<Issue>();
            for (int a = 0; a < roads.Length; a++) for (int b = a + 1; b < roads.Length; b++)
            for (int i = 0; i + 1 < roads[a].line.Length; i++) for (int j = 0; j + 1 < roads[b].line.Length; j++)
            {
                if (!Intersect(roads[a].line[i], roads[a].line[i + 1], roads[b].line[j], roads[b].line[j + 1], out var point, out float ta, out float tb)) continue;
                bool endpointJoin = (ta < .08f || ta > .92f) && (tb < .08f || tb > .92f);
                Vector3 da = roads[a].line[i + 1] - roads[a].line[i]; da.y = 0;
                Vector3 db = roads[b].line[j + 1] - roads[b].line[j]; db.y = 0;
                float angle = Vector3.Angle(da, db); angle = Mathf.Min(angle, 180 - angle);
                if (roads[a].name.IndexOf("JunctionPatch", StringComparison.OrdinalIgnoreCase) >= 0 || roads[b].name.IndexOf("JunctionPatch", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (angle < 25f) continue; // continuous seams and nearly parallel overlays: audit separately from visual junctions.
                if (endpointJoin && angle > 25f) continue; // intended connection, already covered by existing junction layer.
                float separation = Mathf.Abs(Vector3.Lerp(roads[a].line[i], roads[a].line[i + 1], ta).y - Vector3.Lerp(roads[b].line[j], roads[b].line[j + 1], tb).y);
                if (separation > .35f) continue; // grade-separated visual crossing.
                output.Add(new Issue { a = roads[a], b = roads[b], ai = i, bi = j, point = point, angle = angle, separation = separation });
            }
            return output;
        }

        static float Score(Issue x) => (x.a.half + x.b.half) * (1 + (90 - Mathf.Min(x.angle, 90)) / 90f) - x.separation * 2;

        static bool Intersect(Vector3 aa, Vector3 ab, Vector3 ba, Vector3 bb, out Vector3 p, out float ta, out float tb)
        {
            Vector2 a = new Vector2(aa.x, aa.z), r = new Vector2(ab.x - aa.x, ab.z - aa.z), b = new Vector2(ba.x, ba.z), s = new Vector2(bb.x - ba.x, bb.z - ba.z);
            float cross = r.x * s.y - r.y * s.x; p = default; ta = tb = 0;
            if (Mathf.Abs(cross) < .0001f) return false;
            Vector2 q = b - a; ta = (q.x * s.y - q.y * s.x) / cross; tb = (q.x * r.y - q.y * r.x) / cross;
            if (ta < 0 || ta > 1 || tb < 0 || tb > 1) return false;
            p = Vector3.Lerp(aa, ab, ta); p.y = Mathf.Max(Vector3.Lerp(aa, ab, ta).y, Vector3.Lerp(ba, bb, tb).y) + .014f; return true;
        }

        static void AddPreviewPatches(Transform root, Issue[] issues)
        {
            var holder = new GameObject("MINI180_PREVIEW_ONLY").transform; holder.SetParent(root, false);
            foreach (var x in issues)
            {
                var go = new GameObject("PreviewJunction_" + Safe(x.a.name) + "_" + Safe(x.b.name)); go.transform.SetParent(holder, true); go.transform.position = x.point;
                var mesh = new Mesh { name = go.name };
                var outline = JunctionOutline(x);
                var v = new List<Vector3> { Vector3.zero }; v.AddRange(outline.Select(p => p - x.point + Vector3.up * .025f));
                var t = new List<int>();
                for (int i = 0; i < outline.Count; i++) { t.Add(0); t.Add((i + 1) % outline.Count + 1); t.Add(i + 1); }
                mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals();
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var roadMaterial = x.a.filter.GetComponent<MeshRenderer>()?.sharedMaterial;
                go.AddComponent<MeshRenderer>().sharedMaterial = roadMaterial != null ? roadMaterial : new Material(Shader.Find("Standard")) { color = new Color(.22f, .23f, .23f) };
            }
        }

        static List<Vector3> JunctionOutline(Issue x)
        {
            var points = new List<Vector3>();
            Action<Road, int> addRoad = (road, segment) =>
            {
                var direction = road.line[segment + 1] - road.line[segment]; direction.y = 0; direction.Normalize();
                var normal = new Vector3(-direction.z, 0, direction.x);
                float extension = Mathf.Clamp(road.half * .7f, 1.6f, 2.3f);
                foreach (float side in new[] { -1f, 1f }) foreach (float across in new[] { -1f, 1f })
                    points.Add(x.point + direction * side * extension + normal * across * road.half);
            };
            addRoad(x.a, x.ai); addRoad(x.b, x.bi);
            return points.OrderBy(p => Mathf.Atan2(p.z - x.point.z, p.x - x.point.x)).ToList();
        }

        static void WriteAudit(Road[] roads, List<Issue> issues)
        {
            var lines = new List<string> { "MINI-180 read-only whole-map road audit", "roads=" + roads.Length, "At-grade interior road intersections/crossings=" + issues.Count };
            foreach (var x in issues) lines.Add($"{x.a.name} segment{x.ai} x {x.b.name} segment{x.bi} at ({x.point.x:F2},{x.point.y:F2},{x.point.z:F2}) angle={x.angle:F1}deg verticalGap={x.separation:F3}m widths={x.a.half * 2:F2}/{x.b.half * 2:F2} score={Score(x):F2}");
            lines.Add("Preview adds temporary only, unsaved road-shaped beveled junction surfaces at each measured at-grade crossing. It does not constitute a scene fix or approval.");
            File.WriteAllLines(Out + "/ROAD-AUDIT.txt", lines);
        }

        static void Render(string name, Vector3 pos, Vector3 target, bool ortho, float size)
        {
            foreach (var existingCamera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) existingCamera.enabled = false;
            RenderSettings.fog = false; RenderSettings.ambientLight = new Color(.66f, .69f, .72f);
            var c = new GameObject("MINI180Camera").AddComponent<Camera>(); c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(.55f, .70f, .86f); c.farClipPlane = 1800; c.orthographic = ortho; c.orthographicSize = size; c.fieldOfView = size; c.transform.position = pos; c.transform.LookAt(target);
            var rt = new RenderTexture(1600, 1000, 24); c.targetTexture = rt; c.Render(); RenderTexture.active = rt; var tx = new Texture2D(1600, 1000, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tx.Apply(); File.WriteAllBytes(Out + "/Renders/" + name + ".png", tx.EncodeToPNG()); c.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(tx); Object.DestroyImmediate(rt); Object.DestroyImmediate(c.gameObject);
        }
        static string Safe(string s) => s.Replace("ExpansionRoad_", "R");
    }
}
