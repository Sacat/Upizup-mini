using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-066. Dumps what is actually inside the purchased Animo Mocap
    /// motorcycle FBX files - clip names, lengths, loop flags and rig type.
    ///
    /// Exists because the store listing promised "30 animations" but the
    /// download is 6 FBX files, and the user reported seeing a mounting
    /// animation that has no correspondingly-named file. Rather than guess
    /// which clip is which (and then build a rider around a wrong
    /// assumption), this reports the ground truth so the mount/ride/pillion
    /// clips can be chosen from real names.
    /// </summary>
    public static class Mini066MotoClipInspector
    {
        private const string MotoFolder = "Assets/UpIzUpMini/Art/Animations/Moto";

        [MenuItem("Up Iz Up Mini/MINI-066/Inspect Moto Animation Clips")]
        public static void Inspect()
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { MotoFolder });
            if (guids.Length == 0)
            {
                Debug.LogError($"MINI-066 CLIPS: no model files found in {MotoFolder}.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== MINI-066 MOTO CLIP INSPECTION ===");

            int totalClips = 0;

            foreach (var guid in guids.OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;

                sb.AppendLine();
                sb.AppendLine($"--- {System.IO.Path.GetFileName(path)} ---");
                sb.AppendLine($"  rig: {(importer != null ? importer.animationType.ToString() : "?")}");

                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    // Unity emits a hidden __preview__ clip per model; not real content.
                    .Where(c => !c.name.StartsWith("__preview__"))
                    .ToList();

                if (clips.Count == 0)
                {
                    sb.AppendLine("  (no AnimationClips found)");
                    continue;
                }

                foreach (var c in clips)
                {
                    totalClips++;
                    sb.AppendLine($"  clip: \"{c.name}\"  len={c.length:F2}s  loop={c.isLooping}  humanoid={c.humanMotion}");
                }

                if (importer != null && importer.clipAnimations != null && importer.clipAnimations.Length > 0)
                {
                    sb.AppendLine($"  (importer defines {importer.clipAnimations.Length} split clip(s))");
                    foreach (var ca in importer.clipAnimations)
                        sb.AppendLine($"    split: \"{ca.name}\"  frames {ca.firstFrame}-{ca.lastFrame}  loop={ca.loopTime}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"TOTAL real clips found: {totalClips}");
            Debug.Log(sb.ToString());
        }
    }
}
