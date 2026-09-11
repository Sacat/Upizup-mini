using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace UpIzUpMini.EditorTools
{
    public static class Mini159BetterProof
    {
        const string Out = "Logs/Tasks/MINI-159";
        const string ControllerPath = "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller";

        [MenuItem("Up Iz Up Mini/MINI-159/Better Live Proof (posed, well lit)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var originalAmbientMode = RenderSettings.ambientMode;
            var originalSky = RenderSettings.ambientSkyColor;
            var originalEq = RenderSettings.ambientEquatorColor;
            var originalGround = RenderSettings.ambientGroundColor;
            var originalFog = RenderSettings.fog;
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
            camera.backgroundColor = new Color(.82f, .82f, .85f);
            camera.nearClipPlane = .05f; camera.farClipPlane = 200; camera.fieldOfView = 32;

            var idleClip = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));

            float offsetX = 0f;
            foreach (var name in new[] { "Sacat", "Franki" })
            {
                var root = GameObject.Find(name);
                if (root == null) continue;
                Vector3 originalPos = root.transform.position;
                root.transform.position = new Vector3(offsetX, 300f, 0f);
                offsetX += 3f;

                var animator = root.GetComponentInChildren<Animator>(true);
                var savedController = animator.runtimeAnimatorController;
                var savedApplyRoot = animator.applyRootMotion;
                var savedCulling = animator.cullingMode;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                Sample(animator, idleClip, idleClip.length * 0.4f);
                var handAfterFirst = animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                Debug.Log($"MINI159BETTER: {name} LeftHand after Sample() = {handAfterFirst}");

                var bodyRenderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .FirstOrDefault(r => r.name == "Ch06" || r.name == "Ch28_Body");
                var bounds = bodyRenderer.bounds;
                Vector3 focus = bounds.center;
                float dist = Mathf.Max(bounds.size.magnitude, 1.0f) * 2.0f;
                Capture(camera, focus - root.transform.forward * dist + Vector3.up * 0.1f, focus, $"{Out}/Better-{name}-Front.png");
                Capture(camera, focus + root.transform.right * dist + Vector3.up * 0.1f, focus, $"{Out}/Better-{name}-Side.png");

                root.transform.position = originalPos;
                animator.applyRootMotion = savedApplyRoot;
                animator.cullingMode = savedCulling;
            }

            DestroyImmediate(camera.gameObject);
            DestroyImmediate(key.gameObject);
            DestroyImmediate(fill.gameObject);
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientSkyColor = originalSky;
            RenderSettings.ambientEquatorColor = originalEq;
            RenderSettings.ambientGroundColor = originalGround;
            RenderSettings.fog = originalFog;

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini159BetterProof");
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
            var rt = new RenderTexture(700, 900, 24) { antiAliasing = 4 };
            var old = RenderTexture.active;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(700, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 700, 900), 0, 0);
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
