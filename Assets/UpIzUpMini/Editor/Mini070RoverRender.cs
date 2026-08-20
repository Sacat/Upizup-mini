using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-070. Photographs the Range Rover where it actually stands in the
    /// built scene, so the swap is checked rather than assumed.
    ///
    /// Aimed at the car's own renderer bounds and framed from its bounding
    /// size - the MINI-067 chain check taught that guessing a camera height
    /// from a character root, and forgetting other objects can stand between
    /// the camera and the subject, produces confident-looking empty photos.
    /// </summary>
    public static class Mini070RoverRender
    {
        private const string OutDir = "Logs/rover-render";

        [MenuItem("Up Iz Up Mini/MINI-070/Render Range Rover Check")]
        public static void Render()
        {
            Directory.CreateDirectory(OutDir);
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var suv = GameObject.Find("BossC_SUV");
            if (suv == null) { Debug.LogError("MINI-070 RENDER: BossC_SUV not found."); return; }

            var renderers = suv.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { Debug.LogError("MINI-070 RENDER: the SUV has no renderers."); return; }

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            Debug.Log($"MINI-070 RENDER: SUV at {suv.transform.position}, world bounds size={b.size}, centre={b.center}, renderers={renderers.Length}");

            Shoot(b, "rover-front", 35f);
            Shoot(b, "rover-side", 110f);
            Shoot(b, "rover-rear", 200f);
        }

        private static void Shoot(Bounds b, string name, float yaw)
        {
            var camGo = new GameObject("RoverRenderCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Pull back from the subject's own size, so the whole car is in
            // frame whatever scale it ended up at.
            float radius = Mathf.Max(b.size.x, b.size.y, b.size.z);
            float dist = radius * 1.9f;

            Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * Vector3.forward;
            camGo.transform.position = b.center + dir * dist + Vector3.up * radius * 0.45f;
            camGo.transform.LookAt(b.center);

            var rt = new RenderTexture(1000, 640, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            File.WriteAllBytes(Path.Combine(OutDir, name + ".png"), tex.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGo);
        }
    }
}
