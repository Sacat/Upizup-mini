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
