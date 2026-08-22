using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-071. A deliberately MINIMAL four-wheel car, per the user's choice
    /// of "just get in and drive" - enough to sit in the Range Rover and move
    /// around the map for testing, not a physics showcase.
    ///
    /// Written fresh rather than generalised out of TmaxBikeController. The
    /// bike carries a lot of hard-won machinery that means nothing to a car -
    /// wheelie pose driving, visual lean, upright assist, rider IK sync - and
    /// dragging a car through it would risk the wheelie/rider tuning that took
    /// most of MINI-064 to 066 to settle. The shared vehicle layer is worth
    /// extracting once two vehicles exist and the real commonality is visible,
    /// which is a refactor toward a known shape rather than a guess.
    ///
    /// Input is decoupled the same way the bike's is: SetInput() is called by
    /// whoever is driving, so a test harness or an AI driver can use this
    /// without faking key presses.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CarController : MonoBehaviour
    {
        [Header("Wheels (all four steer/drive per the flags below)")]
        [SerializeField] private WheelCollider frontLeft;
        [SerializeField] private WheelCollider frontRight;
        [SerializeField] private WheelCollider rearLeft;
        [SerializeField] private WheelCollider rearRight;

        [Header("Drive")]
        // MINI-077: "the range rover should better be able to climb a hill" -
        // a real Range Rover is four-wheel drive, which is both the honest
        // fix (matches the actual vehicle) and the effective one: driving
        // all four wheels roughly doubles available traction on a loose or
        // steep grade versus rear-only, which was the actual limiter, not a
        // torque NUMBER being too small. Torque per wheel raised too
        // (2200 -> 3200) since a heavier, AWD car needs more to match.
        [Tooltip("Torque per DRIVEN wheel at full throttle.")]
        [SerializeField] private float motorTorque = 3200f;
        [SerializeField] private float brakeTorque = 6000f;
        [Tooltip("Applied to all four wheels when nothing is pressed, so the car rolls to a stop instead of coasting forever.")]
        [SerializeField] private float idleDrag = 350f;
        [SerializeField] private float maxSpeedKmh = 140f;
        [SerializeField] private float maxReverseSpeedKmh = 25f;

        [Header("Steering")]
        [SerializeField] private float maxSteerAngle = 32f;
        [Tooltip("Steering is reduced as speed rises, or the car darts at motorway speed and spins on a flick of the key.")]
        [SerializeField] private float steerReductionAtTopSpeed = 0.55f;
        [SerializeField] private float steerSmoothing = 8f;

        [Header("Stability")]
        [Tooltip("Centre of mass offset from the body origin. Low is what stops a tall SUV rolling over on every corner - the single most important number here.")]
        [SerializeField] private Vector3 centreOfMass = new Vector3(0f, -0.75f, 0f);
        [Tooltip("Downforce scaled by speed, keeping the tyres planted at speed.")]
        [SerializeField] private float downforce = 110f;

        private Rigidbody _rb;
        private float _throttle;
        private float _steer;
        private float _brake;
        private float _smoothedSteer;

        public float SpeedKmh => _rb != null ? _rb.linearVelocity.magnitude * 3.6f : 0f;

        /// <summary>Signed along the car's own forward, so reverse reads negative.</summary>
        public float ForwardSpeedKmh => _rb != null
            ? Vector3.Dot(_rb.linearVelocity, transform.forward) * 3.6f
            : 0f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.centerOfMass += centreOfMass;
            // Same reasoning as the TMAX: 50Hz physics against a higher display
            // refresh reads as stutter without interpolation, and a fast vehicle
            // needs continuous collision or it tunnels through thin geometry.
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // MINI-119 follow-up, user: "when you drive really fast, and
            // you hit an object hard it does abnormal flipping right up
            // in the air." This car had NO anti-flip system at all - only
            // downforce and a low centre of mass, neither of which does
            // anything to stop a genuine hard-collision angular impulse.
            // Researched Unity's own documented vehicle-rollover practice:
            // Rigidbody.maxAngularVelocity defaults to 7 rad/s (~401deg/s),
            // easily enough for one hard hit to spin a whole car through
            // several rotations in under a second. Capped well below that.
            _rb.maxAngularVelocity = 4f;
        }

        /// <summary>throttle -1..1 (negative reverses), steer -1..1, brake 0..1.</summary>
        public void SetInput(float throttle, float steer, float brake)
        {
            _throttle = Mathf.Clamp(throttle, -1f, 1f);
            _steer = Mathf.Clamp(steer, -1f, 1f);
            _brake = Mathf.Clamp01(brake);
        }

        private void FixedUpdate()
        {
            if (frontLeft == null || frontRight == null || rearLeft == null || rearRight == null) return;

            ApplySteering();
            ApplyDrive();

            // Speed-scaled downforce. Constant downforce would glue the car to
            // the ground while parked, which makes it feel heavy off the line.
            _rb.AddForce(-transform.up * downforce * _rb.linearVelocity.magnitude);

            ApplyLaunchCap();
            ApplySelfRighting();
        }

        // MINI-119 follow-up, user: "the vehicle should not even go that
        // far in the air much less for fliping abnormally in the air."
        // Same fix as the bike's own ApplyLaunchCap: a hard hit's vertical
        // impulse is capped directly at the source, not just reacted to
        // afterward. Horizontal motion is completely untouched.
        [Header("Launch Cap (prevents hard hits flinging the car upward)")]
        [SerializeField] private float maxUpwardLaunchSpeed = 6f;

        private void ApplyLaunchCap()
        {
            if (maxUpwardLaunchSpeed <= 0f) return;
            float verticalSpeed = Vector3.Dot(_rb.linearVelocity, Vector3.up);
            if (verticalSpeed <= maxUpwardLaunchSpeed) return;
            _rb.linearVelocity -= Vector3.up * (verticalSpeed - maxUpwardLaunchSpeed);
        }

        [Header("Self-Righting (anti-flip)")]
        [Tooltip("Researched Unity's own documented arcade-vehicle-stability practice (stabilizer/anti-roll bars, in-flight self-righting) - gently pulls the car back toward level roll/pitch every step, strongest while airborne (nothing else is holding it upright then) and lighter while grounded (so it doesn't fight normal, legitimate body roll through a corner).")]
        [SerializeField] private float selfRightingStrength = 6f;
        [SerializeField] private float selfRightingAirborneMultiplier = 4f;

        private void ApplySelfRighting()
        {
            if (selfRightingStrength <= 0f) return;

            bool grounded = frontLeft.isGrounded || frontRight.isGrounded || rearLeft.isGrounded || rearRight.isGrounded;
            float strength = grounded ? selfRightingStrength : selfRightingStrength * selfRightingAirborneMultiplier;

            Quaternion levelled = Quaternion.LookRotation(
                Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized.sqrMagnitude > 0.0001f
                    ? Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized
                    : transform.forward,
                Vector3.up);

            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, levelled, Mathf.Clamp01(strength * Time.fixedDeltaTime)));
        }

        // MINI-119 follow-up, user: "hit an object hard it does abnormal
        // flipping." A real chassis collision (as opposed to ordinary
        // WheelCollider ground contact, which never raises this event)
        // dumps a large one-off angular impulse into the Rigidbody -
        // ApplySelfRighting above only pulls it back out gradually over
        // several steps. This clamps the SPIKE itself, immediately, the
        // instant a hard hit happens, same technique already proven on
        // the bike (TmaxBikeController.HandleChassisCollision).
        [Tooltip("Hard cap (deg/s) on roll+pitch spin the instant the car's body collides with something. Lower = stays closer to level through a hard hit.")]
        [SerializeField] private float collisionSpinCap = 90f;

        private void OnCollisionEnter(Collision collision) => DampCollisionSpin();
        private void OnCollisionStay(Collision collision) => DampCollisionSpin();

        private void DampCollisionSpin()
        {
            if (collisionSpinCap <= 0f) return;
            Vector3 av = _rb.angularVelocity;
            // Yaw (around the car's own up axis) is left alone - a real
            // spin-out from a side hit is a legitimate, expected outcome,
            // not the "flipping" the user described. Only roll/pitch
            // (around forward/right) get capped.
            Vector3 localAv = transform.InverseTransformDirection(av);
            float capRad = collisionSpinCap * Mathf.Deg2Rad;
            localAv.x = Mathf.Clamp(localAv.x, -capRad, capRad);
            localAv.z = Mathf.Clamp(localAv.z, -capRad, capRad);
            _rb.angularVelocity = transform.TransformDirection(localAv);
        }

        private void ApplySteering()
        {
            float speedFactor = Mathf.Clamp01(SpeedKmh / Mathf.Max(1f, maxSpeedKmh));
            float limit = maxSteerAngle * Mathf.Lerp(1f, 1f - steerReductionAtTopSpeed, speedFactor);

            // Eased rather than applied raw: keyboard steering is a hard 0/1, and
            // snapping the wheels to full lock upsets the suspension instantly.
            _smoothedSteer = Mathf.MoveTowards(_smoothedSteer, _steer, steerSmoothing * Time.fixedDeltaTime);

            float angle = _smoothedSteer * limit;
            frontLeft.steerAngle = angle;
            frontRight.steerAngle = angle;
        }

        private void ApplyDrive()
        {
            float fwd = ForwardSpeedKmh;

            // Separate forward and reverse caps - without this, reverse inherits
            // the forward top speed and the car reverses like a rally car.
            bool overSpeed = _throttle > 0f ? fwd >= maxSpeedKmh : fwd <= -maxReverseSpeedKmh;
            // Split evenly across all four driven wheels rather than giving
            // each the full torque - AWD means the SAME total driving force
            // spread over more contact patches (more available traction),
            // not simply doubling the power.
            float torque = overSpeed ? 0f : _throttle * motorTorque * 0.5f;

            frontLeft.motorTorque = torque;
            frontRight.motorTorque = torque;
            rearLeft.motorTorque = torque;
            rearRight.motorTorque = torque;

            float braking = _brake * brakeTorque;
            // Coasting drag, so letting go actually slows the car down.
            if (Mathf.Approximately(_throttle, 0f) && _brake <= 0f) braking = idleDrag;

            frontLeft.brakeTorque = braking;
            frontRight.brakeTorque = braking;
            rearLeft.brakeTorque = braking;
            rearRight.brakeTorque = braking;
        }
    }
}
