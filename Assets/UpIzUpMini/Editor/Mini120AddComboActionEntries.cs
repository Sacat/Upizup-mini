using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 combo fix, round 3 - the REAL cause. User's
    /// Player.log showed the combo step cycling correctly but
    /// PlayAction returning false for every move except the jab, even
    /// after force-reimporting the Animator Controller (which did not
    /// help, since the controller was never the problem).
    ///
    /// Real cause: HumanoidAnimationManager.PlayAction gates on its OWN
    /// per-character `_byId` dictionary, rebuilt at Awake() from the
    /// SERIALIZED `actions` list - NOT on Animator.HasState directly
    /// (that check only happens after the _byId lookup already
    /// succeeds). Mini120PatchPunchClip.cs (the very first fix, this
    /// session) only replaced the EXISTING "Melee" entry's clip in each
    /// character's `actions` array - it never ADDED entries for the new
    /// "MeleeHook"/"MeleeRightHook"/"MeleeFinisher" ids, because those
    /// ids didn't exist anywhere yet at the time that tool was written.
    /// Every character's saved `actions` list in the live scene still
    /// has ONLY the one "Melee" entry - so _byId.TryGetValue fails
    /// immediately for the other three, before Animator.HasState is
    /// ever consulted. This is exactly why Mini120DiagnoseHasState.cs
    /// (which called Animator.HasState directly, bypassing _byId
    /// entirely) found nothing wrong - it was checking a completely
    /// different, unaffected code path.
    ///
    /// Fix: add the three missing ActionEntry rows (not just patch an
    /// existing one) to every HumanoidAnimationManager in the live
    /// scene. Delete after use.</summary>
    public static class Mini120AddComboActionEntries
    {
        [MenuItem("Up Iz Up Mini/MINI-120/Add Missing Combo Action Entries (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var hookClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MeleeMoveLibrary.HookClipPath);
            var rightHookClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MeleeMoveLibrary.RightHookClipPath);
            var finisherClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(MeleeMoveLibrary.FinisherClipPath);

            (string id, AnimationClip clip)[] missing =
            {
                (MeleeMoveLibrary.HookId, hookClip),
                (MeleeMoveLibrary.RightHookId, rightHookClip),
                (MeleeMoveLibrary.FinisherId, finisherClip),
            };

            var managers = Object.FindObjectsByType<HumanoidAnimationManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int managersPatched = 0, entriesAdded = 0;

            foreach (var manager in managers)
            {
                var so = new SerializedObject(manager);
                var actionsProp = so.FindProperty("actions");
                if (actionsProp == null) continue;

                // Only patch characters that already carry the jab - a
                // character with no melee at all shouldn't suddenly gain
                // combo moves as a side effect of this fix.
                bool hasJab = false;
                for (int i = 0; i < actionsProp.arraySize; i++)
                {
                    var idProp = actionsProp.GetArrayElementAtIndex(i).FindPropertyRelative("id");
                    if (idProp != null && idProp.stringValue == SimpleMeleeCombat.ActionId) { hasJab = true; break; }
                }
                if (!hasJab) continue;

                bool changedThisManager = false;
                foreach (var (id, clip) in missing)
                {
                    bool alreadyHas = false;
                    for (int i = 0; i < actionsProp.arraySize; i++)
                    {
                        var idProp = actionsProp.GetArrayElementAtIndex(i).FindPropertyRelative("id");
                        if (idProp != null && idProp.stringValue == id) { alreadyHas = true; break; }
                    }
                    if (alreadyHas) continue;

                    int newIndex = actionsProp.arraySize;
                    actionsProp.arraySize++;
                    var newElement = actionsProp.GetArrayElementAtIndex(newIndex);
                    newElement.FindPropertyRelative("id").stringValue = id;
                    newElement.FindPropertyRelative("clip").objectReferenceValue = clip;
                    newElement.FindPropertyRelative("fullBody").boolValue = false;
                    entriesAdded++;
                    changedThisManager = true;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                if (changedThisManager) managersPatched++;
            }

            Debug.Log($"MINI-120 ADD COMBO ENTRIES: {managers.Length} HumanoidAnimationManager(s) found, {managersPatched} patched (already had the jab), {entriesAdded} total new entries added.");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI-120 ADD COMBO ENTRIES: scene saved.");
        }
    }
}
