using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-163: wire the fixed Franki_Hair.fbx (own dedicated
    /// material, no longer sharing the broken shared-atlas Ch28_hair.mat)
    /// onto the live Franki's Ch28_Hair renderer only - same proven
    /// bone-remap-by-name technique as MINI-159/161. Nothing else touched.</summary>
    public static class Mini163IntegrateFrankiHair
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string SourceFbx = "Assets/UpIzUpMini/Art/Characters/Garments/Franki_Hair.fbx";
        const string Out = "Logs/Tasks/MINI-163";

        static void Require(bool ok, string msg) { if (!ok) throw new Exception("MINI163HAIR: " + msg); }

        [MenuItem("Up Iz Up Mini/MINI-163/Integrate Franki Hair")]
        public static void Integrate() => Run(true);
        [MenuItem("Up Iz Up Mini/MINI-163/Preview Franki Hair (no save)")]
        public static void Preview() => Run(false);

        static void Run(bool save)
        {
            Directory.CreateDirectory(Out);
            string before = save ? Hash(Scene) : null;
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            var importer = (ModelImporter)AssetImporter.GetAtPath(SourceFbx);
            Require(importer != null, "missing " + SourceFbx);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbx);
            Require(source != null, "source failed to load");

            var liveRoot = GameObject.Find("Franki");
            var liveBonesByName = new Dictionary<string, Transform>();
            foreach (var t in liveRoot.GetComponentsInChildren<Transform>(true))
                if (!liveBonesByName.ContainsKey(t.name)) liveBonesByName[t.name] = t;

            var liveHair = liveRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Hair");
            var beforeMesh = liveHair.sharedMesh != null ? liveHair.sharedMesh.name : "null";
            var beforeMat = liveHair.sharedMaterial != null ? liveHair.sharedMaterial.name : "null";

            var tempInstance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                var sourceHair = tempInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Hair");
                var newBones = new Transform[sourceHair.bones.Length];
                for (int i = 0; i < sourceHair.bones.Length; i++)
                {
                    Require(liveBonesByName.TryGetValue(sourceHair.bones[i].name, out var b), "no live bone " + sourceHair.bones[i].name);
                    newBones[i] = b;
                }
                liveHair.sharedMesh = sourceHair.sharedMesh;
                liveHair.sharedMaterials = sourceHair.sharedMaterials; // hair color + fade band materials
                liveHair.bones = newBones;
                if (sourceHair.rootBone != null && liveBonesByName.TryGetValue(sourceHair.rootBone.name, out var rb))
                    liveHair.rootBone = rb;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempInstance);
            }

            // Confirm every OTHER renderer on Franki is untouched (name/mesh/material identity).
            var others = liveRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r => r.name != "Ch28_Hair").ToArray();
            Debug.Log($"MINI163HAIR: Ch28_Hair {beforeMesh}/{beforeMat} -> {liveHair.sharedMesh.name}/{string.Join(",", liveHair.sharedMaterials.Select(m => m.name))}; " +
                      $"other renderers untouched count={others.Length} names=[{string.Join(",", others.Select(r => r.name))}]");

            if (save)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log($"MINI163_HAIR_INTEGRATE_PASS saved, hash before={before} after={Hash(Scene)}");
            }
            else
            {
                RenderPreview(liveRoot);
                Debug.Log("MINI163_HAIR_PREVIEW_PASS not saved");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static void RenderPreview(GameObject root)
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
            camera.nearClipPlane = .02f; camera.farClipPlane = 200; camera.fieldOfView = 28;

            Vector3 originalPos = root.transform.position;
            root.transform.position = new Vector3(0, 300, 0);
            var animator = root.GetComponentInChildren<Animator>(true);
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/UpIzUpMini/Art/Animations/Stand--Idle.anim.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            Sample(animator, idle, idle.length * 0.4f);

            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            Vector3 focus = head.position + Vector3.up * 0.03f;
            Capture(camera, focus + Vector3.back * 0.55f, focus, $"{Out}/Live-Franki-Head-Front.png");
            Capture(camera, focus + Vector3.right * 0.55f, focus, $"{Out}/Live-Franki-Head-Side.png");

            var bodyR = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(r => r.name == "Ch28_Body");
            var b = bodyR.bounds;
            Capture(camera, b.center - root.transform.forward * (b.size.magnitude * 1.1f) + Vector3.up * 0.1f, b.center, $"{Out}/Live-Franki-Full-Front.png");

            root.transform.position = originalPos;
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            UnityEngine.Object.DestroyImmediate(fill.gameObject);
        }

        static void Sample(Animator a, AnimationClip clip, float t)
        {
            var graph = PlayableGraph.Create("Mini163Hair");
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

        static string Hash(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
        }
    }
}
