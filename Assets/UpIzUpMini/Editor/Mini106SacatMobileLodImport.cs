using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    internal sealed class Mini106SacatMobileLodPostprocessor : AssetPostprocessor
    {
        internal const string Folder = "Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Mobile/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder, StringComparison.OrdinalIgnoreCase) ||
                !assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                return;

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
            importer.normalSmoothingAngle = 80f;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.maxBonesPerVertex = 4;
            importer.minBoneWeight = 0.001f;
            importer.isReadable = false;
        }
    }

    public static class Mini106SacatMobileLodImport
    {
        private const string Folder = Mini106SacatMobileLodPostprocessor.Folder;
        private const string MaterialPath = "Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Materials/SacatModularBase.mat";
        private const string SourceProofPath = "Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Prefabs/SacatModularBase_ImportProof.prefab";
        private const string PrefabFolder = Folder + "Prefabs";
        private const string CombinedPrefabPath = PrefabFolder + "/SacatModularBase_Mobile.prefab";
        private const string EvidenceFolder = "Logs/Tasks/MINI-106";

        private static readonly string[] Labels = { "LOD0", "LOD1", "LOD2" };
        private static readonly int[] TriangleCaps = { 25000, 12000, 4500 };

        [Serializable]
        private sealed class LodAudit
        {
            public string label;
            public string modelPath;
            public bool avatarValid;
            public bool avatarHuman;
            public int fingerBonesFound;
            public int fingerBonesExpected;
            public int rendererCount;
            public int materialCount;
            public int vertices;
            public int triangles;
            public int maximumBonesPerVertex;
        }

        [Serializable]
        private sealed class MobileAudit
        {
            public LodAudit[] lods;
            public string combinedPrefabPath;
            public int lodGroupCount;
            public int lodRendererCount;
            public string comparisonScreenshot;
        }

        [MenuItem("Tools/Up Iz Up Mini/MINI-106/Build Validate Capture Mobile LODs")]
        public static void BuildValidateCapture()
        {
            Directory.CreateDirectory(EvidenceFolder);
            EnsureFolder(PrefabFolder);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) throw new InvalidOperationException("MINI-106 requires the approved MINI-105 material.");

            List<GameObject> prefabs = new List<GameObject>();
            List<LodAudit> audits = new List<LodAudit>();
            for (int i = 0; i < Labels.Length; i++)
            {
                string label = Labels[i];
                string modelPath = Folder + $"SacatModularBase_{label}.fbx";
                AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null) throw new InvalidOperationException("MINI-106 model import failed: " + modelPath);

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = $"SacatModularBase_{label}";
                instance.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                try
                {
                    AssignMaterial(instance, material);
                    string prefabPath = PrefabFolder + $"/SacatModularBase_{label}.prefab";
                    GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    if (saved == null) throw new InvalidOperationException("MINI-106 prefab save failed: " + prefabPath);
                    prefabs.Add(saved);
                    LodAudit audit = Validate(modelPath, saved, label);
                    audits.Add(audit);
                    if (!audit.avatarValid || !audit.avatarHuman)
                        throw new InvalidOperationException(label + " is not a valid Humanoid.");
                    if (audit.fingerBonesFound != audit.fingerBonesExpected)
                        throw new InvalidOperationException($"{label} finger chain incomplete: {audit.fingerBonesFound}/{audit.fingerBonesExpected}.");
                    if (audit.maximumBonesPerVertex > 4 || audit.triangles > TriangleCaps[i])
                        throw new InvalidOperationException($"{label} exceeds its mobile budget.");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            GameObject combined = BuildCombinedPrefab(prefabs, material);
            LODGroup group = combined.GetComponent<LODGroup>();
            LOD[] lods = group.GetLODs();
            MobileAudit mobileAudit = new MobileAudit
            {
                lods = audits.ToArray(),
                combinedPrefabPath = CombinedPrefabPath,
                lodGroupCount = lods.Length,
                lodRendererCount = lods.Sum(lod => lod.renderers.Length),
                comparisonScreenshot = Path.Combine(EvidenceFolder, "Sacat-Mobile-LOD-Comparison.png").Replace('\\', '/')
            };
            UnityEngine.Object.DestroyImmediate(combined);

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceProofPath);
            if (source == null) throw new InvalidOperationException("MINI-106 source proof prefab missing.");
            CaptureComparison(new[] { source, prefabs[0], prefabs[1], prefabs[2] },
                new[] { "100K SOURCE", "25K LOD0", "12K LOD1", "4.5K LOD2" }, mobileAudit.comparisonScreenshot);
            CaptureSingle(prefabs[0], Path.Combine(EvidenceFolder, "Sacat-Mobile-LOD0-front.png"), true);
            CaptureSingle(prefabs[0], Path.Combine(EvidenceFolder, "Sacat-Mobile-LOD0-back.png"), false);
            CaptureSingle(prefabs[1], Path.Combine(EvidenceFolder, "Sacat-Mobile-LOD1-front.png"), true);
            CaptureSingle(prefabs[2], Path.Combine(EvidenceFolder, "Sacat-Mobile-LOD2-front.png"), true);

            File.WriteAllText(Path.Combine(EvidenceFolder, "Sacat-Mobile-LOD-Audit.json"), JsonUtility.ToJson(mobileAudit, true));
            AssetDatabase.SaveAssets();
            Debug.Log($"MINI-106 PASS: LOD0 {audits[0].triangles}, LOD1 {audits[1].triangles}, LOD2 {audits[2].triangles}; " +
                      $"fingers 30/30; max weights 4; LODGroup {lods.Length}; evidence {mobileAudit.comparisonScreenshot}");
        }

        private static void AssignMaterial(GameObject root, Material material)
        {
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();
                renderer.quality = SkinQuality.Bone4;
            }
        }

        private static LodAudit Validate(string modelPath, GameObject prefab, string label)
        {
            Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
            HashSet<string> names = new HashSet<string>(transforms.Select(t => t.name), StringComparer.Ordinal);
            List<string> fingers = new List<string>();
            foreach (string side in new[] { "L", "R" })
                foreach (string digit in new[] { "Thumb", "Index", "Mid", "Ring", "Pinky" })
                    for (int joint = 1; joint <= 3; joint++) fingers.Add($"CC_Base_{side}_{digit}{joint}");

            int vertices = 0, triangles = 0, maxWeights = 0, materials = 0;
            SkinnedMeshRenderer[] renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null) continue;
                vertices += mesh.vertexCount;
                for (int sub = 0; sub < mesh.subMeshCount; sub++) triangles += (int)(mesh.GetIndexCount(sub) / 3);
                materials += renderer.sharedMaterials.Length;
                using (var counts = mesh.GetBonesPerVertex())
                    for (int i = 0; i < counts.Length; i++) maxWeights = Mathf.Max(maxWeights, counts[i]);
            }

            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
            return new LodAudit
            {
                label = label,
                modelPath = modelPath,
                avatarValid = avatar != null && avatar.isValid,
                avatarHuman = avatar != null && avatar.isHuman,
                fingerBonesFound = fingers.Count(names.Contains),
                fingerBonesExpected = fingers.Count,
                rendererCount = renderers.Length,
                materialCount = materials,
                vertices = vertices,
                triangles = triangles,
                maximumBonesPerVertex = maxWeights
            };
        }

        private static GameObject BuildCombinedPrefab(IReadOnlyList<GameObject> prefabs, Material material)
        {
            GameObject root = new GameObject("SacatModularBase_Mobile");
            GameObject visualRig = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[0]);
            visualRig.name = "VisualRig";
            visualRig.transform.SetParent(root.transform, false);
            AssignMaterial(visualRig, material);
            SkinnedMeshRenderer lod0Renderer = visualRig.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Dictionary<string, Transform> baseBones = visualRig.GetComponentsInChildren<Transform>(true)
                .GroupBy(t => t.name).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            List<Renderer> renderers = new List<Renderer> { lod0Renderer };

            for (int index = 1; index < prefabs.Count; index++)
            {
                GameObject temporary = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[index]);
                try
                {
                    SkinnedMeshRenderer source = temporary.GetComponentInChildren<SkinnedMeshRenderer>(true);
                    GameObject holder = new GameObject("Renderer_" + Labels[index]);
                    holder.transform.SetParent(lod0Renderer.transform.parent, false);
                    holder.transform.localPosition = source.transform.localPosition;
                    holder.transform.localRotation = source.transform.localRotation;
                    holder.transform.localScale = source.transform.localScale;
                    SkinnedMeshRenderer target = holder.AddComponent<SkinnedMeshRenderer>();
                    target.sharedMesh = source.sharedMesh;
                    target.sharedMaterials = new[] { material };
                    target.quality = SkinQuality.Bone4;
                    target.updateWhenOffscreen = false;
                    target.localBounds = source.localBounds;
                    target.bones = source.bones.Select(bone => baseBones.TryGetValue(bone.name, out Transform mapped) ? mapped : null).ToArray();
                    target.rootBone = source.rootBone != null && baseBones.TryGetValue(source.rootBone.name, out Transform rootBone)
                        ? rootBone : lod0Renderer.rootBone;
                    if (target.bones.Any(bone => bone == null)) throw new InvalidOperationException("MINI-106 failed to remap an LOD bone.");
                    renderers.Add(target);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(temporary);
                }
            }

            LODGroup group = root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.animateCrossFading = false;
            group.SetLODs(new[]
            {
                new LOD(0.30f, new[] { renderers[0] }),
                new LOD(0.12f, new[] { renderers[1] }),
                new LOD(0.04f, new[] { renderers[2] })
            });
            group.RecalculateBounds();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, CombinedPrefabPath);
            if (saved == null) throw new InvalidOperationException("MINI-106 combined prefab save failed.");
            UnityEngine.Object.DestroyImmediate(root);
            return (GameObject)PrefabUtility.InstantiatePrefab(saved);
        }

        private static void CaptureComparison(IReadOnlyList<GameObject> prefabs, IReadOnlyList<string> labels, string outputPath)
        {
            GameObject stage = new GameObject("MINI106_ComparisonStage");
            List<GameObject> subjects = new List<GameObject>();
            try
            {
                for (int i = 0; i < prefabs.Count; i++)
                {
                    GameObject subject = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
                    subject.transform.SetParent(stage.transform, false);
                    subject.transform.position = new Vector3((i - 1.5f) * 1.25f, 0f, 0f);
                    subjects.Add(subject);
                    TextMesh label = new GameObject("Label_" + labels[i]).AddComponent<TextMesh>();
                    label.transform.SetParent(stage.transform, false);
                    label.transform.position = subject.transform.position + new Vector3(0f, 2.25f, 0f);
                    label.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    label.text = labels[i];
                    label.anchor = TextAnchor.MiddleCenter;
                    label.alignment = TextAlignment.Center;
                    label.fontSize = 42;
                    label.characterSize = 0.025f;
                    label.color = Color.white;
                }
                CaptureStage(stage, subjects.SelectMany(s => s.GetComponentsInChildren<Renderer>(true)).ToArray(), outputPath, true, 1600, 900, 1.28f);
            }
            finally { UnityEngine.Object.DestroyImmediate(stage); }
        }

        private static void CaptureSingle(GameObject prefab, string outputPath, bool front)
        {
            GameObject stage = new GameObject("MINI106_SingleStage");
            try
            {
                GameObject subject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                subject.transform.SetParent(stage.transform, false);
                CaptureStage(stage, subject.GetComponentsInChildren<Renderer>(true), outputPath, front, 768, 1024, 2.25f);
            }
            finally { UnityEngine.Object.DestroyImmediate(stage); }
        }

        private static void CaptureStage(GameObject stage, Renderer[] renderers, string outputPath, bool front, int width, int height, float distanceFactor)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Camera camera = new GameObject("Camera").AddComponent<Camera>();
            camera.transform.SetParent(stage.transform, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.018f, 0.03f, 1f);
            camera.fieldOfView = 28f;
            Light key = new GameObject("Key").AddComponent<Light>();
            key.transform.SetParent(stage.transform, false);
            key.type = LightType.Directional; key.intensity = 1.15f; key.transform.rotation = Quaternion.Euler(32f, -38f, 0f);
            Light fill = new GameObject("Fill").AddComponent<Light>();
            fill.transform.SetParent(stage.transform, false);
            fill.type = LightType.Directional; fill.intensity = 0.55f; fill.transform.rotation = Quaternion.Euler(20f, 145f, 0f);
            Vector3 target = bounds.center + Vector3.up * bounds.extents.y * 0.03f;
            float distance = Mathf.Max(3f, Mathf.Max(bounds.size.y, bounds.size.x) * distanceFactor);
            camera.transform.position = target + new Vector3(bounds.size.y * 0.35f, bounds.size.y * 0.05f, front ? distance : -distance);
            camera.transform.LookAt(target);

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/'); string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
