using Gadd420;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-119, user: "backup our controller... get the best motocross
    /// code from online and we will adjust it with sliders... use this
    /// riding and then integrate our control mapping to it." Builds a
    /// NEW, separate prefab from the imported Motorbike Physics Tool's own
    /// SuperMoto ("the cross") + ragdoll rider, swapping its input source
    /// for our own decoupled one (GaddInputAdapter) and adding our facade
    /// (TmaxBikeController) so the rest of the game can eventually talk to
    /// it the same way it already talks to our original bike.
    ///
    /// Deliberately does NOT touch or replace TMAX_560.prefab - that stays
    /// exactly as it was (the backed-up, fully working original system,
    /// now under TmaxBikeControllerCustom). This is an additive, parallel
    /// prefab for evaluating the new physics without any risk to the old
    /// one.
    /// </summary>
    public static class Mini119SuperMotoBikeSetup
    {
        private const string SourcePrefabPath = "Assets/MotorbikePhysicsTool/Prefabs/BikesWithRagdolls/SuperMotoWRagdoll.prefab";
        private const string OutputPrefabPath = "Assets/UpIzUpMini/Vehicles/TMAX_560_SuperMoto.prefab";

        [MenuItem("Up Iz Up Mini/MINI-119/Build SuperMoto Bike Prefab (Motorbike Physics Tool)")]
        public static void Build()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (source == null)
            {
                Debug.LogError($"MINI-119 SUPERMOTO BUILD FAIL: source prefab not found at {SourcePrefabPath}.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            // Fully unpack - we're building an independent derived prefab,
            // not a variant, so later changes to the source asset (e.g. a
            // future asset update) don't silently reach into ours.
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var rbController = instance.GetComponent<RB_Controller>();
            if (rbController == null)
            {
                Debug.LogError("MINI-119 SUPERMOTO BUILD FAIL: source prefab root has no Gadd420.RB_Controller.");
                Object.DestroyImmediate(instance);
                return;
            }

            // Swap the stock, raw-Input.GetKey-reading Input_Manager for
            // our own decoupled subclass - see GaddInputAdapter's own
            // header for why. RB_Controller's [RequireComponent] is
            // satisfied by any Input_Manager subtype, so this is a clean
            // swap, not a workaround.
            var stockInput = instance.GetComponent<Input_Manager>();
            if (stockInput != null) Object.DestroyImmediate(stockInput);
            if (instance.GetComponent<GaddInputAdapter>() == null)
                instance.AddComponent<GaddInputAdapter>();

            // Our facade - the one thing every other system in this game
            // (BikeInteractable, BikeRiderAnimation, SaveLoadSystem, the
            // camera) already knows how to talk to.
            if (instance.GetComponent<TmaxBikeController>() == null)
                instance.AddComponent<TmaxBikeController>();

            // Standalone mount/possess/dismount - see SuperMotoInteractable's
            // own header for why this is separate from BikeInteractable.
            if (instance.GetComponent<SuperMotoInteractable>() == null)
                instance.AddComponent<SuperMotoInteractable>();

            var rb = instance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }

            instance.name = "TMAX_560_SuperMoto";

            System.IO.Directory.CreateDirectory("Assets/UpIzUpMini/Vehicles");
            PrefabUtility.SaveAsPrefabAsset(instance, OutputPrefabPath, out bool success);
            Object.DestroyImmediate(instance);

            if (success)
                Debug.Log($"MINI-119 SUPERMOTO BUILD OK: saved to {OutputPrefabPath}. Original TMAX_560.prefab (TmaxBikeControllerCustom) left completely untouched.");
            else
                Debug.LogError("MINI-119 SUPERMOTO BUILD FAIL: SaveAsPrefabAsset reported failure.");
        }
    }
}
