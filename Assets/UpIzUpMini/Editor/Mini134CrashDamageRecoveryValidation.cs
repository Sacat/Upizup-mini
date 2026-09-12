#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Combat;
using UpIzUpMini.Interaction;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    public static class Mini134CrashDamageRecoveryValidation
    {
        [MenuItem("Up Iz Up Mini/Validation/MINI-134 Crash Damage Recovery")]
        public static void Validate()
        {
            GameObject vehicle = new GameObject("MINI-134 Vehicle Probe");
            try
            {
                vehicle.AddComponent<Rigidbody>();
                var impact = vehicle.AddComponent<VehicleImpactResponder>();
                var damage = vehicle.AddComponent<VehicleDamageController>();
                var crash = vehicle.AddComponent<BikeCrashEjectionController>();

                Require(impact.DamageForSpeed(3f) <= 0.001f, "low-speed NPC contact is harmless");
                Require(impact.DamageForSpeed(20f) <= 82.01f, "NPC impact damage is capped");
                Require(impact.ImpactVelocityFor(Vector3.right * 30f, 20f).magnitude < 13f,
                    "NPC launch force is bounded");
                Require(damage.DamageForSpeed(5f) <= 0.001f, "low-speed vehicle scrape is harmless");
                Require(damage.DamageForSpeed(20f) <= 62.01f, "vehicle collision damage is capped");
                Require(crash.HardImpactSpeed >= 40f, "hard-crash threshold is too sensitive"); // MINI-167: was >=16f at the original 16.5 baseline, then >=24f after the first +50%
                Require(BikeCrashEjectionController.MaximumWheelieDegrees == 89f,
                    "shared wheelie ceiling is not 89 degrees");
                Require(Mathf.Approximately(BikeCrashEjectionController.ClampWheelieDegrees(140f), 89f),
                    "configured wheelie target can exceed 89 degrees");
                Require(BikeCrashEjectionController.ClampWheelieDegrees(-10f) == 0f,
                    "wheelie target can become negative");
                Require(!crash.ShouldEjectForTilt(89f, 8f), "89-degree wheelie ejects");
                Require(!crash.ShouldEjectForTilt(95f, 8f), "past-vertical wheelie ejects");
                Require(!crash.ShouldEjectForTilt(180f, 30f), "extreme tilt ejects without a collision");
                // MINI-167: hardImpactSpeed raised 16.5 -> 24.75 (+50%), then
                // still too easy -> 43.31 (+75% further, "bikes crash too
                // easily") - these fixed test speeds are scaled up again to
                // match, same discipline as every prior crash-tuning pass.
                // NOTE: 43.31 m/s (156 km/h) exceeds the TMAX's own 150 km/h
                // (41.67 m/s) top speed - a dead-on wall hit at full throttle
                // no longer reaches this threshold at all. Flagged to the
                // user, not silently shipped - see Vehicles.md.
                Require(crash.ShouldEjectForImpact(
                        Vector3.forward * 50f, Vector3.back, 50f, false, false),
                    "normal high-speed wall collision no longer ejects");
                Require(!crash.ShouldEjectForImpact(
                        Vector3.forward * 30f, Vector3.back, 5.36f, false, true),
                    "12 mph wheelie tail contact ejects from rotational velocity");
                Require(crash.ImpactThreshold(false, true) > crash.ImpactThreshold(false, false),
                    "wheelie collision threshold is not higher than normal riding");
                Require(crash.ShouldEjectForImpact(
                        Vector3.forward * 55f, Vector3.back, 44f, false, true),
                    "genuine high-speed wheelie wall collision cannot eject");
                Require(crash.ShouldIgnoreGroundContact(Vector3.up), "ground bumps can eject riders");
                Require(!crash.ShouldIgnoreGroundContact(Vector3.forward), "vertical wall is ignored");
                Require(crash.ImpactSpeedFor(Vector3.forward * 20f, Vector3.right) < 0.01f,
                    "sideways scrape is treated as a head-on crash");

                ValidateStationarySellerImpact(impact);

                RequirePublicCrashBridge(typeof(BikeInteractable));
                RequirePublicCrashBridge(typeof(SuperMotoVehicleInteractable));
                Require(typeof(NpcRagdoll).GetMethod("IsSettled", BindingFlags.Instance | BindingFlags.Public) != null,
                    "ragdoll exposes settle-aware recovery");

                Debug.Log("[MINI-138] PASS: 89-degree cap, wheelie real-speed crash filtering, collision-only ejection, and seller impact validated.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(vehicle);
            }
        }

        static void ValidateStationarySellerImpact(VehicleImpactResponder impact)
        {
            GameObject seller = new GameObject("MINI-135 Stationary Seller");
            try
            {
                TownNPCInteractable townNpc = seller.AddComponent<TownNPCInteractable>();
                SerializedObject serialized = new SerializedObject(townNpc);
                serialized.FindProperty("role").enumValueIndex = (int)NpcRole.FarmShop;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Collider first = TownNPCInteractable.EnsurePhysicalHitCollider(seller);
                Collider second = TownNPCInteractable.EnsurePhysicalHitCollider(seller);
                Require(first != null && !first.isTrigger, "stationary seller has no solid hit collider");
                Require(ReferenceEquals(first, second), "seller hit collider duplicates itself");
                Require(impact.TryApplyImpact(first, 8f, Vector3.forward * 8f,
                    seller.transform.position + Vector3.up), "seller impact did not resolve");

                NpcCombatHealth health = seller.GetComponent<NpcCombatHealth>();
                Require(health != null && health.DefeatPolicy == NpcDefeatPolicy.RecoverAndReturnToPost,
                    "seller did not receive protected combat health");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(seller);
            }
        }

        static void RequirePublicCrashBridge(Type type)
        {
            MethodInfo method = type.GetMethod("CrashEject", BindingFlags.Instance | BindingFlags.Public);
            Require(method != null && method.ReturnType == typeof(bool), type.Name + " exposes CrashEject");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[MINI-135] FAIL: " + message);
        }
    }
}
#endif
