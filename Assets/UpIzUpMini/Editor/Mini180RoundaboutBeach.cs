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
    /// <summary>Creates the user-authorized, compact beach access beside the
    /// expansion roundabout. It never touches the playable GrandBayProof scene.</summary>
    public static class Mini180RoundaboutBeach
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        const string Art = "Assets/UpIzUpMini/Art/Environment/Mini180";
        const string Out = "Logs/Tasks/MINI-180";
        const float StartZ = -58f, EndZ = -28f, BeachWidth = 8f;

        [MenuItem("Up Iz Up Mini/MINI-180/Apply Roundabout Beach")]
        public static void Apply()
        {
            try
            {
                Directory.CreateDirectory(Out + "/Renders"); Directory.CreateDirectory(Art);
                var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
                var terrain = GameObject.Find("ExpansionTerrain")?.GetComponent<MeshCollider>() ?? throw new Exception("ExpansionTerrain collider missing.");
                var wall = GameObject.Find("MINI174_BaySeawall")?.transform ?? throw new Exception("MINI174 seawall missing.");
                foreach (Transform part in wall) part.gameObject.SetActive(true);
                int opened = 0;
                foreach (Transform part in wall)
                    if (part.position.z >= StartZ - 3f && part.position.z <= EndZ + 3f) { part.gameObject.SetActive(false); opened++; }

                var old = GameObject.Find("MINI180_RoundaboutBeach"); if (old) Object.DestroyImmediate(old);
                var root = new GameObject("MINI180_RoundaboutBeach");
                var vertices = new List<Vector3>(); var triangles = new List<int>();
                for (int i = 0; i <= 18; i++)
                {
                    float z = Mathf.Lerp(StartZ, EndZ, i / 18f);
                    // A small flare gives a natural bay pocket while retaining a clear strip to the road.
                    float width = BeachWidth + Mathf.Sin(i / 18f * Mathf.PI) * 1.1f;
                    foreach (float inland in new[] { .10f, width })
                    {
                        float x = Coast(z) - inland;
                        var p = new Vector3(x, 0, z);
                        if (terrain.Raycast(new Ray(new Vector3(x, 200, z), Vector3.down), out var hit, 500)) p.y = hit.point.y + .018f;
                        else if (inland < 1f) p.y = .268f; // water-edge vertex at the established bay level
                        else throw new Exception("Beach terrain missing at " + p);
                        vertices.Add(p);
                    }
                    if (i > 0) { int k = (i - 1) * 2; triangles.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 }); }
                }
                var mesh = new Mesh { name = "Mini180RoundaboutBeach", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                var path = Art + "/RoundaboutBeach.asset"; var asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (asset) { EditorUtility.CopySerialized(mesh, asset); Object.DestroyImmediate(mesh); mesh = asset; } else AssetDatabase.CreateAsset(mesh, path);
                var beach = new GameObject("RoundaboutBeach"); beach.transform.SetParent(root.transform); beach.AddComponent<MeshFilter>().sharedMesh = mesh;
                beach.AddComponent<MeshRenderer>().sharedMaterial = SandMaterial(); beach.AddComponent<MeshCollider>().sharedMesh = mesh;

                var roads = Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.name.IndexOf("Road", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                int roadHits = vertices.Count(p => roads.Any(r => r.Raycast(new Ray(new Vector3(p.x, 200, p.z), Vector3.down), out _, 500)));
                if (roadHits != 0) throw new Exception("Beach overlaps road samples=" + roadHits);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                File.WriteAllText(Out + "/ROUNDABOUT-BEACH.txt", "MINI-180 user-authorized beach\nZ=" + StartZ + ".." + EndZ + "\nwidth=" + BeachWidth + ".." + (BeachWidth + 1.1f) + "m\nvertices=" + vertices.Count + "\nseawall parts opened=" + opened + "\nroad samples hit=0\nThe beach uses existing expansion terrain heights and a MeshCollider; playable GrandBayProof remains untouched.");
                Render(); Debug.Log("MINI180_BEACH_PASS vertices=" + vertices.Count + " wallPartsOpened=" + opened);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static float Coast(float z) => z < -160 ? 250 : z < -60 ? 250 + (z + 160) * .55f : 305 + (z + 60) * .25f;
        static Material SandMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(Art + "/RoundaboutBeachSand.mat");
            if (!material) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, Art + "/RoundaboutBeachSand.mat"); }
            material.color = new Color(.74f, .60f, .35f); material.enableInstancing = true; EditorUtility.SetDirty(material); return material;
        }
        static void Render()
        {
            foreach (var existing in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) existing.enabled = false;
            RenderSettings.fog = false; RenderSettings.ambientLight = new Color(.67f, .69f, .72f);
            var camera = new GameObject("MINI180BeachCamera").AddComponent<Camera>(); camera.farClipPlane = 1500;
            Shot(camera, "roundabout-beach", new Vector3(350, 48, -110), new Vector3(282, 2, -66), 55);
            Shot(camera, "roundabout-beach-close", new Vector3(325, 20, -105), new Vector3(290, 1, -66), 48);
            Object.DestroyImmediate(camera.gameObject);
        }
        static void Shot(Camera camera, string name, Vector3 position, Vector3 target, float fov)
        {
            camera.transform.position = position; camera.transform.LookAt(target); camera.fieldOfView = fov;
            var rt = new RenderTexture(1600, 1000, 24); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply(); File.WriteAllBytes(Out + "/Renders/" + name + ".png", tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
        }
    }
}
