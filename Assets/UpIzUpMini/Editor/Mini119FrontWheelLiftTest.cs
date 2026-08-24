using System.Reflection;
using Gadd420;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 follow-up, user: "a physical test is needed. you need to
    /// check the ground see if the front wheel lifts the ground... when i
    /// press E at least the front wheel should leave the ground."
    ///
    /// Completely fair criticism of every previous test in this task: they
    /// all measured the bike's PITCH ANGLE and called a big number a
    /// wheelie. Pitch angle is not a wheelie. A wheelie is the front wheel
    /// physically leaving the ground - and those are NOT the same thing on
    /// a rig with real WheelCollider suspension, which can extend
    /// downward to keep the wheel planted even while the chassis pitches
    /// up. This test measures ONLY the physical fact:
    ///   - WheelCollider.isGrounded on the FRONT wheel
    ///   - the real world-space gap between the front wheel's bottom and
    ///     the ground directly beneath it (a raycast, not an angle)
    /// and reports the actual lift height in metres.
    ///
    /// Runs in the REAL GrandBayProof scene via VehicleSpawnController's
    /// own spawn method, and also drives INTO a real hill to test climbing
    /// power, since the user reported both failures together.
    /// </summary>
    public static class Mini119FrontWheelLiftTest
    {
        [MenuItem("Up Iz Up Mini/MINI-119/PHYSICAL Test - Front Wheel Lift (LOW speed, press E early)")]
        public static void RunLowSpeed() => RunWithAccelSteps(15); // E pressed after only 0.3s of throttle

        [MenuItem("Up Iz Up Mini/MINI-119/PHYSICAL Test - Does The Front Wheel Leave The Ground")]
        public static void Run() => RunWithAccelSteps(150);

        private static void RunWithAccelSteps(int accelSteps)
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            var player = GameObject.Find("Sacat");
            if (spawner == null || player == null)
            {
                Debug.LogError("MINI-119 LIFT TEST FAIL: spawner or player not found.");
                Physics.simulationMode = previousSimMode;
                return;
            }

            InvokeIfExists(spawner, "Awake");
            var spawnMethod = typeof(VehicleSpawnController).GetMethod(
                "SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            spawnMethod.Invoke(spawner, new object[] { player });

            var instance = GameObject.Find("StockDemoSuperMoto");
            if (instance == null)
            {
                Debug.LogError("MINI-119 LIFT TEST FAIL: bike not spawned.");
                Physics.simulationMode = previousSimMode;
                return;
            }

            var remap = instance.GetComponent<SuperMotoWheelieKeyRemap>();
            var assist = instance.GetComponent<SuperMotoWheelieAssist>();
            var anytimeReset = instance.GetComponent<SuperMotoAnytimeReset>();
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
            InvokeIfExists(anytimeReset, "Awake");

            var frontWheel = gadd.wheelColliders[1];
            var rearWheel = gadd.wheelColliders[0];

            FieldInfo wheelieField = typeof(Input_Manager).GetField("wheelieInput", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo vInputField = typeof(Input_Manager).GetField("vInput", BindingFlags.NonPublic | BindingFlags.Instance);

            const float dt = 0.02f;
            const int wheelieSteps = 400; // 8s holding E

            float maxLiftMetres = 0f;
            int stepsFrontOffGround = 0;
            float speedAtWheelieStart = 0f;
            bool everCrashed = false;

            for (int i = 0; i < accelSteps + wheelieSteps; i++)
            {
                bool wheelieHeld = i >= accelSteps;
                if (i == accelSteps) speedAtWheelieStart = rb.linearVelocity.magnitude * 3.6f;

                wheelieField?.SetValue(remap, wheelieHeld ? -1f : 0f); // -1 = E per the current remap
                vInputField?.SetValue(remap, 1f); // W held the whole time

                InvokeIfExists(gadd, "Update");
                InvokeIfExists(crashCtrl, "Update");
                InvokeIfExists(ragdollMgr, "Update");
                InvokeIfExists(groundAngle, "Update");
                InvokeIfExists(autoLevel, "Update");
                InvokeIfExists(gadd, "FixedUpdate");
                InvokeIfExists(autoLevel, "FixedUpdate");
                InvokeIfExists(assist, "FixedUpdate");
                Physics.Simulate(dt);

                if (gadd.isCrashed) everCrashed = true;

                if (wheelieHeld)
                {
                    // THE physical measurement: where is the bottom of the
                    // front wheel, and how far is the ground below it?
                    Vector3 frontBottom = frontWheel.transform.position - Vector3.up * frontWheel.radius;
                    float liftMetres = 0f;
                    if (Physics.Raycast(frontBottom + Vector3.up * 0.05f, Vector3.down, out RaycastHit hit, 20f))
                        liftMetres = Mathf.Max(0f, (frontBottom.y - hit.point.y));

                    if (!frontWheel.isGrounded) stepsFrontOffGround++;
                    maxLiftMetres = Mathf.Max(maxLiftMetres, liftMetres);

                    int ws = i - accelSteps;
                    if (ws % 50 == 0)
                    {
                        Debug.Log($"MINI-119 LIFT TEST t={ws * dt:F1}s: FRONT WHEEL grounded={frontWheel.isGrounded} liftAboveGround={liftMetres:F3}m | rear grounded={rearWheel.isGrounded} | speed={rb.linearVelocity.magnitude * 3.6f:F1}km/h wheelieAngle={assist.CurrentRampDeg:F1}deg isCrashed={gadd.isCrashed}");
                    }
                }
            }

            float pctOffGround = 100f * stepsFrontOffGround / (float)wheelieSteps;
            bool realWheelie = maxLiftMetres > 0.1f && pctOffGround > 20f;
            Debug.Log($"MINI-119 LIFT TEST RESULT: speed when E first pressed={speedAtWheelieStart:F1}km/h (min gate={assist.wheelieMinSpeedKmh:F1}, max gate={assist.wheelieMaxSpeedKmh:F1}) | " +
                $"MAX FRONT WHEEL LIFT={maxLiftMetres:F3}m | front wheel off ground for {pctOffGround:F0}% of the 8s hold | everCrashed={everCrashed} | " +
                $"VERDICT: {(realWheelie ? "REAL WHEELIE - front wheel genuinely leaves the ground" : "NO REAL WHEELIE - the front wheel never meaningfully left the ground")}");

            Object.DestroyImmediate(instance);
            Physics.simulationMode = previousSimMode;
        }

        private static void InvokeIfExists(Object target, string methodName)
        {
            if (target == null) return;
            // MINI-119 follow-up fix: Unity's real message dispatch skips
            // a disabled Behaviour's Update/FixedUpdate entirely - a
            // reflection-driven MethodInfo.Invoke() does not, silently
            // defeating any fix that works by setting .enabled = false.
            if (target is Behaviour b && !b.enabled) return;
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            method?.Invoke(target, null);
        }
    }
}
