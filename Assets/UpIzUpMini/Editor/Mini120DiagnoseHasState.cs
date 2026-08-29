using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Combat;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 combo fix, round 2: user's real Player.log shows
    /// the combo step cycling correctly (0->1->2->3->0) but PlayAction
    /// returning false for every move except the jab. Reproduces
    /// HumanoidAnimationManager.PlayAction's exact HasState check
    /// against Sacat's REAL runtime Animator in the live scene (not just
    /// reading the controller asset in isolation, which already showed
    /// the states exist with the right motion) - to find where the two
    /// disagree. Delete after use.</summary>
    public static class Mini120DiagnoseHasState
    {
        [MenuItem("Up Iz Up Mini/MINI-120/Diagnose HasState Mismatch (one-off, read-only)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var sacat = GameObject.Find("Sacat");
            if (sacat == null) { Debug.LogError("MINI-120 DIAGNOSE: no Sacat."); return; }
            var animator = sacat.GetComponentInChildren<Animator>();
            if (animator == null) { Debug.LogError("MINI-120 DIAGNOSE: no Animator on Sacat."); return; }

            Debug.Log($"MINI-120 DIAGNOSE: Sacat's Animator.runtimeAnimatorController = {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}, instance ID={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.GetInstanceID().ToString() : "n/a")}.");

            var assetController = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
                "Assets/UpIzUpMini/Art/Animations/StarterAssetsThirdPerson.controller");
            Debug.Log($"MINI-120 DIAGNOSE: the asset I've been patching = {assetController.name}, instance ID={assetController.GetInstanceID()}.");
            Debug.Log($"MINI-120 DIAGNOSE: SAME OBJECT? {ReferenceEquals(animator.runtimeAnimatorController, assetController)}");

            // Find HumanoidAnimationManager's cached layer indices via
            // reflection (private fields) - exactly what PlayAction reads.
            var manager = sacat.GetComponent<UpIzUpMini.Character.HumanoidAnimationManager>();
            if (manager == null) { Debug.LogError("MINI-120 DIAGNOSE: no HumanoidAnimationManager on Sacat."); return; }

            var awakeMethod = typeof(UpIzUpMini.Character.HumanoidAnimationManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awakeMethod?.Invoke(manager, null);

            var actionLayerField = typeof(UpIzUpMini.Character.HumanoidAnimationManager).GetField("_actionLayer", BindingFlags.NonPublic | BindingFlags.Instance);
            int actionLayer = actionLayerField != null ? (int)actionLayerField.GetValue(manager) : -999;
            Debug.Log($"MINI-120 DIAGNOSE: _actionLayer resolved to index {actionLayer} (name should be \"{UpIzUpMini.Character.HumanoidAnimationManager.ActionLayerName}\").");

            string[] ids = { MeleeMoveLibrary.JabId, MeleeMoveLibrary.HookId, MeleeMoveLibrary.RightHookId, MeleeMoveLibrary.FinisherId };
            foreach (var id in ids)
            {
                int hash = Animator.StringToHash(id);
                bool hasState = actionLayer >= 0 && animator.HasState(actionLayer, hash);
                Debug.Log($"MINI-120 DIAGNOSE: id='{id}' hash={hash} -> animator.HasState(layer={actionLayer}, hash)={hasState}");
            }

            // Cross-check directly against the controller asset's own
            // layer/state list, independent of Animator.HasState.
            if (assetController.layers.Length > actionLayer && actionLayer >= 0)
            {
                var sm = assetController.layers[actionLayer].stateMachine;
                Debug.Log($"MINI-120 DIAGNOSE: controller asset layer[{actionLayer}] name='{assetController.layers[actionLayer].name}', states on it:");
                foreach (var child in sm.states)
                {
                    Debug.Log($"MINI-120 DIAGNOSE:   state name='{child.state.name}', motion='{(child.state.motion != null ? child.state.motion.name : "NULL")}'");
                }
            }
        }
    }
}
