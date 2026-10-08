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
    /// MINI-202 (user: rider and pillion should move realistically with the bike, lean and wheelie, bump with the terrain):
    ///  4. Rider anchors follow the body. The seats, foot pegs and pillion grab/foot points were siblings of VisualLeanRoot,
    ///     so the scooter leaned and squatted while both riders stayed upright and their feet came off the pegs. At Awake
    ///     they are re-parented under VisualLeanRoot (world pose unchanged at rest), so riders lean, squat and dive WITH the
    ///     body around the tyre contact line, like on the physically leaning SuperMoto. The prefab asset is not modified.
    ///  5. Rider bounce: each rider's seat offset is a spring-damper driven by the bike's own vertical acceleration, so bumps
    ///     and landings are absorbed by the riders' legs with a slight lag instead of the riders moving rigidly with the frame.
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

        [Header("MINI-202 riders follow the body")]
        public bool ridersLeanWithBody = true;
        [Tooltip("Direct children of the bike root that carry rider placement / IK targets. Re-parented under VisualLeanRoot at Awake.")]
        public string[] riderAnchorNames = { "Seat", "PillionSeat", "LeftFootTarget", "RightFootTarget", "PillionGrabLeft", "PillionGrabRight", "PillionFootLeft", "PillionFootRight" };

        [Header("MINI-202 rider bounce (legs absorbing bumps)")]
        public bool riderBounce = true;
        public float driverBounceFrequency = 11f, driverBounceDamping = 0.55f;
        public float pillionBounceFrequency = 8f, pillionBounceDamping = 0.45f;
        [Tooltip("Metres of rider sink per m/s^2 of the bike's vertical acceleration (before the spring).")]
        public float bounceMetresPerAccel = 0.0035f;
        public float maxBounce = 0.045f;
        [Tooltip("Rider torso nod (degrees) per metre of bounce - the upper body pitches slightly forward when the legs absorb a hit.")]
        public float bounceNodDegreesPerMetre = 60f;

        Rigidbody rb;
        float roll, rollVel, pitch, pitchVel;
        float lastUpSpeed, upAccel, driverBounce, driverBounceVel, pillionBounce, pillionBounceVel;
        bool upPrimed;
        public bool RidersAttached { get; private set; }
        /// <summary>Seat-local offset (metres, Y) and nod (degrees) for the driver / pillion.</summary>
        public Vector3 DriverBounceOffset { get { return new Vector3(0f, driverBounce, 0f); } }
        public float DriverBounceNod { get { return -driverBounce * bounceNodDegreesPerMetre; } }
        public Vector3 PillionBounceOffset { get { return new Vector3(0f, pillionBounce, 0f); } }
        public float PillionBounceNod { get { return -pillionBounce * bounceNodDegreesPerMetre; } }

        /// <summary>Re-parent the rider anchors under the leaning visual body (runtime only, world pose kept).</summary>
        public void AttachRiderAnchors(Transform visualLeanRoot)
        {
            if (!ridersLeanWithBody || visualLeanRoot == null || RidersAttached) return;
            int moved = 0;
            foreach (var n in riderAnchorNames)
            {
                Transform t = transform.Find(n);
                if (t == null || t == visualLeanRoot || visualLeanRoot.IsChildOf(t)) continue;
                t.SetParent(visualLeanRoot, true);
                moved++;
            }
            RidersAttached = moved > 0;
        }
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

            // vertical (bike-up) acceleration for the rider bounce; gravity is not included (riders are already seated in it)
            float upSpeed = Vector3.Dot(rb.linearVelocity, transform.up);
            if (!upPrimed) { lastUpSpeed = upSpeed; upPrimed = true; }
            float a = (upSpeed - lastUpSpeed) / Time.fixedDeltaTime;
            lastUpSpeed = upSpeed;
            upAccel = Mathf.Lerp(upAccel, Mathf.Clamp(a, -40f, 40f), 1f - Mathf.Exp(-25f * Time.fixedDeltaTime));
            if (riderBounce)
            {
                // the frame pushing UP compresses the rider's legs (rider sinks), a drop lets them rise
                float target = Mathf.Clamp(-upAccel * bounceMetresPerAccel, -maxBounce, maxBounce);
                Spring(ref driverBounce, ref driverBounceVel, target, driverBounceFrequency, driverBounceDamping, Time.fixedDeltaTime);
                Spring(ref pillionBounce, ref pillionBounceVel, target * 1.25f, pillionBounceFrequency, pillionBounceDamping, Time.fixedDeltaTime);
                driverBounce = Mathf.Clamp(driverBounce, -maxBounce, maxBounce);
                pillionBounce = Mathf.Clamp(pillionBounce, -maxBounce * 1.25f, maxBounce * 1.25f);
            }
            else { driverBounce = pillionBounce = driverBounceVel = pillionBounceVel = 0f; }
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
