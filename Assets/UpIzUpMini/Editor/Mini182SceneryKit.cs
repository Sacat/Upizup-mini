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
    /// MINI-182 stage 4: import the additive scenery export (Scenery*.json) as LOD prefabs under
    /// Assets/UpIzUpMini/Art/Environment/Mini182/Prefabs/Scenery, sharing the Mini142 Palette.mat. Palette cells
    /// 0..29 (shipped + house colours) are verified unchanged before the superset palette replaces the shared one.
    /// Preview() drops the trees into the isolated copy scene TEMPORARILY, renders, and does not save.
    /// </summary>
    public static class Mini182SceneryKit
    {
        const string Live = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Copy = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini182";
        const string OldArt = "Assets/UpIzUpMini/Art/Environment/Mini142";
        const string Export = "Logs/Tasks/MINI-182/Export";
        const string Out = "Logs/Tasks/MINI-182";

        [Serializable] class Part { public string name; public Vector3[] vertices; public Vector3[] normals; public int[] triangles; public Vector2[] uv; }
        [Serializable] class Detail { public string name; public Part[] parts; }
        [Serializable] class Model { public string name; public Part[] parts; public Vector3 dimensions; public Detail[] lods; public string family; public float bodyHeight; public float footprintW; public float footprintD; }

        static string Sha(string p) { using var s = System.Security.Cryptography.SHA256.Create(); using var f = File.OpenRead(p); return BitConverter.ToString(s.ComputeHash(f)).Replace("-", ""); }

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

        [MenuItem("Up Iz Up Mini/MINI-182/Import Scenery Kit")]
        public static void Import()
        {
            try { ImportInner(); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static void ImportInner()
        {
            Directory.CreateDirectory(Art + "/Prefabs/Scenery");
            var log = new List<string> { "MINI-182 scenery import" };
            string liveHash = Sha(Live); log.Add("live sha before=" + liveHash);
            string oldPng = OldArt + "/palette.png", newPng = Export + "/palette.png";
            var a = new Texture2D(2, 2); a.LoadImage(File.ReadAllBytes(oldPng)); var b = new Texture2D(2, 2); b.LoadImage(File.ReadAllBytes(newPng));
            int oldCells = 30;
            for (int c = 0; c < oldCells; c++)
            {
                int px = c % 8 * 16 + 8, py = (c / 8) * 16 + 8; Color32 ca = a.GetPixel(px, py), cb = b.GetPixel(px, py);
                if (ca.r != cb.r || ca.g != cb.g || ca.b != cb.b) throw new Exception("SAFETY STOP: palette cell " + c + " changed; not overwriting.");
            }
            log.Add("palette cells 0..29 verified identical");
            if (!File.ReadAllBytes(oldPng).SequenceEqual(File.ReadAllBytes(newPng)))
            {
                File.Copy(newPng, oldPng, true); AssetDatabase.Refresh();
                var ti = (TextureImporter)AssetImporter.GetAtPath(oldPng);
                ti.textureType = TextureImporterType.Default; ti.sRGBTexture = true; ti.mipmapEnabled = false; ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed; ti.maxTextureSize = 128; ti.SaveAndReimport();
                log.Add("palette.png replaced by superset (appended cells only)");
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OldArt + "/Palette.mat");
            mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(oldPng);

            foreach (var json in Directory.GetFiles(Export, "Scenery*.json").OrderBy(f => f))
            {
                var m = JsonUtility.FromJson<Model>(File.ReadAllText(json));
                var m0 = ImportMesh(m.name + "_Body", m.parts[0]); var m1 = ImportMesh(m.name + "_LOD1_Body", m.lods[0].parts[0]);
                var root = new GameObject(m.name);
                GameObject Child(string n, Mesh me)
                {
                    var g = new GameObject(n); g.transform.SetParent(root.transform, false); g.AddComponent<MeshFilter>().sharedMesh = me;
                    var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return g;
                }
                var l0 = Child("Tree_LOD0", m0).GetComponent<Renderer>(); var l1 = Child("Tree_LOD1", m1).GetComponent<Renderer>();
                var group = root.AddComponent<LODGroup>(); group.SetLODs(new[] { new LOD(.06f, new[] { l0 }), new LOD(.008f, new[] { l1 }) }); group.RecalculateBounds();
                if (m.family == "palm" || m.family == "broadleaf")
                {
                    var cap = root.AddComponent<CapsuleCollider>(); cap.direction = 1; cap.radius = m.family == "palm" ? .28f : .3f; cap.height = 3f; cap.center = new Vector3(0, 1.5f, 0);
                }
                PrefabUtility.SaveAsPrefabAsset(root, Art + "/Prefabs/Scenery/" + m.name + ".prefab"); UnityEngine.Object.DestroyImmediate(root);
                log.Add($"{m.name}: family={m.family} tris LOD0={m0.triangles.Length / 3} LOD1={m1.triangles.Length / 3} height={m.bodyHeight:F1} crown={m.footprintW:F1}");
            }
            AssetDatabase.SaveAssets();
            if (Sha(Live) != liveHash) throw new Exception("SAFETY STOP: live scene changed.");
            log.Add("live sha after=" + Sha(Live));
            Directory.CreateDirectory(Out); File.WriteAllLines(Out + "/SceneryImport-Report.txt", log);
            Debug.Log("MINI182SCENERY_IMPORT_PASS " + string.Join(" | ", log));
        }

        [MenuItem("Up Iz Up Mini/MINI-182/Preview Scenery Kit (temporary, not saved)")]
        public static void Preview()
        {
            try
            {
                Directory.CreateDirectory(Out + "/Renders");
                EditorSceneManager.OpenScene(Copy, OpenSceneMode.Single); Physics.SyncTransforms();
                var pfs = Directory.GetFiles(Art + "/Prefabs/Scenery", "*.prefab").OrderBy(f => f).Select(f => AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace('\\', '/'))).ToList();
                var terrain = UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.enabled && c.name.Contains("Terrain")).ToList();
                float G(float x, float z) { float best = float.NegativeInfinity; foreach (var t in terrain) if (t.Raycast(new Ray(new Vector3(x, 500, z), Vector3.down), out var h, 900)) best = Mathf.Max(best, h.point.y); return best; }
                Vector3 c0 = new Vector3(70f, 0, -196f); float x0 = c0.x - (pfs.Count - 1) * 8f / 2f;
                var placed = new List<GameObject>();
                for (int i = 0; i < pfs.Count; i++) { float x = x0 + i * 8f; float y = G(x, c0.z); var g = (GameObject)PrefabUtility.InstantiatePrefab(pfs[i]); g.transform.position = new Vector3(x, y, c0.z); placed.Add(g); }
                var go = new GameObject("SceneryCam"); var cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f); cam.farClipPlane = 2000; cam.fieldOfView = 55;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white * .85f;
                var rt = new RenderTexture(1600, 1000, 24); cam.targetTexture = rt;
                void Shot(string n, Vector3 pos, Vector3 look)
                {
                    cam.transform.position = pos; cam.transform.LookAt(look); cam.Render();
                    var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply();
                    File.WriteAllBytes(Out + "/Renders/" + n + ".png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
                }
                var mid = new Vector3(c0.x, G(c0.x, c0.z) + 3f, c0.z);
                Shot("t1_trees_front", mid + new Vector3(0, 2, -34), mid);
                Shot("t2_trees_oblique", mid + new Vector3(-24, 9, -26), mid);
                Shot("t3_trees_close", placed[0].transform.position + new Vector3(4, 4, -14), placed[0].transform.position + Vector3.up * 5f);
                Debug.Log("MINI182SCENERY_PREVIEW_DONE prefabs=" + pfs.Count);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }
}
