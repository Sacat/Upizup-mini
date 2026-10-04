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
    /// <summary>MINI-193 read-only road junction audit + renders for the new-map scene. Never saves.
    /// Env MINI193_SCENE overrides the scene; MINI193_OUT the output folder.</summary>
    public static class Mini193RoadAudit
    {
        public static string ScenePath { get { return Environment.GetEnvironmentVariable("MINI193_SCENE") ?? "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity"; } }
        public static string Out { get { return Environment.GetEnvironmentVariable("MINI193_OUT") ?? "Logs/Tasks/MINI-193/Roads"; } }

        public class RoadInfo { public string name; public MeshFilter filter; public Vector3[] line; public float half; public bool strip; }

        public static List<RoadInfo> ReadRoads()
        {
            var list = new List<RoadInfo>();
            foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                string n = f.name;
                if (!(n.StartsWith("Road_") || n.StartsWith("ExpansionRoad_") || n.StartsWith("ImportTrim_Road") || n.IndexOf("Junction", StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                var mr = f.GetComponent<MeshRenderer>(); if (mr == null || !mr.enabled) continue;
                var v = f.sharedMesh != null ? f.sharedMesh.vertices : null;
                if (v == null || v.Length < 4) continue;
                bool strip = v.Length % 2 == 0 && n.IndexOf("Junction", StringComparison.OrdinalIgnoreCase) < 0;
                var info = new RoadInfo { name = n, filter = f, strip = strip };
                if (strip)
                {
                    info.line = Enumerable.Range(0, v.Length / 2).Select(i => f.transform.TransformPoint((v[2 * i] + v[2 * i + 1]) * .5f)).ToArray();
                    info.half = Vector3.Distance(f.transform.TransformPoint(v[0]), f.transform.TransformPoint(v[1])) * .5f;
                }
                else { info.line = new[] { f.transform.TransformPoint(f.sharedMesh.bounds.center) }; info.half = 0; }
                list.Add(info);
            }
            return list;
        }

        [MenuItem("Up Iz Up Mini/MINI-193/Audit And Render Roads")]
        public static void Run()
        {
            try
            {
                Directory.CreateDirectory(Out + "/Renders");
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var roads = ReadRoads();
                var report = new List<string> { "roads=" + roads.Count(r => r.strip) + " patches=" + roads.Count(r => !r.strip) };
                foreach (var r in roads.Where(x => x.strip)) report.Add(string.Format("{0}: pts={1} width={2:0.0} start=({3:0},{4:0.0},{5:0}) end=({6:0},{7:0.0},{8:0})", r.name, r.line.Length, r.half * 2, r.line[0].x, r.line[0].y, r.line[0].z, r.line[r.line.Length - 1].x, r.line[r.line.Length - 1].y, r.line[r.line.Length - 1].z));
                // junction candidates: every road end that lies within 9 m of another road centreline (or crosses it)
                var spots = new List<Vector3>();
                foreach (var a in roads.Where(x => x.strip))
                    foreach (var endIndex in new[] { 0, a.line.Length - 1 })
                    {
                        Vector3 e = a.line[endIndex];
                        foreach (var b in roads.Where(x => x.strip && x != a))
                            for (int j = 0; j + 1 < b.line.Length; j++)
                            {
                                float d = DistToSeg(e, b.line[j], b.line[j + 1]);
                                if (d < 9f && !spots.Any(s => Vector3.Distance(s, e) < 14f)) { spots.Add(e); report.Add("JUNCTION " + a.name + " end" + endIndex + " -> " + b.name + " d=" + d.ToString("0.0") + " at (" + e.x.ToString("0") + "," + e.y.ToString("0.0") + "," + e.z.ToString("0") + ")"); }
                            }
                    }
                File.WriteAllLines(Out + "/AUDIT.txt", report);
                Render("network", new Vector3(105, 260, -60), new Vector3(105, 0, -60), true, 200, false);
                int k = 0;
                int max = int.Parse(Environment.GetEnvironmentVariable("MINI193_MAXSPOTS") ?? "10");
                foreach (var s in spots.Take(max))
                {
                    Render("j" + k + "_top", s + Vector3.up * 60, s, true, 22, false);
                    Render("j" + k + "_low", s + new Vector3(10, 4.5f, -12), s + Vector3.up * .3f, false, 0, true);
                    k++;
                }
                Debug.Log("MINI193_ROAD_AUDIT roads=" + roads.Count + " junctions=" + spots.Count);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        public static float DistToSeg(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pa = new Vector2(p.x, p.z), aa = new Vector2(a.x, a.z), bb = new Vector2(b.x, b.z);
            Vector2 ab = bb - aa; float t = ab.sqrMagnitude < 1e-6f ? 0 : Mathf.Clamp01(Vector2.Dot(pa - aa, ab) / ab.sqrMagnitude);
            return Vector2.Distance(pa, aa + ab * t);
        }

        public static void Render(string name, Vector3 pos, Vector3 target, bool ortho, float size, bool perspective)
        {
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) cam.enabled = false;
            RenderSettings.fog = false; RenderSettings.ambientLight = new Color(.66f, .69f, .72f);
            var go = new GameObject("MINI193Camera"); var c = go.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(.55f, .70f, .86f); c.farClipPlane = 1800;
            c.orthographic = ortho; c.orthographicSize = size; c.fieldOfView = 55;
            go.transform.position = pos; go.transform.rotation = ortho ? Quaternion.Euler(90, 0, 0) : Quaternion.LookRotation(target - pos);
            var rt = new RenderTexture(1600, 1000, 24); c.targetTexture = rt; c.Render();
            RenderTexture.active = rt; var tx = new Texture2D(1600, 1000, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tx.Apply();
            File.WriteAllBytes(Out + "/Renders/" + name + ".png", tx.EncodeToPNG());
            RenderTexture.active = null; c.targetTexture = null; Object.DestroyImmediate(go); Object.DestroyImmediate(rt);
        }
    }
}
