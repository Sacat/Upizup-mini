using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Character;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up: user reported "i didnt see the bike
    /// spawn" - diagnoses the REAL scene's actual state rather than
    /// guessing (is the preplaced bike still there and active, is the
    /// test-mode gate still on, does the spawn method still run clean
    /// against the real, current scene). Delete after use.</summary>
    public static class Mini119SpawnDiagnose
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Diagnose Bike Spawn (one-off)")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var preplaced = GameObject.Find("StockDemoSuperMoto");
            Debug.Log($"MINI-119 SPAWN DIAGNOSE: preplaced bike found={preplaced != null}" +
                (preplaced != null ? $", activeInHierarchy={preplaced.activeInHierarchy}, activeSelf={preplaced.activeSelf}, pos={preplaced.transform.position}, parent={preplaced.transform.parent?.name ?? "none"}" : ""));

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            Debug.Log($"MINI-119 SPAWN DIAGNOSE: VehicleSpawnController found={spawner != null}, GameObject active={(spawner != null ? spawner.gameObject.activeInHierarchy.ToString() : "n/a")}, component enabled={(spawner != null ? spawner.enabled.ToString() : "n/a")}");

            var testModeField = typeof(VehicleSpawnController).GetField("StockDemoBikeTestMode", BindingFlags.NonPublic | BindingFlags.Static);
            if (testModeField != null)
                Debug.Log($"MINI-119 SPAWN DIAGNOSE: StockDemoBikeTestMode={testModeField.GetRawConstantValue()}");
            else
                Debug.LogError("MINI-119 SPAWN DIAGNOSE: couldn't find StockDemoBikeTestMode field via reflection.");

            var player = GameObject.Find("Sacat");
            Debug.Log($"MINI-119 SPAWN DIAGNOSE: Sacat found={player != null}, active={(player != null ? player.activeInHierarchy.ToString() : "n/a")}");

            var switcher = CharacterSwitchManager.Instance;
            Debug.Log($"MINI-119 SPAWN DIAGNOSE: CharacterSwitchManager.Instance={(switcher != null)} (static Instance is set in Awake - null here is expected/normal outside Play, not itself a bug)");

            if (spawner != null && player != null)
            {
                var method = typeof(VehicleSpawnController).GetMethod("SpawnStockDemoBikeAndDisableOurCharacter", BindingFlags.NonPublic | BindingFlags.Instance);
                try
                {
                    method.Invoke(spawner, new object[] { player });
                    var afterCall = GameObject.Find("StockDemoSuperMoto");
                    var interactable = afterCall?.GetComponent<SuperMotoStockInteractable>();
                    Debug.Log($"MINI-119 SPAWN DIAGNOSE: direct spawn-method call OK, bike present after={afterCall != null}, interactable attached={interactable != null}");

                    var rbController = afterCall?.GetComponent<Gadd420.RB_Controller>();
                    if (rbController != null)
                    {
                        var inputsField = typeof(Gadd420.RB_Controller).GetField("inputs", BindingFlags.NonPublic | BindingFlags.Instance);
                        var cached = inputsField?.GetValue(rbController) as UnityEngine.Object;
                        Debug.Log($"MINI-119 SPAWN DIAGNOSE: RB_Controller.inputs after spawn = {(cached != null ? cached.GetType().Name : "NULL")} (should be SuperMotoWheelieKeyRemap, NOT null/destroyed)");
                    }
                }
                catch (TargetInvocationException ex)
                {
                    Debug.LogError($"MINI-119 SPAWN DIAGNOSE: spawn method THREW: {ex.InnerException}");
                }
            }
        }
    }
}
