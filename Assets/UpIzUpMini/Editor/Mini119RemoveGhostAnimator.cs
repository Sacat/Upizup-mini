using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: removes the blank, auto-created
    /// Animator that VehicleRider's own (now-removed)
    /// [RequireComponent(typeof(Animator))] left permanently baked onto
    /// Sacat's root, back when it was added during an earlier Edit-mode
    /// mount+save. Sacat's real Animator lives on "Visual" - this one
    /// (avatar=NULL, controller=NULL) was always dead weight.</summary>
    public static class Mini119RemoveGhostAnimator
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Remove Ghost Animator On Sacat (one-off)")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-119 REMOVE GHOST FAIL: Sacat not found."); return; }

            var rootAnimator = player.GetComponent<Animator>();
            if (rootAnimator == null)
            {
                Debug.Log("MINI-119 REMOVE GHOST: no Animator directly on Sacat's root - nothing to remove.");
                return;
            }

            if (rootAnimator.avatar != null || rootAnimator.runtimeAnimatorController != null)
            {
                Debug.LogError($"MINI-119 REMOVE GHOST: refusing to remove - this Animator has a real avatar/controller assigned (avatar={rootAnimator.avatar}, controller={rootAnimator.runtimeAnimatorController}), doesn't look like the blank ghost.");
                return;
            }

            Object.DestroyImmediate(rootAnimator);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("MINI-119 REMOVE GHOST OK: blank Animator removed from Sacat's root and saved.");
        }
    }
}
