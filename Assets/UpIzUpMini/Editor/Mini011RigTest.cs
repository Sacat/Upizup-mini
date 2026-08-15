using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// One-off diagnostic for MINI-011 Phase B: does the Low Poly Character
    /// Pack's FBX rig convert cleanly to a Unity Humanoid avatar? Needed
    /// before Smart/Strong switching (retargeting) can be built on it.
    /// Not part of the ongoing build - safe to delete once the answer is
    /// recorded in Docs/MINI-011-VISUAL-PLAN.md.
    /// </summary>
    public static class Mini011RigTest
    {
        private static readonly string[] TestModels =
        {
            "Assets/Floreswa/Models/male01_1.fbx",
            "Assets/Floreswa/Models/male02_1.fbx",
        };

        [MenuItem("Up Iz Up Mini/MINI-011/Test Humanoid Rig Conversion")]
        public static void Run()
        {
            bool allPass = true;

            foreach (var path in TestModels)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogError($"RIGTEST: could not load ModelImporter for {path}");
                    allPass = false;
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(path);
                bool ok = avatar != null && avatar.isValid && avatar.isHuman;
                allPass &= ok;

                Debug.Log(ok
                    ? $"RIGTEST PASS: {path} -> valid Humanoid avatar."
                    : $"RIGTEST FAIL: {path} -> avatar={(avatar == null ? "null" : "non-null")}, " +
                      $"isValid={(avatar != null && avatar.isValid)}, isHuman={(avatar != null && avatar.isHuman)}");
            }

            Debug.Log(allPass ? "RIGTEST OVERALL: PASS" : "RIGTEST OVERALL: FAIL");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(allPass ? 0 : 1);
            }
        }
    }
}
