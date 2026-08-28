using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: verifies buying "koss" from the Car
    /// Dealer actually spawns a fully-wired, mountable SuperMoto through
    /// VehicleSpawnController.SpawnPurchasedVehicle, using the newly
    /// extracted WireSuperMotoInstance helper - and that the existing
    /// dev-spawned StockDemoSuperMoto (from before this refactor) is
    /// still intact and not duplicated. Delete after use.</summary>
    public static class Mini119KossPurchaseCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Koss Purchase (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            if (spawner == null) { Debug.LogError("MINI-119 KOSS CHECK: no VehicleSpawnController in scene."); return; }

            // Regression check: the pre-existing preplaced dev bike should
            // be untouched by the WireSuperMotoInstance extraction - still
            // exactly one of each key component, not duplicated.
            var existingBike = GameObject.Find("StockDemoSuperMoto");
            if (existingBike != null)
            {
                int seats = existingBike.GetComponents<VehicleSeat>().Length;
                int interactables = existingBike.GetComponents<SuperMotoVehicleInteractable>().Length;
                Debug.Log($"MINI-119 KOSS CHECK: existing StockDemoSuperMoto regression check - VehicleSeat count={seats} (expect 2: driver+pillion), SuperMotoVehicleInteractable count={interactables} (expect 1).");
            }
            else
            {
                Debug.Log("MINI-119 KOSS CHECK: no preplaced StockDemoSuperMoto found in this scene (fine - just means the dev spawn path wasn't exercised here).");
            }

            string result1 = spawner.SpawnPurchasedVehicle("koss");
            Debug.Log($"MINI-119 KOSS CHECK: first SpawnPurchasedVehicle(\"koss\") returned: \"{result1}\"");

            var koss = GameObject.Find("PlayerKoss");
            if (koss == null) { Debug.LogError("MINI-119 KOSS CHECK: PlayerKoss not found after purchase - spawn failed."); return; }

            var interactable = koss.GetComponent<SuperMotoVehicleInteractable>();
            var seat = koss.GetComponents<VehicleSeat>();
            var rb = koss.GetComponent<Rigidbody>();
            Debug.Log($"MINI-119 KOSS CHECK: PlayerKoss has SuperMotoVehicleInteractable={interactable != null}, VehicleSeat count={seat.Length} (expect 2), Rigidbody.interpolation={rb?.interpolation}, position={koss.transform.position}.");

            var hasRider = interactable != null && (bool)typeof(SuperMotoVehicleInteractable).GetProperty("HasRider").GetValue(interactable);
            Debug.Log($"MINI-119 KOSS CHECK: HasRider immediately after purchase={hasRider} (expect False - purchase should NOT auto-mount, player walks up and presses F like the TMAX).");

            string result2 = spawner.SpawnPurchasedVehicle("koss");
            Debug.Log($"MINI-119 KOSS CHECK: second SpawnPurchasedVehicle(\"koss\") returned: \"{result2}\" (expect the already-have message, no second bike).");

            int kossCount = 0;
            foreach (var go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name == "PlayerKoss") kossCount++;
            Debug.Log($"MINI-119 KOSS CHECK: PlayerKoss instance count={kossCount} (expect 1, not 2).");

            Debug.Log("MINI-119 KOSS CHECK: done.");
        }
    }
}
