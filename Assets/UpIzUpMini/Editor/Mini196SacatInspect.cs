using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-196 read-only: lists Sacat's renderers and renders front/side/back (all, body-only, garments-only) in bind pose. Never saves.</summary>
    public static class Mini196SacatInspect
    {
        const string Out = "Logs/Tasks/MINI-196";

        [MenuItem("Up Iz Up Mini/MINI-196/Inspect Sacat")]
        public static void Run()
        {
            try
            {
                Directory.CreateDirectory(Out);
                string scene = Environment.GetEnvironmentVariable("MINI196_SCENE") ?? "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                var sacat = GameObject.Find(Environment.GetEnvironmentVariable("MINI196_NAME") ?? "Sacat");
                if (sacat == null) throw new Exception("character not found");
                var sb = new StringBuilder();
                var rends = sacat.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    var smr = r as SkinnedMeshRenderer;
                    sb.AppendLine((r.enabled && r.gameObject.activeInHierarchy ? "ON  " : "off ") + PathOf(r.transform, sacat.transform) + " | " + r.GetType().Name + " | mesh=" + (smr != null && smr.sharedMesh != null ? smr.sharedMesh.name + " v" + smr.sharedMesh.vertexCount : "-") + " | mats=" + string.Join(",", r.sharedMaterials.Select(m => m != null ? m.name : "null").ToArray()) + " | bounds=" + r.bounds.size.ToString("0.00"));
                }
                File.WriteAllText(Out + "/renderers.txt", sb.ToString());
                var bounds = rends.Where(r => r.enabled && r.gameObject.activeInHierarchy).Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
                Vector3 c = bounds.center; float h = bounds.size.y;
                foreach (var r in rends) { var smr = r as SkinnedMeshRenderer; if (smr != null) smr.updateWhenOffscreen = true; }
                Shot("orig", sacat, rends, r => r.transform.parent != null && r.name == "Ch06", c, h, true);
                Shot("current_body", sacat, rends, r => r.name == "WardrobeBody", c, h, true);
                Shot("current_dressed", sacat, rends, r => r.name == "WardrobeBody" || r.name == "WardrobeShirt" || r.name == "WardrobePants" || r.name == "WardrobeShoes" || r.name == "HeadphonesAccessory", c, h, true);
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }

        static bool IsBody(Renderer r) { string n = (r.name + " " + string.Join(" ", r.sharedMaterials.Select(m => m != null ? m.name : ""))).ToLowerInvariant(); return n.Contains("body") || n.Contains("skin") || n.Contains("eyelash") || n.Contains("hair") || n.Contains("eye"); }
        static string PathOf(Transform t, Transform root) { string s = t.name; while (t.parent != null && t.parent != root) { t = t.parent; s = t.name + "/" + s; } return s; }

        static void Shot(string tag, GameObject root, Renderer[] rends, Func<Renderer, bool> keep, Vector3 c, float h, bool force = false)
        {
            var state = rends.Select(r => r.enabled).ToArray();
            if (keep != null) for (int i = 0; i < rends.Length; i++) rends[i].enabled = keep(rends[i]) && (force || state[i]);
            var go = new GameObject("InspectCam"); var cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .62f, .7f); cam.fieldOfView = 24;
            var light = new GameObject("L").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(35, 25, 0); light.intensity = 1.2f;
            RenderSettings.ambientLight = new Color(.55f, .55f, .55f);
            string[] names = { "front", "side", "back" }; Vector3[] dirs = { root.transform.forward, root.transform.right, -root.transform.forward };
            for (int i = 0; i < 3; i++)
            {
                go.transform.position = c + dirs[i] * (h * 2.6f); go.transform.LookAt(c);
                var rt = new RenderTexture(900, 1200, 24); cam.targetTexture = rt; cam.Render(); RenderTexture.active = rt;
                var tx = new Texture2D(900, 1200, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 900, 1200), 0, 0); tx.Apply();
                File.WriteAllBytes(Out + "/" + tag + "_" + names[i] + ".png", tx.EncodeToPNG()); RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt);
            }
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(light.gameObject);
            for (int i = 0; i < rends.Length; i++) rends[i].enabled = state[i];
        }
    }
}
