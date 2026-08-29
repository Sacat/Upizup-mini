using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 (combat bug-fix pass) fix, user: "i am still
    /// seeing the old punch so put in the new punch." Real cause found:
    /// Mini120PatchPunchClip.cs (the previous attempt) only updated the
    /// per-character ActionEntry DATA on each HumanoidAnimationManager -
    /// that data is used for clip.length/HasState checks, but the actual
    /// clip that plays is baked as the "Melee" state's Motion on the
    /// SHARED StarterAssetsThirdPerson.controller asset every character's
    /// Animator uses (Animator.CrossFadeInFixedTime plays a state by
    /// name/hash on the runtime controller, it never reads the ActionEntry
    /// clip at all). Calling HumanoidAnimationLayerBuilder.EnsureActionLayers
    /// again - the exact same idempotent call BuildScene already makes -
    /// against the real controller asset re-authors that Motion in place,
    /// per EnsureState's own documented behaviour ("re-authoring a clip
    /// takes effect... without leaving a stale duplicate state behind").
    /// Delete after use.</summary>
    public static class Mini120PatchAnimatorController
    {
        private const string ControllerPath = "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller";

        [MenuItem("Up Iz Up Mini/MINI-120/Patch Shared Animator Controller Punch Clip (one-off)")]
        public static void Run()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) { Debug.LogError("MINI-120 PATCH CONTROLLER: couldn't load " + ControllerPath); return; }

            // Read the Melee state's motion BEFORE, so the log proves this
            // actually changed something rather than silently no-op'ing.
            string before = FindMeleeMotionName(controller);

            var entries = Mini011PhaseBSetup.GetSharedActionEntriesPublic();
            HumanoidAnimationLayerBuilder.EnsureActionLayers(controller, entries);
            AssetDatabase.SaveAssets();

            string after = FindMeleeMotionName(controller);
            Debug.Log($"MINI-120 PATCH CONTROLLER: 'Melee' state motion BEFORE='{before}', AFTER='{after}'.");

            if (before == after)
                Debug.LogWarning("MINI-120 PATCH CONTROLLER: motion name unchanged - if this still isn't the new clip, something else is wrong.");
        }

        private static string FindMeleeMotionName(AnimatorController controller)
        {
            foreach (var layer in controller.layers)
            {
                foreach (var child in layer.stateMachine.states)
                {
                    if (child.state.name == "Melee")
                        return child.state.motion != null ? child.state.motion.name : "NULL";
                }
            }
            return "NOT FOUND";
        }
    }
}
