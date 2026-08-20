using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-064/065 visual proof render, against the real
    /// TMAX_Physics_Test scene.</summary>
    public static class Mini065TmaxSnapshot
    {
        [MenuItem("Up Iz Up Mini/MINI-065/Snapshot TMAX Test Scene")]
        public static void Snapshot()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/TMAX_Physics_Test.unity", OpenSceneMode.Single);

            var bike = GameObject.Find("TMAX_560");
            if (bike == null)
            {
                Debug.LogError("Snapshot FAIL: TMAX_560 not found in the test scene.");
                return;
            }

            var camGo = new GameObject("SnapshotCam");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = bike.transform.position + new Vector3(2.6f, 1.6f, -3.2f);
            camGo.transform.LookAt(bike.transform.position + Vector3.up * 0.9f);
            cam.fieldOfView = 45f;
            cam.farClipPlane = 100f;

            int w = 1280, h = 800;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            System.IO.Directory.CreateDirectory("Logs/Snapshots");
            string path = "Logs/Snapshots/mini065-tmax-test-scene.png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"Saved snapshot: {path}");
        }
    }
}
