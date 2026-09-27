using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "can you put the lean back animation
    /// with the wheelie to make it more real from the ragdoll." Drives
    /// VehicleRider's own keyframed wheelie lean (already built and
    /// proven for the TMAX - wheelieOffset/wheeliePitch, blended in via
    /// SetWheelieBlend) from the bike's REAL live wheelie angle, so
    /// Sacat leans back progressively as the front comes up, exactly in
    /// sync with the actual physical wheelie rather than a fixed timer.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class SuperMotoRiderWheelieLink : MonoBehaviour
    {
        private SuperMotoWheelieAssist _assist;
        private VehicleRider _rider;

        public void Configure(SuperMotoWheelieAssist assist, VehicleRider rider)
        {
            _assist = assist;
            _rider = rider;
        }

        private void FixedUpdate()
        {
            if (_assist == null || _rider == null || !_rider.IsMounted) return;

            float ceiling = Mathf.Max(1f, _assist.rampCeilingDeg);
            // MINI-181: the rider works the wheelie - throws weight back as the
            // front pops up, eases forward as it comes down - instead of only
            // tracking the angle. Only while the front is actually up.
            float lean = _assist.CurrentRampDeg > 0.5f ? _assist.PitchVelocityDegPerSec / 400f : 0f;
            float blend01 = Mathf.Clamp01(_assist.CurrentRampDeg / ceiling + lean);
            _rider.SetWheelieBlend(blend01);
        }
    }
}
