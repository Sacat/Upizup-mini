using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-156 diagnostic: list every SafehouseInteractable in the
    /// live scene with its position/spawnPoint, plus Sacat/Franki's current
    /// positions, so the actual nearest/most-relevant safehouse can be
    /// identified before moving the spawn point.</summary>
    public static class Mini156FindSafehouses
    {
        [MenuItem("Up Iz Up Mini/MINI-156/Find Safehouses")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            foreach (var sh in Object.FindObjectsByType<SafehouseInteractable>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(sh);
                Debug.Log($"MINI156SAFEHOUSE: '{sh.name}' safehouseName={so.FindProperty("safehouseName").stringValue} " +
                          $"requiredItemId='{so.FindProperty("requiredItemId").stringValue}' " +
                          $"pos={sh.transform.position} spawnPoint={so.FindProperty("spawnPoint").vector3Value}");
            }
            foreach (var name in new[] { "Sacat", "Franki" })
            {
                var go = GameObject.Find(name);
                if (go != null) Debug.Log($"MINI156SAFEHOUSE: character '{name}' pos={go.transform.position}");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
