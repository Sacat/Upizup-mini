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
    /// MINI-201: one road look and smooth, ground-level roads for the whole live network.
    /// The heights were solved outside Unity from the live scene's real meshes (cloud design lane,
    /// Tools/RoadPipeline/mini201_road_smooth) and stored in Docs/WorkPackets/MINI-201/mini201_apply.json:
    /// per road object the new world Y of every vertex (X/Z never change), covered duplicate triangles to drop,
    /// the reshaped terrain heights, and the few props whose ground moved.
    /// This tool only APPLIES that data, checks every vertex against its recorded X/Z/Y before touching it,
    /// writes new mesh assets (originals are kept), swaps every road to ExpansionAsphalt and validates drive lines.
    /// "Preview On Copy" never touches GrandBayProof.unity. "Apply To Live" backs the scene up first.
    /// </summary>
    public static class Mini201RoadSmooth
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_RoadFix201.unity";
        const string Data = "Docs/WorkPackets/MINI-201/mini201_apply.json";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini201";
        const string Out = "Logs/Tasks/MINI-201";
        const string Backup = "Builds/PreMini201SceneBackup/GrandBayProof.unity";

        [Serializable] public class RoadRec { public string name; public string path; public float[] x; public float[] z; public float[] y0; public float[] y; public int[] dropTriangles; public bool disable; public int triangleCount; public int priority; }
        [Serializable] public class FollowRec { public string path; public float dy; public float x; public float z; }
        [Serializable] public class ApplyData { public string material; public RoadRec[] roads; public RoadRec terrain; public FollowRec[] groundFollow; }

        [MenuItem("Up Iz Up Mini/MINI-201/Preview On Copy (safe)")]
        public static void PreviewOnCopy()
        {
            try
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Copy) != null) AssetDatabase.DeleteAsset(Copy);
                if (!AssetDatabase.CopyAsset(Live, Copy)) throw new Exception("Could not copy " + Live);
                var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
                Directory.CreateDirectory(Out + "/Renders");
                Capture("before");
                Apply();
                Capture("after");
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                bool ok = Validate("copy");
                Debug.Log((ok ? "MINI201_PREVIEW_PASS " : "MINI201_PREVIEW_FAIL ") + Copy);
                if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        [MenuItem("Up Iz Up Mini/MINI-201/Apply To Live GrandBayProof (after preview approval)")]
        public static void ApplyToLive()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Backup));
                if (!File.Exists(Backup)) File.Copy(Live, Backup);
                var scene = EditorSceneManager.OpenScene(Live, OpenSceneMode.Single);
                Directory.CreateDirectory(Out + "/Renders");
                Apply();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                bool ok = Validate("live");
                Debug.Log((ok ? "MINI201_LIVE_PASS" : "MINI201_LIVE_FAIL") + " backup=" + Backup);
                if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static ApplyData Load()
        {
            if (!File.Exists(Data)) throw new Exception("Missing " + Data);
            var d = JsonUtility.FromJson<ApplyData>(File.ReadAllText(Data));
            if (d == null || d.roads == null || d.roads.Length == 0 || d.terrain == null) throw new Exception("Unreadable " + Data);
            return d;
        }

        static GameObject FindPath(string path)
        {
            var parts = path.Split('/');
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                Transform t = root.transform;
                for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        /// <summary>Map recorded vertex index -> mesh vertex index, verifying world X/Z/Y. Falls back to nearest match.</summary>
        static int[] MatchVertices(Transform t, Vector3[] local, RoadRec r)
        {
            int n = r.x.Length; var map = new int[n];
            if (local.Length != n) throw new Exception(r.name + ": vertex count " + local.Length + " != recorded " + n + " (scene changed since the data was made)");
            var world = local.Select(v => t.TransformPoint(v)).ToArray();
            int direct = 0, fallback = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 w = world[i];
                if (Mathf.Abs(w.x - r.x[i]) < .02f && Mathf.Abs(w.z - r.z[i]) < .02f && Mathf.Abs(w.y - r.y0[i]) < .02f) { map[i] = i; direct++; continue; }
                int best = -1; float bd = float.MaxValue;
                for (int j = 0; j < n; j++)
                {
                    float d = Mathf.Abs(world[j].x - r.x[i]) + Mathf.Abs(world[j].z - r.z[i]) + Mathf.Abs(world[j].y - r.y0[i]);
                    if (d < bd) { bd = d; best = j; }
                }
                if (bd > .05f) throw new Exception(r.name + ": recorded vertex " + i + " (" + r.x[i] + "," + r.y0[i] + "," + r.z[i] + ") not found in the scene mesh (nearest " + bd + " m). Data is stale; nothing was saved.");
                map[i] = best; fallback++;
            }
            Debug.Log("MINI201 " + r.name + " vertices matched direct=" + direct + " nearest=" + fallback);
            return map;
        }

        static Mesh Rebuild(GameObject go, RoadRec r, bool dropCovered)
        {
            var mf = go.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) throw new Exception(r.name + " has no mesh");
            var src = mf.sharedMesh; var mesh = Object.Instantiate(src); mesh.name = r.name + "_MINI201";
            var v = mesh.vertices; var map = MatchVertices(go.transform, v, r);
            for (int i = 0; i < map.Length; i++)
            {
                var w = go.transform.TransformPoint(v[map[i]]); w.y = r.y[i]; v[map[i]] = go.transform.InverseTransformPoint(w);
            }
            mesh.vertices = v;
            if (dropCovered && r.dropTriangles != null && r.dropTriangles.Length > 0)
            {
                if (mesh.subMeshCount != 1 || mesh.triangles.Length / 3 != r.triangleCount)
                    Debug.LogWarning("MINI201 " + r.name + ": triangle layout differs from the data; covered triangles kept");
                else
                {
                    var drop = new HashSet<int>(r.dropTriangles); var tri = mesh.triangles; var keep = new List<int>(tri.Length);
                    for (int i = 0; i < tri.Length / 3; i++) if (!drop.Contains(i)) { keep.Add(tri[3 * i]); keep.Add(tri[3 * i + 1]); keep.Add(tri[3 * i + 2]); }
                    mesh.triangles = keep.ToArray();
                }
            }
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            Directory.CreateDirectory(Art);
            string path = Art + "/" + mesh.name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved != null) { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); mesh = saved; }
            else AssetDatabase.CreateAsset(mesh, path);
            mf.sharedMesh = mesh;
            var col = go.GetComponent<MeshCollider>(); if (col != null) { col.sharedMesh = null; col.sharedMesh = mesh; }
            return mesh;
        }

        public static void Apply()
        {
            var d = Load(); var log = new List<string> { "MINI-201 apply " + DateTime.Now.ToString("s") };
            var asphalt = AssetDatabase.LoadAssetAtPath<Material>(d.material);
            if (asphalt == null) throw new Exception("Missing material " + d.material);
            // resolve everything first so a missing object aborts before any change
            var targets = d.roads.Select(r => new { r, go = FindPath(r.path) }).ToList();
            var missing = targets.Where(t => t.go == null).Select(t => t.r.path).ToList();
            var terrainGo = FindPath(d.terrain.path);
            if (terrainGo == null) missing.Add(d.terrain.path);
            if (missing.Count > 0) throw new Exception("MINI201 objects not found (scene changed?): " + string.Join(", ", missing));
            foreach (var t in targets)
            {
                if (t.r.disable) { t.go.SetActive(false); log.Add(t.r.name + ": fully covered by another road -> disabled"); continue; }
                Rebuild(t.go, t.r, true);
                var mr = t.go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterials = Enumerable.Repeat(asphalt, Mathf.Max(1, mr.sharedMaterials.Length)).ToArray();
                log.Add(t.r.name + ": heights applied, dropped " + (t.r.dropTriangles == null ? 0 : t.r.dropTriangles.Length) + " covered triangles, material ExpansionAsphalt");
            }
            Rebuild(terrainGo, d.terrain, false); log.Add("terrain: " + d.terrain.y.Length + " vertex heights applied");
            foreach (var f in d.groundFollow ?? new FollowRec[0])
            {
                var go = FindPath(f.path);
                if (go == null) { log.Add("ground-follow: not found " + f.path); continue; }
                go.transform.position += Vector3.up * f.dy; log.Add("ground-follow " + f.path + " dy=" + f.dy.ToString("0.000"));
            }
            Physics.SyncTransforms();
            Directory.CreateDirectory(Out); File.WriteAllLines(Out + "/APPLY-LOG.txt", log);
            Debug.Log("MINI201_APPLIED roads=" + targets.Count + " follow=" + (d.groundFollow == null ? 0 : d.groundFollow.Length));
        }

        static bool IsRoad(Collider c)
        {
            string n = c.name;
            return n.StartsWith("Road_") || n.StartsWith("ExpansionRoad_") || n.StartsWith("ImportTrim_Road") || n.Contains("Junction") || n == "Driveable_Deck";
        }

        /// <summary>Three drive lines along every ribbon road, real collider raycasts every 0.25 m.</summary>
        public static bool Validate(string tag)
        {
            Physics.SyncTransforms();
            var lines = new List<string>(); float worst = 0; string worstAt = ""; int samples = 0, gaps = 0, terrainAbove = 0;
            foreach (var rec in Load().roads.Where(r => !r.disable && r.triangleCount == r.x.Length - 2 && !r.name.Contains("Junction")))
            {
                var go = FindPath(rec.path); if (go == null || !go.activeInHierarchy) continue;
                var f = go.GetComponent<MeshFilter>(); string n = rec.name;
                var v = f.sharedMesh.vertices; if (v.Length < 6 || v.Length % 2 != 0) continue; // ribbons: two vertices per cross-section
                var L = Enumerable.Range(0, v.Length / 2).Select(i => f.transform.TransformPoint(v[2 * i])).ToArray();
                var R = Enumerable.Range(0, v.Length / 2).Select(i => f.transform.TransformPoint(v[2 * i + 1])).ToArray();
                float roadWorst = 0;
                foreach (float lane in new[] { .25f, .5f, .75f })
                {
                    float prev = float.NaN;
                    for (int i = 0; i + 1 < L.Length; i++)
                    {
                        Vector3 a = Vector3.Lerp(L[i], R[i], lane), b = Vector3.Lerp(L[i + 1], R[i + 1], lane);
                        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / .25f));
                        for (int s = 0; s < steps; s++)
                        {
                            Vector3 p = Vector3.Lerp(a, b, s / (float)steps);
                            var hits = Physics.RaycastAll(new Vector3(p.x, p.y + 30, p.z), Vector3.down, 60).OrderBy(h => h.distance).ToArray();
                            var road = hits.Where(h => IsRoad(h.collider)).ToArray();
                            if (road.Length == 0) { gaps++; prev = float.NaN; continue; }
                            float y = road[0].point.y; samples++;
                            var ground = hits.Where(h => h.collider.name == "ExpansionTerrain").ToArray();
                            if (ground.Length > 0 && ground[0].point.y > y + .02f) terrainAbove++;
                            if (!float.IsNaN(prev)) { float st = Mathf.Abs(y - prev); roadWorst = Mathf.Max(roadWorst, st); if (st > worst) { worst = st; worstAt = n + " at " + p.ToString("F1"); } }
                            prev = y;
                        }
                    }
                }
                lines.Add(n + ": worst height change per 0.25 m = " + roadWorst.ToString("0.000") + " m");
            }
            bool ok = worst < .06f && terrainAbove < samples * .005f;
            lines.Insert(0, string.Format("{0}: samples={1} gaps={2} terrainAboveRoad={3} worstStep={4:0.000} m ({5}) => {6}", tag, samples, gaps, terrainAbove, worst, worstAt, ok ? "PASS" : "CHECK"));
            Directory.CreateDirectory(Out); File.WriteAllLines(Out + "/VALIDATION-" + tag + ".txt", lines);
            Debug.Log("MINI201_VALIDATION " + lines[0]);
            return ok;
        }

        static readonly (string name, Vector3 target, Vector3 offset)[] Spots =
        {
            ("E0_E1_overlap", new Vector3(20, 0, -80), new Vector3(-14, 6, -26)),
            ("E0_E5_junction", new Vector3(-37, 0, -62), new Vector3(16, 6, -14)),
            ("backstreet_crossing", new Vector3(2, 0, -172), new Vector3(-16, 5, 18)),
            ("junction_patch1", new Vector3(231, 0, 75), new Vector3(-18, 6, -14)),
            ("dog_life", new Vector3(-63, 0, -142), new Vector3(14, 5, -14)),
        };

        static void Capture(string tag)
        {
            Physics.SyncTransforms();
            Directory.CreateDirectory(Mini193RoadAudit.Out + "/Renders");
            var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Select(c => (c, c.enabled)).ToList();
            var fog = RenderSettings.fog; var ambient = RenderSettings.ambientLight;
            foreach (var s in Spots)
            {
                Vector3 t = s.target;
                if (Physics.Raycast(new Vector3(t.x, 200, t.z), Vector3.down, out var hit, 400)) t.y = hit.point.y;
                Mini193RoadAudit.Render(tag + "_" + s.name, t + s.offset, t + Vector3.up * .3f, false, 0, true);
            }
            // Mini193RoadAudit.Render writes into its own Out folder; move this task's captures next to the MINI-201 logs
            string src = Mini193RoadAudit.Out + "/Renders";
            foreach (var s in Spots)
            {
                string f = src + "/" + tag + "_" + s.name + ".png", dst = Out + "/Renders/" + tag + "_" + s.name + ".png";
                if (File.Exists(f)) { if (File.Exists(dst)) File.Delete(dst); File.Move(f, dst); }
            }
            foreach (var (c, was) in cams) if (c != null) c.enabled = was;   // the render helper disables scene cameras
            RenderSettings.fog = fog; RenderSettings.ambientLight = ambient;
        }
    }
}
