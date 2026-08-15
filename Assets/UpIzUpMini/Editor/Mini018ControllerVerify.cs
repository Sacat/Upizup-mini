using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Verifies the authored StarterAssets controller actually resolved its
    /// animation clips in this project.
    ///
    /// This matters because the controller references clips by GUID. The
    /// animation FBXs were first copied without their .meta files, so Unity
    /// assigned fresh GUIDs and every reference would have been broken -
    /// producing a controller that loads fine but plays nothing. The
    /// original .meta files were copied afterwards to restore the GUIDs;
    /// this confirms that worked rather than assuming it.
    /// </summary>
    public static class Mini018ControllerVerify
    {
        private const string Path = "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller";

        [MenuItem("Up Iz Up Mini/MINI-018/Verify Locomotion Controller")]
        public static void Verify()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Path);
            if (controller == null)
            {
                Debug.LogError($"CTRLVERIFY: could not load {Path}");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            foreach (var p in controller.parameters)
            {
                Debug.Log($"CTRLVERIFY param: {p.name} ({p.type})");
            }

            int states = 0, motions = 0, missing = 0;
            foreach (var layer in controller.layers)
            {
                Inspect(layer.stateMachine, ref states, ref motions, ref missing);
            }

            Debug.Log($"CTRLVERIFY TOTAL: states={states} withMotion={motions} missingMotion={missing}");
            if (missing > 0)
            {
                Debug.LogError("CTRLVERIFY: some states have no motion - clip GUIDs did not resolve.");
            }

            if (Application.isBatchMode) EditorApplication.Exit(missing > 0 ? 1 : 0);
        }

        private static void Inspect(AnimatorStateMachine sm, ref int states, ref int motions, ref int missing)
        {
            foreach (var s in sm.states)
            {
                states++;
                var motion = s.state.motion;
                if (motion == null)
                {
                    missing++;
                    Debug.LogWarning($"CTRLVERIFY: state '{s.state.name}' has NO motion");
                    continue;
                }

                motions++;
                if (motion is BlendTree tree)
                {
                    foreach (var child in tree.children)
                    {
                        if (child.motion == null)
                        {
                            missing++;
                            Debug.LogWarning($"CTRLVERIFY: blend tree '{tree.name}' has an unresolved child clip");
                        }
                        else
                        {
                            Debug.Log($"CTRLVERIFY clip: {tree.name} -> {child.motion.name} @ {child.threshold}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"CTRLVERIFY clip: state '{s.state.name}' -> {motion.name}");
                }
            }

            foreach (var child in sm.stateMachines)
            {
                Inspect(child.stateMachine, ref states, ref motions, ref missing);
            }
        }
    }
}
