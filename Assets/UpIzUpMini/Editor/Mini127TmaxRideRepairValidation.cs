using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-127 structural proof for the purchasable/test TMAX controller
    /// repair. Real driving feel still belongs to the built-player playtest.
    /// </summary>
    public static class Mini127TmaxRideRepairValidation
    {
        private const string PrefabPath = "Assets/UpIzUpMini/Vehicles/TMAX_560.prefab";

        [MenuItem("Up Iz Up Mini/MINI-127/Validate TMAX Ride Repair")]
        public static void Validate()
        {
            try
            {
                RunChecks();
                Debug.Log("MINI-127 VALIDATION PASS: Purchasable TMAX keeps its proven custom controller, mount interaction targets it, and the broken vendor stack is disabled by spawn preparation.");
            }
            catch (Exception ex)
            {
                Debug.LogError("MINI-127 VALIDATION FAIL: " + ex.Message);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        private static void RunChecks()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Require(prefab != null, "TMAX_560.prefab is missing.");
            Require(prefab.GetComponent<TmaxBikeControllerCustom>() != null,
                "TMAX prefab has no TmaxBikeControllerCustom.");
            Require(prefab.GetComponent<BikeInteractable>() != null,
                "TMAX prefab has no BikeInteractable.");

            var bikeField = typeof(BikeInteractable).GetField("_bike", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(bikeField != null && bikeField.FieldType == typeof(TmaxBikeControllerCustom),
                "BikeInteractable is not wired to TmaxBikeControllerCustom.");

            var prepare = typeof(VehicleSpawnController).GetMethod(
                "PrepareTmaxForCustomControl", BindingFlags.Static | BindingFlags.NonPublic);
            Require(prepare != null, "VehicleSpawnController has no TMAX preparation hook.");

            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                prepare.Invoke(null, new object[] { instance });

                Require(instance.GetComponent<TmaxBikeControllerCustom>().enabled,
                    "Custom TMAX controller was not enabled.");
                RequireDisabled<TmaxBikeController>(instance);
                RequireDisabled<TmaxTestInput>(instance);
                RequireDisabled<GaddInputAdapter>(instance);
                RequireDisabled<Gadd420.RB_Controller>(instance);
                RequireDisabled<Gadd420.NitrousManager>(instance);
                RequireDisabled<Gadd420.CrashController>(instance);
                RequireDisabled<Gadd420.GroundAngle>(instance);
                RequireDisabled<Gadd420.AutoLeveling>(instance);

                var rb = instance.GetComponent<Rigidbody>();
                Require(rb != null && Mathf.Approximately(rb.mass, 480f),
                    "MINI-127 must not retune the approved 480kg TMAX Rigidbody.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void RequireDisabled<T>(GameObject root) where T : Behaviour
        {
            foreach (var behaviour in root.GetComponentsInChildren<T>(true))
                Require(!behaviour.enabled, typeof(T).Name + " remained enabled.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
