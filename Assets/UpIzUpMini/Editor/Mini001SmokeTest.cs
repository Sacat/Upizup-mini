using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Headless play-mode smoke test for GrandBayProof. Opens the scene,
    /// enters Play mode for a short window, captures any console
    /// errors/exceptions, then exits. In batch mode it exits the Unity
    /// process with code 0 (pass) or 1 (fail) so it can be scripted from
    /// the command line without a Unity Test Framework package.
    ///
    /// No test-framework package is installed in this project yet
    /// (see PROJECT-HANDOFF.md MINI-001 known issues), so this is a
    /// deliberately minimal stand-in, not a replacement for Edit/Play
    /// Mode tests once com.unity.test-framework is added.
    /// </summary>
    public static class Mini001SmokeTest
    {
        private const string ScenePath = "Assets/UpIzUpMini/Scenes/GrandBayProof.unity";
        private const int FramesToRun = 90;

        private static List<string> _errors;
        private static int _frameCount;
        private static bool _running;

        [MenuItem("Up Iz Up Mini/MINI-001/Run Headless Smoke Test")]
        public static void Run()
        {
            if (_running)
            {
                Debug.LogWarning("MINI-001 smoke test already running.");
                return;
            }

            _errors = new List<string>();
            _frameCount = 0;
            _running = true;

            EditorSceneManager.OpenScene(ScenePath);
            Application.logMessageReceived += OnLogMessage;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying)
            {
                // Still waiting for the deferred play-mode transition to begin.
                return;
            }

            _frameCount++;
            if (_frameCount < FramesToRun) return;

            EditorApplication.update -= Tick;
            EditorApplication.isPlaying = false;
            Application.logMessageReceived -= OnLogMessage;
            _running = false;

            Finish();
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _errors.Add($"[{type}] {condition}");
            }
        }

        private static void Finish()
        {
            bool pass = _errors.Count == 0;
            if (pass)
            {
                Debug.Log("MINI-001 SMOKE TEST PASS: GrandBayProof ran with no console errors.");
            }
            else
            {
                Debug.LogError("MINI-001 SMOKE TEST FAIL:\n" + string.Join("\n", _errors));
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(pass ? 0 : 1);
            }
        }
    }
}
