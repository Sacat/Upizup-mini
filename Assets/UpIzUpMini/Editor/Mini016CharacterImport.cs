using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Imports the proven Franki (Mainchar) and Sacat (Strong) character
    /// models copied from the larger Up Iz Up project, and reports their
    /// avatar quality against the same checks used on the Floreswa models.
    ///
    /// Why this exists: the Floreswa models were authored as Generic rigs
    /// and force-converted to Humanoid in MINI-013. Their bone mapping is
    /// complete (23 bones, no missing) but the avatar's rest pose is
    /// auto-estimated from a model that was not authored in a T-pose, and
    /// retargeting against a bad rest pose is what produced the distorted
    /// "spaghetti legs" locomotion. These models were authored for
    /// Humanoid and are the ones the larger project animates correctly
    /// with these exact clips.
    /// </summary>
    public static class Mini016CharacterImport
    {
        private static readonly string[] Models =
        {
            "Assets/UpIzUpMini/Art/Characters/Mainchar.fbx",
            "Assets/UpIzUpMini/Art/Characters/Strong.fbx",
        };

        [MenuItem("Up Iz Up Mini/MINI-016/Import Franki and Sacat")]
        public static void Import()
        {
            foreach (var path in Models)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogError($"CHARIMPORT: missing {path}");
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = false; // locomotion comes from the shared clips
                importer.SaveAndReimport();
                Debug.Log($"CHARIMPORT: {path} -> Humanoid");
            }

            foreach (var path in Models)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var animator = go != null ? go.GetComponent<Animator>() : null;
                var avatar = animator != null ? animator.avatar : null;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                var desc = importer != null ? importer.humanDescription : default;

                Debug.Log($"CHARCHECK {path}: valid={(avatar != null && avatar.isValid)} " +
                          $"human={(avatar != null && avatar.isHuman)} " +
                          $"humanBones={desc.human?.Length ?? 0} skeletonBones={desc.skeleton?.Length ?? 0}");
            }

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
