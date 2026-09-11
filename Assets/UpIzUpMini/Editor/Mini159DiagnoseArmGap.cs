using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace UpIzUpMini.EditorTools
{
    public static class Mini159DiagnoseArmGap
    {
        const string Out = "Logs/Tasks/MINI-159";

        [MenuItem("Up Iz Up Mini/MINI-159/Diagnose Arm Gap (Franki, skin only)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .67f, .75f);
            RenderSettings.ambientEquatorColor = new Color(.35f, .39f, .45f);
            RenderSettings.ambientGroundColor = new Color(.22f, .20f, .18f);
            RenderSettings.fog = false;
            var key = new GameObject("k").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.7f; key.transform.rotation = Quaternion.Euler(35, -35, 0);
            var fill = new GameObject("f").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = .8f; fill.transform.rotation = Quaternion.Euler(25, 145, 0);
            var camera = new GameObject("cam").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1f, 0f, 1f); // magenta bg so any gap is obvious
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 30;

            var root = GameObject.Find("Franki");
            Vector3 originalPos = root.transform.position;
            root.transform.position = new Vector3(0, 300, 0);

            var animator = root.GetComponentInChildren<Animator>(true);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var idleClip = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            Sample(animator, idleClip, idleClip.length * 0.4f);

            // Hide everything except the skin mesh so ONLY Ch28_Body is visible.
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var r in renderers) r.enabled = (r.name == "Ch28_Body");

            var bodyRenderer = renderers.First(r => r.name == "Ch28_Body");
            var leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var leftForearm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Debug.Log($"MINI159GAP: LeftUpperArm={leftUpperArm.position} LeftForeArm(LowerArm)={leftForearm.position} LeftHand={leftHand.position}");
            Debug.Log($"MINI159GAP: forearm bone length={Vector3.Distance(leftForearm.position, leftHand.position):F4}m");

            Vector3 focus = (leftUpperArm.position + leftHand.position) / 2f;
            Capture(camera, focus + Vector3.back * 1.0f + Vector3.up * 0.05f, focus, $"{Out}/Diag-Franki-SkinOnly-Front.png");
            Capture(camera, focus + Vector3.right * 1.0f, focus, $"{Out}/Diag-Franki-SkinOnly-Side.png");
            Capture(camera, focus + Vector3.up * 1.0f, focus + Vector3.up * 0.05f, $"{Out}/Diag-Franki-SkinOnly-Top.png");

            foreach (var r in renderers) r.enabled = true;
            root.transform.position = originalPos;

            DestroyImmediate(camera.gameObject);
            DestroyImmediate(key.gameObject);
            DestroyImmediate(fill.gameObject);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini159DiagnoseArmGap");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var p = AnimationClipPlayable.Create(graph, clip);
            var o = AnimationPlayableOutput.Create(graph, "Pose", a);
            o.SetSourcePlayable(p);
            graph.Play();
            p.SetTime(t);
            graph.Evaluate(0);
            graph.Destroy();
        }

        static void Capture(Camera camera, Vector3 position, Vector3 target, string path)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            var rt = new RenderTexture(800, 800, 24) { antiAliasing = 4 };
            var old = RenderTexture.active;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(800, 800, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 800, 800), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = old;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }

        static void DestroyImmediate(Object o) => Object.DestroyImmediate(o);
    }
}
