using System.Reflection;
using Gadd420;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: renders a real camera frame of Sacat
    /// mounted on the SuperMoto (spawned via the real spawn method, same
    /// as every other test in this task), so Claude can actually SEE the
    /// hand/foot IK placement and seated pose instead of relying on the
    /// user's own screenshots for every iteration.</summary>
    public static class Mini119RiderPoseSnapshot
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Snapshot Rider On Bike")]
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
            if (instance == null) { Debug.LogError("MINI-119 SNAPSHOT FAIL: bike not spawned."); return; }

            // MINI-119 follow-up fix: Physics.Simulate() advances physics
            // ONLY - it does NOT call MonoBehaviour Update/FixedUpdate in
            // Edit Mode, so VehicleRider's own pose logic (and the
            // Animator's IK pass, which needs Animator.Update to run at
            // all) never fired on the first version of this tool, which
            // is exactly why it rendered a raw bind T-pose instead of the
            // real seated/IK-corrected one. Same class of test-vs-reality
            // gap already found twice before in this task (InvokeIfExists
            // skipping disabled Behaviours; Physics.Simulate not driving
            // real gameplay constraints) - fixed the same way, by
            // explicitly invoking what Unity's own PlayerLoop would.
            var riderComp = player.GetComponent<VehicleRider>();
            var riderUpdate = typeof(VehicleRider).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            var animator = player.GetComponentInChildren<Animator>();

            // MINI-119 follow-up fix: the FIRST version of this fix only
            // ticked the rider - nothing was ticking the BIKE's own
            // stabilization (SuperMotoUprightAssist/TrikeStabilizer/
            // WheelieAssist/RB_Controller), so 0.1s of unmanaged
            // Physics.Simulate with nothing holding it upright let the
            // bike tip and crash before the camera ever rendered. Same
            // full component list every other real-scene test in this
            // task already uses.
            var gadd = instance.GetComponent<RB_Controller>();
            var assist = instance.GetComponent<SuperMotoWheelieAssist>();
            var trike = instance.GetComponent<SuperMotoTrikeStabilizer>();
            var upright = instance.GetComponent<SuperMotoUprightAssist>();
            var crashCtrl = instance.GetComponent<CrashController>();
            var ragdollMgr = instance.GetComponentInChildren<RagdollManager>(true);
            var autoLevel = instance.GetComponent<AutoLeveling>();
            var groundAngle = instance.GetComponent<GroundAngle>();
            var remap = instance.GetComponent<SuperMotoWheelieKeyRemap>();

            InvokeIfExists(gadd, "Start");
            InvokeIfExists(remap, "Start");
            InvokeIfExists(ragdollMgr, "Start");
            InvokeIfExists(crashCtrl, "Start");
            InvokeIfExists(autoLevel, "Start");
            InvokeIfExists(groundAngle, "Start");
            InvokeIfExists(assist, "Awake");
            InvokeIfExists(trike, "Awake");
            InvokeIfExists(upright, "Awake");

            for (int i = 0; i < 5; i++) // a few ticks so the IK/pose blend actually settles, not just one raw frame
            {
                InvokeIfExists(gadd, "Update");
                InvokeIfExists(crashCtrl, "Update");
                InvokeIfExists(ragdollMgr, "Update");
                InvokeIfExists(groundAngle, "Update");
                InvokeIfExists(autoLevel, "Update");
                riderUpdate?.Invoke(riderComp, null);
                InvokeIfExists(gadd, "FixedUpdate");
                InvokeIfExists(autoLevel, "FixedUpdate");
                InvokeIfExists(assist, "FixedUpdate");
                InvokeIfExists(trike, "FixedUpdate");
                InvokeIfExists(upright, "FixedUpdate");
                animator?.Update(0.02f);
                Physics.Simulate(0.02f);
            }

            // MINI-119 follow-up diagnostic, user (screenshot): arms are
            // spread wide, not reaching either handlebar - is IK even
            // running? PlayAction/BeginSustainedAction return bool but
            // VehicleRider.Mount() never checks it, so a silent failure
            // (missing action id, or the baked controller has no state
            // for it) would leave the Animator on whatever state it
            // already had, with no visibility into that having happened.
            Debug.Log($"MINI-119 SNAPSHOT DIAG: animator={(animator != null ? "found" : "NULL")}, runtimeController={(animator != null ? (animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL") : "n/a")}, layerCount={(animator != null ? animator.layerCount : -1)}, riderComp={(riderComp != null ? "found" : "NULL")}");
            if (animator != null)
            {
                for (int layer = 0; layer < animator.layerCount; layer++)
                {
                    var st = animator.GetCurrentAnimatorStateInfo(layer);
                    Debug.Log($"MINI-119 SNAPSHOT DIAG: layer {layer} ('{animator.GetLayerName(layer)}') weight={animator.GetLayerWeight(layer):F2} stateHash={st.fullPathHash} normalizedTime={st.normalizedTime:F2} IsName(RideBike)={st.IsName("RideBike")} IsName(MountBike)={st.IsName("MountBike")}");
                }
            }

            Vector3 center = instance.transform.position + Vector3.up * 1.0f;

            void Shoot(string name, Vector3 offset)
            {
                var camGo = new GameObject("SnapshotCam");
                var cam = camGo.AddComponent<Camera>();
                camGo.transform.position = center + offset;
                camGo.transform.LookAt(center);
                cam.fieldOfView = 40f;
                cam.farClipPlane = 100f;
                cam.nearClipPlane = 0.05f;

                int w = 1000, h = 1000;
                var rt = new RenderTexture(w, h, 24);
                cam.targetTexture = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);

                System.IO.Directory.CreateDirectory("Logs/Snapshots");
                string path = $"Logs/Snapshots/mini119-rider-{name}.png";
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(camGo);
                Debug.Log($"MINI-119 SNAPSHOT: saved {path}");
            }

            Shoot("front", new Vector3(0f, 0.3f, -2.4f));
            Shoot("side", new Vector3(2.2f, 0.2f, 0f));
            Shoot("closeup-hands", new Vector3(0.9f, -0.1f, -1.1f));

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
