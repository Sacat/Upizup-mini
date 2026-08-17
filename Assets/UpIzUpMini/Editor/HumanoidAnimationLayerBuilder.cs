using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UpIzUpMini.Character;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-031. Bakes the "Action" and "FullBodyOverride" layers that
    /// <see cref="HumanoidAnimationManager"/> drives at runtime into a
    /// shared AnimatorController — normally the authored
    /// StarterAssetsThirdPerson controller every character in the scene
    /// already uses, so this is additive and runs once for the whole
    /// project rather than per character.
    ///
    /// Idempotent by design: called on every scene rebuild (see
    /// Mini011PhaseBSetup), it must not duplicate layers or states, and
    /// must pick up clip changes without wiping anything else in the
    /// controller (in particular the authored base locomotion layer, which
    /// this never touches).
    /// </summary>
    public static class HumanoidAnimationLayerBuilder
    {
        /// <summary>
        /// Ensures the Action/FullBodyOverride layers and one Animator
        /// state per entry exist on <paramref name="controller"/>. Pass the
        /// union of every character's action list — this is meant to be
        /// called once per controller with everything any character in the
        /// scene might play, since the layers/states are shared.
        /// </summary>
        public static void EnsureActionLayers(AnimatorController controller, IEnumerable<HumanoidAnimationManager.ActionEntry> allActions)
        {
            if (controller == null) return;

            int actionLayerIndex = EnsureLayer(controller, HumanoidAnimationManager.ActionLayerName, BuildUpperBodyMask());
            int fullBodyLayerIndex = EnsureLayer(controller, HumanoidAnimationManager.FullBodyLayerName, null);

            EnsureEmptyState(controller, actionLayerIndex);
            EnsureEmptyState(controller, fullBodyLayerIndex);

            foreach (var entry in allActions)
            {
                if (string.IsNullOrEmpty(entry.id) || entry.clip == null) continue;
                int layerIndex = entry.fullBody ? fullBodyLayerIndex : actionLayerIndex;
                EnsureState(controller, layerIndex, entry.id, entry.clip);
            }

            EditorUtility.SetDirty(controller);
        }

        /// <summary>
        /// Finds a layer by name, creating it (default weight 0, with the
        /// given mask if any) if missing. Returns its index. Safe to call
        /// repeatedly — never adds a duplicate.
        /// </summary>
        private static int EnsureLayer(AnimatorController controller, string layerName, AvatarMask mask)
        {
            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName) return i;
            }

            controller.AddLayer(layerName);
            layers = controller.layers;
            int newIndex = layers.Length - 1;
            layers[newIndex].defaultWeight = 0f;
            if (mask != null) layers[newIndex].avatarMask = mask;
            controller.layers = layers;
            return newIndex;
        }

        /// <summary>
        /// Adds a motion-less default state named "Empty" to the given
        /// layer if it doesn't already have one — the state PlayAction
        /// fades back into once a one-shot action finishes, so the layer
        /// visibly does nothing even on the rare frame its weight isn't
        /// quite at zero yet.
        /// </summary>
        private static void EnsureEmptyState(AnimatorController controller, int layerIndex)
        {
            var stateMachine = controller.layers[layerIndex].stateMachine;
            foreach (var child in stateMachine.states)
            {
                if (child.state.name == HumanoidAnimationManager.EmptyStateName)
                {
                    stateMachine.defaultState = child.state;
                    return;
                }
            }

            var empty = stateMachine.AddState(HumanoidAnimationManager.EmptyStateName);
            empty.motion = null;
            stateMachine.defaultState = empty;
        }

        /// <summary>
        /// Adds (or updates the motion of, if it already exists — so
        /// re-authoring a clip takes effect on the next scene rebuild
        /// without leaving a stale duplicate state behind) one action
        /// state. No transitions are added; HumanoidAnimationManager drives
        /// entry/exit entirely through code via CrossFadeInFixedTime, since
        /// these are one-off context moves, not part of a parameter-driven
        /// state graph.
        /// </summary>
        private static void EnsureState(AnimatorController controller, int layerIndex, string stateName, AnimationClip clip)
        {
            var stateMachine = controller.layers[layerIndex].stateMachine;
            foreach (var child in stateMachine.states)
            {
                if (child.state.name == stateName)
                {
                    child.state.motion = clip;
                    return;
                }
            }

            var state = stateMachine.AddState(stateName);
            state.motion = clip;
        }

        /// <summary>
        /// Upper body only (spine/chest/head/arms/fingers) — legs, feet and
        /// root stay driven entirely by the base locomotion layer
        /// underneath, so an Action-layer clip (e.g. a melee swing) plays
        /// while the character keeps walking or running instead of
        /// planting their feet.
        /// </summary>
        private static AvatarMask BuildUpperBodyMask()
        {
            const string path = "Assets/UpIzUpMini/Art/Animations/UpperBodyMask.mask";
            var existing = AssetDatabase.LoadAssetAtPath<AvatarMask>(path);
            if (existing != null) return existing;

            var mask = new AvatarMask();
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, false);

            var dir = System.IO.Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets/UpIzUpMini/Art", "Animations");
            }
            AssetDatabase.CreateAsset(mask, path);
            return mask;
        }
    }
}
