using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Combat;
using UpIzUpMini.Economy;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-194 Play Mode proof on the new-map scene: does the active player start with the sidearm in hand? Logs the state and renders the player.
    /// Run WITHOUT -quit and WITHOUT -nographics. Never saves the scene.</summary>
    [InitializeOnLoad]
    public static class Mini194GunInHandProof
    {
        const string Out = "Logs/Tasks/MINI-194";
        const string Scene = "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity";
        static Mini194GunInHandProof() { if (SessionState.GetBool("Mini194Proof", false)) EditorApplication.playModeStateChanged += Changed; }

        [MenuItem("Up Iz Up Mini/MINI-194/Gun In Hand Proof (Play Mode)")]
        public static void Run()
        {
            Directory.CreateDirectory(Out);
            SessionState.SetBool("Mini194Proof", true);
            EditorSceneManager.OpenScene(Scene);
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.EnterPlaymode();
        }

        static double start; static int phase; static StringBuilder log = new StringBuilder();

        static void Changed(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode) { start = EditorApplication.timeSinceStartup; phase = 0; Cursor.lockState = CursorLockMode.Locked; EditorApplication.update += Tick; }
            if (s == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("Mini194Proof", false); EditorApplication.playModeStateChanged -= Changed;
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }

        static void Report(string tag)
        {
            var eco = EconomyManager.Instance;
            log.AppendLine("== " + tag + " cursor=" + Cursor.lockState + " owns=" + FirearmController.PlayerOwnsGun + " drawn=" + FirearmController.GunDrawn + " mag=" + (eco != null ? eco.SidearmMagazine : -1) + " money=" + (eco != null ? eco.Money : -1));
            foreach (var fc in UnityEngine.Object.FindObjectsByType<FirearmController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var pc = fc.GetComponent<PlayerController>(); var pose = fc.GetComponentInChildren<FirearmPose>(true);
                bool anyRenderer = false; if (fc.WeaponRoot != null) foreach (var r in fc.WeaponRoot.GetComponentsInChildren<Renderer>(true)) anyRenderer |= r.enabled;
                log.AppendLine(fc.name + " controlled=" + (pc != null && pc.IsControlled) + " active=" + fc.gameObject.activeInHierarchy + " lowReady=" + fc.IsLowReady + " aiming=" + fc.IsAiming
                    + " weaponVisible=" + (pose != null ? pose.WeaponVisible.ToString() : "nopose") + " aimBlend=" + (pose != null ? pose.AimBlend.ToString("0.00") : "-") + " weaponRoot=" + (fc.WeaponRoot != null) + " rendererOn=" + anyRenderer);
            }
        }

        static void Shot(string name)
        {
            FirearmController target = null;
            foreach (var fc in UnityEngine.Object.FindObjectsByType<FirearmController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) { var pc = fc.GetComponent<PlayerController>(); if (pc != null && pc.IsControlled) target = fc; }
            if (target == null) { log.AppendLine("no controlled player for shot"); return; }
            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) cam.enabled = false;
            var go = new GameObject("ProofCam"); var c = go.AddComponent<Camera>(); c.tag = "MainCamera"; c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(.55f, .7f, .86f);
            var t = target.transform; go.transform.position = t.position + t.forward * 2.4f + t.right * 1.2f + Vector3.up * 1.3f; go.transform.LookAt(t.position + Vector3.up * 1.1f);
            var rt = new RenderTexture(1280, 800, 24); c.targetTexture = rt; c.Render(); RenderTexture.active = rt;
            var tx = new Texture2D(1280, 800, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0); tx.Apply();
            File.WriteAllBytes(Out + "/" + name + ".png", tx.EncodeToPNG()); RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.Destroy(go);
        }

        static void Tick()
        {
            double t = EditorApplication.timeSinceStartup - start;
            if (phase == 0 && t > 6) { Report("t=6s"); Shot("start_in_hand"); phase = 1; }
            else if (phase == 1 && t > 9) { Report("t=9s"); File.WriteAllText(Out + "/ProofLog.txt", log.ToString()); phase = 2; EditorApplication.update -= Tick; Debug.Log("MINI194_PROOF_DONE"); EditorApplication.ExitPlaymode(); }
        }
    }
}
