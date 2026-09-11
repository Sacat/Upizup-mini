using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-156: user asked to spawn Sacat and Franki by the safehouse -
    /// clarified as the Highland Safehouse (the free starting property,
    /// FarmSafehouse_Rest). Small, additive live-scene edit: move only the
    /// two characters' Transform.position (and FollowTarget-driven Franki
    /// offset) to sit beside the safehouse's own spawnPoint. Does not touch
    /// the safehouse, terrain, roads, or any other object, and does not
    /// rerun the full scene generator.
    /// </summary>
    public static class Mini156MoveSpawnToSafehouse
    {
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";

        static void Require(bool ok, string text)
        {
            if (!ok) throw new Exception("MINI156MOVE: " + text);
        }

        [MenuItem("Up Iz Up Mini/MINI-156/Move Spawn To Highland Safehouse")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);

            var safehouse = Array.Find(
                UnityEngine.Object.FindObjectsByType<SafehouseInteractable>(FindObjectsSortMode.None),
                s => s.name == "FarmSafehouse_Rest");
            Require(safehouse != null, "FarmSafehouse_Rest not found");
            var so = new SerializedObject(safehouse);
            Vector3 spawnPoint = so.FindProperty("spawnPoint").vector3Value;

            var sacat = GameObject.Find("Sacat");
            var franki = GameObject.Find("Franki");
            Require(sacat != null, "Sacat not found");
            Require(franki != null, "Franki not found");

            Vector3 sacatBefore = sacat.transform.position;
            Vector3 frankiBefore = franki.transform.position;

            // Same relative offset the original scene-setup code uses
            // between the two characters (Mini011PhaseBSetup.cs
            // BuildControllableCharacter calls), so they don't overlap.
            sacat.transform.position = spawnPoint;
            franki.transform.position = spawnPoint + new Vector3(1.4f, 0f, -1.2f);

            EditorSceneManager.MarkSceneDirty(SceneManager());
            EditorSceneManager.SaveScene(SceneManager());

            Debug.Log($"MINI156MOVE_PASS Sacat {sacatBefore} -> {sacat.transform.position}; " +
                      $"Franki {frankiBefore} -> {franki.transform.position}; " +
                      $"safehouse spawnPoint={spawnPoint}");

            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        static UnityEngine.SceneManagement.Scene SceneManager()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        }
    }
}
