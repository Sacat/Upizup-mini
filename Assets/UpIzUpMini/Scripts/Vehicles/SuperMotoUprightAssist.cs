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
        // MINI-170: measured (Mini119RealSceneWheelieTest, real scene, real
        // spawn) that a sustained wheelie makes roll climb ~63deg in the
        // FIRST SECOND alone (5.5 -> 68.6deg), then settle stuck at
        // 78.7deg - visually the bike lying on its side / the rider
        // "falling off". A flat 60deg/sec correction cannot win that race.
        // Mirrors TmaxBikeControllerCustom's own wheelieRollAssist, which
        // already strengthens (not disables) roll correction the deeper a
        // wheelie goes - same fix family, ported to this bike.
        [Tooltip("Multiplier applied to correctionDegPerSecond while a wheelie is active, so roll correction can actually win against wheelie-coupled roll growth instead of just barely keeping pace.")]
        public float wheelieCorrectionMultiplier = 4f;
        [Tooltip("Fraction (0-1) of the Rigidbody's roll-axis (forward-axis) angular velocity removed every physics step, independent of the MoveRotation snap. MoveRotation alone corrects the visible ORIENTATION but does not stop the underlying spin that produced it - next step's physics integration keeps re-adding it. This directly removes energy from the roll spin (never adds any), so it cannot resonate the way the earlier torque-based attempts did.")]
        [Range(0f, 1f)] public float rollSpinDamping = 0.6f;

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
            // MINI-170: user reported the bike still crashes/tips over
            // while wheelieing. Root cause: this early-return disabled ALL
            // roll correction the instant any wheelie started, leaving the
            // bike with zero protection against tipping sideways at
            // exactly its most vulnerable moment - a real, confirmed
            // asymmetry with TmaxBikeControllerCustom's own controller,
            // which instead STRENGTHENS roll correction during a wheelie
            // via wheelieRollAssist rather than turning it off. Safe to run
            // unconditionally: zeroRollTarget below is built from the
            // CURRENT measured pitch (TruePitchDeg()) and reapplies it
            // unchanged, so this only ever corrects unwanted ROLL - it
            // never fights or overrides the wheelie system's own pitch.
            float pitchDeg = TruePitchDeg();
            Quaternion zeroRollTarget = Quaternion.Euler(0f, StableYawDegrees(), 0f) * Quaternion.Euler(-pitchDeg, 0f, 0f);

            bool wheelieing = _wheelieAssist != null && _wheelieAssist.CurrentRampDeg > 0.01f;
            float effectiveSpeed = correctionDegPerSecond * (wheelieing ? wheelieCorrectionMultiplier : 1f);
            Quaternion newRot = Quaternion.RotateTowards(
                transform.rotation, zeroRollTarget, effectiveSpeed * Time.fixedDeltaTime);

            _body.MoveRotation(newRot);

            // Remove the underlying roll-axis spin too, not just the
            // visible orientation - see rollSpinDamping's own tooltip for
            // why MoveRotation alone isn't enough against a sustained
            // roll-inducing coupling.
            if (rollSpinDamping > 0f)
            {
                Vector3 rollAxis = transform.forward;
                float rollSpin = Vector3.Dot(_body.angularVelocity, rollAxis);
                _body.angularVelocity -= rollAxis * (rollSpin * rollSpinDamping);
            }
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

        /// <summary>MINI-119 follow-up fix, user: "the pitch value staying
        /// constant at 73deg for the second half seems suspicious." Real
        /// bug, not a fluke: this used to measure pitch with
        /// Mathf.Asin(forward.y), which folds back past 90deg and can
        /// misread during a hard sustained turn (yaw and pitch coupling
        /// in a single vector component). Because this component then
        /// MoveRotation's the bike to a target BUILT from that same
        /// measured pitch every single frame, a bad reading didn't just
        /// display wrong - it got physically baked into the bike's real
        /// rotation and re-asserted every frame after, turning a
        /// measurement glitch into a genuine, self-reinforcing stuck
        /// pitch. Signed angle around the flattened-forward-derived right
        /// axis reads the full range correctly instead.</summary>
        private float TruePitchDeg()
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.ProjectOnPlane(-transform.up, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) return 0f;
            flatForward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flatForward);
            return Vector3.SignedAngle(flatForward, transform.forward, right);
        }
    }
}
