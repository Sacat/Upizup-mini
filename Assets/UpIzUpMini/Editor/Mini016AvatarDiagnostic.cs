using System.Text;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Diagnoses why retargeted locomotion looks wrong ("spaghetti legs")
    /// on the Mini's characters when the same clips look correct in the
    /// larger Up Iz Up project.
    ///
    /// Humanoid retargeting only works well when the target avatar's bone
    /// mapping AND its T-pose/skeleton proportions are sane. A model
    /// authored as Generic and force-converted to Humanoid (MINI-013) gets
    /// an auto-generated avatar, which can map bones wrongly or invent a
    /// bad rest pose - both of which produce exactly this symptom.
    ///
    /// This reports, per model: avatar validity, which required human
    /// bones are actually mapped, and the leg-chain bone names, so the
    /// Mini's characters can be compared directly against the larger
    /// project's proven ones.
    /// </summary>
    public static class Mini016AvatarDiagnostic
    {
        private static readonly HumanBodyBones[] Required =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.Head,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand,
        };

        [MenuItem("Up Iz Up Mini/MINI-016/Diagnose Character Avatars")]
        public static void Diagnose()
        {
            string[] models =
            {
                "Assets/Floreswa/Models/male01_1.fbx",
                "Assets/Floreswa/Models/male02_1.fbx",
                "Assets/Floreswa/Models/male03_1.fbx",
            };

            foreach (var path in models) Report(path);

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Report(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (importer == null || go == null)
            {
                Debug.LogWarning($"AVATAR {path}: not found");
                return;
            }

            var animator = go.GetComponent<Animator>();
            var avatar = animator != null ? animator.avatar : null;

            var sb = new StringBuilder();
            sb.AppendLine($"AVATAR {path}");
            sb.AppendLine($"  animationType={importer.animationType} avatarSetup={importer.avatarSetup}");
            sb.AppendLine($"  avatar valid={(avatar != null && avatar.isValid)} human={(avatar != null && avatar.isHuman)}");

            // The human description is the actual bone mapping Unity built.
            var desc = importer.humanDescription;
            sb.AppendLine($"  humanBoneCount={desc.human?.Length ?? 0} skeletonBoneCount={desc.skeleton?.Length ?? 0}");
            sb.AppendLine($"  armStretch={desc.armStretch:F2} legStretch={desc.legStretch:F2} " +
                          $"upperArmTwist={desc.upperArmTwist:F2} feetSpacing={desc.feetSpacing:F2} hasTranslationDoF={desc.hasTranslationDoF}");

            if (desc.human != null)
            {
                foreach (var required in Required)
                {
                    string name = required.ToString();
                    string mappedTo = null;
                    foreach (var h in desc.human)
                    {
                        if (h.humanName == HumanBoneName(required)) { mappedTo = h.boneName; break; }
                    }
                    if (mappedTo == null) sb.AppendLine($"  MISSING MAPPING: {name}");
                }
            }

            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// HumanDescription uses Unity's display names ("LeftUpperLeg" is
        /// "LeftUpperLeg", but several differ from the enum), so map the
        /// few that matter explicitly.
        /// </summary>
        private static string HumanBoneName(HumanBodyBones bone)
        {
            return bone switch
            {
                HumanBodyBones.LeftUpperLeg => "LeftUpperLeg",
                HumanBodyBones.LeftLowerLeg => "LeftLowerLeg",
                HumanBodyBones.LeftFoot => "LeftFoot",
                HumanBodyBones.RightUpperLeg => "RightUpperLeg",
                HumanBodyBones.RightLowerLeg => "RightLowerLeg",
                HumanBodyBones.RightFoot => "RightFoot",
                HumanBodyBones.LeftUpperArm => "LeftUpperArm",
                HumanBodyBones.LeftLowerArm => "LeftLowerArm",
                HumanBodyBones.LeftHand => "LeftHand",
                HumanBodyBones.RightUpperArm => "RightUpperArm",
                HumanBodyBones.RightLowerArm => "RightLowerArm",
                HumanBodyBones.RightHand => "RightHand",
                _ => bone.ToString(),
            };
        }
    }
}
