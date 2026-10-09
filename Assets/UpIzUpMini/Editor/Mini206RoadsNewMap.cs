// MINI-206 (cloud design lane) - written without a Unity Editor, NOT COMPILED HERE. Review before use.
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
    /// MINI-206 Stage 3: applies the new-map road solve (Docs/WorkPackets/MINI-206/newmap_apply.json, made offline from
    /// GrandBayProof_HouseEnhance.unity by Tools/RoadPipeline/mini206_newmap) to a COPY of the new map:
    ///  - every road vertex gets its solved world Y (X/Z never change); each recorded vertex is matched by world X/Z/Y (2 cm)
    ///    before anything changes, so stale data aborts without saving;
    ///  - asphalt roads get ExpansionAsphalt; roads whose record names a material (the DirtTrack farm spur) keep that material;
    ///  - covered duplicate triangles (incl. road triangles lying on a bridge deck) are dropped; a fully covered road is disabled;
    ///  - the terrain (39,160 vertices) gets its fitted heights; MINI193_RoadJunctions is NOT touched (re-run Mini193RoadJunctions
    ///    afterwards so the junction patches follow the new road heights);
    ///  - new meshes go to Assets/UpIzUpMini/Art/Environment/Mini206Roads; originals are kept.
    /// Then it validates three drive lines per ribbon road with real collider raycasts (pass: step &lt; 6 cm per 0.25 m and
    /// terrain above road &lt; 0.5 %).
    /// </summary>
    public static class Mini206RoadsNewMap
    {
        const string Src = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance_RoadFix206.unity";
        const string Data = "Docs/WorkPackets/MINI-206/newmap_apply.json";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini206Roads";
        const string Out = "Logs/Tasks/MINI-206";

        [Serializable] public class RoadRec { public string name, path, material; public float[] x, z, y0, y; public int[] dropTriangles; public bool disable; public int triangleCount, priority; }
        [Serializable] public class ApplyData { public string task, scene, material; public int terrainVertexCount; public RoadRec[] roads; public RoadRec terrain; }

        [MenuItem("Up Iz Up Mini/MINI-206/Roads: Preview On Copy Of New Map (safe)")]
        public static void PreviewOnCopy()
        {
            try
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Copy) != null) AssetDatabase.DeleteAsset(Copy);
                if (!AssetDatabase.CopyAsset(Src, Copy)) throw new Exception("Could not copy " + Src);
                var scene = EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
                Apply();
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                bool ok = Validate("copy");
                Debug.Log((ok ? "MINI206_ROADS_PASS " : "MINI206_ROADS_CHECK ") + Copy + "  -> now run Mini193RoadJunctions on this copy");
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

        static int[] MatchVertices(Transform t, Vector3[] local, RoadRec r)
        {
            int n = r.x.Length; var map = new int[n];
            if (local.Length != n) throw new Exception(r.name + ": vertex count " + local.Length + " != recorded " + n + " (scene changed since the data was made)");
            var world = local.Select(v => t.TransformPoint(v)).ToArray();
            for (int i = 0; i < n; i++)
            {
                Vector3 w = world[i];
                if (Mathf.Abs(w.x - r.x[i]) < .02f && Mathf.Abs(w.z - r.z[i]) < .02f && Mathf.Abs(w.y - r.y0[i]) < .02f) { map[i] = i; continue; }
                int best = -1; float bd = float.MaxValue;
                for (int j = 0; j < n; j++)
                {
                    float d = Mathf.Abs(world[j].x - r.x[i]) + Mathf.Abs(world[j].z - r.z[i]) + Mathf.Abs(world[j].y - r.y0[i]);
                    if (d < bd) { bd = d; best = j; }
                }
                if (bd > .05f) throw new Exception(r.name + ": recorded vertex " + i + " not found in the scene mesh (nearest " + bd + " m). Data is stale; nothing was saved.");
                map[i] = best;
            }
            return map;
        }

        static void Rebuild(GameObject go, RoadRec r, bool dropCovered)
        {
            var mf = go.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) throw new Exception(r.name + " has no mesh");
            var mesh = Object.Instantiate(mf.sharedMesh); mesh.name = r.name + "_MINI206";
            var v = mesh.vertices; var map = MatchVertices(go.transform, v, r);
            for (int i = 0; i < map.Length; i++) { var w = go.transform.TransformPoint(v[map[i]]); w.y = r.y[i]; v[map[i]] = go.transform.InverseTransformPoint(w); }
            mesh.vertices = v;
            if (dropCovered && r.dropTriangles != null && r.dropTriangles.Length > 0 && mesh.subMeshCount == 1 && mesh.triangles.Length / 3 == r.triangleCount)
            {
                var drop = new HashSet<int>(r.dropTriangles); var tri = mesh.triangles; var keep = new List<int>(tri.Length);
                for (int i = 0; i < tri.Length / 3; i++) if (!drop.Contains(i)) { keep.Add(tri[3 * i]); keep.Add(tri[3 * i + 1]); keep.Add(tri[3 * i + 2]); }
                mesh.triangles = keep.ToArray();
            }
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            Directory.CreateDirectory(Art);
            string path = Art + "/" + mesh.name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved != null) { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); mesh = saved; } else AssetDatabase.CreateAsset(mesh, path);
            mf.sharedMesh = mesh;
            var col = go.GetComponent<MeshCollider>(); if (col != null) { col.sharedMesh = null; col.sharedMesh = mesh; }
        }

        public static void Apply()
        {
            var d = Load(); var log = new List<string> { "MINI-206 new-map road apply " + DateTime.Now.ToString("s") };
            var asphalt = AssetDatabase.LoadAssetAtPath<Material>(d.material);
            if (asphalt == null) throw new Exception("Missing material " + d.material);
            var targets = d.roads.Select(r => new { r, go = FindPath(r.path) }).ToList();
            var missing = targets.Where(t => t.go == null).Select(t => t.r.path).ToList();
            var terrainGo = FindPath(d.terrain.path); if (terrainGo == null) missing.Add(d.terrain.path);
            if (missing.Count > 0) throw new Exception("MINI206 objects not found (scene changed?): " + string.Join(", ", missing));
            int tv = terrainGo.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            if (d.terrainVertexCount > 0 && tv != d.terrainVertexCount) throw new Exception("ExpansionTerrain has " + tv + " vertices, data expects " + d.terrainVertexCount + " (map changed; re-run the offline solve)");
            foreach (var t in targets)
            {
                if (t.r.disable) { t.go.SetActive(false); log.Add(t.r.name + ": fully covered by another road -> disabled"); continue; }
                Rebuild(t.go, t.r, true);
                Material m = asphalt; string label = "ExpansionAsphalt";
                if (!string.IsNullOrEmpty(t.r.material)) { var keep = AssetDatabase.LoadAssetAtPath<Material>(t.r.material); if (keep != null) { m = keep; label = Path.GetFileNameWithoutExtension(t.r.material) + " (kept)"; } }
                var mr = t.go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterials = Enumerable.Repeat(m, Mathf.Max(1, mr.sharedMaterials.Length)).ToArray();
                log.Add(t.r.name + ": heights applied, dropped " + (t.r.dropTriangles == null ? 0 : t.r.dropTriangles.Length) + " covered triangles, material " + label);
            }
            Rebuild(terrainGo, d.terrain, false); log.Add("terrain: " + d.terrain.y.Length + " vertex heights applied");
            Physics.SyncTransforms();
            Directory.CreateDirectory(Out); File.WriteAllLines(Out + "/ROADS-APPLY-LOG.txt", log);
            Debug.Log("MINI206_ROADS_APPLIED roads=" + targets.Count);
        }

        static bool IsRoad(Collider c)
        {
            string n = c.name;
            return n.StartsWith("Road_") || n.StartsWith("ExpansionRoad_") || n.StartsWith("ImportTrim_Road") || n.Contains("Junction") || n == "Driveable_Deck";
        }

        public static bool Validate(string tag)
        {
            Physics.SyncTransforms();
            var lines = new List<string>(); float worst = 0; string worstAt = ""; int samples = 0, gaps = 0, terrainAbove = 0;
            foreach (var rec in Load().roads.Where(r => !r.disable && !r.name.Contains("Junction")))
            {
                var go = FindPath(rec.path); if (go == null || !go.activeInHierarchy) continue;
                var f = go.GetComponent<MeshFilter>(); var v = f.sharedMesh.vertices; if (v.Length < 6 || v.Length % 2 != 0) continue;
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
                            if (!float.IsNaN(prev)) { float st = Mathf.Abs(y - prev); roadWorst = Mathf.Max(roadWorst, st); if (st > worst) { worst = st; worstAt = rec.name + " at " + p.ToString("F1"); } }
                            prev = y;
                        }
                    }
                }
                lines.Add(rec.name + ": worst height change per 0.25 m = " + roadWorst.ToString("0.000") + " m");
            }
            bool ok = worst < .06f && terrainAbove < samples * .005f;
            lines.Insert(0, string.Format("{0}: samples={1} gaps={2} terrainAboveRoad={3} worstStep={4:0.000} m ({5}) => {6}", tag, samples, gaps, terrainAbove, worst, worstAt, ok ? "PASS" : "CHECK"));
            Directory.CreateDirectory(Out); File.WriteAllLines(Out + "/ROADS-VALIDATION-" + tag + ".txt", lines);
            Debug.Log("MINI206_ROADS_VALIDATION " + lines[0]);
            return ok;
        }
    }
}
