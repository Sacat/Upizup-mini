using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 (combat bug-fix pass): patches every already-
    /// placed HumanoidAnimationManager in the live GrandBayProof scene so
    /// its "Melee" action entry points at the new Mixamo jab-punch clip
    /// instead of the old weapon-swing clip - additive/in-place, not a
    /// full scene rebuild (re-running the whole builder risks the same
    /// manual-placement damage documented elsewhere in this project).
    /// Mini011PhaseBSetup.cs's own GetSharedActionEntries() source was
    /// already updated to match, for a future full rebuild. Delete after
    /// use.</summary>
    public static class Mini120PatchPunchClip
    {
        private const string NewClipPath = "Assets/Mixamo/Animations/Mixamo_JabPunch.fbx";

        [MenuItem("Up Iz Up Mini/MINI-120/Patch Punch Clip In Live Scene (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(NewClipPath);
            if (newClip == null) { Debug.LogError("MINI-120 PATCH: couldn't load the new clip at " + NewClipPath); return; }

            var managers = Object.FindObjectsByType<HumanoidAnimationManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int patched = 0, alreadyCorrect = 0, noMeleeEntry = 0;

            foreach (var manager in managers)
            {
                var so = new SerializedObject(manager);
                var actionsProp = so.FindProperty("actions");
                if (actionsProp == null) continue;

                bool foundMelee = false;
                for (int i = 0; i < actionsProp.arraySize; i++)
                {
                    var element = actionsProp.GetArrayElementAtIndex(i);
                    var idProp = element.FindPropertyRelative("id");
                    if (idProp == null || idProp.stringValue != SimpleMeleeCombat.ActionId) continue;

                    foundMelee = true;
                    var clipProp = element.FindPropertyRelative("clip");
                    if (clipProp.objectReferenceValue == newClip)
                    {
                        alreadyCorrect++;
                    }
                    else
                    {
                        clipProp.objectReferenceValue = newClip;
                        patched++;
                    }
                }

                if (!foundMelee) noMeleeEntry++;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log($"MINI-120 PATCH: {managers.Length} HumanoidAnimationManager(s) found - patched={patched}, alreadyCorrect={alreadyCorrect}, noMeleeEntry={noMeleeEntry}.");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI-120 PATCH: scene saved.");
        }
    }
}
