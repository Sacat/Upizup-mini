using UnityEditor;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Prepares the locomotion clips copied from the larger Up Iz Up
    /// project (Unity StarterAssets / Mixamo-sourced) for use here:
    /// forces Humanoid rigs so they retarget onto the Floreswa characters,
    /// and loops the cyclic ones. Then measures their authored root speed
    /// so movement can be matched to the animation rather than guessed.
    /// </summary>
    public static class Mini014AnimationImport
    {
        private const string Folder = "Assets/UpIzUpMini/Art/Animations";

        private static readonly (string file, bool loop)[] Clips =
        {
            ("Stand--Idle.anim.fbx", true),
            ("Locomotion--Walk_N.anim.fbx", true),
            ("Locomotion--Run_N.anim.fbx", true),
            ("Locomotion--Run_S.anim.fbx", true),
            ("Jump--Jump.anim.fbx", false),
            ("Jump--InAir.anim.fbx", true),
            ("Locomotion--Walk_N_Land.anim.fbx", false),
            ("Locomotion--Run_N_Land.anim.fbx", false),
        };

        [MenuItem("Up Iz Up Mini/MINI-014/Prepare Locomotion Clips")]
        public static void Prepare()
        {
            foreach (var (file, loop) in Clips)
            {
                string path = $"{Folder}/{file}";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"ANIMPREP: no importer at {path}");
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

                var clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        clips[i].loopTime = loop;
                        // Keep the character rooted; PlayerController drives
                        // horizontal movement via CharacterController, so
                        // baked XZ root motion would fight it.
                        clips[i].lockRootHeightY = true;
                        clips[i].keepOriginalPositionY = true;
                    }
                    importer.clipAnimations = clips;
                }

                importer.SaveAndReimport();
                Debug.Log($"ANIMPREP: {file} -> Humanoid, loop={loop}");
            }

            Measure();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Measure()
        {
            foreach (var (file, _) in Clips)
            {
                string path = $"{Folder}/{file}";
                AnimationClip clip = null;
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (a is AnimationClip c && !c.name.StartsWith("__preview__")) { clip = c; break; }
                }

                if (clip == null)
                {
                    Debug.LogWarning($"ANIMSPEED {file}: no clip");
                    continue;
                }

                Vector3 v = clip.averageSpeed;
                Debug.Log($"ANIMSPEED {file}: name='{clip.name}' len={clip.length:F2}s " +
                          $"planar={new Vector2(v.x, v.z).magnitude:F2} m/s loop={clip.isLooping}");
            }
        }
    }
}
