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

            // MINI-119 follow-up fix (own test bug, found by the user
            // rightly pushing back on "pitch=0.0deg the entire test"):
            // wheel-bottom offset MUST be measured with the root at
            // Vector3.zero, THEN applied to the real spawn position -
            // Mini119SuperMotoSettleTest already does this correctly.
            // This test instead placed the root at Y=5 FIRST and then
            // measured the wheel colliders' ABSOLUTE world Y (which
            // already included that +5), producing a wildly wrong offset
            // (~-4.36) that spawned the bike about 9 REAL METRES below
            // the ground plane - it was in freefall for the entire test,
            // never once touching down, which is exactly why pitch never
            // moved from 0.0deg no matter what. The roll-lock still
            // "passed" throughout because it doesn't require the bike to
            // be grounded at all, which is precisely how this went
            // unnoticed until the settle-diagnostic below caught it.
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var gaddForOffset = instance.GetComponent<RB_Controller>();
            float wheelBottomOffset = 1f;
            if (gaddForOffset != null && gaddForOffset.wheelColliders != null && gaddForOffset.wheelColliders.Length >= 2
                && gaddForOffset.wheelColliders[0] != null && gaddForOffset.wheelColliders[1] != null)
            {
                float rearBottom = gaddForOffset.wheelColliders[0].transform.position.y - gaddForOffset.wheelColliders[0].radius;
                float frontBottom = gaddForOffset.wheelColliders[1].transform.position.y - gaddForOffset.wheelColliders[1].radius;
                wheelBottomOffset = -Mathf.Min(rearBottom, frontBottom);
            }
            instance.transform.position = new Vector3(0f, wheelBottomOffset + 0.1f, 0f);
            Debug.Log($"MINI-119 WHEELIE TEST DIAG: wheelBottomOffset={wheelBottomOffset:F3} (correctly measured at root Y=0), real spawn Y={instance.transform.position.y:F3}, ground plane at Y={ground.transform.position.y:F3}.");

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

            // MINI-119 follow-up, user: "when i bring down the auto-level
            // force it wheelies but it doesnt stay upright." Matches the
            // exact scenario reported - AutoLeveling's own correction
            // deliberately weakened so the new roll-lock (not AutoLeveling)
            // is what's actually being tested here.
            if (autoLevel != null) autoLevel.autoLevelForce = 0.3f;

            // Force the wheelie key "held" for the whole run via
            // reflection on the base Input_Manager's protected field -
            // same technique as driving any other real input in these
            // batch-mode tests, no keyboard available. Throttle also
            // forced on - a stationary bike with no forward motion barely
            // moves under wheelieTorque at all (confirmed by an earlier
            // run of this exact test), which isn't representative of real
            // play and gives the roll-lock nothing real to correct.
            FieldInfo wheelieField = typeof(Input_Manager).GetField("wheelieInput", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo vInputField = typeof(Input_Manager).GetField("vInput", BindingFlags.NonPublic | BindingFlags.Instance);
            // MINI-119 follow-up, user: "i bring down the wheelie torque
            // to 4.00 and its still too high." Real second cause found by
            // reading RB_Controller.Stoppies() directly: it overwrites
            // wheelieTorque with stoppieTorque (1500) whenever BOTH the
            // wheelie key and the brake are held. Held here too, so this
            // test actually exercises that path instead of missing it.
            FieldInfo brakeField = typeof(Input_Manager).GetField("frontBreakInput", BindingFlags.NonPublic | BindingFlags.Instance);

            // Nudge an initial roll in before the hold starts, so the
            // roll-lock has something real to correct rather than a
            // perfectly symmetric setup that would never naturally lean.
            instance.transform.rotation = Quaternion.Euler(0f, 0f, 15f) * instance.transform.rotation;

            const float dt = 0.02f;

            // MINI-119 follow-up, user: "test the wheelie and even after
            // test to see if the bike is still not leaning and can
            // wheelie again while the degrees are good and it doesnt
            // lean." Four real phases, not one continuous hold: SETTLE
            // (confirm both wheels genuinely grounded before anything
            // starts - a spawn artifact was previously making
            // frontWheelOffGround read true even at t=0, before any
            // torque, which was a test bug, not a real wheelie), HOLD #1,
            // RELEASE #1 (confirm it settles back to ~0 roll, not stuck
            // leaned), HOLD #2 (confirm a second wheelie right after the
            // first is just as clean, no carried-over lean), RELEASE #2.
            int settleSteps = 50;   // 1.0s, no input at all
            int hold1Steps = 125;   // 2.5s
            int release1Steps = 75; // 1.5s
            int hold2Steps = 125;   // 2.5s
            int release2Steps = 200; // 4s - extended to see whether auto-recover eventually catches a divergence
            int totalSteps = settleSteps + hold1Steps + release1Steps + hold2Steps + release2Steps;

            float maxAbsRollHold1 = 0f, maxAbsRollHold2 = 0f;
            float rollAfterRelease1 = 0f, rollAfterRelease2 = 0f;
            float maxPitchHold1 = 0f, maxPitchHold2 = 0f;
            float maxWheelieTorqueSeen = 0f;
            bool frontLeftGroundHold1 = false, frontLeftGroundHold2 = false;

            for (int i = 0; i < totalSteps; i++)
            {
                bool inHold1 = i >= settleSteps && i < settleSteps + hold1Steps;
                bool inHold2 = i >= settleSteps + hold1Steps + release1Steps
                    && i < settleSteps + hold1Steps + release1Steps + hold2Steps;
                bool holding = inHold1 || inHold2;

                wheelieField?.SetValue(remap, holding ? 1f : 0f);
                vInputField?.SetValue(remap, holding ? 1f : 0f); // no throttle during settle/release either
                brakeField?.SetValue(remap, holding ? 1f : 0f); // brake+wheelie together - the Stoppies() scenario

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
                float pitch = Mathf.Asin(Mathf.Clamp(instance.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                bool frontOffGround = gadd.wheelColliders != null && gadd.wheelColliders.Length > 1
                    && gadd.wheelColliders[1] != null && !gadd.wheelColliders[1].isGrounded;
                maxWheelieTorqueSeen = Mathf.Max(maxWheelieTorqueSeen, gadd.wheelieTorque);

                if (inHold1) { maxAbsRollHold1 = Mathf.Max(maxAbsRollHold1, Mathf.Abs(roll)); maxPitchHold1 = Mathf.Max(maxPitchHold1, pitch); if (frontOffGround) frontLeftGroundHold1 = true; }
                if (inHold2) { maxAbsRollHold2 = Mathf.Max(maxAbsRollHold2, Mathf.Abs(roll)); maxPitchHold2 = Mathf.Max(maxPitchHold2, pitch); if (frontOffGround) frontLeftGroundHold2 = true; }
                if (i == settleSteps + hold1Steps + release1Steps - 1) rollAfterRelease1 = roll;
                if (i == totalSteps - 1) rollAfterRelease2 = roll;

                if (i == settleSteps - 1)
                {
                    bool rearGrounded = gadd.wheelColliders != null && gadd.wheelColliders.Length > 0
                        && gadd.wheelColliders[0] != null && gadd.wheelColliders[0].isGrounded;
                    bool frontGrounded = gadd.wheelColliders != null && gadd.wheelColliders.Length > 1
                        && gadd.wheelColliders[1] != null && gadd.wheelColliders[1].isGrounded;
                    Debug.Log($"MINI-119 WHEELIE TEST: after 1.0s settle (no input) - rearGrounded={rearGrounded}, frontGrounded={frontGrounded}, roll={roll:F1}deg, pitch={pitch:F1}deg, position={instance.transform.position}, velocity={rb.linearVelocity}. Both grounded flags should be TRUE before the wheelie test below means anything.");
                }
                if (i % 25 == 0 && i >= settleSteps)
                    Debug.Log($"MINI-119 WHEELIE TEST t={(i - settleSteps) * dt:F2}s [{(inHold1 ? "HOLD#1" : inHold2 ? "HOLD#2" : "release")}]: ramp={assist.CurrentRampDeg:F1}deg REAL pitch={pitch:F1}deg REAL roll={roll:F1}deg frontWheelOffGround={frontOffGround} wheelieTorque={gadd.wheelieTorque:F1}");
            }

            Debug.Log($"MINI-119 WHEELIE TEST RESULT (2 full wheelie cycles, wheelie+brake held together each time):\n" +
                $"  HOLD #1: max pitch reached={maxPitchHold1:F1}deg, front wheel actually left ground={frontLeftGroundHold1}, max |roll| during={maxAbsRollHold1:F1}deg\n" +
                $"  after releasing #1 and settling 1.5s: roll={rollAfterRelease1:F1}deg (should be ~0, not stuck leaned)\n" +
                $"  HOLD #2 (right after #1, checking for carried-over lean): max pitch reached={maxPitchHold2:F1}deg, front wheel actually left ground={frontLeftGroundHold2}, max |roll| during={maxAbsRollHold2:F1}deg\n" +
                $"  after releasing #2 and settling 1.5s: roll={rollAfterRelease2:F1}deg (should be ~0)\n" +
                $"  max wheelieTorque seen anywhere={maxWheelieTorqueSeen:F1} (should never exceed maxWheelieTorque={assist.maxWheelieTorque:F1})\n" +
                $"  isCrashed at end={gadd.isCrashed}");

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(ground);
            Physics.simulationMode = previousSimMode;
        }

        /// <summary>MINI-119 follow-up, user (with a screenshot of the
        /// bike lying fully on its side): "i need something that can put
        /// up the bike to straight equal on both side just like when i
        /// press F it respawns straight." Directly reproduces that: lays
        /// the bike flat on its side AND flags it crashed (both wheel-
        /// stabilizer/roll-lock systems used to bail out on isCrashed -
        /// see SuperMotoWheelieAssist's own fix comment) and confirms it
        /// self-rights without ever pressing F.</summary>
        [MenuItem("Up Iz Up Mini/MINI-119/Test Auto-Recover From Fallen (isolated)")]
        public static void RunAutoRecoverTest()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab");
            if (prefab == null) { Debug.LogError("MINI-119 AUTO-RECOVER TEST FAIL: source prefab not found."); return; }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "AutoRecoverTestGround";
            ground.transform.localScale = Vector3.one * 10f;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            // Lying fully on its side (90deg roll), well above the ground
            // so it also has to fall/settle, same as a real toppled bike.
            instance.transform.SetPositionAndRotation(new Vector3(0f, 3f, 0f), Quaternion.Euler(0f, 0f, 90f));

            var stockInput = instance.GetComponent<Input_Manager>();
            if (stockInput != null) Object.DestroyImmediate(stockInput);
            var remap = instance.AddComponent<SuperMotoWheelieKeyRemap>();
            var assist = instance.AddComponent<SuperMotoWheelieAssist>();
            var trike = instance.AddComponent<SuperMotoTrikeStabilizer>();
            foreach (var mgr in instance.GetComponents<Input_Manager>())
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

            // The exact state from the user's screenshot: flagged crashed,
            // constraints removed - this is what silently disabled every
            // correction before the fix.
            gadd.isCrashed = true;
            rb.constraints = RigidbodyConstraints.None;

            const float dt = 0.02f;
            const int steps = 250; // 5 real seconds
            bool recovered = false;
            float recoveredAtSeconds = -1f;
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
                Physics.Simulate(dt);

                float roll = Vector3.SignedAngle(Vector3.up, instance.transform.up, instance.transform.forward);
                if (!recovered && Mathf.Abs(roll) < 5f && instance.transform.position.y > 0f)
                {
                    recovered = true;
                    recoveredAtSeconds = i * dt;
                }
            }

            float finalRoll = Vector3.SignedAngle(Vector3.up, instance.transform.up, instance.transform.forward);
            float finalDot = Vector3.Dot(instance.transform.up, Vector3.up);
            Debug.Log($"MINI-119 AUTO-RECOVER TEST RESULT: started lying on its side (90deg) and flagged crashed - {(recovered ? $"self-righted at t={recoveredAtSeconds:F2}s" : "NEVER SELF-RIGHTED within 5s")}. Final: roll={finalRoll:F1}deg, upright dot={finalDot:F2} (1=perfectly upright), isCrashed={gadd.isCrashed}.");

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
