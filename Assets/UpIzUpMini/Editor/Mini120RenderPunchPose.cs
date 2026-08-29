using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 (combat bug-fix pass): renders Sacat posed by the
    /// new Mixamo jab-punch clip at a mid-swing frame, for direct visual
    /// comparison against the current warped weapon-swing clip
    /// (HumanM@Attack1H01_R.fbx). Uses AnimationClip.SampleAnimation
    /// directly on Sacat's own rig/avatar - no Play Mode needed, same
    /// technique this project's own Mini011AssetSnapshot.cs already uses
    /// for skin-tint/chain-placement checks. Delete after use.</summary>
    public static class Mini120RenderPunchPose
    {
        private const string MixamoClipPath = "Assets/Mixamo/Animations/Mixamo_JabPunch.fbx";
        private const string OldClipPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx";

        [MenuItem("Up Iz Up Mini/MINI-120/Sample Hook Clip Frames (one-off)")]
        public static void SampleHookFrames()
        {
            SampleClipFrames("Assets/Mixamo/Animations/Mixamo_Hook.fbx", "hook_sample");
        }

        [MenuItem("Up Iz Up Mini/MINI-120/Sample Right Hook Clip Frames (one-off)")]
        public static void SampleRightHookFrames()
        {
            SampleClipFrames("Assets/Mixamo/Animations/Mixamo_RightHook.fbx", "righthook_sample");
        }

        private static void SampleClipFrames(string clipPath, string prefix)
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var sacat = GameObject.Find("Sacat");
            var visual = sacat.transform.Find("Visual");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            Directory.CreateDirectory("Logs/Tasks/MINI-120");

            Vector3 originalPos = sacat.transform.position;
            sacat.transform.position = new Vector3(0f, 300f, 0f);

            float[] fractions = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f };
            foreach (var f in fractions)
            {
                RenderAtTime(sacat, visual.gameObject, clip, clip.length * f, $"Logs/Tasks/MINI-120/{prefix}_{(int)(f * 100)}pct.png");
            }
            sacat.transform.position = originalPos;
        }

        [MenuItem("Up Iz Up Mini/MINI-120/Render Punch Pose Comparison (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var sacat = GameObject.Find("Sacat");
            if (sacat == null) { Debug.LogError("MINI-120 RENDER: no Sacat in scene."); return; }
            var visual = sacat.transform.Find("Visual");
            if (visual == null) { Debug.LogError("MINI-120 RENDER: no Sacat/Visual."); return; }

            // Isolate in clear open air well above the market/bike clutter
            // at his real position - a clean render background, and no
            // risk of clipping through nearby geometry. Not saved.
            Vector3 originalPos = sacat.transform.position;
            sacat.transform.position = new Vector3(0f, 300f, 0f);

            var newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MixamoClipPath);
            var oldClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OldClipPath);
            if (newClip == null) { Debug.LogError("MINI-120 RENDER: couldn't load the Mixamo clip."); return; }

            Directory.CreateDirectory("Logs/Tasks/MINI-120");

            RenderAtTime(sacat, visual.gameObject, newClip, newClip.length * 0.45f, "Logs/Tasks/MINI-120/new_mixamo_punch_front.png");
            if (oldClip != null)
                RenderAtTime(sacat, visual.gameObject, oldClip, oldClip.length * 0.45f, "Logs/Tasks/MINI-120/old_weapon_swing_punch_front.png");
            else
                Debug.LogWarning("MINI-120 RENDER: old clip not found for comparison (not fatal).");

            sacat.transform.position = originalPos;
            Debug.Log("MINI-120 RENDER: done - see Logs/Tasks/MINI-120/*.png");
        }

        private static void RenderAtTime(GameObject sacat, GameObject visual, AnimationClip clip, float time, string outPath)
        {
            clip.SampleAnimation(visual, time);

            // AnimationClip.SampleAnimation bypasses the Animator entirely
            // (this project's real playback always has applyRootMotion=
            // false, so gameplay never actually drifts) - it applies any
            // root-motion translation baked into the clip directly to the
            // bone hierarchy, which can carry the visible mesh well away
            // from the root GameObject's own static position. Frame on the
            // renderers' actual combined bounds instead of a fixed offset
            // from the root, so the character stays in shot regardless.
            var renderers = visual.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(sacat.transform.position, Vector3.one);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);

            var camGo = new GameObject("Mini120SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            cam.fieldOfView = 35f;

            Vector3 focus = bounds.center;
            float dist = Mathf.Max(2.2f, bounds.extents.magnitude * 1.6f);
            camGo.transform.position = focus + new Vector3(-dist * 0.5f, dist * 0.15f, dist * 0.9f);
            camGo.transform.LookAt(focus);

            var rt = new RenderTexture(768, 1024, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes(outPath, tex.EncodeToPNG());

            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(camGo);

            Debug.Log($"MINI-120 RENDER: saved {outPath} (clip={clip.name}, time={time:F2}s of {clip.length:F2}s).");
        }
    }
}
