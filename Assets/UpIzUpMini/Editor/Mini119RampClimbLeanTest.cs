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
    /// MINI-119 follow-up, user: "check the ramp as well." The ledge test
    /// never actually reached its own ramp - the bike stalled dead
    /// against the kerb the whole run. This spawns the bike RIGHT in
    /// front of a steep ramp (no kerb in the way at all) so the climb
    /// itself gets genuinely exercised.
    /// </summary>
    public static class Mini119RampClimbLeanTest
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Test Lean While Climbing A Steep Ramp")]
        public static void Run()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            if (spawner == null) { Debug.LogError("MINI-119 RAMP TEST FAIL: no VehicleSpawnController in scene."); return; }
            var method = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-119 RAMP TEST FAIL: player 'Sacat' not found."); return; }

            InvokeIfExists(spawner, "Awake");
            method.Invoke(spawner, new object[] { player });

            var instance = GameObject.Find("StockDemoSuperMoto");
            if (instance == null) { Debug.LogError("MINI-119 RAMP TEST FAIL: bike not spawned."); return; }

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

            // A steep 25deg ramp, close (10m) and long (20m), directly in
            // the bike's path - no kerb in front of it this time.
            Vector3 forward = instance.transform.forward;
            Vector3 rampPos = instance.transform.position + forward * 10f + Vector3.up * 1.0f;
            var rampGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampGo.name = "TestRamp";
            rampGo.transform.position = rampPos;
            rampGo.transform.rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(-25f, 0f, 0f);
            rampGo.transform.localScale = new Vector3(6f, 0.3f, 20f);

            const float dt = 0.02f;
            const int settleSteps = 50;
            const int driveSteps = 500; // 10s

            FieldInfo vInputField = typeof(Input_Manager).GetField("vInput", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo wheelieField = typeof(Input_Manager).GetField("wheelieInput", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo hzInputField = typeof(Input_Manager).GetField("hzInput", BindingFlags.NonPublic | BindingFlags.Instance);

            float maxRollDuringClimb = 0f;
            bool onRamp;

            for (int i = 0; i < settleSteps + driveSteps; i++)
            {
                bool driving = i >= settleSteps;
                vInputField?.SetValue(remap, driving ? 1f : 0f); // full throttle throughout
                wheelieField?.SetValue(remap, 0f);

                // MINI-119, user: "ramp and turn while on the ramp as
                // well." Steer right the whole time the bike is actually
                // on/near the ramp's height band - the harder, more
                // realistic case (real riders do turn mid-climb).
                onRamp = driving && instance.transform.position.y > rampPos.y - 1.5f;
                hzInputField?.SetValue(remap, onRamp ? 1f : 0f);

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

                float trueRoll = assist != null ? assist.TrueRollDeg : -999f;
                if (driving && instance.transform.position.y > rampPos.y - 1.5f)
                    maxRollDuringClimb = Mathf.Max(maxRollDuringClimb, trueRoll);

                if (i % 25 == 0)
                {
                    float pitch = TruePitchDeg(instance.transform);
                    Debug.Log($"MINI-119 RAMP TEST t={(i - settleSteps) * dt:F1}s: pos={instance.transform.position} pitch={pitch:F1}deg roll={trueRoll:F1}deg speed={rb.linearVelocity.magnitude * 3.6f:F0}km/h isCrashed={gadd.isCrashed}");
                }
            }

            float finalRoll = assist != null ? assist.TrueRollDeg : -999f;
            Debug.Log($"MINI-119 RAMP TEST RESULT: final pos={instance.transform.position}, final roll={finalRoll:F1}deg, max roll seen once on/near the ramp={maxRollDuringClimb:F1}deg. VERDICT: {(finalRoll < 5f && maxRollDuringClimb < 15f ? "PASS" : "FAIL")}");

            Object.DestroyImmediate(rampGo);
            Object.DestroyImmediate(instance);
            Physics.simulationMode = previousSimMode;
        }

        /// <summary>MINI-119, user: "the pitch value staying constant at
        /// 73deg for the second half seems suspicious. maybe the asin."
        /// Right - Mathf.Asin(forward.y) folds back past 90deg (it can't
        /// tell 73deg from 107deg, both give the same forward.y), and
        /// while turning hard the bike's true pitch can genuinely cross
        /// that. Same fix already used for roll: a SIGNED ANGLE around
        /// the bike's own flattened-forward-derived right axis, which
        /// reads the FULL range correctly instead of folding.</summary>
        private static float TruePitchDeg(Transform t)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(t.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.ProjectOnPlane(-t.up, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) return 0f;
            flatForward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flatForward);
            return Vector3.SignedAngle(flatForward, t.forward, right);
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
