using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>MINI-119 follow-up, user: "when i mount the bike and i
    /// press E for wheelie the bike moves far and awkward." Real cause:
    /// Mini119EditModeMountSetup ran the full runtime spawn/wiring
    /// method (SpawnStockDemoBikeAndDisableOurCharacter) against the
    /// PREPLACED bike and then SAVED the result - baking
    /// SuperMotoWheelieKeyRemap/SuperMotoTrikeStabilizer/
    /// SuperMotoWheelieAssist/SuperMotoUprightAssist/etc PERMANENTLY
    /// onto the saved bike. None of those have duplicate-guards. Every
    /// real Play session since then, VehicleSpawnController finds this
    /// same preplaced bike and wires it up AGAIN on top of what's
    /// already baked in - two of each assist/input component, both
    /// applying their own torque/lean/wheelie forces independently.
    /// Exactly an "awkward, moves far" wheelie.
    ///
    /// Fix: delete the contaminated bike outright and re-place a fresh,
    /// bare instance via Mini119PlaceStockDemoBike - matching the
    /// intended architecture (bare preplaced prefab; the game's own
    /// runtime spawn method wires it up exactly once per real Play
    /// session, never baked into the save).</summary>
    public static class Mini119CleanupContaminatedBike
    {
        [MenuItem("Up Iz Up Mini/MINI-119/Cleanup Contaminated Bike (one-off)")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var bike = GameObject.Find("StockDemoSuperMoto");
            if (bike != null)
            {
                Object.DestroyImmediate(bike);
                Debug.Log("MINI-119 CLEANUP: destroyed the contaminated StockDemoSuperMoto (had baked-in runtime components from the earlier Edit-mode mount tool).");
            }
            else
            {
                Debug.LogWarning("MINI-119 CLEANUP: no StockDemoSuperMoto found to clean up - nothing to do here, but placing a fresh one anyway.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Mini119PlaceStockDemoBike.Place();
        }
    }
}
