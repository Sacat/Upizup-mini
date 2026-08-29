using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 (combat bug-fix pass, not yet numbered as its own
    /// task): user chose to try Mixamo's free "Punching" (Jab Punch) clip
    /// as a real unarmed punch animation, replacing the current placeholder
    /// (a one-handed WEAPON swing clip, HumanM@Attack1H01_R.fbx, reused
    /// with no weapon in hand - the actual root cause of "the character
    /// looks warped when punching", confirmed by inspecting the already-
    /// owned Kevin Iglesias pack and finding it has zero bare-fist
    /// animations, every "Attack" clip in it is 1H/2H/Polearm/Shield
    /// weapon-based).
    ///
    /// Sets the imported FBX to Humanoid so Mecanim retargets it onto
    /// Sacat's own avatar automatically - any two Humanoid avatars share
    /// animation clips without needing a matching source skeleton. Delete
    /// after use once the swap itself is committed.</summary>
    public static class Mini120SetupMixamoPunch
    {
        private const string FbxPath = "Assets/Mixamo/Animations/Mixamo_JabPunch.fbx";

        private static readonly string[] ComboFbxPaths =
        {
            "Assets/Mixamo/Animations/Mixamo_JabPunch.fbx",
            "Assets/Mixamo/Animations/Mixamo_Hook.fbx",
            "Assets/Mixamo/Animations/Mixamo_RightHook.fbx",
            "Assets/Mixamo/Animations/Mixamo_PunchCombo4.fbx",
            "Assets/Mixamo/Animations/Mixamo_ComboPunch8.fbx",
        };

        [MenuItem("Up Iz Up Mini/MINI-120/Setup Mixamo Punch Import (one-off)")]
        public static void Run()
        {
            SetupOne(FbxPath);
        }

        [MenuItem("Up Iz Up Mini/MINI-120/Setup Mixamo Combo Clips Import (one-off)")]
        public static void RunCombo()
        {
            foreach (var path in ComboFbxPaths) SetupOne(path);
        }

        private static void SetupOne(string fbxPath)
        {
            AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) { Debug.LogError("MINI-120 SETUP: couldn't find ModelImporter for " + fbxPath); return; }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            // The dummy Mixamo character mesh came along with "With Skin" -
            // we only need the animation curves, not its mesh/materials.
            importer.importAnimation = true;
            importer.SaveAndReimport();

            var clips = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            int clipCount = 0;
            foreach (var obj in clips)
            {
                if (obj is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    Debug.Log($"MINI-120 SETUP: [{fbxPath}] found clip '{clip.name}', length={clip.length:F2}s, frameRate={clip.frameRate}, isHumanMotion={clip.isHumanMotion}, isLooping={clip.isLooping}");
                    clipCount++;
                }
            }

            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(fbxPath);
            Debug.Log($"MINI-120 SETUP: [{fbxPath}] import done. animationType={importer.animationType}, avatar valid={(avatar != null && avatar.isValid)}, avatar isHuman={(avatar != null && avatar.isHuman)}, clips found={clipCount}.");
        }
    }
}
