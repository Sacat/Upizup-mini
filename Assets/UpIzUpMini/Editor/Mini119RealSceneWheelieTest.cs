using System.Reflection;
using Gadd420;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 follow-up, user (with screenshot from the real EXE, at the
    /// real Lalay road spawn, next to the sidewalk): "the bike still rides
    /// like this permanently after a wheelie... your tests are not
    /// working out... your tests are hopeless." Fair - every test this
    /// task ran so far used an isolated flat plane with nothing else on
    /// it. This one doesn't: it opens the REAL GrandBayProof scene and
    /// calls VehicleSpawnController's own private
    /// SpawnStockDemoBikeAndDisableOurCharacter method directly via
    /// reflection - the EXACT same method, same spawn position (the real
    /// Lalay road stall midpoint), same component wiring order - the real
    /// game runs, not a reimplementation of it that could subtly differ.
    /// </summary>
    public static class Mini119RealSceneWheelieTest
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Test Wheelie In REAL Scene At REAL Spawn")]
        public static void Run()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            if (spawner == null) { Debug.LogError("MINI-119 REAL SCENE TEST FAIL: no VehicleSpawnController in the scene."); return; }

            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-119 REAL SCENE TEST FAIL: player 'Sacat' not found."); return; }

            // Awake() sets VehicleSpawnController.Instance - needed since
            // the real spawn method references it indirectly via other
            // calls. Reflection since this normally only runs via Unity's
            // own lifecycle.
            InvokeIfExists(spawner, "Awake");

            var spawnMethod = typeof(VehicleSpawnController).GetMethod(
                "SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            if (spawnMethod == null) { Debug.LogError("MINI-119 REAL SCENE TEST FAIL: SpawnStockDemoBikeAndDisableOurCharacter not found - has it been renamed?"); return; }

            spawnMethod.Invoke(spawner, new object[] { player });

            var instance = GameObject.Find("StockDemoSuperMoto");
            if (instance == null) { Debug.LogError("MINI-119 REAL SCENE TEST FAIL: StockDemoSuperMoto was not spawned - stockDemoBikePrefab not wired?"); return; }

            Debug.Log($"MINI-119 REAL SCENE TEST: spawned at the REAL Lalay road position {instance.transform.position}, exactly as the real game does.");

            var remap = instance.GetComponent<SuperMotoWheelieKeyRemap>();
            var assist = instance.GetComponent<SuperMotoWheelieAssist>();
            var trike = instance.GetComponent<SuperMotoTrikeStabilizer>();
            // MINI-170: this test never invoked SuperMotoUprightAssist's
            // FixedUpdate, even though WireSuperMotoInstance (called by
            // SpawnStockDemoBikeAndDisableOurCharacter above, confirmed by
            // reading the actual call chain, not assumed) DOES attach it
            // to this exact instance - a real harness gap, not evidence
            // the component doesn't run in the live game (Unity's normal
            // lifecycle calls it automatically there). Added so this batch
            // test actually reflects what the real game runs.
            var upright = instance.GetComponent<SuperMotoUprightAssist>();
            var anytimeReset = instance.GetComponent<SuperMotoAnytimeReset>();
            var rb = instance.GetComponent<Rigidbody>();
            var gadd = instance.GetComponent<RB_Controller>();
            var ragdollMgr = instance.GetComponentInChildren<RagdollManager>(true);
            var crashCtrl = instance.GetComponent<CrashController>();
            var autoLevel = instance.GetComponent<AutoLeveling>();
            var groundAngle = instance.GetComponent<GroundAngle>();

            Debug.Log($"MINI-119 REAL SCENE TEST: components present - remap={remap != null}, assist={assist != null}, trike={trike != null}, anytimeReset={anytimeReset != null}, gadd={gadd != null}");

            // MINI-119 follow-up fix, user: "it never worked... wheelie
            // assist not installed." The real bug (found in the user's own
            // Player.log, not a batch test) was that DestroyImmediate on
            // the stock Input_Manager silently failed - Unity blocks
            // removing a component another attached component's
            // RequireComponent depends on, and logs an error instead of
            // throwing, so the old code never noticed. That left TWO
            // Input_Manager-typed components on the bike, with the WRONG
            // (stock, LeftCtrl/LeftShift) one first in GetComponent's
            // resolution order. This is the check that would have caught
            // it: not just "does a remap exist", but "is it the ONLY
            // Input_Manager, and did every dependent component actually
            // resolve to it".
            var allInputMgrs = instance.GetComponents<Gadd420.Input_Manager>();
            bool exactlyOneInputMgr = allInputMgrs.Length == 1 && allInputMgrs[0] is SuperMotoWheelieKeyRemap;
            Debug.Log($"MINI-119 REAL SCENE TEST: Input_Manager count={allInputMgrs.Length}, sole survivor is the E/Q remap={exactlyOneInputMgr}");
            if (!exactlyOneInputMgr)
            {
                Debug.LogError("MINI-119 REAL SCENE TEST FAIL: the stock Input_Manager (LeftCtrl/LeftShift) is still attached alongside (or instead of) the E/Q remap - E will not register.");
            }

            VehicleSpawnController.SetBikeInputEnabled(instance, true); // MINI-181: parked bikes are input-disabled until mounted; simulate the mount
            InvokeIfExists(gadd, "Start");
            InvokeIfExists(remap, "Start");
            InvokeIfExists(ragdollMgr, "Start");
            InvokeIfExists(crashCtrl, "Start");
            InvokeIfExists(autoLevel, "Start");
            InvokeIfExists(groundAngle, "Start");
            Debug.Log($"MINI-181 assist enabled={(assist != null && assist.enabled)} active={instance.activeInHierarchy}");
            InvokeIfExists(assist, "Awake");
            InvokeIfExists(trike, "Awake");
            InvokeIfExists(anytimeReset, "Awake");
            InvokeIfExists(upright, "Awake");
            InvokeIfExists(instance.GetComponent<SuperMotoSuspensionTuning>(), "Awake"); // MINI-181
            bool everCrashed = false; float maxPitchSeen = 0f;

            FieldInfo wheelieField = typeof(Input_Manager).GetField("wheelieInput", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo vInputField = typeof(Input_Manager).GetField("vInput", BindingFlags.NonPublic | BindingFlags.Instance);

            const float dt = 0.02f;
            const int settleSteps = 50; // 1s settle before touching any input, on the REAL road
            const int holdSteps = 400;  // 8s hold - E (wheelie) tapped in bursts, matching the user's own described technique

            for (int i = 0; i < settleSteps + holdSteps; i++)
            {
                bool holding = i >= settleSteps;
                // MINI-119, user: "i should hold down the E to rise and
                // then tap to keep the wheelie going straight like in my
                // original." First 1.5s: E held continuously (the initial
                // rise). After that: tapped in a 0.3s-on/0.2s-off pattern
                // to simulate "tap to sustain" rather than one continuous
                // hold for all 8 seconds.
                bool wheelieHeld;
                if (!holding) wheelieHeld = false;
                else if (i - settleSteps < 75) wheelieHeld = true; // first 1.5s continuous
                else
                {
                    int cyclePos = (i - settleSteps - 75) % 25; // 0.5s cycle
                    wheelieHeld = cyclePos < 15; // 0.3s on, 0.2s off
                }
                wheelieField?.SetValue(remap, wheelieHeld ? -1f : 0f); // -1 = E-equivalent per SuperMotoWheelieKeyRemap's latest mapping
                vInputField?.SetValue(remap, holding ? 1f : 0f);

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
                if (gadd != null && gadd.isCrashed) everCrashed = true; // MINI-181
                if (assist != null) maxPitchSeen = Mathf.Max(maxPitchSeen, assist.CurrentRampDeg);
                if (i >= settleSteps && i % 5 == 0 && i < settleSteps + 200) { var fw = gadd.wheelColliders[1]; var rw = gadd.wheelColliders[0]; Debug.Log($"MINI181TRACE t={(i - settleSteps) * dt:F2} y={instance.transform.position.y:F2} ramp={assist.CurrentRampDeg:F1} fwdY={instance.transform.forward.y:F2} spd={rb.linearVelocity.magnitude * 3.6f:F0}kmh vy={rb.linearVelocity.y:F2} rearG={rw.isGrounded} frontG={fw.isGrounded} held={wheelieHeld} roll={assist.TrueRollDeg:F1} pitchM={assist.TruePitchDeg:F1}"); }

                if (i >= settleSteps && (i - settleSteps) % 50 == 0) // every 1.0s of hold
                {
                    float trueRoll = assist != null ? assist.TrueRollDeg : -999f;
                    float pitch = Mathf.Asin(Mathf.Clamp(instance.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
                    Debug.Log($"MINI-119 REAL SCENE TEST t={(i - settleSteps) * dt:F1}s: pos={instance.transform.position} pitch={pitch:F1}deg TRUE roll={trueRoll:F1}deg isCrashed={(gadd != null ? gadd.isCrashed.ToString() : "n/a")} wheelieTorque={(gadd != null ? gadd.wheelieTorque.ToString("F0") : "n/a")}");
                }
            }

            float finalRoll = assist != null ? assist.TrueRollDeg : -999f;
            float finalPitch = Mathf.Asin(Mathf.Clamp(instance.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            Debug.Log($"MINI-181 CRASH CHECK: everCrashed={everCrashed} maxWheelieDeg={maxPitchSeen:F1} ttcLeft={instance.GetComponentsInChildren<Gadd420.TriggerToCollider>(true).Length}");
            Debug.Log($"MINI-119 REAL SCENE TEST RESULT: after 8s of hold-then-tap wheelieing on the REAL Lalay road spawn - final pos={instance.transform.position}, final pitch={finalPitch:F1}deg, final TRUE roll={finalRoll:F1}deg, isCrashed={(gadd != null ? gadd.isCrashed.ToString() : "n/a")}.");

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
