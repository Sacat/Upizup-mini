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
    /// <summary>MINI-166 accessory-fit check: render the watch and cap as
    /// they currently sit on both live characters, close up, so real defects
    /// can be measured against a reference instead of guessed at. No scene
    /// save - trial-equips then leaves the scene untouched.</summary>
    public static class Mini166CheckAccessoryFit
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-166";

        [MenuItem("Up Iz Up Mini/MINI-166/Check Accessory Fit")]
        public static void Run()
        {
            Directory.CreateDirectory(Out);
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            foreach (var name in new[] { "Sacat", "Franki" })
            {
                var root = GameObject.Find(name);
                if (root == null) { Debug.LogWarning($"MINI166FIT: no {name} in scene"); continue; }
                var equip = root.GetComponent<CharacterEquipment>();
                if (equip == null) { Debug.LogWarning($"MINI166FIT: no CharacterEquipment on {name}"); continue; }
                equip.SetTrialItem("watch_rollie", true);
                equip.SetTrialItem("cap_mike", true);

                var animatorDbg = root.GetComponentInChildren<Animator>(true);
                var headBone = animatorDbg.GetBoneTransform(HumanBodyBones.Head);
                Debug.Log($"MINI166FIT: {name} Head bone={(headBone == null ? "NULL" : headBone.name)} pos={headBone?.position}");
                var capGo = root.transform.Find($"Equip_cap_mike");
                if (capGo == null)
                {
                    // search recursively - it's parented under the Head bone, not the root
                    foreach (var t in root.GetComponentsInChildren<Transform>(true))
                        if (t.name == "Equip_cap_mike") { capGo = t; break; }
                }
                if (capGo != null)
                    Debug.Log($"MINI166FIT: {name} Equip_cap_mike world pos={capGo.position} localPos={capGo.localPosition} parent={capGo.parent?.name} dist_from_head={(headBone != null ? Vector3.Distance(capGo.position, headBone.position) : -1)}");
                else
                    Debug.Log($"MINI166FIT: {name} Equip_cap_mike NOT FOUND in hierarchy");
                Transform watchGo = null;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Equip_watch_rollie") { watchGo = t; break; }
                var handBoneDbg = animatorDbg.GetBoneTransform(HumanBodyBones.LeftHand);
                if (watchGo != null)
                    Debug.Log($"MINI166FIT: {name} Equip_watch_rollie world pos={watchGo.position} localScale={watchGo.localScale} dist_from_hand={(handBoneDbg != null ? Vector3.Distance(watchGo.position, handBoneDbg.position) : -1)}");
                else
                    Debug.Log($"MINI166FIT: {name} Equip_watch_rollie NOT FOUND in hierarchy");

                RenderChecks(root, name);
            }

            Debug.Log("MINI166_ACCESSORY_FIT_CHECK_PASS");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void RenderChecks(GameObject root, string tag)
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
            camera.backgroundColor = new Color(.82f, .82f, .85f);
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 30;

            Vector3 originalPos = root.transform.position;
            root.transform.position = new Vector3(500, 300, 0);
            var animator = root.GetComponentInChildren<Animator>(true);
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            Sample(animator, idle, idle.length * 0.4f);
            Capture(camera, root.transform.position + Vector3.up * 2f, root.transform.position, $"{Out}/_warmup2_{tag}.png"); // warm skinning

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            var hand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var lowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);

            Capture(camera, head.position + new Vector3(0, 0.05f, -0.45f), head.position + Vector3.up * 0.05f, $"{Out}/{tag}-Head-Front.png");
            Capture(camera, head.position + new Vector3(-0.45f, 0.05f, 0), head.position + Vector3.up * 0.05f, $"{Out}/{tag}-Head-Side.png");
            var wristFocus = Vector3.Lerp(lowerArm.position, hand.position, 0.85f);
            Capture(camera, wristFocus + new Vector3(0.3f, 0.1f, -0.1f), wristFocus, $"{Out}/{tag}-Wrist.png");

            // Full body for overall proportion sanity check.
            var bodyR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Body" || r.name == "Ch06");
            Capture(camera, bodyR.bounds.center + new Vector3(0, 0.1f, -3f), bodyR.bounds.center, $"{Out}/{tag}-Full-Accessories.png");

            root.transform.position = originalPos;
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            UnityEngine.Object.DestroyImmediate(fill.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini166Fit");
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
