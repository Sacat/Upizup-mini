using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-193 Play Mode proof: spawns the TMAX prefab on a flat test floor, drives a scripted run (settle, drop, throttle, brake,
    /// left and right turns) and logs suspension travel, lean, squat/dive and speed. Scene is never saved. Run WITHOUT -quit.</summary>
    [InitializeOnLoad]
    public static class Mini193TmaxProof
    {
        const string Out = "Logs/Tasks/MINI-193";
        const string PrefabPath = "Assets/UpIzUpMini/Vehicles/TMAX_560.prefab";
        static Mini193TmaxProof() { if (SessionState.GetBool("Mini193Proof", false)) EditorApplication.playModeStateChanged += Changed; }

        [MenuItem("Up Iz Up Mini/MINI-193/TMAX Dynamics Proof (Play Mode)")]
        public static void Run()
        {
            Directory.CreateDirectory(Out + "/Renders");
            SessionState.SetBool("Mini193Proof", true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.playModeStateChanged += Changed;
            EditorApplication.EnterPlaymode();
        }

        static void Changed(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode) { Setup(); EditorApplication.update += Tick; }
            if (s == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("Mini193Proof", false);
                EditorApplication.playModeStateChanged -= Changed;
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }

        static GameObject bike; static TmaxBikeControllerCustom ctrl; static Rigidbody rb; static WheelCollider fw, rw; static Camera cam;
        static double t0; static StringBuilder log = new StringBuilder(); static bool finished;
        static float minFront = 9f, maxFront = -9f, maxRoll, minRoll, maxPitch, minPitch, maxSpeed, dropBounces; static float lastY, lastVy; static int dropSign;
        static string phase = ""; static Dictionary<string, string> results = new Dictionary<string, string>(); static bool shotR, shotL;

        static void Setup()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(600, 1, 600);
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            bike = (GameObject)UnityEngine.Object.Instantiate(prefab, new Vector3(0, 1.2f, 0), Quaternion.identity);
            ctrl = bike.GetComponent<TmaxBikeControllerCustom>(); rb = bike.GetComponent<Rigidbody>();
            foreach (var w in bike.GetComponentsInChildren<WheelCollider>(true)) { if (w.name.ToLower().Contains("front")) fw = w; else if (w.name.ToLower().Contains("rear")) rw = w; }
            if (Environment.GetEnvironmentVariable("MINI193_BASELINE") == "1")
            {
                var dd = bike.GetComponent<TmaxRideDynamics>(); dd.enabled = false;
                foreach (var w in new[] { fw, rw }) { w.suspensionDistance = .2f; var js = w.suspensionSpring; js.spring = 23544f; js.damper = 7606.691f; js.targetPosition = .5f; w.suspensionSpring = js; }
                Directory.CreateDirectory(Out); log.AppendLine("BASELINE (original suspension, dynamics off)");
            }
            var bi = bike.GetComponent<BikeInteractable>(); if (bi != null) bi.enabled = false;
            var cgo = new GameObject("ProofCam"); cam = cgo.AddComponent<Camera>(); cam.tag = "MainCamera";
            t0 = EditorApplication.timeSinceStartup;
            log.AppendLine("front=" + (fw != null ? fw.name : "null") + " rear=" + (rw != null ? rw.name : "null") + " controller=" + (ctrl != null) + " dynamics=" + (bike.GetComponent<TmaxRideDynamics>() != null));
            if (fw != null) log.AppendLine("front spring=" + fw.suspensionSpring.spring + " damper=" + fw.suspensionSpring.damper + " dist=" + fw.suspensionDistance + " target=" + fw.suspensionSpring.targetPosition);
            lastY = bike.transform.position.y;
        }

        static float Compression(WheelCollider w)
        {
            if (w == null) return 0f;
            if (!w.GetGroundHit(out WheelHit hit)) return 0f;
            Vector3 local = w.transform.InverseTransformPoint(hit.point);
            return Mathf.Clamp01((-local.y - w.radius) / -w.suspensionDistance + 1f);
        }

        static void Tick()
        {
            if (finished || bike == null) return;
            float t = (float)(EditorApplication.timeSinceStartup - t0);
            float throttle = 0, steer = 0, brake = 0; string ph;
            if (t < 3) ph = "settle";
            else if (t < 9) { ph = "throttle"; throttle = 1; }
            else if (t < 11.5f) { ph = "brake"; brake = 1; }
            else if (t < 14) { ph = "reaccel"; throttle = 1; }
            else if (t < 17) { ph = "turnRight"; throttle = .5f; steer = 1; }
            else if (t < 19) { ph = "straight"; throttle = .5f; }
            else if (t < 22) { ph = "turnLeft"; throttle = .5f; steer = -1; }
            else ph = "done";
            if (ph != phase)
            {
                if (phase != "" ) results[phase] = string.Format("speed max {0:0.0} km/h | roll [{1:0.0},{2:0.0}] | pitch [{3:0.00},{4:0.00}] | frontSusp [{5:0.00},{6:0.00}]", maxSpeed, minRoll, maxRoll, minPitch, maxPitch, minFront, maxFront);
                phase = ph; maxRoll = minRoll = maxPitch = minPitch = maxSpeed = 0; minFront = 9; maxFront = -9;
            }
            if (ph == "done") { Finish(); return; }
            ctrl.SetInput(throttle, steer, brake);
            var dyn = bike.GetComponent<TmaxRideDynamics>();
            float roll = dyn != null ? dyn.Roll : 0, pitch = dyn != null ? dyn.Pitch : 0;
            maxRoll = Mathf.Max(maxRoll, roll); minRoll = Mathf.Min(minRoll, roll); maxPitch = Mathf.Max(maxPitch, pitch); minPitch = Mathf.Min(minPitch, pitch);
            maxSpeed = Mathf.Max(maxSpeed, ctrl.SpeedKmh);
            float c = Compression(fw); minFront = Mathf.Min(minFront, c); maxFront = Mathf.Max(maxFront, c);
            // camera: side-rear view tracking the bike
            cam.transform.position = bike.transform.position + bike.transform.right * 3.2f + Vector3.up * 1.4f - bike.transform.forward * 3.6f;
            cam.transform.LookAt(bike.transform.position + Vector3.up * .6f);
            if ((ph == "turnRight" && t > 15.5f && !shotR) || (ph == "turnLeft" && t > 20.5f && !shotL)) { Shot(ph); if (ph == "turnRight") shotR = true; else shotL = true; }
            if (Mathf.FloorToInt(t * 4) != Mathf.FloorToInt((t - Time.unscaledDeltaTime) * 4))
                log.AppendLine(string.Format("t={0:0.0} {1} v={2:0.0} roll={3:0.0} pitch={4:0.00} comp={5:0.00} bodyPitchWorld={6:0.0} tilt={7:0.0}", t, ph, ctrl.SpeedKmh, roll, pitch, c, bike.transform.eulerAngles.x, Vector3.Angle(bike.transform.up, Vector3.up)));
        }

        static void Shot(string name)
        {
            var rt = new RenderTexture(960, 540, 24); cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(960, 540, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply();
            File.WriteAllBytes(Out + "/Renders/" + name + ".png", tex.EncodeToPNG()); RenderTexture.active = null; cam.targetTexture = null;
        }

        static void Finish()
        {
            finished = true; EditorApplication.update -= Tick;
            foreach (var kv in results) log.AppendLine("RESULT " + kv.Key + ": " + kv.Value);
            File.WriteAllText(Out + (Environment.GetEnvironmentVariable("MINI193_BASELINE") == "1" ? "/Baseline.txt" : "/ProofLog.txt"), log.ToString());
            Debug.Log("MINI193_PROOF_DONE");
            EditorApplication.ExitPlaymode();
        }
    }
}
