using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-066. Reimports the purchased Animo Mocap motorcycle FBX files as
    /// Humanoid instead of Generic.
    ///
    /// Why this is needed: the pack ships every clip as Generic
    /// (confirmed by Mini066MotoClipInspector - all 30 clips reported
    /// humanoid=False), but every character in this project is a Humanoid
    /// rig (all 9 were converted and verified back in MINI-016). Generic
    /// clips cannot retarget onto a Humanoid avatar, so as-shipped these
    /// animations cannot play on Franki or Sacat at all.
    ///
    /// Reimporting as Humanoid makes Unity map the source skeleton onto its
    /// standard bone structure. That succeeds or fails depending on whether
    /// the mocap skeleton is close enough to a standard biped - which is why
    /// this reports per-file whether a valid avatar was actually produced,
    /// rather than assuming it worked.
    /// </summary>
    public static class Mini066MotoClipHumanoid
    {
        private const string MotoFolder = "Assets/UpIzUpMini/Art/Animations/Moto";

        [MenuItem("Up Iz Up Mini/MINI-066/Convert Moto Clips To Humanoid")]
        public static void Convert()
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { MotoFolder });
            if (guids.Length == 0)
            {
                Debug.LogError($"MINI-066 HUMANOID: no model files found in {MotoFolder}.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== MINI-066 MOTO -> HUMANOID CONVERSION ===");

            int ok = 0, failed = 0;

            foreach (var guid in guids.OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                importer.animationType = ModelImporterAnimationType.Human;
                // CreateFromThisModel: build the avatar from this file's own
                // skeleton. The alternative (CopyFromOther) would need a
                // source avatar whose bone names match, which these mocap
                // files have no reason to share with the project's character
                // pack.
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();

                // Ground truth: did Unity actually produce a VALID humanoid
                // avatar, or did it silently fall back? isValid is the only
                // honest answer here.
                var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
                bool valid = avatar != null && avatar.isValid && avatar.isHuman;

                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__"))
                    .ToList();
                int humanClips = clips.Count(c => c.humanMotion);

                if (valid && humanClips > 0) ok++; else failed++;

                sb.AppendLine($"{(valid && humanClips > 0 ? "OK  " : "FAIL")} {System.IO.Path.GetFileName(path)}  " +
                              $"avatar valid={valid}  humanoid clips={humanClips}/{clips.Count}");
            }

            sb.AppendLine();
            sb.AppendLine(failed == 0
                ? $"MINI-066 HUMANOID PASS: all {ok} file(s) converted, clips are retargetable onto the project's Humanoid characters."
                : $"MINI-066 HUMANOID PARTIAL: {ok} ok, {failed} failed - failed files cannot retarget onto Humanoid characters as-is.");

            Debug.Log(sb.ToString());
        }
    }
}
