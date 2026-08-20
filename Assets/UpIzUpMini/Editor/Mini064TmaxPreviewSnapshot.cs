using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-064 Pass 1.5: a quick throwaway render of the cleaned
    /// TMAX mesh (before prefab assembly) to visually judge the scale
    /// discrepancy the inspection numbers flagged, per the brief's own
    /// "render it... compare proportions against real TMAX references."</summary>
    public static class Mini064TmaxPreviewSnapshot
    {
        [MenuItem("Up Iz Up Mini/MINI-064/Preview Cleaned TMAX Mesh")]
        public static void Preview()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Vehicles/TMAX_560_clean.glb");
            if (asset == null)
            {
                Debug.LogError("Preview FAIL: TMAX_560_clean.glb not found/importable.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.transform.position = Vector3.zero;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            Debug.Log($"MINI-064 PREVIEW DEBUG: instantiated '{instance.name}', renderer count={renderers.Length}");
            foreach (var r in renderers)
            {
                Debug.Log($"  renderer '{r.name}' enabled={r.enabled} bounds={r.bounds} mat={(r.sharedMaterial != null ? r.sharedMaterial.name : "null")} shader={(r.sharedMaterial != null && r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "null")}");
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * 2f;

            var lightGo = new GameObject("Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.2f;
            // Headless -nographics rendering has been crashing in
            // GfxDevice::RenderShadowMaps this session (documented in the
            // MINI-060 follow-up-2 handoff entry) - shadows off sidesteps
            // that specific crash path.
            light.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

            // 3/4 view.
            var camGo = new GameObject("PreviewCam34");
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(3.2f, 1.6f, -3.2f);
            camGo.transform.LookAt(new Vector3(0f, 0.7f, 0f));
            cam.fieldOfView = 45f;
            cam.farClipPlane = 100f;
            RenderAndSave(cam, "mini064-tmax-cleaned-34.png");
            Object.DestroyImmediate(camGo);

            // Side view (to judge length/height silhouette).
            var camSide = new GameObject("PreviewCamSide");
            var cs = camSide.AddComponent<Camera>();
            camSide.transform.position = new Vector3(4.5f, 1f, 0f);
            camSide.transform.LookAt(new Vector3(0f, 0.7f, 0f));
            cs.fieldOfView = 40f;
            cs.farClipPlane = 100f;
            RenderAndSave(cs, "mini064-tmax-cleaned-side.png");
            Object.DestroyImmediate(camSide);

            // Front view (to judge width).
            var camFront = new GameObject("PreviewCamFront");
            var cf = camFront.AddComponent<Camera>();
            camFront.transform.position = new Vector3(0f, 1f, -3.5f);
            camFront.transform.LookAt(new Vector3(0f, 0.7f, 0f));
            cf.fieldOfView = 40f;
            cf.farClipPlane = 100f;
            RenderAndSave(cf, "mini064-tmax-cleaned-front.png");
            Object.DestroyImmediate(camFront);
        }

        private static void RenderAndSave(Camera cam, string fileName)
        {
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

            string dir = "Logs/Snapshots";
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, fileName);
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"Saved snapshot: {path}");
        }
    }
}
