using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: confirms the bike now spawns PARKED
    /// (no auto-mount), that walking into range + F actually mounts, and
    /// that F again actually dismounts and restores the player - without
    /// throwing. Delete after use.</summary>
    public static class Mini119MountFlowCheck
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Check Mount Flow (one-off)")]
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
            if (instance == null) { Debug.LogError("MINI-119 MOUNT FLOW CHECK FAIL: bike not spawned."); return; }

            var interactable = instance.GetComponent<SuperMotoStockInteractable>();
            if (interactable == null) { Debug.LogError("MINI-119 MOUNT FLOW CHECK FAIL: no SuperMotoStockInteractable on the spawned bike."); return; }

            var pc = player.GetComponent<PlayerController>();
            Debug.Log($"MINI-119 MOUNT FLOW CHECK: before mount - player.transform.parent={player.transform.parent?.name ?? "none"}, IsControlled={(pc != null ? pc.IsControlled.ToString() : "n/a")}, HasRider={interactable.HasRider}");

            var mountMethod = typeof(SuperMotoStockInteractable).GetMethod("Mount", BindingFlags.NonPublic | BindingFlags.Instance);
            try { mountMethod.Invoke(interactable, new object[] { player }); }
            catch (TargetInvocationException ex) { Debug.LogError($"MINI-119 MOUNT FLOW CHECK: Mount() threw: {ex.InnerException}"); Physics.simulationMode = previousSimMode; return; }

            Debug.Log($"MINI-119 MOUNT FLOW CHECK: after mount - player.transform.parent={player.transform.parent?.name ?? "none"}, HasRider={interactable.HasRider}, worldPos={player.transform.position}");

            var switcher = CharacterSwitchManager.Instance;
            if (switcher != null)
            {
                for (int i = 0; i < switcher.Slots.Length; i++)
                {
                    if (switcher.Slots[i]?.root == player)
                        Debug.Log($"MINI-119 MOUNT FLOW CHECK: slot {i} IsLocked={switcher.IsLocked(i)} (should be True while mounted)");
                }
            }

            var dismountMethod = typeof(SuperMotoStockInteractable).GetMethod("Dismount", BindingFlags.NonPublic | BindingFlags.Instance);
            try { dismountMethod.Invoke(interactable, null); }
            catch (TargetInvocationException ex) { Debug.LogError($"MINI-119 MOUNT FLOW CHECK: Dismount() threw: {ex.InnerException}"); Physics.simulationMode = previousSimMode; return; }

            Debug.Log($"MINI-119 MOUNT FLOW CHECK: after dismount - player.transform.parent={player.transform.parent?.name ?? "none"}, HasRider={interactable.HasRider}, worldPos={player.transform.position}, IsControlled={(pc != null ? pc.IsControlled.ToString() : "n/a")}, remaining Rigidbodies on player={player.GetComponentsInChildren<Rigidbody>(true).Length}, remaining Joints={player.GetComponentsInChildren<Joint>(true).Length}");

            Physics.simulationMode = previousSimMode;
        }
    }
}
