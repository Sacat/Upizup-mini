using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119: the class NAME stays "TmaxBikeController" deliberately -
    /// every other system in this game (BikeInteractable, BikeRiderAnimation,
    /// SaveLoadSystem, the minimap/camera code) already depends on that
    /// exact type, and keeping the name means none of them need to change
    /// at all. What changed is the IMPLEMENTATION: this is now a thin
    /// facade over the imported Motorbike Physics Tool's own
    /// Gadd420.RB_Controller (the user's own explicit direction - "backup
    /// our controller... use this riding and then integrate our control
    /// mapping to it"), rather than a from-scratch physics simulation.
    ///
    /// Our original, hand-tuned implementation is fully preserved,
    /// untouched, under TmaxBikeControllerCustom - nothing was deleted.
    ///
    /// Every public member below exists ONLY because something else in
    /// this game already reads it - see each one's own comment for who.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(RB_Controller), typeof(GaddInputAdapter))]
    public class TmaxBikeController : MonoBehaviour
    {
        private Rigidbody _rb;
        private RB_Controller _gadd;
        private GaddInputAdapter _input;

        private float _throttle;
        private float _steer;
        private float _brake;
        private bool _wheelieHeld;

        [Tooltip("Denominator BikeInteractable normalises the live WheelieAngle against for rider-pose blending (wheelie01 = WheelieAngle / MaxWheelieAngle). The underlying physics here has no artificial pitch ceiling of its own - this is purely a reference figure for that blend math, not a real limit.")]
        [SerializeField] private float maxWheelieAngle = 75f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _gadd = GetComponent<RB_Controller>();
            _input = GetComponent<GaddInputAdapter>();
        }

        private void Update()
        {
            // RB_Controller reads its Input_Manager's getters fresh every
            // Update/FixedUpdate of its own - pushing our latest values in
            // every frame keeps it current regardless of Unity's exact
            // script execution order between this and RB_Controller.
            _input.SetRawInputs(_throttle, _steer, _brake, _wheelieHeld);
        }

        /// <summary>Called by BikeInteractable every frame while mounted -
        /// see that file's own header comment on why input stays decoupled
        /// from physics.</summary>
        public void SetInput(float throttle, float steer, float brake)
        {
            _throttle = Mathf.Clamp(throttle, -1f, 1f);
            _steer = Mathf.Clamp(steer, -1f, 1f);
            _brake = Mathf.Clamp01(brake);
        }

        /// <summary>Called by BikeInteractable (E key) - see that file.</summary>
        public void SetWheelieHeld(bool held) => _wheelieHeld = held;

        /// <summary>Read by BikeRiderAnimation (moving/idle pose choice) and
        /// TmaxWheelieTuner-equivalent HUD/speedo readouts.</summary>
        public float SpeedKmh => _rb == null ? 0f : _rb.linearVelocity.magnitude * 3.6f;

        /// <summary>Read by BikeInteractable/BikeRiderAnimation for wheelie
        /// pose blending - the REAL measured pitch (not a target/setpoint,
        /// unlike our own custom controller's own WheelieAngle - this
        /// underlying physics has no separate "target" concept, the pitch
        /// IS whatever the real torque-driven Rigidbody rotation currently
        /// is), clamped to 0 at and below level so it reads as "how much
        /// wheelie" rather than going negative nose-down.</summary>
        public float WheelieAngle => Mathf.Max(0f, CurrentPitchAngle);

        public float MaxWheelieAngle { get => maxWheelieAngle; set => maxWheelieAngle = value; }

        /// <summary>Read by BikeInteractable for the rider's counter-pitch
        /// calculation. Same asin(forward.y) measurement our own custom
        /// controller already used, so downstream rider-pose math that
        /// consumes this number behaves the same regardless of which
        /// physics is actually driving the bike underneath.</summary>
        public float CurrentPitchAngle =>
            transform == null ? 0f : Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        /// <summary>Read by BikeInteractable for rider roll-copy. The
        /// underlying physics here rolls the REAL Rigidbody for lean (no
        /// separate cosmetic-only lean layer the way our custom controller
        /// has) - this just reads that real roll back out, signed.</summary>
        public float CurrentVisualLean
        {
            get
            {
                if (transform == null) return 0f;
                float z = transform.eulerAngles.z;
                return z > 180f ? z - 360f : z;
            }
        }

        /// <summary>Read by BikeInteractable for the hand-grip IK boost.
        /// wheelColliders[1] is the FRONT wheel in RB_Controller's own
        /// convention (its HandleSteering/HandleBrakes both index the
        /// front there).</summary>
        public bool IsFrontWheelGrounded =>
            _gadd == null || _gadd.wheelColliders == null || _gadd.wheelColliders.Length < 2
                || _gadd.wheelColliders[1].isGrounded;

        /// <summary>Read by BikeRiderAnimation to pick the wheelie pose.</summary>
        public bool IsWheelieing => !IsFrontWheelGrounded;

        /// <summary>True once the underlying physics has flagged a crash
        /// (CrashController/RagdollManager take it from here) - exposed in
        /// case anything upstream (mission/HUD code) wants to react to it.</summary>
        public bool IsCrashed => _gadd != null && _gadd.isCrashed;
    }
}
