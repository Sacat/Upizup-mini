using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "its a left to right that needs to
    /// return to equal angle on both left and right so the bike would be
    /// back up right no matter what... a counter force seperate for the
    /// superassist to bring it bck to 0 degrees lean that it constantly
    /// checks for."
    ///
    /// Two earlier torque-based attempts at this (a spring-damper via
    /// AddTorque, TmaxBikeController's own proven approach) were tested
    /// and REJECTED: a real gain strong enough to actually win against a
    /// sustained lean (e.g. resting against a slope) also resonated into
    /// a violent tumble; a gain gentle enough to stay stable at rest was
    /// too weak to stop a slow roll creep while stuck, and confirmed via
    /// a controlled A/B test (same scenario, component removed) that the
    /// torque approach was ITSELF causing both failure modes - the base
    /// bike alone settles cleanly to true upright with no assist running
    /// at all.
    ///
    /// This uses the same technique already proven reliable for the
    /// kinematic wheelie instead: directly steer the Rigidbody's rotation
    /// toward a zero-roll target every physics step via MoveRotation, at
    /// a fixed max degrees/second, preserving whatever pitch and yaw the
    /// bike currently has. No torque, no gain to tune, no velocity-
    /// feedback loop that can resonate - it can only ever monotonically
    /// close the roll error, never overshoot or oscillate.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(RB_Controller), typeof(Rigidbody))]
    public class SuperMotoUprightAssist : MonoBehaviour
    {
        [Tooltip("Max degrees/second the roll correction steers at. Higher = snaps upright faster after a hit; too high can look unnatural mid-turn.")]
        public float correctionDegPerSecond = 60f;

        private RB_Controller _rb;
        private Rigidbody _body;
        private SuperMotoWheelieAssist _wheelieAssist;

        private void Awake()
        {
            _rb = GetComponent<RB_Controller>();
            _body = GetComponent<Rigidbody>();
            _wheelieAssist = GetComponent<SuperMotoWheelieAssist>();

            // MINI-119 follow-up fix: the vendor's own AutoLeveling is
            // ALSO applying its own continuous roll-correcting torque
            // every FixedUpdate - two independent systems both trying to
            // own the same roll axis. Disabling it gives this component
            // sole ownership of roll during normal riding.
            var autoLevel = GetComponent<AutoLeveling>();
            if (autoLevel != null) autoLevel.enabled = false;
        }

        private void FixedUpdate()
        {
            if (_rb == null || _body == null || _rb.isCrashed) return; // let a real crash/ragdoll moment play out unopposed
            if (_wheelieAssist != null && _wheelieAssist.CurrentRampDeg > 0.01f) return;

            float pitchDeg = Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            Quaternion zeroRollTarget = Quaternion.Euler(0f, StableYawDegrees(), 0f) * Quaternion.Euler(-pitchDeg, 0f, 0f);

            Quaternion newRot = Quaternion.RotateTowards(
                transform.rotation, zeroRollTarget, correctionDegPerSecond * Time.fixedDeltaTime);

            _body.MoveRotation(newRot);
        }

        /// <summary>Yaw derived from the flattened forward vector rather
        /// than raw transform.eulerAngles.y, which jumps 180deg once
        /// pitch crosses vertical.</summary>
        private float StableYawDegrees()
        {
            Vector3 flat = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f)
                flat = Vector3.ProjectOnPlane(-transform.up, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f) return transform.eulerAngles.y;
            return Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
        }
    }
}
