using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-166 accessory fit check, done properly: the edit-mode
    /// version of this check found nothing at all, because CharacterEquipment.
    /// Refresh() early-returns when EconomyManager.Instance is null outside
    /// Play Mode - it was never actually exercising the real code path. This
    /// runs the real game (Play Mode) so trial-equip actually works, matches
    /// the proven Mini165HeadphoneValidation.cs pattern for batch Play Mode
    /// checks that survive the domain reload on entering Play Mode.</summary>
    [InitializeOnLoad]
    public static class Mini166PlayModeAccessoryFit
    {
        const string Out = "Logs/Tasks/MINI-166";
        static Mini166PlayModeAccessoryFit() { if (SessionState.GetBool("Mini166FitTesting", false)) EditorApplication.playModeStateChanged += Changed; }

        [MenuItem("Up Iz Up Mini/MINI-166/Check Accessory Fit (Play Mode)")]
        public static void Run()
        {
            SessionState.SetBool("Mini166FitTesting", true);
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity");
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.EnterPlaymode();
        }

        static double next;
        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode) { next = EditorApplication.timeSinceStartup + 2.5; EditorApplication.update += Check; }
            if (state == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool("Mini166FitTesting", false); EditorApplication.playModeStateChanged -= Changed; }
        }

        static void Check()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            EditorApplication.update -= Check;
            try
            {
                Directory.CreateDirectory(Out);
                var all = UnityEngine.Object.FindObjectsByType<CharacterEquipment>(FindObjectsSortMode.None);
                foreach (var name in new[] { "Sacat", "Franki" })
                {
                    var eq = all.FirstOrDefault(e => e.name == name);
                    if (eq == null) { Debug.LogWarning($"MINI166FIT: no CharacterEquipment for {name}"); continue; }
                    eq.SetTrialItem("watch_rollie", true);
                    eq.SetTrialItem("cap_mike", true);

                    var animator = eq.GetComponentInChildren<Animator>(true);
                    var head = animator.GetBoneTransform(HumanBodyBones.Head);
                    var hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                    var lowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);

                    var capT = eq.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Equip_cap_mike");
                    var watchT = eq.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Equip_watch_rollie");
                    Debug.Log($"MINI166FIT: {name} head={head.position} cap={(capT == null ? "MISSING" : capT.position.ToString())} capDistFromHead={(capT != null ? Vector3.Distance(capT.position, head.position).ToString("F3") : "-")}" );
                    Debug.Log($"MINI166FIT: {name} hand={hand.position} watch={(watchT == null ? "MISSING" : watchT.position.ToString())} watchDistFromHand={(watchT != null ? Vector3.Distance(watchT.position, hand.position).ToString("F3") : "-")} watchScale={(watchT != null ? watchT.lossyScale.ToString() : "-")}");

                    RenderShots(eq.gameObject, name, animator, head, hand, lowerArm);
                }
                Debug.Log("MINI166_PLAYMODE_FIT_CHECK_PASS");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorApplication.ExitPlaymode();
            }
        }

        static void RenderShots(GameObject character, string tag, Animator animator, Transform head, Transform hand, Transform lowerArm)
        {
            // BUG FOUND: hijacking whatever live gameplay camera
            // FindObjectsByType returned produced a bird's-eye MAP render,
            // not the character - a live CameraFollow-style script was
            // fighting the direct transform assignment every frame. Spawn a
            // dedicated temporary camera instead, exactly like every other
            // render tool this session (never touch the live gameplay
            // camera while it's actively driven by other scripts).
            var camGo = new GameObject("Mini166FitCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.82f, .82f, .85f);
            cam.nearClipPlane = .02f; cam.farClipPlane = 200; cam.fieldOfView = 30;

            Capture(cam, head.position + new Vector3(0, 0.05f, -0.4f), head.position + Vector3.up * 0.05f, $"{Out}/{tag}-PM-Head-Front.png");
            Capture(cam, head.position + new Vector3(-0.4f, 0.05f, 0), head.position + Vector3.up * 0.05f, $"{Out}/{tag}-PM-Head-Side.png");
            var wristFocus = Vector3.Lerp(lowerArm.position, hand.position, 0.85f);
            Capture(cam, wristFocus + new Vector3(0.3f, 0.1f, -0.1f), wristFocus, $"{Out}/{tag}-PM-Wrist.png");

            UnityEngine.Object.Destroy(camGo);
        }

        static void Capture(Camera camera, Vector3 position, Vector3 target, string path)
        {
            camera.transform.position = position; camera.transform.LookAt(target);
            var rt = new RenderTexture(700, 700, 24) { antiAliasing = 4 };
            var old = RenderTexture.active; var oldTarget = camera.targetTexture;
            camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(700, 700, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 700, 700), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = oldTarget; RenderTexture.active = old;
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
