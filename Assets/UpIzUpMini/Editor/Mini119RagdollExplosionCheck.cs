using System.Reflection;
using Gadd420;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "mission failed fell off
    /// playable area do some research first and them fix up." Tracks
    /// Sacat's position/velocity frame-by-frame from the instant the
    /// ragdoll+joints engage, to find exactly when/how he launches,
    /// rather than guessing at spring values blind. Also runs the full
    /// bike component tick list (unlike earlier checks that only ticked
    /// the rider) so the bike itself stays stable and isn't a
    /// confounding variable.</summary>
    public static class Mini119RagdollExplosionCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Ragdoll Explosion (one-off)")]
        public static void Run()
        {
            var previousSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            var method = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            var player = GameObject.Find("Sacat");
            method.Invoke(spawner, new object[] { player });

            var instance = GameObject.Find("StockDemoSuperMoto");
            if (instance == null) { Debug.LogError("MINI-119 EXPLOSION CHECK FAIL: bike not spawned."); return; }

            var rider = player.GetComponent<SuperMotoRagdollRider>();
            var riderUpdate = typeof(SuperMotoRagdollRider).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);

            var gadd = instance.GetComponent<RB_Controller>();
            var remap = instance.GetComponent<SuperMotoWheelieKeyRemap>();
            var assist = instance.GetComponent<SuperMotoWheelieAssist>();
            var trike = instance.GetComponent<SuperMotoTrikeStabilizer>();
            var upright = instance.GetComponent<SuperMotoUprightAssist>();
            var crashCtrl = instance.GetComponent<CrashController>();
            var ragdollMgr = instance.GetComponentInChildren<RagdollManager>(true);
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

            Rigidbody hipsRb = null;

            for (int i = 0; i < 150; i++) // 3s at 0.02
            {
                InvokeIfExists(gadd, "Update");
                InvokeIfExists(crashCtrl, "Update");
                InvokeIfExists(ragdollMgr, "Update");
                InvokeIfExists(groundAngle, "Update");
                InvokeIfExists(autoLevel, "Update");
                try { riderUpdate?.Invoke(rider, null); }
                catch (TargetInvocationException ex) { Debug.LogError($"MINI-119 EXPLOSION CHECK: rider Update() threw on tick {i}: {ex.InnerException}"); break; }
                InvokeIfExists(gadd, "FixedUpdate");
                InvokeIfExists(autoLevel, "FixedUpdate");
                InvokeIfExists(assist, "FixedUpdate");
                InvokeIfExists(trike, "FixedUpdate");
                InvokeIfExists(upright, "FixedUpdate");
                Physics.Simulate(0.02f);

                if (hipsRb == null)
                {
                    var animator = player.GetComponentInChildren<Animator>(true);
                    var hips = animator != null ? animator.GetBoneTransform(HumanBodyBones.Hips) : null;
                    if (hips != null) hipsRb = hips.GetComponent<Rigidbody>();
                }

                if (i % 5 == 0 || (hipsRb != null && hipsRb.linearVelocity.magnitude > 5f))
                {
                    string hipsInfo = hipsRb != null
                        ? $"hipsPos={hipsRb.transform.position} hipsVel={hipsRb.linearVelocity} hipsSpeed={hipsRb.linearVelocity.magnitude:F1}"
                        : "hips rb not found yet";
                    Debug.Log($"MINI-119 EXPLOSION CHECK t={i * 0.02f:F2}s: playerPos={player.transform.position} bikePos={instance.transform.position} {hipsInfo}");
                }
            }

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
