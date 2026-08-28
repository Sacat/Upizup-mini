using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: verifies the OTHER main character
    /// auto-boards the SuperMoto's pillion seat when the driver mounts,
    /// same as BikeInteractable already proves for the TMAX. Delete
    /// after use.</summary>
    public static class Mini119PillionCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Pillion Boarding (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var switcher = CharacterSwitchManager.Instance ?? Object.FindFirstObjectByType<CharacterSwitchManager>();
            if (switcher == null) { Debug.LogError("MINI-119 PILLION CHECK: no CharacterSwitchManager in the scene."); return; }

            // CharacterSwitchManager.Instance is only ever set inside its
            // own Awake() - this Edit-mode reflection-driven test never
            // runs a real Unity frame loop that would trigger that
            // automatically (same category of limitation already
            // established repeatedly this task for Animator/Avatar
            // binding). Force it so BoardPillion's own
            // CharacterSwitchManager.Instance check (the real game code,
            // no fallback) has something to find - otherwise this test
            // cannot tell "the feature is broken" apart from "Instance
            // was never set in this specific test harness".
            if (CharacterSwitchManager.Instance == null)
            {
                var awakeMethod = typeof(CharacterSwitchManager).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
                awakeMethod?.Invoke(switcher, null);
                Debug.Log($"MINI-119 PILLION CHECK: CharacterSwitchManager.Instance was null - forced Awake(), now set={CharacterSwitchManager.Instance != null}");
            }

            Debug.Log($"MINI-119 PILLION CHECK: {switcher.Slots?.Length ?? 0} slot(s). Active index={switcher.ActiveIndex}.");
            for (int i = 0; i < (switcher.Slots?.Length ?? 0); i++)
                Debug.Log($"MINI-119 PILLION CHECK: slot {i} root={switcher.Slots[i]?.root?.name ?? "null"}, locked={switcher.IsLocked(i)}");

            var sacat = GameObject.Find("Sacat");
            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            if (sacat == null || spawner == null) { Debug.LogError("MINI-119 PILLION CHECK: Sacat or VehicleSpawnController missing."); return; }

            var spawnMethod = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
            spawnMethod.Invoke(spawner, new object[] { sacat });

            // Move the OTHER slot's character right next to the bike
            // first, so the range check inside BoardPillion passes -
            // otherwise this test can't tell "not nearby" apart from
            // "the logic doesn't work at all".
            var bike = GameObject.Find("StockDemoSuperMoto");
            int otherIndex = 1 - switcher.ActiveIndex;
            Debug.Log($"MINI-119 PILLION CHECK: bike found={bike != null}, otherIndex={otherIndex}, slots ok={(switcher.Slots != null && otherIndex >= 0 && otherIndex < switcher.Slots.Length)}");
            if (bike != null && switcher.Slots != null && otherIndex >= 0 && otherIndex < switcher.Slots.Length)
            {
                var otherRoot = switcher.Slots[otherIndex]?.root;
                Debug.Log($"MINI-119 PILLION CHECK: otherRoot={(otherRoot != null ? otherRoot.name : "null")}, bikePos={bike.transform.position}");
                if (otherRoot != null)
                {
                    otherRoot.transform.position = bike.transform.position + Vector3.right * 1.5f;
                    Debug.Log($"MINI-119 PILLION CHECK: set {otherRoot.name} position to {otherRoot.transform.position}");
                }
            }

            var interactable = bike?.GetComponent<SuperMotoVehicleInteractable>();
            if (interactable == null) { Debug.LogError("MINI-119 PILLION CHECK: no SuperMotoVehicleInteractable after spawn."); return; }

            // AutoMountSuperMotoOnSpawn already fired Mount() (and a
            // first, too-far BoardPillion attempt) DURING spawnMethod.
            // Invoke above, before this test ever got a chance to
            // reposition Franki - matching real gameplay exactly (the
            // driver auto-mounts instantly, with no walk-up window for
            // the passenger to already be close). BoardPillion is now a
            // per-frame retry (this task's own fix for that), so
            // simulate the NEXT frame - after Franki has actually
            // arrived - by invoking Update() directly.
            var updateMethod = typeof(SuperMotoVehicleInteractable).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            updateMethod.Invoke(interactable, null);

            var pillionField = typeof(SuperMotoVehicleInteractable).GetField("_pillion", BindingFlags.NonPublic | BindingFlags.Instance);
            var pillion = pillionField.GetValue(interactable) as VehicleRider;
            Debug.Log($"MINI-119 PILLION CHECK: after simulating the next frame, pillion boarded={pillion != null}" + (pillion != null ? $", pillion rider={pillion.gameObject.name}, IsMounted={pillion.IsMounted}" : ""));
        }
    }
}
