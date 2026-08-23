using System.Collections;
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

        [Tooltip("MINI-119, user: \"the bike when down flat so i couldnt ride to test.\" Root cause, confirmed by a real read-only diagnostic (Mini119SuperMotoDiagnose), not guessed: the underlying CrashController flags a crash on ANY sudden deceleration (its own default threshold is small), and the instant it does, the bike's roll-lock constraint is removed entirely so the ragdoll can take over - a completely ordinary landing/settling jolt at spawn was enough to trip it before the player ever touched the controls. This briefly disables the crash detector right after spawn so a spawn-moment settle can never be misread as a crash.")]
        [SerializeField] private float spawnCrashGraceSeconds = 1.5f;

        // MINI-119 follow-up, user: "does the ragdoll have anything to
        // the bike not staying up i see the ragdoll put down its foot
        // before the bike moves." Confirmed real by Mini119SuperMotoDiagnose:
        // the rider's own bone colliders (Hip/chest/arms/legs) measured as
        // SOLID, non-trigger colliders the instant the prefab is
        // instantiated - RagdollManager's own Start() is what normally
        // flips them to harmless triggers, but Start() runs no earlier
        // than the FIRST Update, which is at least one full physics step
        // after Awake. If a solid foot/leg is touching the ground or
        // overlapping the bike's OWN body collider during that window (a
        // kinematic collider CAN still push a dynamic Rigidbody it
        // overlaps), that is a real, physical shove at the worst possible
        // moment. Forced to trigger here in OUR Awake - guaranteed to run
        // before Start() ever gets a chance to leave that window open.
        [SerializeField] private bool forceRagdollTriggersOnAwake = true;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _gadd = GetComponent<RB_Controller>();
            _input = GetComponent<GaddInputAdapter>();

            var crashController = GetComponent<CrashController>();
            if (crashController != null && spawnCrashGraceSeconds > 0f)
            {
                crashController.enabled = false;
                StartCoroutine(ReenableCrashDetectionAfterDelay(crashController, spawnCrashGraceSeconds));
            }

            if (forceRagdollTriggersOnAwake)
            {
                var ragdoll = GetComponentInChildren<RagdollManager>(true);
                if (ragdoll != null)
                {
                    foreach (var col in ragdoll.GetComponentsInChildren<Collider>(true))
                        col.isTrigger = true;
                }
            }
        }

        private IEnumerator ReenableCrashDetectionAfterDelay(CrashController crashController, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (crashController != null) crashController.enabled = true;
        }

        // MINI-119 follow-up, user: "i remember when you started working
        // on my bike this use to happen and then you did something to
        // help it stay up." Same real fix, ported over: our own bike
        // needed an active upright-correction system (ApplyStability/
        // ApplyUprightAssist) because nothing else reliably kept a two-
        // wheeled Rigidbody standing on its own. This asset relies
        // instead on Rigidbody.constraints = FreezeRotationZ - a WORLD-
        // space axis lock, not the bike's own roll axis. That only
        // actually prevents rolling over if the bike happens to be facing
        // along world Z; a real settle test measured it still rolling 64
        // degrees despite that constraint once spawned facing an
        // arbitrary direction (a live player's own heading, never
        // guaranteed to align with world Z). This adds the same kind of
        // correction our own bike already proved works - measured against
        // the bike's OWN flattened-forward axis, not a fixed world one -
        // so it holds itself upright regardless of which way it's facing.
        [Header("Upright Assist (works regardless of facing direction - see this field's own header comment)")]
        [SerializeField] private float uprightAssistStrength = 8f;
        [SerializeField] private float uprightAssistDamping = 2.5f;

        private void FixedUpdate()
        {
            if (_rb == null || _gadd == null || _gadd.isCrashed) return; // let a real crash/ragdoll moment play out unopposed

            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = transform.forward;
            flatForward.Normalize();

            float rollError = Vector3.SignedAngle(transform.up, Vector3.up, flatForward);
            float rollVelocity = Vector3.Dot(_rb.angularVelocity, flatForward);
            float correction = (-rollError * uprightAssistStrength) - (rollVelocity * uprightAssistDamping);

            _rb.AddTorque(flatForward * correction, ForceMode.Acceleration);
        }

        // MINI-119 follow-up, user: "the new bike spawns in the same area
        // [as the old one] so they clash and the new bike falls... if the
        // bike falls I cannot keep testing something is wrong, maybe there
        // should be a temp key to reset the bike so it can come upright."
        // Temporary dev-only aid - press to snap the bike back onto its
        // wheels wherever it currently is, without reloading the scene.
        // Mirrors the asset's OWN "flip over" recovery (see the imported
        // Gadd420.KeyBoardShortCuts.cs, F-when-crashed) rather than
        // inventing a new convention: reposition upright a little above
        // its current spot, zero both velocities, and tell the ragdoll to
        // re-arm (RagdollManager.resetRider - its own Update() then clears
        // isCrashed and puts the rider's bones back to kinematic/trigger).
        [SerializeField] private KeyCode devResetUprightKey = KeyCode.R;

        private void Update()
        {
            // RB_Controller reads its Input_Manager's getters fresh every
            // Update/FixedUpdate of its own - pushing our latest values in
            // every frame keeps it current regardless of Unity's exact
            // script execution order between this and RB_Controller.
            _input.SetRawInputs(_throttle, _steer, _brake, _wheelieHeld);

            if (devResetUprightKey != KeyCode.None && Input.GetKeyDown(devResetUprightKey))
                ResetUpright();
        }

        /// <summary>Snaps the bike back onto its wheels in place - see the
        /// field comment above. Public so SuperMotoInteractable can also
        /// call it automatically when mounting a bike that fell over.</summary>
        public void ResetUpright()
        {
            if (_rb == null) return;

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(
                transform.position + Vector3.up * 1f,
                Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            Physics.SyncTransforms();

            var ragdoll = GetComponentInChildren<RagdollManager>(true);
            if (ragdoll != null) ragdoll.resetRider = true;
            if (_gadd != null) _gadd.isCrashed = false;

            var crashController = GetComponent<CrashController>();
            if (crashController != null) { crashController.rbSpeed = 0f; crashController.lateRbSpeed = 0f; }
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
