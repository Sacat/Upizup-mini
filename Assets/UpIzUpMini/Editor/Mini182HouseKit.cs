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
    /// MINI-182 house kit import. Imports the additive house_export.py output (Logs/Tasks/MINI-182/Export) into
    /// Assets/UpIzUpMini/Art/Environment/Mini182, appends new palette cells to the SHARED Mini142 palette (existing cells
    /// verified pixel-identical first), builds one prefab per wall colour, and places a test row in the isolated copy
    /// scene GrandBayProof_HouseEnhance.unity. The live scene is never saved (hash checked).
    /// </summary>
    public static class Mini182HouseKit
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini182";
        const string OldArt = "Assets/UpIzUpMini/Art/Environment/Mini142";
        const string Export = "Logs/Tasks/MINI-182/Export";
        const string Out = "Logs/Tasks/MINI-182";
        const string Root = "MINI182_KitTest";

        [Serializable] class Part { public string name; public Vector3[] vertices; public Vector3[] normals; public int[] triangles; public Vector2[] uv; }
        [Serializable] class Detail { public string name; public Part[] parts; }
        [Serializable] class Model { public string name; public Part[] parts; public Vector3 dimensions; public Detail[] lods; }

        static string Sha(string p)
        {
            using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p);
            return BitConverter.ToString(s.ComputeHash(f)).Replace("-", "");
        }

        static Mesh ImportMesh(string key, Part p)
        {
            string path = Art + "/" + key + ".asset";
            var me = AssetDatabase.LoadAssetAtPath<Mesh>(path); bool fresh = me == null;
            if (fresh) me = new Mesh(); else me.Clear();
            me.name = key; me.indexFormat = p.vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            me.vertices = p.vertices; me.triangles = p.triangles; me.normals = p.normals; me.uv = p.uv; me.RecalculateBounds(); me.RecalculateTangents();
            if (fresh) AssetDatabase.CreateAsset(me, path); else EditorUtility.SetDirty(me);
            return me;
        }

        static Color32 Cell(Texture2D t, int cell) => t.GetPixel(cell % 8 * 16 + 8, (cell / 8) * 16 + 8);

        static void ImportPalette(List<string> log)
        {
            string oldPng = OldArt + "/palette.png", newPng = Export + "/palette.png";
            var a = new Texture2D(2, 2); a.LoadImage(File.ReadAllBytes(oldPng));
            var b = new Texture2D(2, 2); b.LoadImage(File.ReadAllBytes(newPng));
            int existing = 20; // cells 0..19 are the shipped MINI-142 palette
            for (int c = 0; c < existing; c++)
            {
                // Unity GetPixel is bottom-up: cell row r sits at y = r*16..r*16+15 (matches the exporter's UV v)
                int px = c % 8 * 16 + 8, py = (c / 8) * 16 + 8;
                if ((Color32)a.GetPixel(px, py) is var ca && (Color32)b.GetPixel(px, py) is var cb && (ca.r != cb.r || ca.g != cb.g || ca.b != cb.b))
                    throw new Exception("SAFETY STOP: palette cell " + c + " differs between the shipped palette and the new one; not overwriting.");
            }
            log.Add("existing palette cells 0..19 verified identical");
            if (File.ReadAllBytes(oldPng).SequenceEqual(File.ReadAllBytes(newPng))) { log.Add("palette already up to date"); return; }
            File.Copy(newPng, oldPng, true); AssetDatabase.Refresh();
            var ti = (TextureImporter)AssetImporter.GetAtPath(oldPng);
            ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = false; ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 128; ti.SaveAndReimport();
            log.Add("palette.png replaced by superset (appended cells only)");
        }


        [MenuItem("Up Iz Up Mini/MINI-182/Render Kit Test Row")]
        public static void Render()
        {
            EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single); Physics.SyncTransforms();
            var root = GameObject.Find(Root); if (root == null) throw new Exception("test row missing");
            var rs = root.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var go = new GameObject("KitCam"); var cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f); cam.farClipPlane = 2000; cam.fieldOfView = 55;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white * .85f;
            var rt = new RenderTexture(1600, 1000, 24); cam.targetTexture = rt;
            void Shot(string n, Vector3 pos, Vector3 look, bool ortho = false, float size = 30)
            {
                cam.orthographic = ortho; cam.orthographicSize = size; cam.transform.position = pos; cam.transform.LookAt(look, ortho ? Vector3.forward : Vector3.up); cam.Render();
                var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply();
                File.WriteAllBytes(Out + "/Renders/" + n + ".png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            }
            var first = root.transform.GetChild(0).position; var fwd = -root.transform.GetChild(0).forward; // toward road
            var c = b.center;
            Shot("k1_row_street", c + fwd * 24 + Vector3.up * 3f, c + Vector3.up * 3.5f);
            Shot("k2_row_oblique", c + fwd * 30 + Vector3.Cross(Vector3.up, fwd) * 26 + Vector3.up * 14, c + Vector3.up * 3f);
            Shot("k3_first_close", first + fwd * 15 + Vector3.up * 3f, first + Vector3.up * 3.5f);
            Shot("k4_top", c + Vector3.up * 200, c, true, 22);
            // in-context: from the Lalay road, looking at the row with neighbouring approved houses
            Shot("k5_context", c + fwd * 45 + Vector3.Cross(Vector3.up, fwd) * 40 + Vector3.up * 20, c);
            Debug.Log("MINI182KIT_RENDER_DONE"); if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Import House Kit And Test Row")]
        public static void Run()
        {
            try { RunInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void RunInner()
        {
            Directory.CreateDirectory(Art); Directory.CreateDirectory(Art + "/Prefabs"); Directory.CreateDirectory(Out + "/Renders");
            var log = new List<string> { "MINI-182 house kit import" };
            string liveHash = Sha(Live); log.Add("live sha before=" + liveHash);
            ImportPalette(log);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OldArt + "/Palette.mat");
            if (mat == null) throw new Exception("Shared Palette.mat missing at " + OldArt);
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(OldArt + "/palette.png");

            var prefabs = new List<GameObject>();
            foreach (var json in Directory.GetFiles(Export, "House*.json").OrderBy(f => f))
            {
                var m = JsonUtility.FromJson<Model>(File.ReadAllText(json));
                var m0 = ImportMesh(m.name + "_Body", m.parts[0]); var m1 = ImportMesh(m.name + "_LOD1_Body", m.lods[0].parts[0]);
                var root = new GameObject(m.name);
                GameObject Child(string n, Mesh me) { var g = new GameObject(n); g.transform.SetParent(root.transform, false); g.AddComponent<MeshFilter>().sharedMesh = me; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; return g; }
                var l0 = Child("House_LOD0", m0).GetComponent<Renderer>(); var l1 = Child("House_LOD1", m1).GetComponent<Renderer>();
                var group = root.AddComponent<LODGroup>(); group.SetLODs(new[] { new LOD(.11f, new[] { l0 }), new LOD(.008f, new[] { l1 }) }); group.RecalculateBounds();
                var box = root.AddComponent<BoxCollider>(); box.center = new Vector3(0, .30f + 5.5f * .5f, 0); box.size = new Vector3(6.2f, 5.5f, 5.2f);
                var bx = root.AddComponent<BoxCollider>(); bx.center = new Vector3(0, .16f, 0); bx.size = new Vector3(6.34f, .32f, 5.34f);
                var pf = PrefabUtility.SaveAsPrefabAsset(root, Art + "/Prefabs/" + m.name + ".prefab"); UnityEngine.Object.DestroyImmediate(root);
                prefabs.Add(pf); log.Add($"{m.name}: tris LOD0={m0.triangles.Length / 3} LOD1={m1.triangles.Length / 3} size={m.dimensions}");
            }
            AssetDatabase.SaveAssets();

            // isolated copy scene
            var liveScene = EditorSceneManager.OpenScene(Live, OpenSceneMode.Single);
            if (!File.Exists(Copy)) { if (!EditorSceneManager.SaveScene(liveScene, Copy, false)) throw new Exception("copy save failed"); log.Add("created copy scene " + Copy); }
            else EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single);
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            var scene = EditorSceneManager.GetActiveScene();
            var old = GameObject.Find(Root); if (old != null) UnityEngine.Object.DestroyImmediate(old);

            Physics.SyncTransforms();
            var terrain = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.enabled && c.name.Contains("Terrain")).ToList();
            var roads = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.enabled && (c.name.StartsWith("Road_") || c.name.StartsWith("ExpansionRoad_") || c.name.StartsWith("ImportTrim_Road"))).ToList();
            var roadPts = new List<Vector2>();
            foreach (var r in roads) { var me = r.sharedMesh; if (me == null) continue; foreach (var v in me.vertices) { var w = r.transform.TransformPoint(v); roadPts.Add(new Vector2(w.x, w.z)); } }
            var occupied = new List<Bounds>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.enabled && r.gameObject.activeInHierarchy && r.bounds.size.y > 1.2f && r.bounds.size.y < 14f && r.bounds.size.x < 20f && r.bounds.size.z < 20f) occupied.Add(r.bounds);
            float Ground(float x, float z) { float best = float.NegativeInfinity; foreach (var t in terrain) if (t.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out var h, 900)) best = Mathf.Max(best, h.point.y); return best; }
            Vector2 NearestRoad(Vector2 p, out float d) { var best = roadPts[0]; d = float.MaxValue; foreach (var q in roadPts) { float dd = (q - p).sqrMagnitude; if (dd < d) { d = dd; best = q; } } d = Mathf.Sqrt(d); return best; }
            bool Free(Vector3 c, float half) { var b = new Bounds(c, new Vector3(half * 2, 20, half * 2)); foreach (var o in occupied) if (b.Intersects(o) && o.center.y - o.extents.y < c.y + 12) return false; return true; }

            var rootGo = new GameObject(Root);
            int placed = 0; var report = new List<string>(); int noGround = 0, roadFar = 0, occ = 0;
            // search the open ground south of the Lalay main road for a straight row of 4 slots facing the road
            for (float z = -158; z >= -215 && placed == 0; z -= 2)
                for (float x = 20; x <= 120 && placed == 0; x += 2)
                {
                    var slots = new List<Vector3>(); bool ok = true;
                    for (int i = 0; i < prefabs.Count && ok; i++)
                    {
                        float sx = x + i * 9f; float gy = Ground(sx, z);
                        if (float.IsNegativeInfinity(gy)) { ok = false; noGround++; break; }
                        NearestRoad(new Vector2(sx, z), out float rd);
                        if (rd < 9f || rd > 16f) { ok = false; roadFar++; } else if (!Free(new Vector3(sx, gy, z), 5.5f)) { ok = false; occ++; }
                        slots.Add(new Vector3(sx, gy, z));
                    }
                    if (!ok) continue;
                    for (int i = 0; i < slots.Count; i++)
                    {
                        var p = slots[i]; var rp = NearestRoad(new Vector2(p.x, p.z), out _);
                        var f = new Vector3(rp.x - p.x, 0, rp.y - p.z).normalized;
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], rootGo.transform);
                        inst.transform.SetPositionAndRotation(new Vector3(p.x, p.y + .01f, p.z), Quaternion.LookRotation(-f, Vector3.up));
                        report.Add($"{prefabs[i].name} at ({p.x:F1},{p.y:F1},{p.z:F1}) facing road"); placed++;
                    }
                }
            if (placed == 0) throw new Exception($"No free 4-house row found near the Lalay south side. terrainColliders={terrain.Count} roads={roads.Count} roadPts={roadPts.Count} occupied={occupied.Count} noGround={noGround} roadDistFail={roadFar} occupiedFail={occ}");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            log.AddRange(report);
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed at end.");
            log.Add("live sha after=" + Sha(Live)); log.Add("copy scene in build settings=" + EditorBuildSettings.scenes.Any(s => s.enabled && s.path == Copy));
            File.WriteAllLines(Out + "/KitImport-Report.txt", log);
            Debug.Log("MINI182KIT_PASS " + string.Join(" | ", log));
        }
    }
}
