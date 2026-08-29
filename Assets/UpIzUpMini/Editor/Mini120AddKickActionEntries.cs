using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 combo request, user: "can you add a kick at the
    /// end" then "put one on each main character." Same fix shape as
    /// Mini120AddComboActionEntries.cs (adding a missing ActionEntry row
    /// is what actually makes PlayAction succeed - see that file's own
    /// header for the full story of why), but per-character: only Sacat
    /// and Franki get a kick entry, and each gets a DIFFERENT id/clip
    /// (MeleeMoveLibrary.GetChainFor already picks the right chain by
    /// name at runtime - this tool just needs to make sure the right
    /// entry actually exists on each of their HumanoidAnimationManagers).
    /// Delete after use.</summary>
    public static class Mini120AddKickActionEntries
    {
        [MenuItem("Up Iz Up Mini/MINI-120/Add Kick Action Entries (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var kickSacatClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MeleeMoveLibrary.KickSacatClipPath);
            var kickFrankiClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MeleeMoveLibrary.KickFrankiClipPath);

            int added = AddKickTo("Sacat", MeleeMoveLibrary.KickSacatId, kickSacatClip);
            added += AddKickTo("Franki", MeleeMoveLibrary.KickFrankiId, kickFrankiClip);

            Debug.Log($"MINI-120 ADD KICK ENTRIES: {added} kick entries added.");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI-120 ADD KICK ENTRIES: scene saved.");
        }

        private static int AddKickTo(string characterName, string kickId, AnimationClip clip)
        {
            var go = GameObject.Find(characterName);
            if (go == null) { Debug.LogError($"MINI-120 ADD KICK ENTRIES: no '{characterName}' in scene."); return 0; }

            var manager = go.GetComponent<HumanoidAnimationManager>();
            if (manager == null) { Debug.LogError($"MINI-120 ADD KICK ENTRIES: '{characterName}' has no HumanoidAnimationManager."); return 0; }

            var so = new SerializedObject(manager);
            var actionsProp = so.FindProperty("actions");
            if (actionsProp == null) return 0;

            // MINI-120 follow-up, user: "i didnt see the kick" - real
            // cause was fullBody=false (upper-body-masked, legs excluded
            // - see Mini011PhaseBSetup.GetSharedActionEntries' own
            // comment on this same fix). Re-authors the entry in place
            // if it already exists (from the first, wrongly-masked
            // pass) instead of skipping, so re-running this after fixing
            // the shared entry definition actually takes effect here too.
            for (int i = 0; i < actionsProp.arraySize; i++)
            {
                var idProp = actionsProp.GetArrayElementAtIndex(i).FindPropertyRelative("id");
                if (idProp != null && idProp.stringValue == kickId)
                {
                    var existing = actionsProp.GetArrayElementAtIndex(i);
                    existing.FindPropertyRelative("clip").objectReferenceValue = clip;
                    existing.FindPropertyRelative("fullBody").boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"MINI-120 ADD KICK ENTRIES: '{characterName}' already had '{kickId}' - re-authored fullBody=true in place.");
                    return 1;
                }
            }

            int newIndex = actionsProp.arraySize;
            actionsProp.arraySize++;
            var newElement = actionsProp.GetArrayElementAtIndex(newIndex);
            newElement.FindPropertyRelative("id").stringValue = kickId;
            newElement.FindPropertyRelative("clip").objectReferenceValue = clip;
            newElement.FindPropertyRelative("fullBody").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"MINI-120 ADD KICK ENTRIES: added '{kickId}' (clip={(clip != null ? clip.name : "NULL")}, fullBody=true) to '{characterName}'.");
            return 1;
        }
    }
}
