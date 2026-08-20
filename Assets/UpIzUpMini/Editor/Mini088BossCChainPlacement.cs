using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    public static class Mini088BossCChainPlacement
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string ChainPath = "Assets/UpIzUpMini/Art/Accessories/GoldChain18k.prefab";
        private const string SacatProfilePath = "Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset";
        private const string BossChainProfilePath = "Assets/UpIzUpMini/Data/Equipment/BossCChainPlacement.asset";
        private const string BossVisualProfilePath = "Assets/UpIzUpMini/Data/Character/BossCVisualProfile.asset";
        private const string PreviewName = "MANUAL_CHAIN_PREVIEW_BOSSC";

        [MenuItem("Up Iz Up Mini/MINI-088/Prepare Boss C Body And Chain Preview")]
        public static void PreparePreview()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var sacat = GameObject.Find("Sacat");
            var boss = GameObject.Find("NPC_BossC");
            var sacatVisual = sacat != null ? sacat.transform.Find("Visual") : null;
            var bossVisual = boss != null ? boss.transform.Find("Visual") : null;
            if (sacatVisual == null || bossVisual == null)
                throw new System.InvalidOperationException("Sacat or Boss C Visual is missing. Rebuild GrandBayProof first.");

            // Preview reruns must always begin from the generated Boss C body,
            // never compound an earlier scene-only X/Z preview.
            bossVisual.localScale = Vector3.one;
            float sacatShoulders = MeasureShoulderWidth(sacatVisual);
            float bossBefore = MeasureShoulderWidth(bossVisual);
            if (sacatShoulders <= 0f || bossBefore <= 0f)
                throw new System.InvalidOperationException("Could not measure Sacat or Boss C shoulder width.");

            // MINI-083 already matched height. This preview preserves Y and
            // matches shoulder/body width on X only. Z is deliberately left
            // unchanged so matching width cannot flatten or thicken his body.
            Vector3 scale = bossVisual.localScale;
            scale.x *= sacatShoulders / bossBefore;
            bossVisual.localScale = scale;
            float bossAfter = MeasureShoulderWidth(bossVisual);

            var animator = bossVisual.GetComponentInChildren<Animator>();
            var chest = animator != null ? animator.GetBoneTransform(HumanBodyBones.Chest) : null;
            var chainPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChainPath);
            var sacatProfile = AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(SacatProfilePath);
            if (chest == null || chainPrefab == null || sacatProfile == null)
                throw new System.InvalidOperationException("Boss C chest, cleaned chain prefab, or Sacat chain profile is missing.");

            var oldPreview = FindChild(boss.transform, PreviewName);
            if (oldPreview != null) Object.DestroyImmediate(oldPreview.gameObject);

            var bakedChain = FindChild(bossVisual, "BossChain_18k");
            if (bakedChain == null)
                throw new System.InvalidOperationException("Boss C's existing baked chain is missing.");

            var preview = (GameObject)PrefabUtility.InstantiatePrefab(chainPrefab);
            preview.name = PreviewName;
            preview.tag = "EditorOnly";
            preview.transform.SetParent(chest, false);
            preview.transform.localPosition = bakedChain.localPosition;
            preview.transform.localRotation = bakedChain.localRotation;
            preview.transform.localScale = bakedChain.localScale;
            ApplyFittedChildren(preview.transform, sacatProfile);
            CharacterEquipment.NormaliseAccessoryScale(preview.transform, chest, CharacterEquipment.ChainWidth);
            bakedChain.gameObject.SetActive(false);

            Selection.activeGameObject = boss;
            SceneView.lastActiveSceneView?.FrameSelected();
            EditorSceneManager.MarkSceneDirty(boss.scene);
            EditorSceneManager.SaveScene(boss.scene);

            Debug.Log($"MINI-088 PREVIEW READY: Sacat shoulder width={sacatShoulders:F4}m; Boss C before={bossBefore:F4}m; Boss C after={bossAfter:F4}m; Boss C Visual localScale={bossVisual.localScale}. Height Y and depth Z were preserved. Two-piece chain preview created from Sacat's fitted geometry on Boss C's separate chest rig.");
        }

        [MenuItem("Up Iz Up Mini/MINI-088/Capture Approved Boss C Body And Chain")]
        public static void CaptureApproved()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var boss = GameObject.Find("NPC_BossC");
            var visual = boss != null ? boss.transform.Find("Visual") : null;
            var preview = boss != null ? FindChild(boss.transform, PreviewName) : null;
            if (visual == null || preview == null)
                throw new System.InvalidOperationException("Boss C Visual or manual chain preview is missing.");

            EnsureFolder("Assets/UpIzUpMini/Data");
            EnsureFolder("Assets/UpIzUpMini/Data/Character");
            EnsureFolder("Assets/UpIzUpMini/Data/Equipment");

            var visualProfile = AssetDatabase.LoadAssetAtPath<CharacterVisualProfile>(BossVisualProfilePath);
            if (visualProfile == null)
            {
                visualProfile = ScriptableObject.CreateInstance<CharacterVisualProfile>();
                AssetDatabase.CreateAsset(visualProfile, BossVisualProfilePath);
            }
            visualProfile.useManualScale = true;
            visualProfile.localScale = visual.localScale;
            EditorUtility.SetDirty(visualProfile);

            var chainProfile = AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(BossChainProfilePath);
            if (chainProfile == null)
            {
                chainProfile = ScriptableObject.CreateInstance<AccessoryPlacementProfile>();
                AssetDatabase.CreateAsset(chainProfile, BossChainProfilePath);
            }
            chainProfile.useManualPlacement = true;
            chainProfile.localPosition = preview.localPosition;
            chainProfile.localEulerAngles = preview.localEulerAngles;
            chainProfile.localScale = preview.localScale;
            chainProfile.fittedChildren.Clear();
            foreach (var child in preview.GetComponentsInChildren<Transform>(true))
            {
                if (child == preview) continue;
                chainProfile.fittedChildren.Add(new AccessoryPlacementProfile.ChildTransformPose
                {
                    relativePath = RelativePath(preview, child),
                    localPosition = child.localPosition,
                    localEulerAngles = child.localEulerAngles,
                    localScale = child.localScale
                });
            }
            EditorUtility.SetDirty(chainProfile);
            AssetDatabase.SaveAssets();

            Debug.Log($"MINI-088 CAPTURED: Boss C Visual scale={visualProfile.localScale}; chain root position={chainProfile.localPosition}, rotation={chainProfile.localEulerAngles}, scale={chainProfile.localScale}; fitted children={chainProfile.fittedChildren.Count}.");
        }

        private static float MeasureShoulderWidth(Transform visualRoot)
        {
            var animator = visualRoot.GetComponentInChildren<Animator>();
            var left = animator != null ? animator.GetBoneTransform(HumanBodyBones.LeftUpperArm) : null;
            var right = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightUpperArm) : null;
            return left != null && right != null ? Vector3.Distance(left.position, right.position) : 0f;
        }

        private static void ApplyFittedChildren(Transform accessoryRoot, AccessoryPlacementProfile profile)
        {
            foreach (var pose in profile.fittedChildren)
            {
                if (pose == null || string.IsNullOrEmpty(pose.relativePath)) continue;
                Transform child = accessoryRoot.Find(pose.relativePath);
                if (child == null) child = RecreateDuplicate(accessoryRoot, pose.relativePath);
                if (child == null)
                    throw new System.InvalidOperationException($"Cannot reproduce fitted chain child '{pose.relativePath}'.");
                child.localPosition = pose.localPosition;
                child.localRotation = Quaternion.Euler(pose.localEulerAngles);
                child.localScale = pose.localScale;
            }
        }

        private static Transform RecreateDuplicate(Transform root, string path)
        {
            int slash = path.LastIndexOf('/');
            string parentPath = slash >= 0 ? path.Substring(0, slash) : string.Empty;
            string name = slash >= 0 ? path.Substring(slash + 1) : path;
            int suffix = name.LastIndexOf(" (", System.StringComparison.Ordinal);
            if (suffix <= 0 || !name.EndsWith(")", System.StringComparison.Ordinal)) return null;
            Transform parent = string.IsNullOrEmpty(parentPath) ? root : root.Find(parentPath);
            Transform source = parent != null ? parent.Find(name.Substring(0, suffix)) : null;
            if (source == null) return null;
            var copy = Object.Instantiate(source.gameObject, parent, false);
            copy.name = name;
            return copy.transform;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static string RelativePath(Transform root, Transform child)
        {
            var parts = new List<string>();
            for (var current = child; current != null && current != root; current = current.parent)
                parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}
