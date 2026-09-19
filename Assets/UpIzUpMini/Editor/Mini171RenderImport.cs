using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-171 evidence renders of the import scene (run WITHOUT -nographics). Saves nothing but PNGs.</summary>
    public static class Mini171RenderImport
    {
        const string Import = "Assets/UpIzUpMini/Scenes/GrandBayProof_ExpansionImport.unity";
        static readonly (string n, Vector3 pos, Vector3 look)[] Views =
        {
            ("r1_road_edge_east", new Vector3(150, 6, -20), new Vector3(156, 1, -2)),
            ("r2_join_lalay", new Vector3(80, 14, -125), new Vector3(99, 4, -106)),
            ("r3_bay_seam", new Vector3(232, 10, -110), new Vector3(250, 3, -95)),
            ("r4_overview", new Vector3(100, 420, -40), new Vector3(100, 0, -40)),
            ("r5_farm", new Vector3(96, 9, -140), new Vector3(114, 1, -121)),
            ("r6_farm_top", new Vector3(114, 80, -121), new Vector3(114, 0, -121)),
            ("r8_join_top", new Vector3(99, 60, -106), new Vector3(99, 0, -106)),
            ("r9_west_join_top", new Vector3(-55, 70, -100), new Vector3(-55, 0, -100)),
            ("r10_roundabout_top", new Vector3(285, 50, -68), new Vector3(285, 0, -68)),
            ("r7_highland_top", new Vector3(60, 90, -90), new Vector3(60, 0, -90)),
        };

        [MenuItem("Up Iz Up Mini/MINI-171/Render Import Views")]
        public static void Run()
        {
            var sp = System.Environment.GetEnvironmentVariable("MINI171_SCENE"); var tag = sp == null ? "" : "live_"; EditorSceneManager.OpenScene(sp ?? Import, OpenSceneMode.Single);
            Directory.CreateDirectory("Logs/Tasks/MINI-171/Renders");
            var go = new GameObject("Mini171Cam"); var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.55f, .7f, .9f); cam.farClipPlane = 1500; cam.fieldOfView = 55;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white * .8f;
            var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt;
            foreach (var v in Views)
            {
                Physics.SyncTransforms();
                var pos = v.pos; var look = v.look;
                if (v.n != "r4_overview" && !v.n.EndsWith("_top"))
                {
                    if (Physics.Raycast(new Vector3(pos.x, 500, pos.z), Vector3.down, out var h1, 900)) pos.y = h1.point.y + v.pos.y;
                    if (Physics.Raycast(new Vector3(look.x, 500, look.z), Vector3.down, out var h2, 900)) look.y = h2.point.y + v.look.y;
                }
                cam.transform.position = pos; cam.transform.LookAt(look);
                cam.orthographic = v.n == "r4_overview"; if (cam.orthographic) cam.orthographicSize = 260;
                cam.Render();
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
                File.WriteAllBytes("Logs/Tasks/MINI-171/Renders/" + tag + v.n + ".png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            RenderTexture.active = null; Object.DestroyImmediate(go);
            Debug.Log("MINI171RENDER_DONE");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
