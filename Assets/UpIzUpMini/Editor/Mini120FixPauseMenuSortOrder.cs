using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 follow-up: patches the live scene's
    /// PauseMenuCanvas sortingOrder directly (see Mini011PhaseBSetup.cs's
    /// own comment on this same fix for the full diagnosis) rather than
    /// re-running the whole world builder. Delete after use.</summary>
    public static class Mini120FixPauseMenuSortOrder
    {
        [MenuItem("Up Iz Up Mini/MINI-120/Fix Pause Menu Sort Order (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var go = GameObject.Find("PauseMenuCanvas");
            if (go == null) { Debug.LogError("MINI-120 FIX PAUSE SORT: no PauseMenuCanvas in scene."); return; }

            var canvas = go.GetComponent<Canvas>();
            if (canvas == null) { Debug.LogError("MINI-120 FIX PAUSE SORT: PauseMenuCanvas has no Canvas component."); return; }

            int before = canvas.sortingOrder;
            canvas.sortingOrder = 100;
            Debug.Log($"MINI-120 FIX PAUSE SORT: PauseMenuCanvas sortingOrder BEFORE={before}, AFTER={canvas.sortingOrder}.");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("MINI-120 FIX PAUSE SORT: scene saved.");
        }
    }
}
