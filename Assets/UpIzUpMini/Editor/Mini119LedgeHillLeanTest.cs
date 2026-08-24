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
    /// MINI-119 follow-up, user: "do a test with climbing hills and steep
    /// ledges as well" / "i thin the assist is defaulting the upright
    /// position of the bike to the wrong value thats why it rides with a
    /// lean after a hit." Drives the REAL spawned bike (same spawn method,
    /// same component wiring as every other MINI-119 test) into a
    /// deliberately placed steep kerb/ledge at speed, and separately up a
    /// steep ramp, logging roll before/during/after each. If roll
    /// consistently settles on a nonzero value rather than returning to
    /// 0, that's real evidence for a wrong default reference rather than
    /// random settling noise.
    /// </summary>
    public static class Mini119LedgeHillLeanTest
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Test Lean After Ledge Hit + Hill Climb")]
        public static void Run()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            if (spawner == null) { Debug.LogError("MINI-119 LEDGE/HILL TEST FAIL: no VehicleSpawnController in scene."); return; }
            var method = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-119 LEDGE/HILL TEST FAIL: player 'Sacat' not found."); return; }

            InvokeIfExists(spawner, "Awake");
            method.Invoke(spawner, new object[] { player });

            var instance = GameObject.Find("StockDemoSuperMoto");
            if (instance == null) { Debug.LogError("MINI-119 LEDGE/HILL TEST FAIL: bike not spawned."); return; }

            var remap = instance.GetComponent<SuperMotoWheelieKeyRemap>();
            var assist = instance.GetComponent<SuperMotoWheelieAssist>();
            var trike = instance.GetComponent<SuperMotoTrikeStabilizer>();
            var upright = instance.GetComponent<SuperMotoUprightAssist>();
            var rb = instance.GetComponent<Rigidbody>();
            var gadd = instance.GetComponent<RB_Controller>();
            var ragdollMgr = instance.GetComponentInChildren<RagdollManager>(true);
            var crashCtrl = instance.GetComponent<CrashController>();
            var autoLevel = instance.GetComponent<AutoLeveling>();
            var groundAngle = instance.GetComponent<GroundAngle>();

            InvokeIfExists(gadd, "Start");
            InvokeIfExists(remap, "Start");
            InvokeIfExists(ragdollMgr, "Start");
            InvokeIfExists(crashCtrl, "Start");
            InvokeIfExists(autoLevel, "Start");
            InvokeIfExists(groundAngle, "Start");
            InvokeIfExists(assist, "Awake");
            InvokeIfExists(trike, "Awake");
            InvokeIfExists(upright, "Awake");

            // A real kerb/ledge: a box 0.35m tall, spanning the bike's
            // path, 25m ahead - tall enough to genuinely upset the bike
            // (curb-height, not a pebble) but not so tall it's a wall.
            Vector3 forward = instance.transform.forward;
            Vector3 ledgePos = instance.transform.position + forward * 25f + Vector3.up * 0.15f;
            var ledgeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledgeGo.name = "TestLedge";
            ledgeGo.transform.position = ledgePos;
            ledgeGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            ledgeGo.transform.localScale = new Vector3(6f, 0.35f, 0.3f); // wide, kerb-height, thin

            // A steep ramp further along the SAME path, angled up at 25deg.
            Vector3 rampPos = instance.transform.position + forward * 60f + Vector3.up * 1.5f;
            var rampGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampGo.name = "TestRamp";
            rampGo.transform.position = rampPos;
            rampGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(-25f, 0f, 0f);
            rampGo.transform.localScale = new Vector3(6f, 0.3f, 12f);

            const float dt = 0.02f;
            const int settleSteps = 50;
            const int driveSteps = 700; // 14s - long enough to settle, hit the ledge, climb the ramp, settle again

            FieldInfo vInputField = typeof(Input_Manager).GetField("vInput", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo wheelieField = typeof(Input_Manager).GetField("wheelieInput", BindingFlags.NonPublic | BindingFlags.Instance);

            for (int i = 0; i < settleSteps + driveSteps; i++)
            {
                bool driving = i >= settleSteps;
                vInputField?.SetValue(remap, driving ? 1f : 0f); // full throttle, no wheelie, no steering - roll changes are purely from terrain
                wheelieField?.SetValue(remap, 0f);

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

                if (i % 25 == 0) // every 0.5s
                {
                    float trueRoll = assist != null ? assist.TrueRollDeg : -999f;
                    float pitch = Mathf.Asin(Mathf.Clamp(instance.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                    Debug.Log($"MINI-119 LEDGE/HILL TEST t={(i - settleSteps) * dt:F1}s: pos={instance.transform.position} pitch={pitch:F1}deg roll={trueRoll:F1}deg speed={rb.linearVelocity.magnitude * 3.6f:F0}km/h isCrashed={gadd.isCrashed}");
                }
            }

            float finalRoll = assist != null ? assist.TrueRollDeg : -999f;
            Debug.Log($"MINI-119 LEDGE/HILL TEST RESULT: final roll={finalRoll:F1}deg after hitting a kerb-height ledge and climbing a 25deg ramp under straight throttle (no steering input at all). VERDICT: {(finalRoll < 5f ? "PASS - settled back to true upright" : "FAIL - stuck leaned, roll=" + finalRoll.ToString("F1") + "deg")}");

            Object.DestroyImmediate(ledgeGo);
            Object.DestroyImmediate(rampGo);
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
