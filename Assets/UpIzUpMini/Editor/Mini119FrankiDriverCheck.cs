using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: verifies the whole mount/pillion
    /// system works symmetrically when FRANKI is the one driving and
    /// Sacat auto-boards as pillion - nothing in the actual game code
    /// is hardcoded to Sacat specifically, but this had only ever been
    /// exercised with Sacat as the driver until now. Delete after
    /// use.</summary>
    public static class Mini119FrankiDriverCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Franki As Driver (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var switcher = CharacterSwitchManager.Instance ?? Object.FindFirstObjectByType<CharacterSwitchManager>();
            if (switcher == null) { Debug.LogError("MINI-119 FRANKI DRIVER CHECK: no CharacterSwitchManager."); return; }

            if (CharacterSwitchManager.Instance == null)
            {
                var awakeMethod = typeof(CharacterSwitchManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                awakeMethod?.Invoke(switcher, null);
            }

            int frankiIndex = -1;
            for (int i = 0; i < switcher.Slots.Length; i++)
                if (switcher.Slots[i]?.root != null && switcher.Slots[i].root.name == "Franki") frankiIndex = i;

            if (frankiIndex < 0) { Debug.LogError("MINI-119 FRANKI DRIVER CHECK: Franki not found in any slot."); return; }

            switcher.SwitchTo(frankiIndex);
            Debug.Log($"MINI-119 FRANKI DRIVER CHECK: switched active to slot {frankiIndex} ({switcher.Active?.root?.name}).");

            var franki = switcher.Active.root;
            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            var spawnMethod = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            spawnMethod.Invoke(spawner, new object[] { franki });

            var bike = GameObject.Find("StockDemoSuperMoto");
            var interactable = bike?.GetComponent<SuperMotoVehicleInteractable>();
            if (interactable == null) { Debug.LogError("MINI-119 FRANKI DRIVER CHECK: no interactable after spawn."); return; }

            var hasRider = (bool)typeof(SuperMotoVehicleInteractable).GetProperty("HasRider").GetValue(interactable);
            Debug.Log($"MINI-119 FRANKI DRIVER CHECK: HasRider after spawn (auto-mount should have fired for Franki)={hasRider}, Franki parent={franki.transform.parent?.name ?? "none"}");

            var mountedPlayerField = typeof(SuperMotoVehicleInteractable).GetField("_mountedPlayer", BindingFlags.NonPublic | BindingFlags.Instance);
            var mountedPlayer = mountedPlayerField.GetValue(interactable) as GameObject;
            Debug.Log($"MINI-119 FRANKI DRIVER CHECK: driver mounted = {mountedPlayer?.name ?? "NULL"} (expect Franki)");

            var sacat = GameObject.Find("Sacat");
            if (sacat != null) sacat.transform.position = bike.transform.position + Vector3.right * 1.5f;

            var updateMethod = typeof(SuperMotoVehicleInteractable).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            updateMethod.Invoke(interactable, null);

            var pillionField = typeof(SuperMotoVehicleInteractable).GetField("_pillion", BindingFlags.NonPublic | BindingFlags.Instance);
            var pillion = pillionField.GetValue(interactable) as VehicleRider;
            Debug.Log($"MINI-119 FRANKI DRIVER CHECK: pillion boarded={pillion != null}" + (pillion != null ? $", pillion rider={pillion.gameObject.name} (expect Sacat), IsMounted={pillion.IsMounted}" : ""));
        }
    }
}
