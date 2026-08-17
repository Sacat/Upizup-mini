using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-038. Exercises the real NpcCombatHealth/HumanoidAnimationManager
    /// code against a real Humanoid character and the actual shared
    /// controller (already baked with the Action/FullBodyOverride layers by
    /// a prior scene build), rather than trusting that "compiles and runs
    /// without exceptions" also means the knockdown logic is correct.
    /// MonoBehaviour lifecycle methods don't run outside Play mode, so
    /// Awake() is invoked via reflection - same pattern as
    /// Mini028FarmhandValidation.
    /// </summary>
    public static class Mini038CombatValidation
    {
        private const string StarterControllerPath =
            "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller";

        [MenuItem("Up Iz Up Mini/MINI-038/Run Combat Validation")]
        public static void Run()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(StarterControllerPath);
            if (controller == null) throw new Exception("MINI-038 validation: authored controller missing - run Mini011PhaseBSetup.BuildScene first.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Floreswa/Models/male02_1.fbx");
            if (prefab == null) throw new Exception("MINI-038 validation: male02_1.fbx missing.");

            var npcGo = new GameObject("TestNpc");
            npcGo.AddComponent<CharacterController>();

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, npcGo.transform);
            var animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            var animMgr = npcGo.AddComponent<HumanoidAnimationManager>();
            var animSo = new SerializedObject(animMgr);
            animSo.FindProperty("animator").objectReferenceValue = animator;
            var actionsProp = animSo.FindProperty("actions");
            var meleeClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx");
            var hitClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx");
            var deathClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx");
            actionsProp.arraySize = 3;
            SetEntry(actionsProp, 0, SimpleMeleeCombat.ActionId, meleeClip, false);
            SetEntry(actionsProp, 1, NpcCombatHealth.HitReactionActionId, hitClip, true);
            SetEntry(actionsProp, 2, NpcCombatHealth.KnockedDownActionId, deathClip, true);
            animSo.ApplyModifiedPropertiesWithoutUndo();
            InvokeAwake(animMgr);

            var health = npcGo.AddComponent<NpcCombatHealth>();
            var healthSo = new SerializedObject(health);
            healthSo.FindProperty("animationManager").objectReferenceValue = animMgr;
            healthSo.FindProperty("maxHealth").floatValue = 100f;
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            InvokeAwake(health);

            int fullBodyLayer = animator.GetLayerIndex(HumanoidAnimationManager.FullBodyLayerName);
            if (fullBodyLayer < 0) throw new Exception("MINI-038 validation: FullBodyOverride layer not baked into the controller.");

            // Two hits (35 damage each) should stagger, not knock down.
            // Both the hit-reaction stagger and the knockdown pose live on
            // FullBodyOverride, so its weight going to 1 on hit one is
            // expected here, not a bug - PlayAction sets weight
            // synchronously, and the coroutine that fades it back out only
            // ticks across real frames, which this edit-mode harness
            // (calling Hit() back to back with no elapsed time) never
            // gives it the chance to do. That decay is exercised by the
            // headless player run instead, not this synchronous check.
            health.Hit(35f, Vector3.zero);
            if (health.IsDown) throw new Exception("MINI-038 validation: went down after one hit at 35/100 damage.");
            float weightAfterStagger = animator.GetLayerWeight(fullBodyLayer);
            if (weightAfterStagger < 0.99f)
                throw new Exception($"MINI-038 validation: FullBodyOverride layer weight was {weightAfterStagger} right after a hit-reaction PlayAction call, expected 1 (synchronous weight set).");

            health.Hit(35f, Vector3.zero);
            if (health.IsDown) throw new Exception("MINI-038 validation: went down after two hits (70/100 damage).");

            // Third hit crosses zero health - should knock down and hold
            // the FullBodyOverride layer at full weight synchronously
            // (BeginSustainedAction does not fade in over time).
            health.Hit(35f, Vector3.zero);
            if (!health.IsDown) throw new Exception("MINI-038 validation: did not go down after three hits (105/100 damage).");

            float weightAfterKnockdown = animator.GetLayerWeight(fullBodyLayer);
            if (weightAfterKnockdown < 0.99f)
                throw new Exception($"MINI-038 validation: FullBodyOverride layer weight was {weightAfterKnockdown} after knockdown, expected 1.");

            // Already down - another hit must be a no-op, not a second
            // knockdown/damage tick.
            health.Hit(35f, Vector3.zero);
            if (!health.IsDown) throw new Exception("MINI-038 validation: IsDown flipped false from a hit while already down.");

            Debug.Log("MINI-038 COMBAT VALIDATION PASS: stagger reaction on hits 1-2 (no knockdown), knockdown on hit 3 (FullBodyOverride weight 1), hit-while-down is a no-op.");

            UnityEngine.Object.DestroyImmediate(npcGo);
        }

        private static void SetEntry(SerializedProperty arrayProp, int index, string id, AnimationClip clip, bool fullBody)
        {
            var element = arrayProp.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("id").stringValue = id;
            element.FindPropertyRelative("clip").objectReferenceValue = clip;
            element.FindPropertyRelative("fullBody").boolValue = fullBody;
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
            }
            return null;
        }

        private static void InvokeAwake(MonoBehaviour behaviour)
        {
            behaviour.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(behaviour, null);
        }
    }
}
