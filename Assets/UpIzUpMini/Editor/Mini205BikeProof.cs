using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>Drives the scene's TMAX with the swapped bare-body Sacat (and Franki as pillion) through straight, left and right corners, braking, and renders side/rear views. Run without -quit.</summary>
    [InitializeOnLoad]
    public static class Mini205BikeProof
    {
        const string Out = "Logs/Tasks/MINI-205/Bike";
        static Mini205BikeProof() { if (SessionState.GetBool("Mini205Bike", false)) EditorApplication.playModeStateChanged += Changed; }

        [MenuItem("Up Iz Up Mini/MINI-205/Bike Ride Proof (Play Mode)")]
        public static void Run()
        {
            Directory.CreateDirectory(Out); SessionState.SetBool("Mini205Bike", true);
            EditorSceneManager.OpenScene(Environment.GetEnvironmentVariable("MINI205_SCENE") ?? "Assets/UpIzUpMini/Scenes/GrandBayProof_HouseEnhance.unity", OpenSceneMode.Single);
            EditorApplication.playModeStateChanged += Changed; EditorApplication.EnterPlaymode();
        }

        static void Changed(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode) { t0 = EditorApplication.timeSinceStartup; EditorApplication.update += Tick; }
            if (s == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool("Mini205Bike", false); EditorApplication.playModeStateChanged -= Changed; if (Application.isBatchMode) EditorApplication.Exit(0); }
        }

        class Driver : MonoBehaviour { public TmaxBikeControllerCustom bike; public float throttle, steer, brake; public bool wheelie; void LateUpdate() { if (bike != null) { bike.SetInput(throttle, steer, brake); bike.SetWheelieHeld(wheelie); } } }

        static double t0, tMount; static BikeInteractable bi; static Driver drv; static int phase; static string log = ""; static GameObject sacat, franki; static Camera cam; static float yawOffset = 90f;

        static void Shot(string name, float side, float back)
        {
            if (bi == null) return; var tf = bi.transform;
            var go = new GameObject("shotcam"); var c = go.AddComponent<Camera>(); c.fieldOfView = 34; c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(.6f, .72f, .85f);
            go.transform.position = tf.position + tf.right * side - tf.forward * back + Vector3.up * 1.3f; go.transform.LookAt(tf.position + Vector3.up * 1.0f);
            var rt = new RenderTexture(900, 700, 24); c.targetTexture = rt; c.Render(); RenderTexture.active = rt;
            var tx = new Texture2D(900, 700, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 900, 700), 0, 0); tx.Apply();
            File.WriteAllBytes(Out + "/" + name + ".png", tx.EncodeToPNG()); RenderTexture.active = null; c.targetTexture = null; UnityEngine.Object.Destroy(go); UnityEngine.Object.Destroy(rt);
        }

        static void Tick()
        {
            double t = EditorApplication.timeSinceStartup - t0;
            if (phase == 0 && t > 5)
            {
                bi = UnityEngine.Object.FindObjectsByType<BikeInteractable>(FindObjectsSortMode.None).FirstOrDefault();
                sacat = GameObject.Find("Sacat"); franki = GameObject.Find("Franki");
                log += "bike found=" + (bi != null) + "\n"; if (bi == null || sacat == null) { Finish(); return; }
                var sacatPos = sacat.transform.position; log += "bike dist to Sacat=" + Vector3.Distance(bi.transform.position, sacatPos).ToString("0.0") + "\n";
                if (franki != null) { var fc = franki.GetComponent<CharacterController>(); fc.enabled = false; franki.transform.position = bi.transform.position - bi.transform.forward * 1.4f + bi.transform.right * .6f; fc.enabled = true; }
                var cc = sacat.GetComponent<CharacterController>(); cc.enabled = false; sacat.transform.position = bi.transform.position + bi.transform.right * 1.2f; cc.enabled = true;
                bool ok = bi.TryMountFor(sacat); log += "mounted=" + ok + "\n";
                drv = bi.gameObject.AddComponent<Driver>(); drv.bike = bi.GetComponent<TmaxBikeControllerCustom>();
                tMount = t; phase = 1;
            }
            else if (phase == 1 && t > tMount + 1.5) { Shot("1_seated_side", 3.0f, 0f); Shot("1_seated_rear34", 2.2f, 3.0f); drv.throttle = 1f; phase = 2; }
            else if (phase == 2 && t > tMount + 5.5) { Shot("2_cruise_side", 3.0f, 0f); drv.throttle = .6f; drv.steer = 1f; phase = 3; }
            else if (phase == 3 && t > tMount + 7.5) { Shot("3_right_corner_rear34", 2.2f, 3.5f); Shot("3_right_corner_side", 3.0f, 0f); drv.steer = -1f; phase = 4; }
            else if (phase == 4 && t > tMount + 9.5) { Shot("4_left_corner_rear34", -2.2f, 3.5f); drv.steer = 0; drv.throttle = 1f; phase = 5; }
            else if (phase == 5 && t > tMount + 13.5) { drv.wheelie = true; phase = 6; }
            else if (phase == 6 && t > tMount + 15.5) { Shot("5_wheelie_side", 3.2f, 0f); Shot("5_wheelie_rear34", 2.4f, 3.2f); drv.wheelie = false; drv.throttle = 0; drv.brake = 1f; phase = 7; }
            else if (phase == 7 && t > tMount + 18.0) { Shot("6_braking_side", 3.0f, 0f); Finish(); }
        }

        static void Finish()
        {
            EditorApplication.update -= Tick;
            if (bi != null) log += "pillion=" + (franki != null ? franki.transform.parent != null ? franki.transform.parent.name : "free" : "none") + " speed=" + (drv != null && drv.bike != null ? drv.bike.SpeedKmh.ToString("0") : "-") + "\n";
            File.WriteAllText(Out + "/log.txt", log); Debug.Log("MINI205_BIKE_DONE"); EditorApplication.ExitPlaymode();
        }
    }
}
