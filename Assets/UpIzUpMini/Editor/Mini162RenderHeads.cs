using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace UpIzUpMini.EditorTools
{
    public static class Mini162RenderHeads
    {
        const string Out = "Logs/Tasks/MINI-162";

        [MenuItem("Up Iz Up Mini/MINI-162/Render Heads (cap on and off)")]
        public static void Run()
        {
            System.IO.Directory.CreateDirectory(Out);
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
            camera.backgroundColor = new Color(.82f, .82f, .85f);
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 26;

            var idleClip = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));

            float offsetX = 0f;
            foreach (var name in new[] { "Sacat", "Franki" })
            {
                var root = GameObject.Find(name);
                Vector3 originalPos = root.transform.position;
                root.transform.position = new Vector3(offsetX, 300f, 0f);
                offsetX += 3f;

                var animator = root.GetComponentInChildren<Animator>(true);
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Sample(animator, idleClip, idleClip.length * 0.4f);

                // find any cap/hat GameObject to hide for a bare-head shot
                Transform cap = root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(t => t.name.ToLower().Contains("cap") || t.name.ToLower().Contains("equip_cap"));

                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                Vector3 focus = head.position + Vector3.up * 0.05f;
                Capture(camera, focus + Vector3.back * 0.55f, focus, $"{Out}/Head-{name}-WithCap-Front.png");
                Capture(camera, focus + Vector3.right * 0.55f, focus, $"{Out}/Head-{name}-WithCap-Side.png");

                if (cap != null)
                {
                    cap.gameObject.SetActive(false);
                    Capture(camera, focus + Vector3.back * 0.55f, focus, $"{Out}/Head-{name}-NoCap-Front.png");
                    Capture(camera, focus + Vector3.right * 0.55f, focus, $"{Out}/Head-{name}-NoCap-Side.png");
                    cap.gameObject.SetActive(true);
                }
                else
                {
                    Debug.Log($"MINI162HEADS: {name} no cap object found (name search)");
                }

                root.transform.position = originalPos;
            }

            DestroyImmediate(camera.gameObject);
            DestroyImmediate(key.gameObject);
            DestroyImmediate(fill.gameObject);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini162Heads");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var p = AnimationClipPlayable.Create(graph, clip);
            var o = AnimationPlayableOutput.Create(graph, "Pose", a);
            o.SetSourcePlayable(p);
            graph.Play(); p.SetTime(t); graph.Evaluate(0); graph.Destroy();
        }

        static void Capture(Camera camera, Vector3 position, Vector3 target, string path)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            var rt = new RenderTexture(700, 700, 24) { antiAliasing = 4 };
            var old = RenderTexture.active;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(700, 700, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 700, 700), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = old;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }

        static void DestroyImmediate(Object o) => Object.DestroyImmediate(o);
    }
}
