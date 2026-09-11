using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Object = UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-154/MINI-157/MINI-158: import each reshaped garment FBX
    /// (Tools/CharacterPipeline/mini15{4,7}_*.py output) as its own
    /// self-contained Humanoid character, sample idle/walk/run on an
    /// isolated stage, and capture screenshots at several frames of each
    /// clip to check for shoulder/sleeve/neckline/ankle deformation
    /// problems. Does not touch the canonical scene or any playable asset.
    /// See Docs/WorkPackets/MINI-154.md, MINI-157.md, MINI-158.md.
    ///
    /// MINI-158: extended to cover both Sacat and Franki's reshaped
    /// assets in one pass, keyed by the "body root" renderer name each
    /// FBX actually uses so the same loop works for both (Franki/Ch28_*
    /// is six separate objects with root renderer "Ch28_Body"; Sacat/Ch06
    /// is one fused object whose renderer IS named "Ch06").
    /// </summary>
    public static class Mini154GarmentMotionProof
    {
        const string ControllerPath = "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller";
        const string CanonicalScene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Out = "Logs/Tasks/MINI-158";

        private static readonly (string fbxPath, string label, string bodyRendererName)[] Subjects =
        {
            ("Assets/UpIzUpMini/Art/Characters/Garments/Franki_ReshapedGarments_Colored.fbx", "Franki", "Ch28_Body"),
            ("Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Ch06_Reshaped.fbx", "Sacat", "Ch06"),
        };

        static void Require(bool ok, string text)
        {
            if (!ok) throw new Exception("MINI158MOTION: " + text);
        }

        [MenuItem("Up Iz Up Mini/MINI-158/Garment Motion Proof (Both Characters)")]
        public static void Run()
        {
            Directory.CreateDirectory(Out);
            string canonicalBefore = Hash(CanonicalScene);

            EditorSceneManager.OpenScene(CanonicalScene, OpenSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.62f, .67f, .75f);
            RenderSettings.ambientEquatorColor = new Color(.35f, .39f, .45f);
            RenderSettings.ambientGroundColor = new Color(.22f, .20f, .18f);
            RenderSettings.fog = false;
            var key = new GameObject("Soft key").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.7f; key.transform.rotation = Quaternion.Euler(35, -35, 0);
            var fillLight = new GameObject("Rim light").AddComponent<Light>();
            fillLight.type = LightType.Directional; fillLight.intensity = .8f; fillLight.transform.rotation = Quaternion.Euler(25, 145, 0);
            var camera = new GameObject("Motion proof camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.82f, .82f, .85f);
            camera.nearClipPlane = .01f; camera.farClipPlane = 50; camera.fieldOfView = 32;

            // DIAGNOSTIC: prove the camera/render/capture path itself works
            // in THIS invocation (batch/-nographics vs plain -batchmode)
            // before trusting it for the actual characters, by rendering a
            // plain lit cube first. If this comes back blank too, the
            // characters below will too - not worth chasing further.
            var diagCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diagCube.transform.position = new Vector3(0f, 300.9f, 0f);
            diagCube.transform.localScale = Vector3.one * 0.5f;
            Capture(camera, diagCube.transform.position - Vector3.forward * 3f, diagCube.transform.position, $"{Out}/Motion-DIAG-cube.png");
            Object.DestroyImmediate(diagCube);

            var evidence = new List<string>();
            var clips = new (string path, string label, int[] frames)[]
            {
                ("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx", "Idle", new[] { 0, 6, 11 }),
                ("Assets/UpIzUpMini/Art/Animations/Locomotion--Walk_N.anim.fbx", "Walk", new[] { 0, 3, 6, 9 }),
                ("Assets/UpIzUpMini/Art/Animations/Locomotion--Run_N.anim.fbx", "Run", new[] { 0, 3, 6, 9 }),
            };

            float spawnOffsetX = 0f;
            foreach (var (fbxPath, label, bodyRendererName) in Subjects)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
                Require(importer != null, "missing reshaped garment FBX " + fbxPath);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false;
                importer.SaveAndReimport();

                var source = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                Require(source != null, $"{label}: reshaped FBX failed to load as GameObject");
                var sourceAnimator = source.GetComponent<Animator>();
                Require(sourceAnimator != null && sourceAnimator.avatar != null && sourceAnimator.avatar.isValid && sourceAnimator.avatar.isHuman,
                    $"{label}: reshaped FBX avatar invalid/not Humanoid");

                var subject = (GameObject)PrefabUtility.InstantiatePrefab(source);
                subject.name = $"{label}_ReshapedGarments_MotionProof";
                subject.transform.position = new Vector3(spawnOffsetX, 300f, 0f);
                subject.transform.rotation = Quaternion.identity;
                spawnOffsetX += 2.5f;

                var animator = subject.GetComponent<Animator>();
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
                Require(controller != null, "missing shared locomotion controller");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

                var bodyRenderer = subject.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(r => r.name == bodyRendererName);
                Require(bodyRenderer != null, $"{label}: body renderer '{bodyRendererName}' not found");

                foreach (var (path, clipLabel, frames) in clips)
                {
                    var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
                    foreach (int frame in frames)
                    {
                        float t = clip.length * frame / 12f;
                        Sample(animator, clip, t);
                        var bounds = bodyRenderer.bounds;
                        Vector3 focus = bounds.center;
                        float dist = Mathf.Max(bounds.size.magnitude, 0.5f) * 1.8f;
                        Vector3 fwd = subject.transform.forward;
                        Vector3 right = subject.transform.right;
                        Capture(camera, focus - fwd * dist + Vector3.up * 0.05f, focus, $"{Out}/{label}-{clipLabel}-{frame}-Front.png");
                        Capture(camera, focus + right * dist * 0.85f + Vector3.up * 0.05f, focus, $"{Out}/{label}-{clipLabel}-{frame}-Side.png");
                        evidence.Add($"{label} {clipLabel} frame {frame}: t={t:F3} boundsCenter={focus} boundsSize={bounds.size} captured Front+Side");
                    }
                }

                Object.DestroyImmediate(subject);
            }

            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(key.gameObject);
            Object.DestroyImmediate(fillLight.gameObject);
            // Deliberately NOT saving the canonical scene - it was only opened
            // to reuse its lighting/environment for a working render, and every
            // proof subject/camera/light is destroyed again before exit.
            Require(Hash(CanonicalScene) == canonicalBefore, "canonical scene file changed on disk");
            evidence.Add("Canonical scene file on disk unchanged (opened in-memory only, not saved). Every proof subject destroyed after capture.");
            File.WriteAllLines($"{Out}/unity-garment-motion-proof.txt", evidence);
            Debug.Log("MINI158_MOTION_PROOF_PASS captured " + evidence.Count + " lines");

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini158MotionProof");
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

        static string Hash(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
        }
    }
}
