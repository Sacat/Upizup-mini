using System.Reflection;
using Gadd420;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: numerically verifies the vendor's own
    /// IK.cs, now attached to Sacat's hand/foot bones, actually closes
    /// the gap to its target - unlike Mecanim IK, this component has no
    /// Animator Controller dependency, so this check is trustworthy even
    /// in batch mode. Delete after use.</summary>
    public static class Mini119VendorIKCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Vendor IK Contact (one-off)")]
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
            if (instance == null) { Debug.LogError("MINI-119 VENDOR IK CHECK FAIL: bike not spawned."); return; }

            var animator = player.GetComponent<Animator>();
            var ikScripts = player.GetComponentsInChildren<IK>(true);
            Debug.Log($"MINI-119 VENDOR IK CHECK: found {ikScripts.Length} IK components on Sacat.");

            var lateUpdateMethod = typeof(IK).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < 5; i++)
            {
                foreach (var ik in ikScripts) lateUpdateMethod?.Invoke(ik, null);
            }

            foreach (var ik in ikScripts)
            {
                if (ik.target == null) continue;
                float dist = Vector3.Distance(ik.transform.position, ik.target.position);
                Debug.Log($"MINI-119 VENDOR IK CHECK: {ik.gameObject.name} -> target {ik.target.name}: distance={dist:F4}m (target pos={ik.target.position}, bone pos={ik.transform.position})");
            }

            Physics.simulationMode = previousSimMode;
        }
    }
}
