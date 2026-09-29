using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace UpIzUpMini.EditorTools
{
    /// <summary>Fixed cameras for the bay wall / beach / Geneva field evidence (prefix from env MINI182_SHOTS, default "c").</summary>
    public static class Mini182CoastShots
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single); Physics.SyncTransforms();
            string pre = System.Environment.GetEnvironmentVariable("MINI182_SHOTS") ?? "c"; Directory.CreateDirectory("Logs/Tasks/MINI-182/Renders");
            var go = new GameObject("CoastCam"); var cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f); cam.farClipPlane = 3000; cam.fieldOfView = 55;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white * .85f;
            var rt = new RenderTexture(1600, 1000, 24); cam.targetTexture = rt;
            void Shot(string n, Vector3 pos, Vector3 look, bool ortho = false, float size = 30)
            { cam.orthographic = ortho; cam.orthographicSize = size; cam.transform.position = pos; cam.transform.LookAt(look, ortho ? Vector3.forward : Vector3.up); cam.Render(); var tex = new Texture2D(1600, 1000, TextureFormat.RGB24, false); RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); tex.Apply(); File.WriteAllBytes("Logs/Tasks/MINI-182/Renders/" + pre + "_" + n + ".png", tex.EncodeToPNG()); Object.DestroyImmediate(tex); }
            Shot("wall_top", new Vector3(285, 400, -110), new Vector3(285, 0, -110), true, 45);
            Shot("wall_oblique", new Vector3(215, 24, -175), new Vector3(275, 1, -118));
            Shot("wall_close", new Vector3(238, 9, -140), new Vector3(262, 1.5f, -138));
            Shot("wall_north", new Vector3(345, 26, -60), new Vector3(315, 1.5f, -15));
            Shot("field_top", new Vector3(273, 400, 57), new Vector3(273, 0, 57), true, 55);
            Shot("field_oblique", new Vector3(215, 32, 20), new Vector3(273, 3, 60));
            Shot("field_close", new Vector3(300, 12, 20), new Vector3(273, 2, 62));
            EditorApplication.Exit(0);
        }
    }
}
