using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: verifies SuperMotoVehicleInteractable
    /// dials BikeRiderAnimation.WheelieClipWeight down to 0.22 on mount
    /// and restores the original (0.45) on dismount, since that
    /// component lives on the shared player object (also used by the
    /// TMAX). Delete after use.</summary>
    public static class Mini119WheelieDampCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Wheelie Damp Mount-Dismount (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var bike = GameObject.Find("StockDemoSuperMoto");
            if (bike == null) { Debug.LogError("MINI-119 WHEELIE DAMP CHECK: no StockDemoSuperMoto."); return; }

            var switcher = CharacterSwitchManager.Instance ?? Object.FindFirstObjectByType<CharacterSwitchManager>();
            if (switcher != null && CharacterSwitchManager.Instance == null)
            {
                var awakeMethod = typeof(CharacterSwitchManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                awakeMethod?.Invoke(switcher, null);
            }
            var player = switcher?.Active?.root ?? switcher?.Slots[0]?.root;
            if (player == null) { Debug.LogError("MINI-119 WHEELIE DAMP CHECK: no player found."); return; }

            var interactable = bike.GetComponent<SuperMotoVehicleInteractable>();
            if (interactable == null)
            {
                var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
                var spawnMethod = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
                spawnMethod.Invoke(spawner, new object[] { player });
                interactable = bike.GetComponent<SuperMotoVehicleInteractable>();
            }
            if (interactable == null) { Debug.LogError("MINI-119 WHEELIE DAMP CHECK: still no interactable."); return; }

            var riderAnim = player.GetComponent<BikeRiderAnimation>();
            float before = riderAnim != null ? riderAnim.WheelieClipWeight : -1f;
            Debug.Log($"MINI-119 WHEELIE DAMP CHECK: BikeRiderAnimation.WheelieClipWeight BEFORE mount = {before} (expect the component's own default, 0.45, or none yet if never mounted before).");

            var mountMethod = typeof(SuperMotoVehicleInteractable).GetMethod("Mount", BindingFlags.NonPublic | BindingFlags.Instance);
            mountMethod.Invoke(interactable, new object[] { player });

            riderAnim = player.GetComponent<BikeRiderAnimation>();
            float duringMount = riderAnim != null ? riderAnim.WheelieClipWeight : -1f;
            Debug.Log($"MINI-119 WHEELIE DAMP CHECK: WheelieClipWeight WHILE MOUNTED = {duringMount} (expect 0.22).");

            var dismountMethod = typeof(SuperMotoVehicleInteractable).GetMethod("Dismount", BindingFlags.NonPublic | BindingFlags.Instance);
            dismountMethod.Invoke(interactable, null);

            float afterDismount = riderAnim != null ? riderAnim.WheelieClipWeight : -1f;
            Debug.Log($"MINI-119 WHEELIE DAMP CHECK: WheelieClipWeight AFTER DISMOUNT = {afterDismount} (expect restored to {before}).");

            Debug.Log("MINI-119 WHEELIE DAMP CHECK: done.");
        }
    }
}
