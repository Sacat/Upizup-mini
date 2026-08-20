using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    public static class Mini088BossCValidation
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const string VisualProfilePath = "Assets/UpIzUpMini/Data/Character/BossCVisualProfile.asset";
        private const string ChainProfilePath = "Assets/UpIzUpMini/Data/Equipment/BossCChainPlacement.asset";
        private const string SacatProfilePath = "Assets/UpIzUpMini/Data/Equipment/SacatChainPlacement.asset";

        [MenuItem("Up Iz Up Mini/MINI-088/Validate Boss C Manual Body And Chain")]
        public static void Validate()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var boss = GameObject.Find("NPC_BossC");
            var sacat = GameObject.Find("Sacat");
            var visual = boss != null ? boss.transform.Find("Visual") : null;
            var bossChain = visual != null ? FindChild(visual, "BossChain_18k") : null;
            var visualProfile = AssetDatabase.LoadAssetAtPath<CharacterVisualProfile>(VisualProfilePath);
            var chainProfile = AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(ChainProfilePath);
            var sacatProfile = AssetDatabase.LoadAssetAtPath<AccessoryPlacementProfile>(SacatProfilePath);

            Require(boss != null && sacat != null && visual != null, "Boss C, Sacat, or Boss C Visual is missing.");
            Require(visualProfile != null && visualProfile.useManualScale, "Boss C visual profile is missing or disabled.");
            Require(chainProfile != null && chainProfile.useManualPlacement, "Boss C chain profile is missing or disabled.");
            Require(bossChain != null, "Rebuilt Boss C chain is missing.");
            Require(Close(visual.localScale, visualProfile.localScale), "Boss C visual scale does not match the captured profile.");
            Require(Close(bossChain.localPosition, chainProfile.localPosition)
                && Quaternion.Angle(bossChain.localRotation, Quaternion.Euler(chainProfile.localEulerAngles)) < 0.01f
                && Close(bossChain.localScale, chainProfile.localScale),
                "Boss C chain root does not match the captured profile.");

            foreach (var pose in chainProfile.fittedChildren)
            {
                var child = bossChain.Find(pose.relativePath);
                Require(child != null, $"Boss C fitted chain child '{pose.relativePath}' is missing.");
                Require(Close(child.localPosition, pose.localPosition)
                    && Quaternion.Angle(child.localRotation, Quaternion.Euler(pose.localEulerAngles)) < 0.01f
                    && Close(child.localScale, pose.localScale),
                    $"Boss C fitted chain child '{pose.relativePath}' does not match the captured profile.");
            }

            Require(FindChild(boss.transform, "MANUAL_CHAIN_PREVIEW_BOSSC") == null,
                "Editor-only Boss C preview survived the canonical rebuild.");
            Require(sacatProfile != null && sacatProfile.useManualPlacement
                && Close(sacatProfile.localPosition, new Vector3(-0.047002427f, 0.48330128f, 0.07208218f))
                && Close(sacatProfile.localScale, new Vector3(0.9118362f, 1.1559348f, 0.9118362f))
                && sacatProfile.fittedChildren != null && sacatProfile.fittedChildren.Count == 4,
                "Sacat's VA-002 chain lock changed during Boss C work.");

            float sacatWidth = ShoulderWidth(sacat.transform.Find("Visual"));
            float bossWidth = ShoulderWidth(visual);
            Require(Mathf.Abs(sacatWidth - bossWidth) < 0.005f,
                $"Boss C shoulder width {bossWidth:F4}m no longer matches Sacat {sacatWidth:F4}m.");

            Debug.Log($"MINI-088 VALIDATION PASS: captured Boss C body scale {visual.localScale}, shoulder width {bossWidth:F4}m matches Sacat {sacatWidth:F4}m, all {chainProfile.fittedChildren.Count} fitted chain transforms rebuild exactly, and Sacat VA-002 remains unchanged.");
        }

        private static float ShoulderWidth(Transform visual)
        {
            var animator = visual != null ? visual.GetComponentInChildren<Animator>() : null;
            var left = animator != null ? animator.GetBoneTransform(HumanBodyBones.LeftUpperArm) : null;
            var right = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightUpperArm) : null;
            return left != null && right != null ? Vector3.Distance(left.position, right.position) : 0f;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            return null;
        }

        private static bool Close(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 0.0001f;

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new System.InvalidOperationException("MINI-088 VALIDATION FAIL: " + message);
        }
    }
}
