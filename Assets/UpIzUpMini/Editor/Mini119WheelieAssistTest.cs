using System.Reflection;
using Gadd420;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 follow-up, user: "use the wheelie method of the original
    /// tmax and finish." SuperMotoWheelieAssist is now a direct,
    /// kinematic port of TmaxBikeControllerCustom.ApplyWheelie - these
    /// tests were rewritten to match (the old torque/damping/taper
    /// mechanics they used to check no longer exist). Isolated flat-plane
    /// tests; see Mini119RealSceneWheelieTest for the real-scene,
    /// real-spawn equivalent - that one is what actually caught the
    /// spawn-placement bug this task's earlier isolated tests all missed.
    /// </summary>
    public static class Mini119WheelieAssistTest
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Test Wheelie Assist (isolated)")]
        public static void Run()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab");
            if (prefab == null) { Debug.LogError("MINI-119 WHEELIE TEST FAIL: source prefab not found."); return; }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "WheelieTestGround";
            ground.transform.localScale = Vector3.one * 10f;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
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

            var stockInput = instance.GetComponent<Input_Manager>();
            if (stockInput != null) Object.DestroyImmediate(stockInput);
            var remap = instance.AddComponent<SuperMotoWheelieKeyRemap>();
            var assist = instance.AddComponent<SuperMotoWheelieAssist>();
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

            FieldInfo wheelieField = typeof(Input_Manager).GetField("wheelieInput", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo vInputField = typeof(Input_Manager).GetField("vInput", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo brakeField = typeof(Input_Manager).GetField("frontBreakInput", BindingFlags.NonPublic | BindingFlags.Instance);

            const float dt = 0.02f;
            int settleSteps = 50;
            int hold1Steps = 125;
            int release1Steps = 75;
            int hold2Steps = 125;
            int release2Steps = 75;
            int totalSteps = settleSteps + hold1Steps + release1Steps + hold2Steps + release2Steps;

            float maxAbsRollHold1 = 0f, maxAbsRollHold2 = 0f;
            float rollAfterRelease1 = 0f, rollAfterRelease2 = 0f;
            float maxPitchHold1 = 0f, maxPitchHold2 = 0f;
            bool frontLeftGroundHold1 = false, frontLeftGroundHold2 = false;

            for (int i = 0; i < totalSteps; i++)
            {
                bool inHold1 = i >= settleSteps && i < settleSteps + hold1Steps;
                bool inHold2 = i >= settleSteps + hold1Steps + release1Steps
                    && i < settleSteps + hold1Steps + release1Steps + hold2Steps;
                bool holding = inHold1 || inHold2;

                wheelieField?.SetValue(remap, holding ? 1f : 0f);
                vInputField?.SetValue(remap, holding ? 1f : 0f);
                brakeField?.SetValue(remap, 0f);

                InvokeIfExists(gadd, "Update");
                InvokeIfExists(crashCtrl, "Update");
                InvokeIfExists(ragdollMgr, "Update");
                InvokeIfExists(groundAngle, "Update");
                InvokeIfExists(autoLevel, "Update");
                InvokeIfExists(gadd, "FixedUpdate");
                InvokeIfExists(autoLevel, "FixedUpdate");
                InvokeIfExists(assist, "FixedUpdate");
                Physics.Simulate(dt);

                float roll = assist.TrueRollDeg;
                float pitch = assist.TruePitchDeg;

                if (inHold1) { maxAbsRollHold1 = Mathf.Max(maxAbsRollHold1, roll); maxPitchHold1 = Mathf.Max(maxPitchHold1, pitch); if (assist.CurrentRampDeg > 1f) frontLeftGroundHold1 = true; }
                if (inHold2) { maxAbsRollHold2 = Mathf.Max(maxAbsRollHold2, roll); maxPitchHold2 = Mathf.Max(maxPitchHold2, pitch); if (assist.CurrentRampDeg > 1f) frontLeftGroundHold2 = true; }
                if (i == settleSteps + hold1Steps + release1Steps - 1) rollAfterRelease1 = roll;
                if (i == totalSteps - 1) rollAfterRelease2 = roll;

                if (i % 25 == 0)
                    Debug.Log($"MINI-119 WHEELIE TEST t={i * dt:F2}s: wheelieAngle={assist.CurrentRampDeg:F1}deg pitch={pitch:F1}deg TRUE roll={roll:F1}deg isCrashed={gadd.isCrashed}");
            }

            Debug.Log($"MINI-119 WHEELIE TEST RESULT (2 full wheelie cycles, kinematic port of the original):\n" +
                $"  HOLD #1: max pitch reached={maxPitchHold1:F1}deg, wheelie engaged={frontLeftGroundHold1}, max TRUE roll during={maxAbsRollHold1:F1}deg\n" +
                $"  after releasing #1: roll={rollAfterRelease1:F1}deg (should be ~0)\n" +
                $"  HOLD #2: max pitch reached={maxPitchHold2:F1}deg, wheelie engaged={frontLeftGroundHold2}, max TRUE roll during={maxAbsRollHold2:F1}deg\n" +
                $"  after releasing #2: roll={rollAfterRelease2:F1}deg (should be ~0)\n" +
                $"  isCrashed at end={gadd.isCrashed}");

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(ground);
            Physics.simulationMode = previousSimMode;
        }

        [MenuItem("Up Iz Up Mini/MINI-119/Test 15s Sustained Wheelie From Stop (isolated)")]
        public static void Run15SecondTest()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab");
            if (prefab == null) { Debug.LogError("MINI-119 15S TEST FAIL: source prefab not found."); return; }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FifteenSecondTestGround";
            ground.transform.localScale = Vector3.one * 10f;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var gaddForOffset = instance.GetComponent<RB_Controller>();
            float wheelBottomOffset = 1f;
            if (gaddForOffset.wheelColliders != null && gaddForOffset.wheelColliders.Length >= 2)
            {
                float rearBottom = gaddForOffset.wheelColliders[0].transform.position.y - gaddForOffset.wheelColliders[0].radius;
                float frontBottom = gaddForOffset.wheelColliders[1].transform.position.y - gaddForOffset.wheelColliders[1].radius;
                wheelBottomOffset = -Mathf.Min(rearBottom, frontBottom);
            }
            instance.transform.position = new Vector3(0f, wheelBottomOffset + 0.1f, 0f);

            var stockInput = instance.GetComponent<Input_Manager>();
            if (stockInput != null) Object.DestroyImmediate(stockInput);
            var remap = instance.AddComponent<SuperMotoWheelieKeyRemap>();
            var assist = instance.AddComponent<SuperMotoWheelieAssist>();
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

            FieldInfo wheelieField = typeof(Input_Manager).GetField("wheelieInput", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo vInputField = typeof(Input_Manager).GetField("vInput", BindingFlags.NonPublic | BindingFlags.Instance);

            const float dt = 0.02f;
            int settleSteps = 50;   // 1.0s, no input at all
            int holdSteps = 750;    // 15.0s, W+E held together from a dead stop

            float maxAbsRoll = 0f, minPitch = 0f, maxPitch = 0f;
            for (int i = 0; i < settleSteps + holdSteps; i++)
            {
                bool holding = i >= settleSteps;
                wheelieField?.SetValue(remap, holding ? 1f : 0f);
                vInputField?.SetValue(remap, holding ? 1f : 0f);

                InvokeIfExists(gadd, "Update");
                InvokeIfExists(crashCtrl, "Update");
                InvokeIfExists(ragdollMgr, "Update");
                InvokeIfExists(groundAngle, "Update");
                InvokeIfExists(autoLevel, "Update");
                InvokeIfExists(gadd, "FixedUpdate");
                InvokeIfExists(autoLevel, "FixedUpdate");
                InvokeIfExists(assist, "FixedUpdate");
                Physics.Simulate(dt);

                if (!holding) continue;

                float roll = assist.TrueRollDeg;
                float pitch = assist.TruePitchDeg;
                maxAbsRoll = Mathf.Max(maxAbsRoll, roll);
                minPitch = Mathf.Min(minPitch, pitch);
                maxPitch = Mathf.Max(maxPitch, pitch);

                int holdStep = i - settleSteps;
                if (holdStep % 100 == 0) // every 2.0s
                    Debug.Log($"MINI-119 15S TEST t={holdStep * dt:F1}s: wheelieAngle={assist.CurrentRampDeg:F1}deg pitch={pitch:F1}deg TRUE roll={roll:F1}deg isCrashed={gadd.isCrashed}");
            }

            Debug.Log($"MINI-119 15S TEST RESULT: over 15.0s from a dead stop with W+E held together (kinematic port of the original) - pitch range=[{minPitch:F1}, {maxPitch:F1}]deg (target ceiling={assist.rampCeilingDeg:F1}deg), max TRUE roll anywhere={maxAbsRoll:F1}deg (should stay ~0 the whole time), isCrashed={gadd.isCrashed}.");

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(ground);
            Physics.simulationMode = previousSimMode;
        }

        /// <summary>MINI-119 follow-up, user (with a screenshot of the
        /// bike lying fully on its side): "i need something that can put
        /// up the bike to straight equal on both side just like when i
        /// press F it respawns straight." Directly reproduces that: lays
        /// the bike flat on its side AND flags it crashed and confirms it
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
            instance.transform.SetPositionAndRotation(new Vector3(0f, 3f, 0f), Quaternion.Euler(0f, 0f, 90f));

            var stockInput = instance.GetComponent<Input_Manager>();
            if (stockInput != null) Object.DestroyImmediate(stockInput);
            var remap = instance.AddComponent<SuperMotoWheelieKeyRemap>();
            var assist = instance.AddComponent<SuperMotoWheelieAssist>();
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
