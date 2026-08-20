using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Dialogue;
using UpIzUpMini.Missions;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-054: proves two real, non-obvious pieces of behaviour rather
    /// than just "it compiles" - (1) OpeningConversationController's
    /// TotalDuration actually sums the same reading-time formula it uses
    /// to advance the sequence, so Mini011PhaseBSetup's
    /// firstBriefingDelay wiring lines up with real playback time, and (2)
    /// MissionSystem.Start() genuinely defers the first briefing when
    /// firstBriefingDelay > 0 instead of showing it immediately - the part
    /// that can't be seen just by reading the diff, since it depends on
    /// which of two code paths a boolean check takes.
    /// </summary>
    public static class Mini054OpeningConversationValidation
    {
        [MenuItem("Up Iz Up Mini/MINI-054/Validate Opening Conversation")]
        public static void Validate()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            bool pass = true;
            string fail = null;

            // --- 1) TotalDuration matches a hand-computed sum of the same
            // formula (min 3.2s, +0.045s/char, max 9s) for known text. ---
            var convoGo = new GameObject("TestConversation");
            var convo = convoGo.AddComponent<OpeningConversationController>();
            var convoSo = new SerializedObject(convo);
            var linesProp = convoSo.FindProperty("lines");
            string[] texts = { "short", "a medium length line here for testing", new string('x', 200) };
            linesProp.arraySize = texts.Length;
            for (int i = 0; i < texts.Length; i++)
            {
                linesProp.GetArrayElementAtIndex(i).FindPropertyRelative("text").stringValue = texts[i];
            }
            convoSo.ApplyModifiedPropertiesWithoutUndo();

            float expected = 0f;
            foreach (var t in texts) expected += Mathf.Clamp(3.2f + t.Length * 0.045f, 3.2f, 9f);
            float actual = convo.TotalDuration;
            if (Mathf.Abs(actual - expected) > 0.001f)
            {
                pass = false;
                fail = $"TotalDuration mismatch: expected {expected:F3}s, got {actual:F3}s.";
            }

            // --- 2) MissionSystem defers vs. shows immediately. ---
            if (pass)
            {
                var mission = new Mission
                {
                    missionId = "TestM1",
                    title = "Test Mission",
                    briefing = "Test briefing.",
                    objectives = new List<MissionObjective>
                    {
                        new MissionObjective { kind = ObjectiveKind.Switch, instruction = "test", hasMarker = false }
                    }
                };

                var deferredGo = new GameObject("MissionDeferred");
                var deferred = deferredGo.AddComponent<MissionSystem>();
                InvokeMethod(deferred, "Awake");
                deferred.SetMissions(new List<Mission> { mission });
                SetField(deferred, "firstBriefingDelay", 5f);
                InvokeMethod(deferred, "Start");
                if (!string.IsNullOrEmpty(deferred.Banner))
                {
                    pass = false;
                    fail = $"MissionSystem with firstBriefingDelay=5 showed a banner immediately on Start() (Banner='{deferred.Banner}') - expected it deferred.";
                }

                if (pass)
                {
                    var immediateGo = new GameObject("MissionImmediate");
                    var immediate = immediateGo.AddComponent<MissionSystem>();
                    InvokeMethod(immediate, "Awake");
                    immediate.SetMissions(new List<Mission> { mission });
                    // firstBriefingDelay left at its default 0.
                    InvokeMethod(immediate, "Start");
                    if (string.IsNullOrEmpty(immediate.Banner) || !immediate.Banner.Contains("Test Mission"))
                    {
                        pass = false;
                        fail = $"MissionSystem with firstBriefingDelay=0 did not show the briefing immediately on Start() (Banner='{immediate.Banner}').";
                    }
                    UnityEngine.Object.DestroyImmediate(immediateGo);
                }

                UnityEngine.Object.DestroyImmediate(deferredGo);
            }

            UnityEngine.Object.DestroyImmediate(convoGo);

            if (pass)
            {
                Debug.Log("MINI-054 OPENING CONVERSATION VALIDATION PASS: TotalDuration matches the reading-time formula it advances by, and MissionSystem genuinely defers its first briefing when firstBriefingDelay is set (and shows it immediately when it isn't).");
            }
            else
            {
                Debug.LogError($"MINI-054 OPENING CONVERSATION VALIDATION FAIL: {fail}");
            }
        }

        private static void InvokeMethod(object target, string name)
        {
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null) throw new Exception($"Field '{name}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
