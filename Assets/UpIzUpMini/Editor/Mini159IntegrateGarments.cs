using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-159: integrate the motion-tested (MINI-158), coloured (MINI-157)
    /// reshaped garments into the LIVE playable Sacat and Franki.
    ///
    /// Technique: reuse the mesh/material from the already-imported,
    /// already-motion-tested reshaped FBX assets exactly as tested - do NOT
    /// re-derive or re-edit anything. Only the renderer's `bones`/`rootBone`
    /// array is rebuilt, mapped by BONE NAME onto the LIVE character's own
    /// existing skeleton Transforms (both skeletons are the same rig at the
    /// same rest pose, just from two separate imports of the same source).
    /// This means:
    ///   - No Animator, avatar, controller, or GameObject hierarchy change.
    ///   - No other component's serialized reference (CharacterEquipment,
    ///     PlayerController, camera target, etc.) is touched or invalidated -
    ///     they all keep pointing at the exact same GameObjects.
    ///   - Only `SkinnedMeshRenderer.sharedMesh`, `.sharedMaterial`,
    ///     `.bones` and `.rootBone` change on the specific renderers whose
    ///     garment was reshaped.
    /// See Docs/WorkPackets/MINI-159.md.
    /// </summary>
    public static class Mini159IntegrateGarments
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        const string FrankiSourceFbx = "Assets/UpIzUpMini/Art/Characters/Garments/Franki_ReshapedGarments_Colored.fbx";
        const string SacatSourceFbx = "Assets/UpIzUpMini/Art/Characters/Garments/Sacat_Ch06_Reshaped.fbx";
        const string Out = "Logs/Tasks/MINI-159";

        static void Require(bool ok, string text)
        {
            if (!ok) throw new Exception("MINI159INTEGRATE: " + text);
        }

        [MenuItem("Up Iz Up Mini/MINI-159/1 Preview Only (no save, renders proof)")]
        public static void PreviewOnly() => Run(save: false, render: true);

        [MenuItem("Up Iz Up Mini/MINI-159/2 Integrate And Save Scene")]
        public static void IntegrateAndSave() => Run(save: true, render: false);

        private static void Run(bool save, bool render)
        {
            Directory.CreateDirectory(Out);
            var evidence = new List<string>();
            string before = save ? Hash(Scene) : null;

            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            IntegrateCharacter("Sacat", SacatSourceFbx, new[] { "Ch06" }, evidence);
            IntegrateCharacter("Franki", FrankiSourceFbx, new[] { "Ch28_Hoody", "Ch28_Pants" }, evidence);

            if (render)
            {
                RenderProof(evidence);
            }

            if (save)
            {
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                evidence.Add($"Scene SAVED. Hash before={before} after={Hash(Scene)}");
            }
            else
            {
                evidence.Add("Preview only - scene NOT saved (changes are in-memory this session only).");
            }
            File.WriteAllLines($"{Out}/integration-log-{(save ? "save" : "preview")}.txt", evidence);
            Debug.Log("MINI159_INTEGRATE_PASS " + evidence.Count + " lines, save=" + save);

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void RenderProof(List<string> evidence)
        {
            // Move each character up into clear open air (same isolation
            // trick Mini120RenderPunchPose.cs uses) and use controlled
            // ambient/lighting instead of the live scene's own environment -
            // the live scene's sun/skybox/reflections blew the first
            // in-place attempt out to near-solid white. Position is restored
            // after capture; not saved either way in preview mode, and this
            // whole method never runs when save=true.
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

            var key = new GameObject("MINI159 key light").AddComponent<Light>();
            key.type = LightType.Directional; key.intensity = 1.7f; key.transform.rotation = Quaternion.Euler(35, -35, 0);
            var fillLight = new GameObject("MINI159 fill light").AddComponent<Light>();
            fillLight.type = LightType.Directional; fillLight.intensity = .8f; fillLight.transform.rotation = Quaternion.Euler(25, 145, 0);
            var camera = new GameObject("MINI159 proof camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.82f, .82f, .85f);
            camera.nearClipPlane = .05f; camera.farClipPlane = 200; camera.fieldOfView = 32;

            float offsetX = 0f;
            foreach (var name in new[] { "Sacat", "Franki" })
            {
                var root = GameObject.Find(name);
                if (root == null) continue;
                Vector3 originalPos = root.transform.position;
                root.transform.position = new Vector3(offsetX, 300f, 0f);
                offsetX += 3f;

                var bodyRenderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .FirstOrDefault(r => r.name == "Ch06" || r.name == "Ch28_Body");
                if (bodyRenderer != null)
                {
                    var bounds = bodyRenderer.bounds;
                    Vector3 focus = bounds.center;
                    float dist = Mathf.Max(bounds.size.magnitude, 1.0f) * 1.6f;
                    Capture(camera, focus - root.transform.forward * dist + Vector3.up * 0.1f, focus, $"{Out}/Live-{name}-InScene-Front.png");
                    Capture(camera, focus + root.transform.right * dist + Vector3.up * 0.1f, focus, $"{Out}/Live-{name}-InScene-Side.png");
                    evidence.Add($"Rendered live in-scene proof for {name} (temporarily moved to open air for a clean shot, restored after)");
                }

                root.transform.position = originalPos;
            }

            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(key.gameObject);
            UnityEngine.Object.DestroyImmediate(fillLight.gameObject);
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientSkyColor = originalSky;
            RenderSettings.ambientEquatorColor = originalEq;
            RenderSettings.ambientGroundColor = originalGround;
            RenderSettings.fog = originalFog;
        }

        private static void Capture(Camera camera, Vector3 position, Vector3 target, string path)
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
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
        }

        private static string Hash(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
        }

        private static void IntegrateCharacter(string liveRootName, string sourceFbxPath, string[] rendererNames, List<string> evidence)
        {
            var liveRoot = GameObject.Find(liveRootName);
            Require(liveRoot != null, $"live root '{liveRootName}' not found");
            var liveAnimator = liveRoot.GetComponentInChildren<Animator>(true);
            Require(liveAnimator != null && liveAnimator.avatar != null && liveAnimator.avatar.isHuman, $"{liveRootName}: no valid live Humanoid animator");

            // Map every bone name in the live skeleton to its live Transform.
            var liveBonesByName = new Dictionary<string, Transform>();
            foreach (var t in liveRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!liveBonesByName.ContainsKey(t.name)) liveBonesByName[t.name] = t;
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourceFbxPath);
            Require(source != null, $"{liveRootName}: source FBX missing at {sourceFbxPath}");
            var tempInstance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                foreach (var rendererName in rendererNames)
                {
                    var liveRenderer = liveRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == rendererName);
                    Require(liveRenderer != null, $"{liveRootName}: live renderer '{rendererName}' not found");

                    var sourceRenderer = tempInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == rendererName);
                    Require(sourceRenderer != null, $"{liveRootName}: source renderer '{rendererName}' not found in {sourceFbxPath}");
                    Require(sourceRenderer.sharedMesh != null, $"{liveRootName}/{rendererName}: source mesh is null");

                    var newBones = new Transform[sourceRenderer.bones.Length];
                    for (int i = 0; i < sourceRenderer.bones.Length; i++)
                    {
                        var srcBone = sourceRenderer.bones[i];
                        Require(srcBone != null, $"{liveRootName}/{rendererName}: source bone[{i}] is null");
                        Require(liveBonesByName.TryGetValue(srcBone.name, out var liveBone),
                            $"{liveRootName}/{rendererName}: no live bone named '{srcBone.name}'");
                        newBones[i] = liveBone;
                    }

                    Transform newRootBone = null;
                    if (sourceRenderer.rootBone != null && liveBonesByName.TryGetValue(sourceRenderer.rootBone.name, out var mappedRoot))
                        newRootBone = mappedRoot;

                    int oldVertCount = liveRenderer.sharedMesh != null ? liveRenderer.sharedMesh.vertexCount : -1;
                    liveRenderer.sharedMesh = sourceRenderer.sharedMesh;
                    liveRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
                    liveRenderer.bones = newBones;
                    if (newRootBone != null) liveRenderer.rootBone = newRootBone;

                    evidence.Add($"{liveRootName}/{rendererName}: mesh {oldVertCount}verts -> {sourceRenderer.sharedMesh.vertexCount}verts, " +
                                 $"material -> '{sourceRenderer.sharedMaterial.name}', bones remapped ({newBones.Length}) by name onto live skeleton");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempInstance);
            }
        }
    }
}
