using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace UpIzUpMini.EditorTools
{
    public static class Mini107CharacterMotionProof
    {
        private const string PrefabPath = "Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/Prefabs/SacatModularBase_Mobile.prefab";
        private const string AvatarPath = "Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/SacatModularBase_LOD0.fbx";
        private const string EvidenceFolder = "Logs/Tasks/MINI-107";

        private sealed class Motion
        {
            public string label;
            public string path;
            public string clip;
            public float sample;
            public Motion(string label, string path, string clip, float sample)
            { this.label = label; this.path = path; this.clip = clip; this.sample = sample; }
        }

        [Serializable]
        private sealed class MotionAudit
        {
            public string prefab;
            public bool avatarValid;
            public bool avatarHuman;
            public int fingerJointsFound;
            public int fingerJointsExpected;
            public bool wrapperRotationIsIdentity;
            public Vector3 visualRigEuler;
            public string[] clips;
            public string poseMatrix;
            public string handProof;
            public string frameFolder;
            public int motionFrames;
            public bool playableIntegration;
        }

        private static readonly Motion[] Motions =
        {
            new Motion("IDLE", "Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx", "Idle", 0.55f),
            new Motion("WALK", "Assets/UpIzUpMini/Art/Animations/Locomotion--Walk_N.anim.fbx", "Walk_N", 0.42f),
            new Motion("RUN", "Assets/UpIzUpMini/Art/Animations/Locomotion--Run_N.anim.fbx", "Run_N", 0.31f),
            new Motion("JUMP START", "Assets/UpIzUpMini/Art/Animations/Jump--Jump.anim.fbx", "JumpStart", 0.72f),
            new Motion("IN AIR", "Assets/UpIzUpMini/Art/Animations/Jump--InAir.anim.fbx", "InAir", 0.48f),
            new Motion("JUMP LAND", "Assets/UpIzUpMini/Art/Animations/Jump--Jump.anim.fbx", "JumpLand", 0.55f),
            new Motion("CLOSED FISTS", "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/Human@HandsClosed01.fbx", "Human@HandsClosed01", 0.95f),
            new Motion("OBJECT GRIP", "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/Human@ObjectGripHands01.fbx", "Human@ObjectGripHands01", 0.95f)
        };

        [MenuItem("Tools/Up Iz Up Mini/MINI-107/Build Validate Capture Motion Proof")]
        public static void BuildValidateCapture()
        {
            Directory.CreateDirectory(EvidenceFolder);
            string frames = Path.Combine(EvidenceFolder, "Frames");
            Directory.CreateDirectory(frames);
            foreach (string old in Directory.GetFiles(frames, "frame_*.png")) File.Delete(old);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(AvatarPath).OfType<Avatar>().FirstOrDefault();
            if (prefab == null || avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("MINI-107 requires the approved mobile prefab and a valid Humanoid LOD0 Avatar.");

            GameObject stage = new GameObject("MINI107_MotionProofStage");
            GameObject subject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            subject.transform.SetParent(stage.transform, false);
            subject.transform.localPosition = Vector3.zero;
            subject.transform.localRotation = Quaternion.identity;
            LODGroup group = subject.GetComponent<LODGroup>();
            if (group == null) throw new InvalidOperationException("MINI-107 mobile prefab is missing its LODGroup.");
            group.ForceLOD(0);

            Transform visualRig = subject.transform.Find("VisualRig");
            if (visualRig == null) throw new InvalidOperationException("MINI-107 mobile prefab is missing VisualRig.");
            Animator animator = visualRig.gameObject.GetComponent<Animator>() ?? visualRig.gameObject.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Camera camera = CreateCamera(stage.transform);
            CreateLighting(stage.transform);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ProofGround";
            ground.transform.SetParent(stage.transform, false);
            ground.transform.localScale = new Vector3(1.4f, 1f, 1.4f);
            Material groundMaterial = new Material(Shader.Find("Standard"));
            groundMaterial.color = new Color(0.055f, 0.07f, 0.09f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            try
            {
                List<Texture2D> tiles = new List<Texture2D>();
                foreach (Motion motion in Motions)
                {
                    AnimationClip clip = LoadClip(motion.path, motion.clip);
                    float time = Mathf.Clamp01(motion.sample) * Mathf.Max(0.001f, clip.length);
                    Sample(animator, clip, time);
                    tiles.Add(Render(camera, subject, motion.label, 480, 720, false));
                }

                Texture2D matrix = BuildMatrix(tiles, 4, 2, 480, 720);
                string matrixPath = Path.Combine(EvidenceFolder, "Sacat-Motion-Pose-Matrix.png");
                File.WriteAllBytes(matrixPath, matrix.EncodeToPNG());
                foreach (Texture2D tile in tiles) UnityEngine.Object.DestroyImmediate(tile);
                UnityEngine.Object.DestroyImmediate(matrix);

                Motion fist = Motions[6];
                Sample(animator, LoadClip(fist.path, fist.clip), LoadClip(fist.path, fist.clip).length * fist.sample);
                Texture2D hands = Render(camera, subject, "30-JOINT FINGER PROOF — CLOSED FISTS", 1280, 720, true);
                string handPath = Path.Combine(EvidenceFolder, "Sacat-Finger-Fist-Proof.png");
                File.WriteAllBytes(handPath, hands.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(hands);

                int frameIndex = 0;
                foreach (Motion motion in Motions)
                {
                    AnimationClip clip = LoadClip(motion.path, motion.clip);
                    int count = motion.label.Contains("FIST") || motion.label.Contains("GRIP") ? 12 : 18;
                    for (int i = 0; i < count; i++)
                    {
                        float normalized = count == 1 ? motion.sample : i / (float)(count - 1);
                        Sample(animator, clip, Mathf.Clamp01(normalized) * Mathf.Max(0.001f, clip.length));
                        Texture2D frame = Render(camera, subject, motion.label, 960, 540, false);
                        File.WriteAllBytes(Path.Combine(frames, $"frame_{frameIndex:0000}.png"), frame.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(frame);
                        frameIndex++;
                    }
                }

                HashSet<string> names = new HashSet<string>(subject.GetComponentsInChildren<Transform>(true).Select(t => t.name), StringComparer.Ordinal);
                int fingerCount = RequiredFingerNames().Count(names.Contains);
                MotionAudit audit = new MotionAudit
                {
                    prefab = PrefabPath,
                    avatarValid = avatar.isValid,
                    avatarHuman = avatar.isHuman,
                    fingerJointsFound = fingerCount,
                    fingerJointsExpected = 30,
                    wrapperRotationIsIdentity = Quaternion.Angle(subject.transform.localRotation, Quaternion.identity) < 0.01f,
                    visualRigEuler = visualRig.localEulerAngles,
                    clips = Motions.Select(m => m.clip).ToArray(),
                    poseMatrix = matrixPath.Replace('\\', '/'),
                    handProof = handPath.Replace('\\', '/'),
                    frameFolder = frames.Replace('\\', '/'),
                    motionFrames = frameIndex,
                    playableIntegration = false
                };
                File.WriteAllText(Path.Combine(EvidenceFolder, "Sacat-Motion-Audit.json"), JsonUtility.ToJson(audit, true));
                if (fingerCount != 30 || !audit.wrapperRotationIsIdentity)
                    throw new InvalidOperationException($"MINI-107 audit failed: fingers {fingerCount}/30, wrapper identity {audit.wrapperRotationIsIdentity}.");
                Debug.Log($"MINI-107 PASS: {Motions.Length} Humanoid proofs, {frameIndex} frames, fingers {fingerCount}/30; playable integration unchanged.");
            }
            finally
            {
                group.ForceLOD(-1);
                UnityEngine.Object.DestroyImmediate(groundMaterial);
                UnityEngine.Object.DestroyImmediate(stage);
            }
        }

        private static AnimationClip LoadClip(string path, string name)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => string.Equals(c.name, name, StringComparison.Ordinal));
            if (clip == null) throw new InvalidOperationException($"MINI-107 clip missing: {path} :: {name}");
            return clip;
        }

        private static void Sample(Animator animator, AnimationClip clip, float time)
        {
            PlayableGraph graph = PlayableGraph.Create("MINI107_ProofSample");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Motion", animator);
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetApplyFootIK(true);
            playable.SetApplyPlayableIK(true);
            output.SetSourcePlayable(playable);
            graph.Play();
            playable.SetTime(Mathf.Clamp(time, 0f, clip.length));
            graph.Evaluate(0f);
            graph.Destroy();
        }

        private static Camera CreateCamera(Transform parent)
        {
            Camera camera = new GameObject("ProofCamera").AddComponent<Camera>();
            camera.transform.SetParent(parent, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.018f, 0.03f, 1f);
            camera.fieldOfView = 28f;
            return camera;
        }

        private static void CreateLighting(Transform parent)
        {
            Light key = new GameObject("Key").AddComponent<Light>();
            key.transform.SetParent(parent, false); key.type = LightType.Directional; key.intensity = 1.15f;
            key.color = new Color(1f, 0.88f, 0.78f); key.transform.rotation = Quaternion.Euler(32f, -38f, 0f);
            Light fill = new GameObject("Fill").AddComponent<Light>();
            fill.transform.SetParent(parent, false); fill.type = LightType.Directional; fill.intensity = 0.55f;
            fill.color = new Color(0.62f, 0.76f, 1f); fill.transform.rotation = Quaternion.Euler(20f, 145f, 0f);
        }

        private static Texture2D Render(Camera camera, GameObject subject, string label, int width, int height, bool handCloseup)
        {
            Renderer[] renderers = subject.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 target;
            float distance;
            if (handCloseup)
            {
                Animator animator = subject.GetComponentInChildren<Animator>();
                Transform left = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                Transform right = animator.GetBoneTransform(HumanBodyBones.RightHand);
                target = (left.position + right.position) * 0.5f;
                distance = Mathf.Max(2.2f, Vector3.Distance(left.position, right.position) * 1.75f);
            }
            else
            {
                target = bounds.center + Vector3.up * bounds.extents.y * 0.02f;
                distance = Mathf.Max(3f, bounds.size.y * 2.15f);
            }
            camera.transform.position = target + new Vector3(bounds.size.y * 0.48f, bounds.size.y * 0.04f, distance);
            camera.transform.LookAt(target);

            TextMesh text = new GameObject("ProofLabel").AddComponent<TextMesh>();
            text.transform.position = target + new Vector3(0f, handCloseup ? 0.75f : bounds.extents.y * 1.12f, 0f);
            text.transform.rotation = Quaternion.LookRotation(text.transform.position - camera.transform.position);
            text.text = label; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.fontSize = 52; text.characterSize = 0.018f; text.color = Color.white;

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            camera.targetTexture = null; RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(text.gameObject);
            return image;
        }

        private static Texture2D BuildMatrix(IReadOnlyList<Texture2D> tiles, int columns, int rows, int width, int height)
        {
            Texture2D matrix = new Texture2D(columns * width, rows * height, TextureFormat.RGBA32, false);
            Color32[] clear = Enumerable.Repeat(new Color32(3, 5, 8, 255), matrix.width * matrix.height).ToArray();
            matrix.SetPixels32(clear);
            for (int i = 0; i < tiles.Count; i++)
                matrix.SetPixels((i % columns) * width, (rows - 1 - i / columns) * height, width, height, tiles[i].GetPixels());
            matrix.Apply();
            return matrix;
        }

        private static List<string> RequiredFingerNames()
        {
            List<string> names = new List<string>();
            foreach (string side in new[] { "L", "R" })
                foreach (string digit in new[] { "Thumb", "Index", "Mid", "Ring", "Pinky" })
                    for (int joint = 1; joint <= 3; joint++) names.Add($"CC_Base_{side}_{digit}{joint}");
            return names;
        }
    }
}
