using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    internal sealed class Mini105SacatModularAssetPostprocessor : AssetPostprocessor
    {
        internal const string ModelPath = "Assets/UpIzUpMini/Art/Characters/Modular/Sacat/SacatModularBase_Rigged.fbx";
        internal const string TexturePath = "Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Textures/SacatModularBase_BaseColor_2K.png";

        private void OnPreprocessModel()
        {
            if (!string.Equals(assetPath, ModelPath, StringComparison.OrdinalIgnoreCase))
                return;

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.skinWeights = ModelImporterSkinWeights.Standard;
            importer.maxBonesPerVertex = 4;
            importer.minBoneWeight = 0.001f;
            importer.isReadable = false;
        }

        private void OnPreprocessTexture()
        {
            if (!string.Equals(assetPath, TexturePath, StringComparison.OrdinalIgnoreCase))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.isReadable = false;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 1024;
            android.textureCompression = TextureImporterCompression.Compressed;
            android.format = TextureImporterFormat.Automatic;
            importer.SetPlatformTextureSettings(android);
        }
    }

    public static class Mini105SacatModularImport
    {
        private const string ModelPath = Mini105SacatModularAssetPostprocessor.ModelPath;
        private const string TexturePath = Mini105SacatModularAssetPostprocessor.TexturePath;
        private const string MaterialFolder = "Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Materials";
        private const string MaterialPath = MaterialFolder + "/SacatModularBase.mat";
        private const string PrefabFolder = "Assets/UpIzUpMini/Art/Characters/Modular/Sacat/Prefabs";
        private const string PrefabPath = PrefabFolder + "/SacatModularBase_ImportProof.prefab";
        private const string EvidenceFolder = "Logs/Tasks/MINI-105";

        [Serializable]
        private sealed class ImportAudit
        {
            public bool avatarValid;
            public bool avatarHuman;
            public int transformCount;
            public int requiredFingerBonesFound;
            public int requiredFingerBonesExpected;
            public int skinnedRendererCount;
            public int vertexCount;
            public int triangleCount;
            public int maximumImportedBonesPerVertex;
            public int materialCount;
            public int textureWidth;
            public int textureHeight;
            public string modelPath;
            public string prefabPath;
            public string screenshotPath;
        }

        [MenuItem("Tools/Up Iz Up Mini/MINI-105/Build Validate Capture")]
        public static void BuildValidateCapture()
        {
            Directory.CreateDirectory(EvidenceFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(PrefabFolder);

            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (baseColor == null)
                throw new InvalidOperationException("MINI-105 texture import failed: " + TexturePath);
            if (model == null)
                throw new InvalidOperationException("MINI-105 model import failed: " + ModelPath);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard");
                if (shader == null)
                    throw new InvalidOperationException("MINI-105 requires the Built-in Standard shader for the isolated proof.");
                material = new Material(shader) { name = "SacatModularBase" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.mainTexture = baseColor;
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.28f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = "SacatModularBase_ImportProof";
            try
            {
                foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    int slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                    renderer.sharedMaterials = Enumerable.Repeat(material, slotCount).ToArray();
                    renderer.quality = SkinQuality.Bone4;
                }

                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out bool saved);
                if (!saved)
                    throw new InvalidOperationException("MINI-105 prefab save failed: " + PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject proofPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (proofPrefab == null)
                throw new InvalidOperationException("MINI-105 proof prefab could not be reloaded.");

            ImportAudit audit = Validate(proofPrefab, baseColor);
            audit.screenshotPath = Path.Combine(EvidenceFolder, "Sacat-UnityImport-front-three-quarter.png").Replace('\\', '/');
            Capture(proofPrefab, audit.screenshotPath, true);
            Capture(proofPrefab, Path.Combine(EvidenceFolder, "Sacat-UnityImport-back-three-quarter.png").Replace('\\', '/'), false);
            File.WriteAllText(Path.Combine(EvidenceFolder, "Sacat-UnityImport-Audit.json"), JsonUtility.ToJson(audit, true));

            if (!audit.avatarValid || !audit.avatarHuman)
                throw new InvalidOperationException("MINI-105 Humanoid Avatar is not valid.");
            if (audit.requiredFingerBonesFound != audit.requiredFingerBonesExpected)
                throw new InvalidOperationException($"MINI-105 finger chain incomplete: {audit.requiredFingerBonesFound}/{audit.requiredFingerBonesExpected}.");
            if (audit.maximumImportedBonesPerVertex > 4)
                throw new InvalidOperationException("MINI-105 imported mesh exceeds the four-weight mobile cap.");
            if (audit.skinnedRendererCount < 1 || audit.vertexCount < 1)
                throw new InvalidOperationException("MINI-105 skinned renderer validation failed.");

            Debug.Log($"MINI-105 PASS: valid Humanoid; fingers {audit.requiredFingerBonesFound}/{audit.requiredFingerBonesExpected}; " +
                      $"vertices {audit.vertexCount}; triangles {audit.triangleCount}; max weights {audit.maximumImportedBonesPerVertex}; " +
                      $"evidence {audit.screenshotPath}");
        }

        private static ImportAudit Validate(GameObject prefab, Texture2D texture)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
                HashSet<string> names = new HashSet<string>(transforms.Select(t => t.name), StringComparer.Ordinal);
                string[] sides = { "L", "R" };
                string[] digits = { "Thumb", "Index", "Mid", "Ring", "Pinky" };
                List<string> required = new List<string>();
                foreach (string side in sides)
                    foreach (string digit in digits)
                        for (int joint = 1; joint <= 3; joint++)
                            required.Add($"CC_Base_{side}_{digit}{joint}");

                SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                int vertices = 0;
                int triangles = 0;
                int maximumWeights = 0;
                int materialCount = 0;
                foreach (SkinnedMeshRenderer renderer in renderers)
                {
                    Mesh mesh = renderer.sharedMesh;
                    if (mesh == null) continue;
                    vertices += mesh.vertexCount;
                    triangles += (int)(mesh.GetIndexCount(0) / 3);
                    materialCount += renderer.sharedMaterials.Length;
                    using (var bonesPerVertex = mesh.GetBonesPerVertex())
                    {
                        for (int i = 0; i < bonesPerVertex.Length; i++)
                            maximumWeights = Mathf.Max(maximumWeights, bonesPerVertex[i]);
                    }
                }

                Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
                return new ImportAudit
                {
                    avatarValid = avatar != null && avatar.isValid,
                    avatarHuman = avatar != null && avatar.isHuman,
                    transformCount = transforms.Length,
                    requiredFingerBonesFound = required.Count(names.Contains),
                    requiredFingerBonesExpected = required.Count,
                    skinnedRendererCount = renderers.Length,
                    vertexCount = vertices,
                    triangleCount = triangles,
                    maximumImportedBonesPerVertex = maximumWeights,
                    materialCount = materialCount,
                    textureWidth = texture.width,
                    textureHeight = texture.height,
                    modelPath = ModelPath,
                    prefabPath = PrefabPath
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void Capture(GameObject prefab, string outputPath, bool front)
        {
            GameObject stage = new GameObject("MINI105_SnapshotStage");
            GameObject subject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            subject.transform.SetParent(stage.transform, false);
            subject.transform.position = Vector3.zero;

            Camera camera = new GameObject("MINI105_Camera").AddComponent<Camera>();
            camera.transform.SetParent(stage.transform, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.012f, 0.018f, 0.03f, 1f);
            camera.fieldOfView = 28f;

            Light key = new GameObject("Key").AddComponent<Light>();
            key.transform.SetParent(stage.transform, false);
            key.type = LightType.Directional;
            key.intensity = 1.15f;
            key.color = new Color(1f, 0.88f, 0.78f);
            key.transform.rotation = Quaternion.Euler(32f, -38f, 0f);

            Light fill = new GameObject("Fill").AddComponent<Light>();
            fill.transform.SetParent(stage.transform, false);
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(0.62f, 0.76f, 1f);
            fill.transform.rotation = Quaternion.Euler(20f, 145f, 0f);

            try
            {
                Renderer[] renderers = subject.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                Vector3 target = bounds.center + Vector3.up * bounds.extents.y * 0.03f;
                float distance = Mathf.Max(3f, bounds.size.y * 2.25f);
                camera.transform.position = target + new Vector3(
                    bounds.size.y * 0.72f,
                    bounds.size.y * 0.08f,
                    front ? distance : -distance);
                camera.transform.LookAt(target);

                RenderTexture rt = new RenderTexture(768, 1024, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                Texture2D image = new Texture2D(768, 1024, TextureFormat.RGBA32, false);
                RenderTexture previous = RenderTexture.active;
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 768, 1024), 0, 0);
                image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(rt);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stage);
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
