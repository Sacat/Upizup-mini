using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-120 follow-up, user: "when i press ESC is doesnt
    /// bring up the quit and resume menu." Read-only inspection of the
    /// live scene's PauseMenuController and its wired panel/buttons -
    /// checks whether the component exists, is enabled, is on an active
    /// GameObject, and whether its references actually resolved,
    /// instead of guessing at the cause. Delete after use.</summary>
    public static class Mini120DiagnosePauseMenu
    {
        [MenuItem("Up Iz Up Mini/MINI-120/Diagnose Pause Menu (one-off, read-only)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var controllers = Object.FindObjectsByType<PauseMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"MINI-120 DIAGNOSE PAUSE: {controllers.Length} PauseMenuController(s) found in scene.");

            foreach (var controller in controllers)
            {
                var go = controller.gameObject;
                Debug.Log($"MINI-120 DIAGNOSE PAUSE: on GameObject '{go.name}', GameObject.activeInHierarchy={go.activeInHierarchy}, GameObject.activeSelf={go.activeSelf}, component.enabled={controller.enabled}.");

                var so = new SerializedObject(controller);
                var panelProp = so.FindProperty("panel");
                var resumeProp = so.FindProperty("resumeButton");
                var quitProp = so.FindProperty("quitButton");

                var panel = panelProp?.objectReferenceValue as GameObject;
                var resume = resumeProp?.objectReferenceValue as Button;
                var quit = quitProp?.objectReferenceValue as Button;

                Debug.Log($"MINI-120 DIAGNOSE PAUSE: panel={(panel != null ? panel.name : "NULL")} (activeSelf={(panel != null ? panel.activeSelf.ToString() : "n/a")}), resumeButton={(resume != null ? resume.name : "NULL")}, quitButton={(quit != null ? quit.name : "NULL")}.");

                // Check the panel's own place in the hierarchy - is it
                // under a Canvas, and is that Canvas active/enabled?
                if (panel != null)
                {
                    var canvas = panel.GetComponentInParent<Canvas>(true);
                    Debug.Log($"MINI-120 DIAGNOSE PAUSE: panel's parent Canvas={(canvas != null ? canvas.name : "NULL")}, canvas.enabled={(canvas != null ? canvas.enabled.ToString() : "n/a")}, canvas GameObject active={(canvas != null ? canvas.gameObject.activeInHierarchy.ToString() : "n/a")}, sortingOrder={(canvas != null ? canvas.sortingOrder.ToString() : "n/a")}.");

                    var rect = panel.GetComponent<RectTransform>();
                    if (rect != null)
                        Debug.Log($"MINI-120 DIAGNOSE PAUSE: panel RectTransform - anchoredPosition={rect.anchoredPosition}, sizeDelta={rect.sizeDelta}, localScale={rect.localScale}.");
                }
            }

            // Check for any OTHER Canvas with a higher sorting order that
            // might visually cover the pause panel even if it IS active.
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log($"MINI-120 DIAGNOSE PAUSE: {canvases.Length} total Canvas(es) in scene:");
            foreach (var c in canvases)
            {
                Debug.Log($"MINI-120 DIAGNOSE PAUSE:   Canvas '{c.gameObject.name}', sortingOrder={c.sortingOrder}, renderMode={c.renderMode}, active={c.gameObject.activeInHierarchy}.");
            }
        }
    }
}
