using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119 follow-up, user: "start from what works which is the
    /// demo... use the controls of the demo as well." Wires the pack's
    /// own completely unmodified SuperMotoWRagdoll.prefab onto
    /// VehicleSpawnController.stockDemoBikePrefab in the real
    /// GrandBayProof scene, via SerializedObject rather than re-running
    /// the full Mini011PhaseBSetup.BuildScene pipeline - a one-field
    /// change doesn't need every farm/road/NPC system rebuilt again.
    /// </summary>
    public static class Mini119WireStockDemoBike
    {
        private const string SourcePrefabPath = "Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab";

        [MenuItem("Up Iz Up Mini/MINI-119/Wire Stock Demo Bike Test")]
        public static void Wire()
        {
            EditorSceneManager.OpenScene("Assets/UpIzUpMini/Scenes/GrandBayProof.unity", OpenSceneMode.Single);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"MINI-119 WIRE STOCK DEMO BIKE FAIL: {SourcePrefabPath} not found.");
                return;
            }

            var spawner = Object.FindFirstObjectByType<VehicleSpawnController>();
            if (spawner == null)
            {
                Debug.LogError("MINI-119 WIRE STOCK DEMO BIKE FAIL: no VehicleSpawnController in the open scene.");
                return;
            }

            var so = new SerializedObject(spawner);
            var prop = so.FindProperty("stockDemoBikePrefab");
            if (prop == null)
            {
                Debug.LogError("MINI-119 WIRE STOCK DEMO BIKE FAIL: stockDemoBikePrefab field not found - VehicleSpawnController.cs out of date?");
                return;
            }
            prop.objectReferenceValue = prefab;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log($"MINI-119 WIRE STOCK DEMO BIKE OK: {SourcePrefabPath} wired to VehicleSpawnController.stockDemoBikePrefab and scene saved.");
        }
    }
}
