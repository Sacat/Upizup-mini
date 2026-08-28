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

            var camGo = new GameObject("Mini120SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            cam.fieldOfView = 35f;

            Vector3 focus = sacat.transform.position + Vector3.up * 1.1f;
            camGo.transform.position = focus + new Vector3(-1.4f, 0.2f, 2.6f);
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
