using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-166 Pants slot pre-check: before attempting a
    /// denim-shorts cut, verify whether real lower-leg skin geometry exists
    /// under the pants for BOTH characters. MINI-159's arm-gap regression
    /// (Ch28_Body had almost no upper-arm geometry, assumed always covered)
    /// is the exact same class of mistake applied to legs - check first,
    /// don't assume.</summary>
    public static class Mini166CheckLegSkin
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-166";

        [MenuItem("Up Iz Up Mini/MINI-166/Check Leg Skin")]
        public static void Run()
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            foreach (var name in new[] { "Sacat", "Franki" })
            {
                var root = GameObject.Find(name);
                var animator = root.GetComponentInChildren<Animator>(true);
                animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                // Hide everything except the base skin renderer so any gap
                // below a hypothetical knee cut is unmistakable.
                var skinName = name == "Sacat" ? "Ch06" : "Ch28_Body";
                var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var hidden = new System.Collections.Generic.List<SkinnedMeshRenderer>();
                foreach (var r in renderers)
                {
                    if (r.name != skinName) { hidden.Add(r); r.enabled = false; }
                }

                RenderLegs(root, animator, name);

                foreach (var r in hidden) r.enabled = true;
            }
            Debug.Log("MINI166_LEG_SKIN_CHECK_PASS");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void RenderLegs(GameObject root, Animator animator, string tag)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .67f, .75f);
            RenderSettings.ambientEquatorColor = new Color(.35f, .39f, .45f);
            RenderSettings.ambientGroundColor = new Color(.22f, .20f, .18f);
            RenderSettings.fog = false;
            var key = new GameObject("k").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.8f; key.transform.rotation = Quaternion.Euler(35, -35, 0);
            var fill = new GameObject("f").AddComponent<Light>();
            fill.type = LightType.Directional; fill.intensity = .9f; fill.transform.rotation = Quaternion.Euler(25, 145, 0);
            var camera = new GameObject("cam").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(1f, 0f, 1f); // magenta - any gap/background bleed is obvious
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 30;

            Vector3 originalPos = root.transform.position;
            root.transform.position = new Vector3(500, 300, 0);
            var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            Sample(animator, idle, idle.length * 0.4f);
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmup3_{tag}.png");

            var skinR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.enabled);
            var b = skinR.bounds;
            Debug.Log($"MINI166LEG: {tag} enabled skin renderer={skinR.name} bounds.center={b.center} bounds.size={b.size}");
            // Two close-up leg-crop attempts both missed (camera pointed at
            // empty space or caught an unrelated body part). Using the ONE
            // framing recipe that has reliably worked all session for a
            // full-body shot (proven repeatedly: Shirt/Pants/Shoes renders) -
            // wide enough that hitting SOME part of the character is not in
            // question, then crop/inspect the lower half of the image
            // instead of trying to aim a tight camera at bone-derived leg
            // coordinates again. Also rotation-aware (root.transform.forward)
            // per the Hat slot's camera lesson - fixed world axes don't work
            // for Sacat's rotated root.
            Capture(camera, b.center - root.transform.forward * (b.size.magnitude * 1.1f) + Vector3.up * 0.1f, b.center, $"{Out}/{tag}-LegSkin-Front.png");
            Capture(camera, b.center - root.transform.right * (b.size.magnitude * 1.1f) + Vector3.up * 0.1f, b.center, $"{Out}/{tag}-LegSkin-Side.png");

            root.transform.position = originalPos;
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            UnityEngine.Object.DestroyImmediate(fill.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini166Legs");
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
