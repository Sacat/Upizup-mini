using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UpIzUpMini.Character;
using UpIzUpMini.Economy;
using UpIzUpMini.UI;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-110 evidence: Normy (favour), the Boat Man (rewritten
    /// intro), and the HUD courier-timer label forced visible by directly
    /// setting GuadeloupeTrade's private trip-state fields (the Editor
    /// never ticks Update() to start a real trip in batch mode).</summary>
    public static class Mini110EvidenceCapture
    {
        [MenuItem("Up Iz Up Mini/MINI-110/Capture Evidence Screenshots")]
        public static void Capture()
        {
            Environment.SetEnvironmentVariable("UPIZUP_EVIDENCE_TASK", "MINI-110");
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);
            Directory.CreateDirectory("Logs/Tasks/MINI-110");

            var captureView = typeof(Mini100GrandBayMapValidation).GetMethod(
                "CaptureView", BindingFlags.NonPublic | BindingFlags.Static);
            if (captureView == null)
            {
                Debug.LogError("MINI-110 EVIDENCE: could not find Mini100GrandBayMapValidation.CaptureView via reflection.");
                return;
            }

            void Shot(string name, GameObject anchor, Vector3 camOffset, Vector3 lookOffset)
            {
                if (anchor == null) { Debug.LogWarning($"MINI-110 EVIDENCE: anchor for '{name}' not found."); return; }
                captureView.Invoke(null, new object[]
                {
                    $"MINI-110-{name}-1280x720.png",
                    anchor.transform.position + camOffset,
                    anchor.transform.position + lookOffset,
                    1280, 720, false, 0f
                });
            }

            Shot("Normy-Favour", GameObject.Find("NPC_Normy"), new Vector3(-2.6f, 2.0f, -3.2f), Vector3.up * 1.2f);
            Shot("BoatMan-Intro", GameObject.Find("NPC_BoatMan"), new Vector3(-2.8f, 2.1f, -3.4f), Vector3.up * 1.2f);

            // Force the HUD courier timer visible: simulate an active
            // Guadeloupe trip via reflection (Editor never ticks Update()
            // to start one for real in batch mode), then render the HUD.
            var economy = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
            var trade = UnityEngine.Object.FindFirstObjectByType<GuadeloupeTrade>();
            var hud = UnityEngine.Object.FindFirstObjectByType<HUDController>();
            var switcher = UnityEngine.Object.FindFirstObjectByType<CharacterSwitchManager>();
            if (economy != null && trade != null && hud != null)
            {
                InvokeAwake(economy);
                InvokeAwake(trade);
                InvokeAwake(hud);
                if (switcher != null) InvokeAwake(switcher);

                SetField(trade, "<TripActive>k__BackingField", true);
                SetField(trade, "<AwayCharacterIndex>k__BackingField", 1);
                SetField(trade, "_returnAt", Time.time + 275f);

                InvokeUpdate(hud);

                var courierField = typeof(HUDController).GetField("courierTimerLabel", BindingFlags.NonPublic | BindingFlags.Instance);
                var courierLabel = courierField?.GetValue(hud) as Text;
                if (courierLabel != null)
                {
                    Debug.Log($"MINI-110 EVIDENCE: courier timer text = '{courierLabel.text}', enabled={courierLabel.enabled}");
                    var canvas = hud.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        var cam = new GameObject("MINI110_HUDCamera").AddComponent<Camera>();
                        cam.orthographic = false;
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = new Color(0.15f, 0.55f, 0.75f);
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = cam;
                        RenderCanvasToPng(cam, "Logs/Tasks/MINI-110/MINI-110-HUD-CourierTimer-1280x720.png");
                        UnityEngine.Object.DestroyImmediate(cam.gameObject);
                    }
                }
                else
                {
                    Debug.LogWarning("MINI-110 EVIDENCE: courierTimerLabel field not found/wired on HUDController.");
                }
            }
            else
            {
                Debug.LogWarning("MINI-110 EVIDENCE: EconomyManager/GuadeloupeTrade/HUDController missing - could not force the courier timer for a screenshot.");
            }

            Debug.Log("MINI-110 EVIDENCE CAPTURE PASS: Normy favour, Boat Man intro and (if wired) the HUD courier timer saved to Logs/Tasks/MINI-110/.");
        }

        private static void RenderCanvasToPng(Camera cam, string path)
        {
            int width = 1280, height = 720;
            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void InvokeAwake(object target)
        {
            var m = target.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            m?.Invoke(target, null);
        }

        private static void InvokeUpdate(object target)
        {
            var m = target.GetType().GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            m?.Invoke(target, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) { Debug.LogWarning($"MINI-110 EVIDENCE: field '{fieldName}' not found on {target.GetType().Name}."); return; }
            f.SetValue(target, value);
        }
    }
}
