using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    public static class Mini163IsolateFrankiHair
    {
        const string Out = "Logs/Tasks/MINI-163";
        [MenuItem("Up Iz Up Mini/MINI-163/Isolate Franki Hair Shape")]
        public static void Run()
        {
            System.IO.Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var root = GameObject.Find("Franki");
            root.transform.position = new Vector3(0, 300, 0);

            var key = new GameObject("k").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.7f; key.transform.rotation = Quaternion.Euler(35, -35, 0);
            var fill = new GameObject("f").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = .8f; fill.transform.rotation = Quaternion.Euler(25, 145, 0);
            var camera = new GameObject("cam").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1f, 0f, 1f);
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 26;

            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers) r.enabled = (r.name == "Ch28_Hair");
            var hair = renderers.First(r => r.name == "Ch28_Hair");
            var b = hair.bounds;
            Capture(camera, b.center + Vector3.back * (b.size.magnitude * 1.2f + 0.1f), b.center, $"{Out}/FrankiHair-Front.png");
            Capture(camera, b.center + Vector3.right * (b.size.magnitude * 1.2f + 0.1f), b.center, $"{Out}/FrankiHair-Side.png");
            Capture(camera, b.center + Vector3.up * (b.size.magnitude * 1.2f + 0.1f), b.center + Vector3.up * 0.02f, $"{Out}/FrankiHair-Top.png");
            foreach (var r in renderers) r.enabled = true;

            DestroyImmediate(camera.gameObject); DestroyImmediate(key.gameObject); DestroyImmediate(fill.gameObject);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        static void Capture(Camera camera, Vector3 position, Vector3 target, string path)
        {
            camera.transform.position = position; camera.transform.LookAt(target);
            var rt = new RenderTexture(700, 700, 24) { antiAliasing = 4 };
            var old = RenderTexture.active; camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(700, 700, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 700, 700), 0, 0); tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old;
            Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
        }
        static void DestroyImmediate(Object o) => Object.DestroyImmediate(o);
    }
}
