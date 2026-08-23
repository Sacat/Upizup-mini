using System.Reflection;
using Gadd420;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 follow-up, user: "do some tests and test the angles as
    /// well." Real Physics.Simulate() test (same reflection-driven
    /// lifecycle pattern as Mini119SuperMotoSettleTest, learned the hard
    /// way earlier this task) that artificially holds the wheelie input
    /// on and watches the REAL measured pitch/roll/yaw over several
    /// seconds - specifically to catch two things before the user ever
    /// sees them: (1) the assist pitching the WRONG way (a pure sign
    /// error in SuperMotoWheelieAssist's torque axis), and (2) any
    /// mid-simulation spin/roll leak, not just a final-frame snapshot -
    /// the exact blind spot that let the earlier upright-assist spin bug
    /// through.
    /// </summary>
    public static class Mini119WheelieAssistTest
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Test Wheelie Assist (isolated)")]
        public static void Run()
        {
            // See Mini119SuperMotoSettleTest's own fix comment - this is
            // the missing piece that made THIS test's first run useless
            // (rb.angularVelocity logged exactly (0,0,0) every single step
            // despite real torque being applied), which is what caught
            // the same gap in the settle test too.
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab");
            if (prefab == null) { Debug.LogError("MINI-119 WHEELIE TEST FAIL: source prefab not found."); return; }

            // A real, solid ground plane - the wheelie assist deliberately
            // gates on real rear-wheel grip (rearGrounded) to START a
            // wheelie, same as the original controller's own canStart
            // gate, so a floating bike with nothing under it can never
            // properly test that path.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "WheelieTestGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = Vector3.one * 10f; // 100x100 units

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(new Vector3(0f, 5f, 0f), Quaternion.identity);

            var gaddForOffset = instance.GetComponent<RB_Controller>();
            float wheelBottomOffset = 1f;
            if (gaddForOffset != null && gaddForOffset.wheelColliders != null && gaddForOffset.wheelColliders.Length >= 2
                && gaddForOffset.wheelColliders[0] != null && gaddForOffset.wheelColliders[1] != null)
            {
                float rearBottom = gaddForOffset.wheelColliders[0].transform.position.y - gaddForOffset.wheelColliders[0].radius;
                float frontBottom = gaddForOffset.wheelColliders[1].transform.position.y - gaddForOffset.wheelColliders[1].radius;
                wheelBottomOffset = -Mathf.Min(rearBottom, frontBottom);
            }
            instance.transform.position = new Vector3(0f, wheelBottomOffset + 0.05f, 0f);

            var stockInput = instance.GetComponent<Input_Manager>();
            if (stockInput != null) Object.DestroyImmediate(stockInput);
            var remap = instance.AddComponent<SuperMotoWheelieKeyRemap>();
            var assist = instance.AddComponent<SuperMotoWheelieAssist>();
            var trike = instance.AddComponent<SuperMotoTrikeStabilizer>();

            // Same safety net VehicleSpawnController now applies for real
            // spawns - see SuperMotoWheelieAssist's own fix comment for
            // why this is necessary, not paranoia.
            var survivingInputMgrs = instance.GetComponents<Input_Manager>();
            foreach (var mgr in survivingInputMgrs)
                if (!(mgr is SuperMotoWheelieKeyRemap)) Object.DestroyImmediate(mgr);

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

            // Force the wheelie key "held" for the whole run via
            // reflection on the base Input_Manager's protected field -
            // same technique as driving any other real input in these
            // batch-mode tests, no keyboard available.
            FieldInfo wheelieField = typeof(Input_Manager).GetField("wheelieInput", BindingFlags.NonPublic | BindingFlags.Instance);

            const float dt = 0.02f;
            const int steps = 300; // 6 real seconds
            float maxAbsRoll = 0f, maxAbsYawRate = 0f;
            for (int i = 0; i < steps; i++)
            {
                wheelieField?.SetValue(remap, 1f); // Q-equivalent held throughout

                InvokeIfExists(gadd, "Update");
                InvokeIfExists(crashCtrl, "Update");
                InvokeIfExists(ragdollMgr, "Update");
                InvokeIfExists(groundAngle, "Update");
                InvokeIfExists(autoLevel, "Update");
                InvokeIfExists(gadd, "FixedUpdate");
                InvokeIfExists(autoLevel, "FixedUpdate");
                InvokeIfExists(assist, "FixedUpdate");
                InvokeIfExists(trike, "FixedUpdate");
                Physics.Simulate(dt);

                float roll = Vector3.SignedAngle(Vector3.up, instance.transform.up, instance.transform.forward);
                maxAbsRoll = Mathf.Max(maxAbsRoll, Mathf.Abs(roll));
                maxAbsYawRate = Mathf.Max(maxAbsYawRate, Mathf.Abs(rb.angularVelocity.y));

                if (i == 49 || i == 149 || i == 299)
                {
                    float pitch = Mathf.Asin(Mathf.Clamp(instance.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                    Debug.Log($"MINI-119 WHEELIE TEST t={i * dt:F1}s: ramp={assist.CurrentRampDeg:F1}deg measured pitch={pitch:F1}deg roll={roll:F1}deg angularVel={rb.angularVelocity}, wheelieTorque now={gadd.wheelieTorque:F1}");
                }
            }

            Debug.Log($"MINI-119 WHEELIE TEST RESULT: over 6.0s with wheelie held - max |roll| seen={maxAbsRoll:F1}deg (should stay near 0, 'keep it straight'), max |yaw rate|={maxAbsYawRate:F1}deg/s, isCrashed={gadd.isCrashed}.");

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(ground);
            Physics.simulationMode = previousSimMode;
        }

        private static void InvokeIfExists(Object target, string methodName)
        {
            if (target == null) return;
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            method?.Invoke(target, null);
        }
    }
}
