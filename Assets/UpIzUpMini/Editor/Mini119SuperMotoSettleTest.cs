using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119, user: "use screenshot verification before building the
    /// exe." Real Physics.Simulate() test against the REAL GrandBayProof
    /// scene, at the same spawn logic VehicleSpawnController's own dev-
    /// spawn uses.
    ///
    /// Correction from the first version of this test: Physics.Simulate()
    /// only steps the physics ENGINE - it does not call Awake/Start/
    /// FixedUpdate on scripts outside Play Mode (the same limitation
    /// noted throughout this project - see Mini065TmaxDropTest's own use
    /// of reflection for exactly this reason). The first run of this test
    /// therefore measured an entirely unscripted, inert Rigidbody falling
    /// over - RB_Controller's own Start() (which sets the roll-lock
    /// constraint) and FixedUpdate() never actually ran, so that result
    /// proved nothing about real gameplay. Every relevant lifecycle
    /// method is now invoked explicitly via reflection, matching this
    /// project's own established pattern.
    ///
    /// Screenshot capture removed - Camera.Render()/RenderTexture
    /// segfaulted Unity outright in this -nographics batch environment,
    /// twice, reproducibly. The numeric roll/pitch/upright measurement is
    /// the actual verification signal; a visual snapshot can be revisited
    /// separately if still wanted once this environment's headless
    /// rendering issue is understood.
    /// </summary>
    public static class Mini119SuperMotoSettleTest
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Settle Test SuperMoto In GrandBayProof")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UpIzUpMini/Vehicles/TMAX_560_SuperMoto.prefab");
            if (prefab == null) { Debug.LogError("MINI-119 SETTLE TEST FAIL: TMAX_560_SuperMoto.prefab not found."); return; }

            var player = GameObject.Find("Sacat");
            if (player == null) { Debug.LogError("MINI-119 SETTLE TEST FAIL: player character 'Sacat' not found in the scene."); return; }

            Vector3 forward = player.transform.forward;
            Vector3 spawnPos = player.transform.position + forward * 6f;
            if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit groundHit, 30f))
                spawnPos = groundHit.point;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "SettleTestSuperMoto";

            var gadd = instance.GetComponent<Gadd420.RB_Controller>();
            float wheelBottomOffset = 1.0f;
            if (gadd != null && gadd.wheelColliders != null && gadd.wheelColliders.Length >= 2
                && gadd.wheelColliders[0] != null && gadd.wheelColliders[1] != null)
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                float rearBottom = gadd.wheelColliders[0].transform.position.y - gadd.wheelColliders[0].radius;
                float frontBottom = gadd.wheelColliders[1].transform.position.y - gadd.wheelColliders[1].radius;
                wheelBottomOffset = -Mathf.Min(rearBottom, frontBottom);
            }
            spawnPos.y += wheelBottomOffset + 0.05f;
            instance.transform.SetPositionAndRotation(spawnPos, Quaternion.LookRotation(forward, Vector3.up));

            // Every MonoBehaviour whose Awake/Start actually matters for
            // whether this bike stands up, invoked explicitly - the same
            // reflection pattern this project already uses everywhere
            // physics behaviour needs testing outside real Play Mode.
            var facade = instance.GetComponent<TmaxBikeController>();
            var input = instance.GetComponent<GaddInputAdapter>();
            var ragdollMgr = instance.GetComponentInChildren<Gadd420.RagdollManager>(true);
            var crashCtrl = instance.GetComponent<Gadd420.CrashController>();
            var autoLevel = instance.GetComponent<Gadd420.AutoLeveling>();
            var groundAngle = instance.GetComponent<Gadd420.GroundAngle>();

            InvokeIfExists(facade, "Awake");
            InvokeIfExists(gadd, "Start");
            InvokeIfExists(input, "Start");
            InvokeIfExists(ragdollMgr, "Start");
            InvokeIfExists(crashCtrl, "Start");
            InvokeIfExists(autoLevel, "Start");
            InvokeIfExists(groundAngle, "Start");

            Debug.Log($"MINI-119 SETTLE TEST: after lifecycle invocation - rb.constraints={instance.GetComponent<Rigidbody>().constraints}");

            const float dt = 0.02f;
            const int steps = 250; // 5 real seconds
            for (int i = 0; i < steps; i++)
            {
                InvokeIfExists(gadd, "Update");
                InvokeIfExists(crashCtrl, "Update");
                InvokeIfExists(ragdollMgr, "Update");
                InvokeIfExists(groundAngle, "Update");
                InvokeIfExists(facade, "Update");
                InvokeIfExists(gadd, "FixedUpdate");
                InvokeIfExists(facade, "FixedUpdate");
                Physics.Simulate(dt);
            }

            float rollDeg = Vector3.SignedAngle(Vector3.up, instance.transform.up, instance.transform.forward);
            float pitchDeg = Mathf.Asin(Mathf.Clamp(instance.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            float uprightDot = Vector3.Dot(instance.transform.up, Vector3.up);
            Debug.Log($"MINI-119 SETTLE TEST: after {steps * dt:F1}s of REAL scripted simulation - roll={rollDeg:F1}deg, pitch={pitchDeg:F1}deg, upright dot={uprightDot:F2} (1=perfectly upright, <0.5 means fallen over), position={instance.transform.position}, rb.constraints={instance.GetComponent<Rigidbody>().constraints}, isCrashed={(gadd != null ? gadd.isCrashed.ToString() : "n/a")}");

            Object.DestroyImmediate(instance);
        }

        private static void InvokeIfExists(Object target, string methodName)
        {
            if (target == null) return;
            var method = target.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            method?.Invoke(target, null);
        }
    }
}
