#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    public static class Mini139TmaxWheelieSpeedValidation
    {
        private const string PrefabPath = "Assets/UpIzUpMini/Vehicles/TMAX_560.prefab";

        [MenuItem("Up Iz Up Mini/Validation/MINI-139 TMAX 12 MPH Wheelie")]
        public static void Validate()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Require(prefab != null, "TMAX prefab is missing");
            TmaxBikeControllerCustom bike = prefab.GetComponent<TmaxBikeControllerCustom>();
            Require(bike != null, "TMAX custom controller is missing");
            Require(Mathf.Approximately(TmaxBikeControllerCustom.MinimumWheelieSpeedMph, 12f),
                "wheelie floor is not 12 mph");
            Require(Mathf.Approximately(
                    TmaxBikeControllerCustom.MinimumWheelieSpeedKmh, 19.312128f),
                "12 mph was converted to km/h incorrectly");
            Require(!bike.MeetsWheelieMinimumSpeed(
                    TmaxBikeControllerCustom.MinimumWheelieSpeedKmh - 0.01f),
                "TMAX can start a wheelie below 12 mph");
            Require(bike.MeetsWheelieMinimumSpeed(
                    TmaxBikeControllerCustom.MinimumWheelieSpeedKmh),
                "TMAX cannot wheelie at exactly 12 mph");
            Require(!bike.CanSustainWheelieAtSpeed(
                    TmaxBikeControllerCustom.MinimumWheelieSpeedKmh - 0.01f),
                "raised TMAX can sustain below 12 mph");
            Require(bike.CanSustainWheelieAtSpeed(
                    TmaxBikeControllerCustom.MinimumWheelieSpeedKmh),
                "raised TMAX cannot sustain at exactly 12 mph");
            Require(Mathf.Approximately(
                    TmaxBikeControllerCustom.ResolveWheelieTarget(true, false, 89f), 0f),
                "ineligible wheelie does not select the existing down target");
            Require(Mathf.Approximately(
                    TmaxBikeControllerCustom.ResolveWheelieTarget(true, true, 120f), 89f),
                "eligible wheelie bypasses the shared 89-degree cap");

            Debug.Log("[MINI-139] PASS: TMAX starts/sustains at 12 mph, lowers below it, and retains the 89-degree cap.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[MINI-139] FAIL: " + message);
        }
    }
}
#endif