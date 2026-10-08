using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Creates researched visual variants and renders them on the saved characters.</summary>
    public static class Mini187ColorwayProof
    {
        const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string Folder = "Assets/UpIzUpMini/Art/Weapons/Mini187";
        const string ShaderName = "UpIzUpMini/Lalay Tool Colorway";
        const string TexturePath = "Assets/UpIzUpMini/Art/Weapons/Mini186/LalayTool_1024.png";
        const string IdleClipPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@CombatIdle1H01.fbx";

        struct Palette
        {
            public string Name;
            public Color Slide, Frame, Accent;
            public Palette(string name, Color slide, Color frame, Color accent)
            { Name = name; Slide = slide; Frame = frame; Accent = accent; }
        }

        static readonly Palette[] Palettes =
        {
            new Palette("MatteBlack", new Color(.31f,.32f,.34f), new Color(.30f,.31f,.33f), new Color(.28f,.35f,.36f)),
            new Palette("CoyoteSand", new Color(.94f,.73f,.47f), new Color(.79f,.62f,.40f), new Color(.35f,.29f,.22f)),
            new Palette("TwoToneOlive", new Color(.30f,.32f,.31f), new Color(.44f,.55f,.34f), new Color(.26f,.38f,.25f)),
        };

        [MenuItem("Up Iz Up Mini/MINI-187/Inspect Character Grip")]
        public static void InspectCharacterGrip()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var clip = AssetDatabase.LoadAllAssetsAtPath(IdleClipPath).OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase));
            if (clip == null) throw new InvalidOperationException("One-hand combat idle clip missing.");
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var animator = player.GetComponentInChildren<Animator>(true);
                clip.SampleAnimation(animator.gameObject, Mathf.Min(.5f, clip.length * .5f));
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var holder = player.transform.Find("Mini183_Sidearm");
                FirearmController.PlaceAtPalm(holder, holder.Find("LalayTool_Visual/GripAnchor"), hand,
                    animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate), animator.transform.forward);
                foreach (var bone in new[] { HumanBodyBones.RightHand, HumanBodyBones.RightThumbProximal,
                    HumanBodyBones.RightIndexProximal, HumanBodyBones.RightMiddleProximal,
                    HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
                    HumanBodyBones.RightRingProximal, HumanBodyBones.RightLittleProximal })
                {
                    var transform = animator.GetBoneTransform(bone);
                    Debug.Log($"MINI-187 GRIP {player.name} {bone}: " +
                        (transform == null ? "unmapped" : $"holderLocal={holder.InverseTransformPoint(transform.position)} handLocal={hand.InverseTransformPoint(transform.position)}"));
                }
                var visual = holder.Find("LalayTool_Visual");
                var mesh = visual != null ? visual.GetComponentInChildren<MeshFilter>(true) : null;
                if (mesh != null)
                    Debug.Log($"MINI-187 GRIP {player.name} meshBounds={mesh.sharedMesh.bounds} visualLocal={visual.localPosition} meshWorldCenterHolder={holder.InverseTransformPoint(mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center))}");
            }
            Debug.Log("MINI-187 GRIP INSPECTION PASS; scene not saved.");
        }

        [MenuItem("Up Iz Up Mini/MINI-187/Create Colorway Materials")]
        public static void CreateMaterials()
        {
            var shader = Shader.Find(ShaderName);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (shader == null || texture == null) throw new InvalidOperationException("MINI-187 shader or source texture missing.");
            foreach (var palette in Palettes)
            {
                string path = $"{Folder}/LalayTool_{palette.Name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = "LalayTool_" + palette.Name };
                    AssetDatabase.CreateAsset(material, path);
                }
                material.shader = shader;
                material.SetTexture("_MainTex", texture);
                material.SetColor("_SlideColor", palette.Slide);
                material.SetColor("_FrameColor", palette.Frame);
                material.SetColor("_AccentColor", palette.Accent);
                material.SetFloat("_CutY", .015f);
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("MINI-187 MATERIAL PASS: MatteBlack, CoyoteSand, TwoToneOlive created without changing the live default.");
        }

        [MenuItem("Up Iz Up Mini/MINI-187/Verify Grip Anchors")]
        public static void VerifyGripAnchors()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length != 2) throw new InvalidOperationException($"Expected two players, found {players.Length}.");
            var clip = AssetDatabase.LoadAllAssetsAtPath(IdleClipPath).OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase));
            if (clip == null) throw new InvalidOperationException("Grip proof clip missing.");
            foreach (var player in players)
            {
                var animator = player.GetComponentInChildren<Animator>(true);
                var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var middle = animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
                var holder = player.transform.Find("Mini183_Sidearm");
                var grip = holder != null ? holder.Find("LalayTool_Visual/GripAnchor") : null;
                var muzzle = holder != null ? holder.Find("LalayTool_Visual/ShotOrigin") : null;
                var gun = holder != null ? holder.GetComponentInChildren<MeshRenderer>(true) : null;
                var controller = player.GetComponent<FirearmController>();
                if (wrist == null || middle == null || grip == null || muzzle == null || gun == null || controller == null)
                    throw new InvalidOperationException($"Grip hierarchy incomplete on {player.name}.");
                var data = new SerializedObject(controller);
                if (data.FindProperty("gripAnchor").objectReferenceValue != grip ||
                    data.FindProperty("muzzle").objectReferenceValue != muzzle)
                    throw new InvalidOperationException($"Grip or muzzle serialization differs on {player.name}.");
                clip.SampleAnimation(animator.gameObject, Mathf.Min(.5f, clip.length * .5f));
                FirearmController.PlaceAtPalm(holder, grip, wrist, middle, animator.transform.forward);
                float error = Vector3.Distance(grip.position, middle.position);
                if (error > .002f) throw new InvalidOperationException($"Grip alignment error {error:F4} m on {player.name}.");
                Debug.Log($"MINI-187 ANCHOR {player.name}: gripError={error:F4}m, muzzleLocal={holder.InverseTransformPoint(muzzle.position)}");
            }
            foreach (var palette in Palettes)
                if (AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/LalayTool_{palette.Name}.mat") == null)
                    throw new InvalidOperationException($"Missing {palette.Name} material.");
            Debug.Log("MINI-187 ANCHOR VERIFY PASS: both players, serialized grip/muzzle anchors and three materials; scene not saved.");
        }

        [MenuItem("Up Iz Up Mini/MINI-187/Render Character Colorways")]
        public static void RenderCharacterColorways()
        {
            CreateMaterials();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length != 2) throw new InvalidOperationException($"Expected two players, found {players.Length}.");
            var clip = AssetDatabase.LoadAllAssetsAtPath(IdleClipPath).OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase));
            if (clip == null) throw new InvalidOperationException("One-hand combat idle clip missing.");
            var output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs/Tasks/MINI-187");
            Directory.CreateDirectory(output);
            foreach (var player in players)
            {
                var animator = player.GetComponentInChildren<Animator>(true);
                var hand = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
                var head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
                var leftFoot = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
                var holder = player.transform.Find("Mini183_Sidearm");
                var gun = holder != null ? holder.GetComponentInChildren<MeshRenderer>(true) : null;
                if (hand == null || head == null || leftFoot == null || gun == null)
                    throw new InvalidOperationException($"Proof setup missing rig or tool for {player.name}.");

                clip.SampleAnimation(animator.gameObject, Mathf.Min(.5f, clip.length * .5f));
                FirearmController.PlaceAtPalm(holder, holder.Find("LalayTool_Visual/GripAnchor"), hand,
                    animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate), animator.transform.forward);
                gun.enabled = true;
                var originalLayer = player.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer => renderer.enabled).Select(renderer => renderer.gameObject).Distinct()
                    .ToDictionary(gameObject => gameObject, gameObject => gameObject.layer);
                foreach (var item in originalLayer) item.Key.layer = 30;
                var originalMaterial = gun.sharedMaterial;
                foreach (var palette in Palettes)
                {
                    gun.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/LalayTool_{palette.Name}.mat");
                    string file = Path.Combine(output, $"{player.name}-{palette.Name}.png");
                    Capture(player, animator, head, leftFoot, file);
                    string closeFile = Path.Combine(output, $"{player.name}-{palette.Name}-Close.png");
                    CaptureClose(animator, holder, closeFile);
                }
                gun.sharedMaterial = originalMaterial;
                foreach (var item in originalLayer) item.Key.layer = item.Value;
                gun.enabled = false;
            }
            Debug.Log("MINI-187 RENDER PASS: six full-character colorway proofs in Logs/Tasks/MINI-187/. Scene not saved.");
        }

        static void Capture(PlayerController player, Animator animator, Transform head, Transform foot, string file)
        {
            var cameraObject = new GameObject("MINI-187 Proof Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.15f, .17f, .19f);
            camera.cullingMask = 1 << 30;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(1.1f, (head.position.y - foot.position.y) * .68f);
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 20f;
            var focus = (head.position + foot.position) * .5f;
            cameraObject.transform.position = focus + animator.transform.forward * 2.8f + animator.transform.right * 1.1f + Vector3.up * .25f;
            cameraObject.transform.LookAt(focus);
            var lightObject = new GameObject("MINI-187 Proof Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.cullingMask = 1 << 30;
            lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            var rt = new RenderTexture(900, 1200, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = rt;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(900, 1200, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 900, 1200), 0, 0);
            image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        static void CaptureClose(Animator animator, Transform holder, string file)
        {
            var cameraObject = new GameObject("MINI-187 Close Proof Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.15f, .17f, .19f);
            camera.cullingMask = 1 << 30;
            camera.orthographic = true;
            camera.orthographicSize = .32f;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 10f;
            var focus = holder.position + holder.forward * .09f + Vector3.up * .03f;
            cameraObject.transform.position = focus + animator.transform.forward * .75f + animator.transform.right * .55f + Vector3.up * .2f;
            cameraObject.transform.LookAt(focus);
            var lightObject = new GameObject("MINI-187 Close Proof Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.cullingMask = 1 << 30;
            lightObject.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            var rt = new RenderTexture(1000, 800, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            camera.targetTexture = rt;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(1000, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1000, 800), 0, 0);
            image.Apply();
            File.WriteAllBytes(file, image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
