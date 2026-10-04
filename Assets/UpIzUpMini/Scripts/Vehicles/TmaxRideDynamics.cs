using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-193 (user request: real physics for the TMAX - lean, spring, proper animation - fixing what is there).
    /// Adds three things on top of the existing TMAX without touching steering, drive, wheelie or crash logic:
    ///  1. Real suspension: the prefab's WheelColliders were heavily over-damped (zeta about 1.6 on a 480 kg bike) so they never rebounded.
    ///     Retuned to a damping ratio near 0.45 with about 45% static sag, so the bike dives under braking, squats on throttle and rebounds over bumps.
    ///  2. Lean as a spring-damper (second-order) roll that follows the steering intent AND the physical cornering lean
    ///     atan(v * yawRate / g), so it overshoots and settles instead of a linear ease.
    ///  3. Body pitch from longitudinal acceleration (squat / dive), also spring-damped, faded out during wheelies.
    /// The controller feeds Step from its existing lean update; TmaxWheelVisuals banks the wheel discs with BodyRotation.
    /// </summary>
    [DisallowMultipleComponent]
    public class TmaxRideDynamics : MonoBehaviour
    {
        [Header("Suspension (applied once at Awake)")]
        public bool retuneSuspension = true;
        public float frontTravel = 0.22f, frontSpring = 23500f, frontDamper = 2200f, frontSag = 0.45f;
        public float rearTravel = 0.20f, rearSpring = 26000f, rearDamper = 2500f, rearSag = 0.5f;

        [Header("Lean spring-damper")]
        public float leanFrequency = 8.5f;        // rad/s
        public float leanDampingRatio = 0.5f;     // below 1 gives a little overshoot
        [Range(0f, 1f)] public float physicalLeanShare = 0.4f;
        public float leanLimitScale = 1.3f;       // sprung lean may exceed the steering lean envelope by this much

        [Header("Squat / dive pitch")]
        public float pitchFrequency = 7f;
        public float pitchDampingRatio = 0.42f;
        public float degreesPerMetrePerSecond2 = 0.55f;   // body pitch per m/s^2 of longitudinal acceleration
        public float maxPitchDegrees = 3.2f;

        Rigidbody rb;
        float roll, rollVel, pitch, pitchVel;
        float lastForwardSpeed, smoothedAccel;
        bool primed;

        public float Roll { get { return roll; } }
        public float Pitch { get { return pitch; } }
        public Quaternion BodyRotation { get { return Quaternion.Euler(pitch, 0f, roll); } }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            // longitudinal acceleration sampled per physics step (per-frame sampling aliases against the fixed step)
            if (rb == null) return;
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (!primed) { lastForwardSpeed = forwardSpeed; primed = true; }
            float accel = (forwardSpeed - lastForwardSpeed) / Time.fixedDeltaTime;
            lastForwardSpeed = forwardSpeed;
            smoothedAccel = Mathf.Lerp(smoothedAccel, Mathf.Clamp(accel, -25f, 25f), 1f - Mathf.Exp(-10f * Time.fixedDeltaTime));
        }

        public void ApplySuspension(WheelCollider front, WheelCollider rear)
        {
            if (!retuneSuspension) return;
            Apply(front, frontTravel, frontSpring, frontDamper, frontSag);
            Apply(rear, rearTravel, rearSpring, rearDamper, rearSag);
        }

        static void Apply(WheelCollider w, float travel, float spring, float damper, float sag)
        {
            if (w == null) return;
            w.suspensionDistance = travel;
            var js = w.suspensionSpring; js.spring = spring; js.damper = damper; js.targetPosition = sag;
            w.suspensionSpring = js;
        }

        static void Spring(ref float x, ref float v, float target, float freq, float zeta, float dt)
        {
            // semi-implicit integration of x'' = w^2 (target - x) - 2 zeta w x', sub-stepped so frame hitches cannot blow it up
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / 0.008f), 1, 8);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                v += (freq * freq * (target - x) - 2f * zeta * freq * v) * h;
                x += v * h;
            }
        }

        /// <summary>steerLeanTarget: the controller's eased steering lean (degrees, negative = right). wheelie01: 0..1 fade during wheelies.</summary>
        public void Step(float steerLeanTarget, float maxVisualLean, float wheelie01, float dt)
        {
            if (dt <= 0f) return;
            if (rb == null) rb = GetComponent<Rigidbody>();
            float fade = 1f - Mathf.Clamp01(wheelie01);

            float target = steerLeanTarget;
            float forwardSpeed = 0f;
            if (rb != null)
            {
                forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
                // physical cornering lean: tan(phi) = v * yawRate / g. Roll Z is negative when leaning right (yaw is positive turning right)
                float physical = -Mathf.Atan(forwardSpeed * rb.angularVelocity.y / 9.81f) * Mathf.Rad2Deg;
                float cap = maxVisualLean * leanLimitScale;
                physical = Mathf.Clamp(physical, -cap, cap);
                target = Mathf.Lerp(steerLeanTarget, physical, physicalLeanShare) * fade;
            }
            float limit = Mathf.Max(1f, maxVisualLean) * leanLimitScale;
            Spring(ref roll, ref rollVel, Mathf.Clamp(target, -limit, limit), leanFrequency, leanDampingRatio, dt);

            // accelerating pitches the nose UP (negative X in Unity); braking dives it
            float pitchTarget = Mathf.Clamp(-smoothedAccel * degreesPerMetrePerSecond2, -maxPitchDegrees, maxPitchDegrees) * fade;
            Spring(ref pitch, ref pitchVel, pitchTarget, pitchFrequency, pitchDampingRatio, dt);
        }
    }
}
