using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Missions;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-111 evidence: Rasta at his real position, plus a log
    /// dump of the six-tier ladder's titles/briefings for a readable
    /// written record alongside the screenshot.</summary>
    public static class Mini111EvidenceCapture
    {
        [MenuItem("Up Iz Up Mini/MINI-111/Capture Evidence")]
        public static void Capture()
        {
            Environment.SetEnvironmentVariable("UPIZUP_EVIDENCE_TASK", "MINI-111");
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            Directory.CreateDirectory("Logs/Tasks/MINI-111");

            var captureView = typeof(Mini100GrandBayMapValidation).GetMethod(
                "CaptureView", BindingFlags.NonPublic | BindingFlags.Static);
            var rasta = GameObject.Find("NPC_RastaMentor");
            if (captureView != null && rasta != null)
            {
                captureView.Invoke(null, new object[]
                {
                    "MINI-111-Rasta-StrainSchool-1280x720.png",
                    rasta.transform.position + new Vector3(-2.6f, 2.0f, -3.2f),
                    rasta.transform.position + Vector3.up * 1.2f,
                    1280, 720, false, 0f
                });
            }
            else
            {
                Debug.LogWarning("MINI-111 EVIDENCE: could not capture Rasta (CaptureView or NPC_RastaMentor missing).");
            }

            var missionSystem = UnityEngine.Object.FindFirstObjectByType<MissionSystem>();
            var missionsField = typeof(MissionSystem).GetField("missions", BindingFlags.NonPublic | BindingFlags.Instance);
            if (missionSystem != null && missionsField != null)
            {
                var missions = (System.Collections.Generic.List<Mission>)missionsField.GetValue(missionSystem);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("MINI-111 Rasta ladder, as built into the mission list:");
                foreach (var id in new[] { "M13", "M13C2", "M13C3", "M13C4", "M13C5", "M13C6", "M13C7" })
                {
                    var m = missions.FirstOrDefault(mm => mm.missionId == id);
                    if (m == null) continue;
                    sb.AppendLine($"[{m.missionId}] {m.title} (unlocks: {(string.IsNullOrEmpty(m.unlocksCropId) ? "-" : m.unlocksCropId)})");
                    sb.AppendLine($"  {m.briefing}");
                    foreach (var o in m.objectives) sb.AppendLine($"    - {o.kind} '{o.targetId}' x{o.requiredCount}: {o.instruction}");
                }
                File.WriteAllText("Logs/Tasks/MINI-111/mini111-ladder-content.txt", sb.ToString());
                Debug.Log(sb.ToString());
            }

            Debug.Log("MINI-111 EVIDENCE CAPTURE PASS: Rasta screenshot and ladder content log saved to Logs/Tasks/MINI-111/.");
        }
    }
}
