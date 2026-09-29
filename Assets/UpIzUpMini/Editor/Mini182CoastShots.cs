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
            Shot("school_top", new Vector3(107, 400, 48), new Vector3(107, 0, 48), true, 34);
            Shot("school_oblique", new Vector3(107, 45, 100), new Vector3(107, 19, 45));
            Shot("school_court", new Vector3(107, 30, 78), new Vector3(107f, 18.5f, 50f));
            Shot("school_street", new Vector3(75, 24, 75), new Vector3(108, 19, 48));
            Shot("housing_a", new Vector3(-20, 34, -20), new Vector3(-48, 12, -42));
            Shot("housing_top", new Vector3(20, 400, 30), new Vector3(20, 0, 30), true, 60);
            Shot("housing_b", new Vector3(55, 40, 90), new Vector3(20, 18, 30));
            Shot("cx_top", new Vector3(140f, 400f, 68f), new Vector3(140f, 0f, 68f), true, 70f);
            Shot("cx_oblique", new Vector3(95f, 40f, 105f), new Vector3(140f, 18f, 68f));
            Shot("cx_street", new Vector3(125f, 26f, 40f), new Vector3(150f, 19f, 74f));
            Shot("cx_wide", new Vector3(60f, 70f, 130f), new Vector3(120f, 15f, 55f));
            Shot("cls_view", new Vector3(80f, 34f, 22f), new Vector3(105f, 19f, 72f));
            Shot("field_top", new Vector3(273, 400, 57), new Vector3(273, 0, 57), true, 55);
            Shot("field_oblique", new Vector3(215, 32, 20), new Vector3(273, 3, 60));
            Shot("field_close", new Vector3(300, 12, 20), new Vector3(273, 2, 62));
            EditorApplication.Exit(0);
        }
    }
}
