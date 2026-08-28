using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    public static class Mini119CheckSacatAnimator
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Sacat Animator (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-119 CHECK: Sacat not found."); return; }

            var animators = player.GetComponentsInChildren<Animator>(true);
            Debug.Log($"MINI-119 CHECK: found {animators.Length} Animator component(s) under Sacat.");
            foreach (var a in animators)
            {
                Debug.Log($"MINI-119 CHECK: Animator on '{a.gameObject.name}' - avatar={(a.avatar != null ? a.avatar.name : "NULL")}, runtimeAnimatorController={(a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "NULL")}, isHuman(edit-mode, may read false regardless)={a.isHuman}");
            }

            var rootAnimator = player.GetComponent<Animator>();
            Debug.Log($"MINI-119 CHECK: Animator directly on Sacat's own root = {(rootAnimator != null)}");
        }
    }
}
