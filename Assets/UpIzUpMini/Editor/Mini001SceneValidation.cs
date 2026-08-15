using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UpIzUpMini.Cameras;
using UpIzUpMini.Character;
using UpIzUpMini.Interaction;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Static (non-Play-Mode) validation of GrandBayProof: confirms the
    /// required MINI-001 actors exist, are wired to the right components,
    /// and that no GameObject has a missing script reference.
    ///
    /// This exists because a full headless Play-Mode smoke test
    /// (see Mini001SmokeTest.cs) hangs in this environment on an
    /// unrelated Unity Editor bug (UnityEditor.Search.SearchDatabase
    /// .GetDefaultSearchDatabase throws ArgumentOutOfRangeException during
    /// "IndexationOnStartup" when Play Mode is entered via -batchmode
    /// -nographics -executeMethod, then spins without completing). That is
    /// an Editor Search-module issue, not a GrandBayProof scene/script
    /// issue, but it means "the scene runs without console errors" has
    /// only been confirmed statically here, not through an actual
    /// Play-Mode run. See PROJECT-HANDOFF.md MINI-001 known issues.
    /// </summary>
    public static class Mini001SceneValidation
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";

        [MenuItem("Up Iz Up Mini/MINI-001/Run Static Scene Validation")]
        public static void Run()
        {
            var problems = new List<string>();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject[] roots = scene.GetRootGameObjects();
            CheckForMissingScripts(roots, problems);

            GameObject player = GameObject.FindWithTag("Player");
            ValidatePlayer(player, problems);

            GameObject mainCamera = GameObject.FindWithTag("MainCamera");
            ValidateCamera(mainCamera, player, problems);

            // MINI-011 Phase C replaced NPCInteractable/FarmPlotInteractable
            // with TownNPCInteractable/FarmPlot; accept either generation
            // so this validator doesn't go stale as the scene evolves.
            int npcCount = Object.FindObjectsByType<NPCInteractable>(FindObjectsSortMode.None).Length
                + Object.FindObjectsByType<UpIzUpMini.Interaction.TownNPCInteractable>(FindObjectsSortMode.None).Length;
            if (npcCount < 1)
            {
                problems.Add("No NPCInteractable/TownNPCInteractable found (need at least one standing NPC).");
            }

            int plotCount = Object.FindObjectsByType<FarmPlotInteractable>(FindObjectsSortMode.None).Length
                + Object.FindObjectsByType<UpIzUpMini.Farming.FarmPlot>(FindObjectsSortMode.None).Length;
            if (plotCount < 1)
            {
                problems.Add("No FarmPlotInteractable/FarmPlot found (need at least one farm plot).");
            }

            bool pass = problems.Count == 0;
            if (pass)
            {
                Debug.Log("MINI-001 STATIC VALIDATION PASS: GrandBayProof hierarchy and components are wired correctly.");
            }
            else
            {
                Debug.LogError("MINI-001 STATIC VALIDATION FAIL:\n- " + string.Join("\n- ", problems));
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(pass ? 0 : 1);
            }
        }

        private static void CheckForMissingScripts(GameObject[] roots, List<string> problems)
        {
            foreach (var root in roots)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var components = t.gameObject.GetComponents<Component>();
                    foreach (var c in components)
                    {
                        if (c == null)
                        {
                            problems.Add($"Missing script reference on GameObject '{GetPath(t)}'.");
                        }
                    }
                }
            }
        }

        private static void ValidatePlayer(GameObject player, List<string> problems)
        {
            if (player == null)
            {
                problems.Add("No GameObject tagged 'Player' found.");
                return;
            }

            if (player.GetComponent<CharacterController>() == null)
                problems.Add("Player is missing CharacterController.");
            if (player.GetComponent<PlayerController>() == null)
                problems.Add("Player is missing PlayerController.");
            if (player.GetComponent<InteractionDetector>() == null)
                problems.Add("Player is missing InteractionDetector.");
        }

        private static void ValidateCamera(GameObject mainCamera, GameObject player, List<string> problems)
        {
            if (mainCamera == null)
            {
                problems.Add("No GameObject tagged 'MainCamera' found.");
                return;
            }

            if (mainCamera.GetComponent<UnityEngine.Camera>() == null)
                problems.Add("MainCamera is missing a Camera component.");

            var follow = mainCamera.GetComponent<ThirdPersonFollowCamera>();
            if (follow == null)
            {
                problems.Add("MainCamera is missing ThirdPersonFollowCamera.");
            }
            else
            {
                float angle = follow.CurrentLookAngleDegrees;
                if (angle < 40f || angle > 50f)
                {
                    problems.Add($"ThirdPersonFollowCamera look angle {angle:F1} deg is outside the 40-50 deg acceptance range.");
                }

                var so = new SerializedObject(follow);
                var targetProp = so.FindProperty("target");
                if (targetProp == null || targetProp.objectReferenceValue == null)
                {
                    problems.Add("ThirdPersonFollowCamera has no target assigned.");
                }
                else if (player != null && targetProp.objectReferenceValue as Transform != player.transform)
                {
                    problems.Add("ThirdPersonFollowCamera target is not the Player.");
                }
            }
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
