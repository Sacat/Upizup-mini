using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-166 Pants pre-check: colour-flag each of Ch06's 4
    /// material slots distinctly to see which one (if any) actually covers
    /// the pants region, before assuming a tint-only Pants piece for Sacat
    /// is safe/possible.</summary>
    public static class Mini166CheckSacatMaterials
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-166";

        [MenuItem("Up Iz Up Mini/MINI-166/Check Sacat Materials")]
        public static void Run()
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var root = GameObject.Find("Sacat");
            var ch06 = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch06");
            var animator = root.GetComponentInChildren<Animator>(true);

            var flagColors = new[] { Color.red, Color.green, Color.blue, Color.yellow };
            var mats = ch06.sharedMaterials;
            Debug.Log($"MINI166MAT: Ch06 material count={mats.Length} names={string.Join(",", mats.Select(m => m?.name))}");
            var flagged = new Material[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                flagged[i] = new Material(Shader.Find("Unlit/Color")) { color = flagColors[i % flagColors.Length] };
            }
            ch06.sharedMaterials = flagged;

            RenderPreview(root, animator);
            Debug.Log("MINI166_MAT_CHECK_PASS red=index0 green=index1 blue=index2 yellow=index3");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void RenderPreview(GameObject root, Animator animator)
        {
            var camera = new GameObject("cam").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.82f, .82f, .85f);
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 32;
            var key = new GameObject("k").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.7f; key.transform.rotation = Quaternion.Euler(35, -35, 0);

            Vector3 originalPos = root.transform.position;
            root.transform.position = new Vector3(500, 300, 0);
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            Sample(animator, idle, idle.length * 0.4f);
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmup7.png");

            var bodyR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch06");
            var b = bodyR.bounds;
            Capture(camera, b.center + new Vector3(0, 0.1f, -3f), b.center, $"{Out}/Sacat-MatFlags-Full.png");
            Capture(camera, b.center + new Vector3(0, 0.1f, 3f), b.center, $"{Out}/Sacat-MatFlags-Back.png");

            root.transform.position = originalPos;
            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(key.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini166Mat");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var p = AnimationClipPlayable.Create(graph, clip);
            var o = AnimationPlayableOutput.Create(graph, "Pose", a);
            o.SetSourcePlayable(p);
            graph.Play(); p.SetTime(t); graph.Evaluate(0); graph.Destroy();
        }

        static void Capture(Camera camera, Vector3 position, Vector3 target, string path)
        {
            camera.transform.position = position; camera.transform.LookAt(target);
            var rt = new RenderTexture(700, 700, 24) { antiAliasing = 4 };
            var old = RenderTexture.active; camera.targetTexture = rt; camera.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(700, 700, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 700, 700), 0, 0); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = old;
            Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
        }
    }
}
