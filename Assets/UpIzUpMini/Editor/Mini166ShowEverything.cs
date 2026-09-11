using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-166: render both characters wearing everything shipped
    /// this session at once, for a single comprehensive look.</summary>
    public static class Mini166ShowEverything
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-166";

        [MenuItem("Up Iz Up Mini/MINI-166/Show Everything")]
        public static void Run()
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            var franki = GameObject.Find("Franki");
            var fOutfit = franki.GetComponent<OutfitWardrobe>();
            fOutfit.Select("shirt_polo_lacos", 0);
            fOutfit.Select("pants_shorts_denim", 0);
            fOutfit.Select("hat_lacos", 0);
            fOutfit.Select("shoes_mike97", 0);
            RenderPreview(franki, "Franki-Everything");

            var sacat = GameObject.Find("Sacat");
            var sOutfit = sacat.GetComponent<OutfitWardrobe>();
            sOutfit.Select("shirt_polo_lacos", 0);
            sOutfit.Select("hat_lacos", 0);
            RenderPreview(sacat, "Sacat-Everything");

            Debug.Log("MINI166_SHOW_EVERYTHING_PASS");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void RenderPreview(GameObject root, string tag)
        {
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
            camera.backgroundColor = new Color(.85f, .85f, .88f);
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 32;

            Vector3 originalPos = root.transform.position;
            root.transform.position = new Vector3(500, 300, 0);
            var animator = root.GetComponentInChildren<Animator>(true);
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            Sample(animator, idle, idle.length * 0.4f);
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmupE_{tag}.png");

            var bodyR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Body" || r.name == "Ch06");
            var b = bodyR.bounds;
            Capture(camera, b.center - root.transform.forward * (b.size.magnitude * 1.15f) + Vector3.up * 0.1f, b.center, $"{Out}/{tag}-A.png");
            Capture(camera, b.center + root.transform.forward * (b.size.magnitude * 1.15f) + Vector3.up * 0.1f, b.center, $"{Out}/{tag}-B.png");
            Capture(camera, b.center - root.transform.right * (b.size.magnitude * 1.15f) + Vector3.up * 0.1f, b.center, $"{Out}/{tag}-C.png");

            root.transform.position = originalPos;
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            UnityEngine.Object.DestroyImmediate(fill.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini166Everything");
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
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
