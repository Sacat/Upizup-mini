using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Two related fixes/diagnostics for MINI-013:
    ///
    /// 1. ForceHumanoidRigs - the Floreswa character pack ships most models
    ///    as Generic rigs (animationType 2) and only male01_1/male02_1 as
    ///    Humanoid (3). Humanoid animation clips cannot retarget onto a
    ///    Generic rig, so every NPC using those models rendered its bind
    ///    (T) pose no matter what animator controller was assigned. This
    ///    converts them to Humanoid and reimports.
    ///
    /// 2. MeasureLocomotionSpeeds - reads the authored root velocity of the
    ///    root-motion locomotion clips, so movement speeds can be matched
    ///    to the animation instead of guessed at. Mismatch here is what
    ///    makes running look like skating.
    /// </summary>
    public static class Mini013RigAndAnimAudit
    {
        private static readonly string[] CharacterModels =
        {
            "Assets/Floreswa/Models/male01_1.fbx",
            "Assets/Floreswa/Models/male01_2.fbx",
            "Assets/Floreswa/Models/male01_3.fbx",
            "Assets/Floreswa/Models/male02_1.fbx",
            "Assets/Floreswa/Models/male02_2.fbx",
            "Assets/Floreswa/Models/male02_3.fbx",
            "Assets/Floreswa/Models/male03_1.fbx",
            "Assets/Floreswa/Models/male03_2.fbx",
            "Assets/Floreswa/Models/male03_3.fbx",
        };

        [MenuItem("Up Iz Up Mini/MINI-013/Force Humanoid Rigs")]
        public static void ForceHumanoidRigs()
        {
            int converted = 0;
            foreach (var path in CharacterModels)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"RIGFIX: no importer at {path}");
                    continue;
                }

                if (importer.animationType == ModelImporterAnimationType.Human)
                {
                    Debug.Log($"RIGFIX: {path} already Humanoid");
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
                converted++;
                Debug.Log($"RIGFIX: converted {path} -> Humanoid");
            }

            // Verify the avatars actually came out valid; an invalid avatar
            // would still T-pose, so this must not be assumed.
            foreach (var path in CharacterModels)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var animator = go != null ? go.GetComponent<Animator>() : null;
                var avatar = animator != null ? animator.avatar : null;
                string state = avatar == null ? "NO AVATAR"
                    : (avatar.isValid ? (avatar.isHuman ? "valid humanoid" : "valid but NOT human") : "INVALID");
                Debug.Log($"RIGCHECK: {path} -> {state}");
            }

            Debug.Log($"RIGFIX DONE: converted={converted}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Up Iz Up Mini/MINI-013/Measure Locomotion Speeds")]
        public static void MeasureLocomotionSpeeds()
        {
            // The [RM] variants keep root motion in the clip, so their root
            // displacement over time is the animation's authored speed.
            Measure("Walk", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/RootMotion/HumanM@Walk01_Forward [RM].fbx");
            Measure("Run", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/RootMotion/HumanM@Run01_Forward [RM].fbx");
            Measure("Sprint", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Sprint/RootMotion/HumanM@Sprint01_Forward [RM].fbx");

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Measure(string label, string fbxPath)
        {
            AnimationClip clip = null;
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__")) { clip = c; break; }
            }

            if (clip == null)
            {
                Debug.LogWarning($"ANIMSPEED {label}: clip not found at {fbxPath}");
                return;
            }

            // averageSpeed is Unity's own measure of the clip's root motion.
            Vector3 v = clip.averageSpeed;
            Debug.Log($"ANIMSPEED {label}: length={clip.length:F2}s averageSpeed={v} " +
                      $"planarSpeed={new Vector2(v.x, v.z).magnitude:F2} m/s hasRootMotion={clip.hasMotionCurves || clip.hasRootCurves}");
        }
    }
}
