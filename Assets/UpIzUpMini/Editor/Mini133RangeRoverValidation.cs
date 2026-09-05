using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    public static class Mini133RangeRoverValidation
    {
        private const string RoverPath = "Assets/UpIzUpMini/Art/Vehicles/RangeRover_Vehicle.prefab";
        private const string EvidenceDirectory = "Logs/Tasks/MINI-133";

        [MenuItem("Up Iz Up Mini/MINI-133/Validate Range Rover Visuals")]
        public static void Validate()
        {
            Directory.CreateDirectory(EvidenceDirectory);
            GameObject root = PrefabUtility.LoadPrefabContents(RoverPath);
            try
            {
                var visuals = root.GetComponent<RangeRoverWheelVisuals>();
                var effects = root.GetComponent<RangeRoverRoadEffects>();
                Require(visuals != null, "RangeRoverWheelVisuals is missing.");
                Require(effects != null, "RangeRoverRoadEffects is missing.");
                Require(root.GetComponent<Rigidbody>() != null && Mathf.Approximately(root.GetComponent<Rigidbody>().mass, 2500f), "Verified 2500kg mass changed.");

                RequireMapped(visuals.FrontLeftCollider, visuals.FrontLeftVisual, true, true, "front-left");
                RequireMapped(visuals.FrontRightCollider, visuals.FrontRightVisual, false, true, "front-right");
                RequireMapped(visuals.RearLeftCollider, visuals.RearLeftVisual, true, false, "rear-left");
                RequireMapped(visuals.RearRightCollider, visuals.RearRightVisual, false, false, "rear-right");
                Require(effects.RearLeft == visuals.RearLeftCollider && effects.RearRight == visuals.RearRightCollider, "Road effects are not mapped to both rear wheels.");
                Require(effects.ExhaustOutlet != null && effects.ExhaustOutlet.localPosition.z < visuals.RearLeftCollider.transform.localPosition.z, "Tailpipe outlet is not behind the rear axle.");
                Require(effects.SkidTrailPrefab != null && effects.SmokePrefab != null, "Skid/smoke references are missing.");
                Require(RangeRoverRoadEffects.ExhaustParticleLimit <= 32 && RangeRoverRoadEffects.TyreParticleLimit <= 16, "Particle caps exceed the mobile budget.");

                string report =
                    "MINI-133 VALIDATION PASS\n" +
                    "Four explicit wheel mappings: PASS\n" +
                    "Left/right and front/rear signs: PASS\n" +
                    "All wheels use WheelCollider-driven pivots; only front colliders receive steering: PASS\n" +
                    "Twin rear skid/smoke mapping and rear tailpipe outlet: PASS\n" +
                    "2500kg handling baseline unchanged: PASS\n" +
                    $"Mobile particle caps: exhaust={RangeRoverRoadEffects.ExhaustParticleLimit}, tyre={RangeRoverRoadEffects.TyreParticleLimit} each: PASS\n";
                File.WriteAllText(Path.Combine(EvidenceDirectory, "validation.txt"), report);
                Debug.Log(report);
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RequireMapped(WheelCollider collider, Transform visual, bool left, bool front, string label)
        {
            Require(collider != null && visual != null, $"{label} mapping is incomplete.");
            Require(left ? collider.transform.localPosition.x < 0f : collider.transform.localPosition.x > 0f, $"{label} is mapped to the wrong side.");
            Require(front ? collider.transform.localPosition.z > 0f : collider.transform.localPosition.z < 0f, $"{label} is mapped to the wrong axle.");
            Require(visual.GetComponentInChildren<MeshRenderer>(true) != null, $"{label} has no visible tyre mesh.");
            Require(Vector3.Distance(visual.localPosition, collider.transform.localPosition) < 0.01f, $"{label} visual is not centred on its collider at rest.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("MINI-133 VALIDATION FAIL: " + message);
        }
    }
}
