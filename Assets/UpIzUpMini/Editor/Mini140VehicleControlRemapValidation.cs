#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// MINI-140. Confirms the "E to mount/enter, Q to wheelie" control remap is
    /// actually wired: the serialized prefab key bindings, the runtime-added
    /// SuperMoto interactable's code defaults, the SuperMoto wheelie remap's
    /// key, and the phone's while-mounted suppression guard. Batch-mode only -
    /// real control feel still needs the user's ride test.
    /// </summary>
    public static class Mini140VehicleControlRemapValidation
    {
        private const string TmaxPrefab = "Assets/UpIzUpMini/Vehicles/TMAX_560.prefab";
        private const string RoverPrefab = "Assets/UpIzUpMini/Art/Vehicles/RangeRover_Vehicle.prefab";
        private const string WheelieRemapSrc = "Assets/UpIzUpMini/Scripts/Vehicles/SuperMotoWheelieKeyRemap.cs";
        private const string PhoneSrc = "Assets/UpIzUpMini/Scripts/Character/CellPhoneController.cs";

        [MenuItem("Up Iz Up Mini/Validation/MINI-140 Vehicle Control Remap")]
        public static void Validate()
        {
            // 1. TMAX prefab: mount + dismount on E, wheelie on Q.
            GameObject tmax = AssetDatabase.LoadAssetAtPath<GameObject>(TmaxPrefab);
            Require(tmax != null, "TMAX prefab is missing");
            var bike = tmax.GetComponent<BikeInteractable>();
            Require(bike != null, "TMAX BikeInteractable is missing");
            Require(Key(bike, "mountKey") == KeyCode.E, "TMAX mountKey is not E");
            Require(Key(bike, "dismountKey") == KeyCode.E, "TMAX dismountKey is not E");
            Require(Key(bike, "wheelieKey") == KeyCode.Q, "TMAX wheelieKey is not Q");

            // 2. Range Rover prefab: enter + exit on E.
            GameObject rover = AssetDatabase.LoadAssetAtPath<GameObject>(RoverPrefab);
            Require(rover != null, "Range Rover prefab is missing");
            var car = rover.GetComponent<CarInteractable>();
            Require(car != null, "Range Rover CarInteractable is missing");
            Require(Key(car, "enterKey") == KeyCode.E, "Rover enterKey is not E");
            Require(Key(car, "exitKey") == KeyCode.E, "Rover exitKey is not E");

            // 3. SuperMoto interactable is AddComponent'd at runtime, so its
            //    code defaults are what ship - check a fresh instance.
            var probe = new GameObject("MINI140_Probe");
            try
            {
                var sm = probe.AddComponent<SuperMotoVehicleInteractable>();
                Require(Key(sm, "mountKey") == KeyCode.E, "SuperMoto mountKey default is not E");
                Require(Key(sm, "dismountKey") == KeyCode.E, "SuperMoto dismountKey default is not E");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }

            // 4. SuperMoto wheelie remap: Q is the trigger, E is gone.
            string remap = ReadSource(WheelieRemapSrc);
            int gm = remap.IndexOf("override void GetLeanBackValue()", StringComparison.Ordinal);
            Require(gm >= 0, "SuperMotoWheelieKeyRemap.GetLeanBackValue override not found");
            string body = remap.Substring(gm, Math.Min(400, remap.Length - gm));
            Require(body.Contains("KeyCode.Q"), "SuperMoto wheelie remap no longer reads Q");
            Require(!body.Contains("KeyCode.E"), "SuperMoto wheelie remap still reads E");

            // 5. Phone Q is suppressed while the active character is mounted.
            string phone = ReadSource(PhoneSrc);
            Require(phone.Contains("IsControlled"),
                "CellPhoneController has no while-mounted (IsControlled) guard on the Q call");

            Debug.Log("[MINI-140] PASS: E mounts/enters and dismounts every vehicle, Q is the sole wheelie key, "
                      + "and the phone's Q call is suppressed while riding.");
        }

        private static KeyCode Key(object component, string field)
        {
            FieldInfo fi = component.GetType().GetField(
                field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Require(fi != null, "field '" + field + "' not found on " + component.GetType().Name);
            return (KeyCode)fi.GetValue(component);
        }

        private static string ReadSource(string assetPath)
        {
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            Require(File.Exists(full), "source file missing: " + assetPath);
            return File.ReadAllText(full);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[MINI-140] FAIL: " + message);
        }
    }
}
#endif
