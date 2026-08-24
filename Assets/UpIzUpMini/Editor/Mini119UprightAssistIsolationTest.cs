using System.Reflection;
using Gadd420;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;
using Object = UnityEngine.Object;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 follow-up diagnostic: verifies SuperMotoUprightAssist is
    /// stable on its own, at rest, with zero input at all - a necessary
    /// baseline before trying it against real riding.
    /// </summary>
    public static class Mini119UprightAssistIsolationTest
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Isolation - Upright Assist Only (no input)")]
        public static void Run()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            if (spawner == null) { Debug.LogError("MINI-119 ISOLATION FAIL: no VehicleSpawnController in scene."); return; }
            var method = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            var playerGo = GameObject.Find("Sacat");
            if (playerGo == null) { Debug.LogError("MINI-119 ISOLATION FAIL: no player character found."); return; }

            InvokeIfExists(spawner, "Awake");
            method.Invoke(spawner, new object[] { playerGo });

            var instance = GameObject.Find("StockDemoSuperMoto");
            if (instance == null) { Debug.LogError("MINI-119 ISOLATION FAIL: bike not spawned."); return; }

            var upright = instance.GetComponent<SuperMotoUprightAssist>();
            var gadd = instance.GetComponent<RB_Controller>();
            var assist = instance.GetComponent<SuperMotoWheelieAssist>();
            var crashCtrl = instance.GetComponent<CrashController>();
            var ragdollMgr = instance.GetComponentInChildren<RagdollManager>(true);
            var autoLevel = instance.GetComponent<AutoLeveling>();
            var groundAngle = instance.GetComponent<GroundAngle>();
            var trike = instance.GetComponent<SuperMotoTrikeStabilizer>();

            InvokeIfExists(gadd, "Start");
            InvokeIfExists(ragdollMgr, "Start");
            InvokeIfExists(crashCtrl, "Start");
            InvokeIfExists(autoLevel, "Start");
            InvokeIfExists(groundAngle, "Start");
            InvokeIfExists(assist, "Awake");
            InvokeIfExists(trike, "Awake");
            InvokeIfExists(upright, "Awake");

            const float dt = 0.02f;
            const int steps = 250; // 5s, NO input at all

            float maxAngSpeed = 0f;
            float maxRoll = 0f;

            for (int i = 0; i < steps; i++)
            {
                InvokeIfExists(gadd, "Update");
                InvokeIfExists(crashCtrl, "Update");
                InvokeIfExists(ragdollMgr, "Update");
                InvokeIfExists(groundAngle, "Update");
                InvokeIfExists(autoLevel, "Update");
                InvokeIfExists(gadd, "FixedUpdate");
                InvokeIfExists(autoLevel, "FixedUpdate");
                InvokeIfExists(assist, "FixedUpdate");
                InvokeIfExists(trike, "FixedUpdate");
                InvokeIfExists(upright, "FixedUpdate");
                Physics.Simulate(dt);

                var body = instance.GetComponent<Rigidbody>();
                float trueRoll = assist != null ? assist.TrueRollDeg : -999f;
                maxAngSpeed = Mathf.Max(maxAngSpeed, body.angularVelocity.magnitude);
                maxRoll = Mathf.Max(maxRoll, trueRoll);

                if (i % 10 == 0)
                {
                    Debug.Log($"MINI-119 ISOLATION t={i * dt:F2}s: pos={instance.transform.position} TRUE roll={trueRoll:F1}deg angVel={body.angularVelocity} linVel={body.linearVelocity}");
                }
            }

            Debug.Log($"MINI-119 ISOLATION RESULT: max angular speed={maxAngSpeed:F2}rad/s, max roll={maxRoll:F1}deg over 5s at rest with zero input. VERDICT: {(maxAngSpeed < 3f && maxRoll < 5f ? "PASS - stable" : "FAIL - unstable")}");

            Object.DestroyImmediate(instance);
            Physics.simulationMode = previousSimMode;
        }

        private static void InvokeIfExists(Object target, string methodName)
        {
            if (target == null) return;
            if (target is Behaviour b && !b.enabled) return;
            var m = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            m?.Invoke(target, null);
        }
    }
}
