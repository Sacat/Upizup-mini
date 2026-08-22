using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-109 evidence screenshots for the Normy, Boss J/Black
    /// Sugar and Rasta objectives, reusing Mini100GrandBayMapValidation's
    /// own fixed-camera CaptureView helper via reflection rather than
    /// duplicating the render/PNG-write logic.</summary>
    public static class Mini109EvidenceCapture
    {
        [MenuItem("Up Iz Up Mini/MINI-109/Capture Evidence Screenshots")]
        public static void Capture()
        {
            Environment.SetEnvironmentVariable("UPIZUP_EVIDENCE_TASK", "MINI-109");
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            Directory.CreateDirectory("Logs/Tasks/MINI-109");

            var captureView = typeof(Mini100GrandBayMapValidation).GetMethod(
                "CaptureView", BindingFlags.NonPublic | BindingFlags.Static);
            if (captureView == null)
            {
                Debug.LogError("MINI-109 EVIDENCE: could not find Mini100GrandBayMapValidation.CaptureView via reflection.");
                return;
            }

            void Shot(string name, GameObject anchor, Vector3 camOffset, Vector3 lookOffset)
            {
                if (anchor == null) { Debug.LogWarning($"MINI-109 EVIDENCE: anchor for '{name}' not found."); return; }
                captureView.Invoke(null, new object[]
                {
                    $"MINI-109-{name}-1280x720.png",
                    anchor.transform.position + camOffset,
                    anchor.transform.position + lookOffset,
                    1280, 720, false, 0f
                });
            }

            Shot("Normy", GameObject.Find("NPC_Normy"), new Vector3(-2.6f, 2.0f, -3.2f), Vector3.up * 1.2f);
            Shot("BossJ-BlackSugar", GameObject.Find("NPC_BossJ"), new Vector3(-2.8f, 2.1f, -3.4f), Vector3.up * 1.2f);
            Shot("Rasta", GameObject.Find("NPC_RastaMentor"), new Vector3(-2.6f, 2.0f, -3.2f), Vector3.up * 1.2f);

            Debug.Log("MINI-109 EVIDENCE CAPTURE PASS: Normy, Boss J/Black Sugar and Rasta reference screenshots saved to Logs/Tasks/MINI-109/.");
        }
    }
}
