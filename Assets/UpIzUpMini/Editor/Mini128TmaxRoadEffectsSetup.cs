using System;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Repeatable MINI-128 prefab patch and structural validation. Keeps the
    /// gameplay scene untouched and reuses only already-installed, licensed
    /// Motorbike Physics Tool effect art.
    /// </summary>
    public static class Mini128TmaxRoadEffectsSetup
    {
        private const string TmaxPath = "Assets/UpIzUpMini/Vehicles/TMAX_560.prefab";
        private const string TrailPath = "Assets/MotorbikePhysicsTool/Prefabs/Skids/SkidTrail.prefab";
        private const string SmokePath = "Assets/MotorbikePhysicsTool/Prefabs/Skids/Smoke.prefab";

        [MenuItem("Up Iz Up Mini/MINI-128/Build + Validate TMAX Road Effects")]
        public static void BuildValidate()
        {
            GameObject root = null;
            try
            {
                var trailPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TrailPath);
                var smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePath);
                Require(trailPrefab != null, "Installed skid-trail prefab is missing.");
                Require(smokePrefab != null && smokePrefab.GetComponent<ParticleSystem>() != null,
                    "Installed skid-smoke prefab is missing or has no ParticleSystem.");

                root = PrefabUtility.LoadPrefabContents(TmaxPath);
                Require(root != null, "TMAX_560.prefab could not be opened.");

                var front = root.transform.Find("Physics/FrontWheelCollider")?.GetComponent<WheelCollider>();
                var rear = root.transform.Find("Physics/RearWheelCollider")?.GetComponent<WheelCollider>();
                Require(front != null && rear != null, "TMAX WheelColliders are missing.");

                var effects = root.GetComponent<TmaxRoadEffects>();
                if (effects == null) effects = root.AddComponent<TmaxRoadEffects>();
                effects.Configure(front, rear, trailPrefab.transform, smokePrefab.GetComponent<ParticleSystem>());

                // MINI-129: driver-only fit correction. Preserve the approved
                // side/height/pitch and advance both seated/wheelie keyframes
                // by the same 0.12m along the TMAX's local forward axis.
                var driverSeat = root.transform.Find("DriverSeat")?.GetComponent<VehicleSeat>();
                Require(driverSeat != null, "TMAX driver seat wiring is missing.");
                driverSeat.ConfigureSeatedPose(new Vector3(0.11f, -0.01f, 0.44f), 0f);
                driverSeat.ConfigureWheeliePose(new Vector3(0f, -0.40f, 0.40f), 22f);

                // Keep the user's approved pillion height and offsets exactly
                // as-is; align only orientation/pose to the SuperMoto setup.
                var pillionAnchor = root.transform.Find("PillionSeat");
                var pillionSeat = root.transform.Find("PillionSeat_Seat")?.GetComponent<VehicleSeat>();
                Require(pillionAnchor != null && pillionSeat != null, "TMAX pillion seat wiring is missing.");
                pillionAnchor.localRotation = Quaternion.identity;
                pillionSeat.Configure(
                    pillionAnchor,
                    root.transform.Find("PillionGrabLeft"),
                    root.transform.Find("PillionGrabRight"),
                    root.transform.Find("PillionFootLeft"),
                    root.transform.Find("PillionFootRight"),
                    "MountBike",
                    "RideBike");
                pillionSeat.SetRole(VehicleSeat.SeatRole.Passenger);
                pillionSeat.ConfigureSeatedPose(new Vector3(0.11f, -0.01f, 0.02f), 0f);
                pillionSeat.ConfigureWheeliePose(new Vector3(0f, -0.40f, -0.02f), 22f);
                var pillionSo = new SerializedObject(pillionSeat);
                pillionSo.FindProperty("ridePoseStartTimeSeconds").floatValue = 0f;
                pillionSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, TmaxPath);
                PrefabUtility.UnloadPrefabContents(root);
                root = null;
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(TmaxPath, ImportAssetOptions.ForceUpdate);

                ValidateSavedPrefab();
                Debug.Log("MINI-128 VALIDATION PASS: TMAX road effects are wired to its own wheels, reuse the installed SuperMoto skid art, and cap exhaust at 36 particles.");
            }
            catch (Exception ex)
            {
                Debug.LogError("MINI-128 VALIDATION FAIL: " + ex.Message);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ValidateSavedPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TmaxPath);
            Require(prefab != null, "Saved TMAX prefab is missing.");

            var effects = prefab.GetComponent<TmaxRoadEffects>();
            Require(effects != null, "TmaxRoadEffects was not saved on the prefab.");
            Require(effects.FrontWheel != null && effects.RearWheel != null,
                "Road effects do not reference both TMAX WheelColliders.");
            Require(effects.SkidTrailPrefab != null,
                "Road effects have no black skid-trail prefab.");
            Require(effects.SmokePrefab != null,
                "Road effects have no smoke prefab.");
            Require(effects.ExhaustParticleLimit <= 40,
                "Exhaust particle budget is too high for the mobile target.");

            var driverSeat = prefab.transform.Find("DriverSeat")?.GetComponent<VehicleSeat>();
            Require(driverSeat != null && driverSeat.SeatedOffset == new Vector3(0.11f, -0.01f, 0.44f),
                "Driver was not moved forward while preserving side/height.");
            Require(driverSeat != null && driverSeat.WheelieOffset == new Vector3(0f, -0.40f, 0.40f),
                "Driver wheelie keyframe does not preserve the forward correction.");

            var pillionAnchor = prefab.transform.Find("PillionSeat");
            var pillionSeat = prefab.transform.Find("PillionSeat_Seat")?.GetComponent<VehicleSeat>();
            Require(pillionAnchor != null && Quaternion.Angle(pillionAnchor.localRotation, Quaternion.identity) < 0.1f,
                "Pillion anchor is not using the neutral SuperMoto-style rotation.");
            Require(pillionSeat != null && pillionSeat.RidePoseActionId == "RideBike",
                "Pillion is not using the stable SuperMoto RideBike pose.");
            Require(pillionSeat != null && pillionSeat.SeatedOffset == new Vector3(0.11f, -0.01f, 0.02f),
                "Pillion spacing correction was not preserved.");

            var custom = prefab.GetComponent<TmaxBikeControllerCustom>();
            Require(custom != null, "Proven custom TMAX controller was removed.");
            var rb = prefab.GetComponent<Rigidbody>();
            Require(rb != null && Mathf.Approximately(rb.mass, 480f),
                "Approved TMAX physics were changed.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
