#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UpIzUpMini.Vehicles;

namespace UpIzUpMini.EditorTools
{
    public static class Mini137TmaxWheeliePolishValidation
    {
        [MenuItem("Up Iz Up Mini/Validation/MINI-137 TMAX Wheelie Polish")]
        public static void Validate()
        {
            Require(TmaxRoadEffects.ExhaustMaximumAlpha <= 0.22f,
                "TMAX exhaust is not sufficiently transparent");
            Require(TmaxRoadEffects.WheelieSparkParticleLimit <= 20,
                "wheelie spark budget exceeds the mobile cap");
            Require(Mathf.Approximately(TmaxRoadEffects.WheelieSparkMinimumSpeedMph, 12f),
                "wheelie spark speed threshold is not 12 mph");
            Require(!TmaxRoadEffects.ShouldEmitWheelieSparks(false, 89f, 30f),
                "unridden TMAX can emit wheelie sparks");
            Require(!TmaxRoadEffects.ShouldEmitWheelieSparks(true, 88.5f, 30f),
                "sparks emit before the 89-degree cap");
            Require(!TmaxRoadEffects.ShouldEmitWheelieSparks(true, 89f,
                    TmaxRoadEffects.WheelieSparkMinimumSpeedKmh - 0.01f),
                "sparks emit below 12 mph");
            Require(TmaxRoadEffects.ShouldEmitWheelieSparks(true, 89f,
                    TmaxRoadEffects.WheelieSparkMinimumSpeedKmh),
                "sparks do not emit at 89 degrees and 12 mph");
            Require(Mathf.Approximately(BikeInteractable.SuperMotoWheelieClipWeight, 0.22f),
                "TMAX is not using the proven original SuperMoto wheelie lift blend");

            Debug.Log("[MINI-137] PASS: lighter TMAX exhaust, 89-degree/12-mph bounded sparks, and original SuperMoto rider lift blend validated.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("[MINI-137] FAIL: " + message);
        }
    }
}
#endif