using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-181, user: "make the bikes move more like a real bike with my
    /// physics springs etc." Retunes the SuperMoto's two WheelColliders from
    /// the vendor's stiff 30000/2500 at 10% target (barely moves) to
    /// supermoto-like values: longer travel, ~40% static sag and a damping
    /// ratio near 0.5, so the fork dives under braking, the rear squats on
    /// throttle, and bumps/landings visibly compress and rebound once
    /// instead of jolting the chassis (those jolts also fed the crash
    /// detector). Runs in Awake, before the WheelColliders simulate.
    /// wheelColliders[0] is the rear, [1] the front (this asset's convention).
    /// </summary>
    public class SuperMotoSuspensionTuning : MonoBehaviour
    {
        public float frontTravel = 0.22f, frontSpring = 24000f, frontDamper = 2300f;
        public float rearTravel = 0.20f, rearSpring = 28000f, rearDamper = 2700f;
        [Range(0f, 1f)] public float sagTarget = 0.4f;

        private void Awake()
        {
            var rb = GetComponent<RB_Controller>();
            if (rb == null || rb.wheelColliders == null || rb.wheelColliders.Length < 2) return;
            Apply(rb.wheelColliders[1], frontTravel, frontSpring, frontDamper);
            Apply(rb.wheelColliders[0], rearTravel, rearSpring, rearDamper);
        }

        private void Apply(WheelCollider w, float travel, float spring, float damper)
        {
            if (w == null) return;
            w.suspensionDistance = travel;
            var js = w.suspensionSpring;
            js.spring = spring;
            js.damper = damper;
            js.targetPosition = sagTarget;
            w.suspensionSpring = js;
        }
    }
}
