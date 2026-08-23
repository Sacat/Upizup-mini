using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "the spawn works well just use the
    /// wheelie method of the original tmax and finish because at this
    /// point you can figure it out."
    ///
    /// Complete rewrite, replacing every earlier attempt at making the
    /// STOCK torque-based wheelie (RB_Controller.wheelieTorque,
    /// AutoLeveling.safeWheelies, Rigidbody.angularDamping tricks, a
    /// roll-correction layered on top fighting all of it) behave. None of
    /// that is used any more. This directly ports
    /// TmaxBikeControllerCustom.ApplyWheelie's own proven mechanism -
    /// the SAME one already tuned across many earlier rounds of this
    /// project on the original bike - instead of reinventing it a third
    /// time on top of the vendor's torque system:
    ///
    /// The wheelie angle is a plain accumulating target (MoveTowards at
    /// riseRateDegPerSecond toward rampCeilingDeg while held, back to 0
    /// when released), and the bike's rotation is DIRECTLY SET each step
    /// as yaw * pitch, with NO roll term at all - mathematically
    /// incapable of leaning, exactly like the original. This is why every
    /// earlier version of this class needed a separate roll-correction
    /// system fighting the vendor's own torque/suspension/drag - a
    /// kinematic set has no such fight to have, because it never asks the
    /// real torque system to produce the rotation in the first place.
    /// RB_Controller's own wheelieTorque, Stoppies and backFlipTorque are
    /// zeroed/disabled below specifically so they can never compete with
    /// this for the same rotation.
    ///
    /// Rotation pivots around the REAR wheel's ground contact point (not
    /// the Rigidbody's own centre) so the front genuinely lifts off the
    /// ground rather than the whole bike rotating in place - same
    /// technique, same reason, as the original.
    /// </summary>
    [RequireComponent(typeof(RB_Controller), typeof(Rigidbody))]
    public class SuperMotoWheelieAssist : MonoBehaviour
    {
        [Tooltip("Degrees per second the wheelie angle climbs while held, and falls back to 0 when released.")]
        public float riseRateDegPerSecond = 72f;
        [Tooltip("Maximum wheelie angle (deg) a full, sustained hold reaches.")]
        public float rampCeilingDeg = 75f;
        [Tooltip("Minimum speed (km/h) to START a wheelie - has no effect on SUSTAINING one already in progress.")]
        public float wheelieMinSpeedKmh = 8f;
        [Tooltip("Maximum speed (km/h) to START a wheelie.")]
        public float wheelieMaxSpeedKmh = 70f;
        [Tooltip("How fast steering turns the bike while airborne mid-wheelie (deg/s at full steering input).")]
        public float wheelieAirSteerTorque = 14f;

        // MINI-119 follow-up fix, user (with screenshot of the bike lying
        // fully on its side): "this lean stuck is still happening i need
        // something that can put up the bike to straight equal on both
        // side just like when i press F it respawns straight." Same
        // recipe as pressing F, automated - runs UNCONDITIONALLY (crashed
        // or not), which is the actual fix: every earlier correction here
        // used to switch itself off exactly when RB_Controller flags a
        // crash and removes all rotation constraints.
        [Tooltip("Roll (deg, measured independent of any real pitch) past which the bike is considered properly fallen over - if sustained, it self-rights automatically, the same way pressing F does.")]
        public float autoRecoverRollLimitDeg = 60f;
        [Tooltip("How long roll has to stay past the limit above before auto-recovery kicks in - avoids reacting to one glitchy frame mid-crash-animation.")]
        public float autoRecoverSustainSeconds = 0.35f;

        private float _autoRecoverTimer;
        private RagdollManager _ragdollForRecover;

        // MINI-119 follow-up, user: "i need more hill assist because it
        // use to climb the hill better than that." Not wheelie-related at
        // all - this is RB_Controller's own engine power, already a real
        // stock field, just never exposed as a slider on this rig.
        [Tooltip("RB_Controller.firstGearTorque (low-gear/starting power) - raise this if hills feel weak.")]
        public float firstGearTorque = 500f;
        [Tooltip("RB_Controller.topGearTorque (high-gear power).")]
        public float topGearTorque = 350f;

        // MINI-119 follow-up, user: "it crashes a bit too easy... not
        // sure if the ragdoll has anything to do with this." Same real
        // cause TmaxBikeController's own facade already diagnosed and
        // fixed for the mapped bike - the rider's ragdoll bone colliders
        // are solid for at least one physics step before RagdollManager.
        // Start() flips them to triggers, and CrashController's
        // deceleration threshold is sensitive enough that an ordinary
        // jolt trips it. Both fixed in Awake() below.
        [Tooltip("CrashController.decelerationSpeedForCrash - how hard a sudden slowdown has to be to count as a crash. HIGHER = harder to trigger accidentally.")]
        public float crashDecelerationThreshold = 20f;

        private CrashController _crash;
        private RB_Controller _rb;
        private Rigidbody _body;
        private Input_Manager _input;

        private float _currentWheelieTarget;
        private bool _wheelieYawLatched;
        private float _lockedWheelieYaw;

        public float CurrentRampDeg => _currentWheelieTarget;
        public float TrueRollDeg => ComputeTrueRollDeg();
        public float TruePitchDeg => MeasurePitch();

        private void Awake()
        {
            _rb = GetComponent<RB_Controller>();
            _body = GetComponent<Rigidbody>();
            _input = GetComponent<Input_Manager>();

            // MINI-119 follow-up, user: "i bring down the wheelie torque
            // to 4.00 and its still too high... use the wheelie method of
            // the original tmax." The kinematic method above is now the
            // ONLY thing that lifts the bike - none of the vendor's own
            // torque/constraint systems get a vote in it any more, so
            // they're all zeroed/disabled here rather than tuned.
            var autoLevel = GetComponent<AutoLeveling>();
            if (autoLevel != null) autoLevel.safeWheelies = false;
            if (_rb != null)
            {
                _rb.enableStoppiesBETA = false;
                _rb.backFlipTorque = 0f;
                _rb.wheelieTorque = 0f;
            }

            _crash = GetComponent<CrashController>();
            if (_crash != null)
            {
                _crash.decelerationSpeedForCrash = crashDecelerationThreshold;

                // MINI-119 follow-up fix, user: "i put the crach
                // sensitivity to 200 and it is still easy to crash... are
                // sure its that." Right to be sceptical - a SECOND,
                // completely independent crash trigger
                // (CrashController.OnTriggerEnter) fires on contact with
                // anything tagged "Ground" OR "Untagged" (Unity's own
                // default tag for anything nobody explicitly tagged, i.e.
                // most of this world), entirely regardless of deceleration.
                // Cleared so the slider above is the only thing deciding
                // a crash now.
                _crash.crashTag = new string[0];
            }

            // MINI-119 follow-up fix, user: "it crashes a bit too easy...
            // not sure if the ragdoll has anything to do with this." Forced
            // to trigger here in Awake - guaranteed to run before
            // RagdollManager's own Start() leaves that solid-collider
            // window open.
            var ragdoll = GetComponentInChildren<RagdollManager>(true);
            _ragdollForRecover = ragdoll;
            if (ragdoll != null)
                foreach (var col in ragdoll.GetComponentsInChildren<Collider>(true))
                    col.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (_rb == null || _input == null || _body == null) return;

            // Runs BEFORE the isCrashed bail below, and regardless of it -
            // see autoRecoverRollLimitDeg's own tooltip for why.
            ApplyAutoRecover();

            if (_rb.isCrashed)
            {
                _currentWheelieTarget = 0f;
                _wheelieYawLatched = false;
                return;
            }

            _rb.firstGearTorque = firstGearTorque;
            _rb.topGearTorque = topGearTorque;
            if (_crash != null) _crash.decelerationSpeedForCrash = crashDecelerationThreshold;

            ApplyWheelie();
        }

        /// <summary>Direct port of TmaxBikeControllerCustom.ApplyWheelie's
        /// own mechanism - see this class's own header for why. Adapted
        /// to this rig's own fields: RB_Controller.wheelColliders[0] is
        /// the rear wheel (this asset's own convention, already
        /// established elsewhere in this project),
        /// Input_Manager.WheelieInput/HzInput/FrontBreakInput stand in for
        /// the original's wheelieHeld/steerInput/brakeInput.</summary>
        private void ApplyWheelie()
        {
            if (_rb.wheelColliders == null || _rb.wheelColliders.Length < 2) return;
            var rearWheel = _rb.wheelColliders[0];
            var frontWheel = _rb.wheelColliders[1];
            if (rearWheel == null || frontWheel == null) return;

            float speedKmh = _body.linearVelocity.magnitude * 3.6f;
            float brakeIn = _input.FrontBreakInput;
            float wheelieIn = _input.WheelieInput;

            bool wheelieForcingPose = Mathf.Abs(_currentWheelieTarget) > 0.01f;

            // Starting a wheelie needs rear grip and a sane speed window;
            // sustaining one already up must not - same split the
            // original uses and the same reason (a high-angle wheelie's
            // rear wheel can chatter on and off the ground).
            bool canStart = brakeIn < 0.1f && speedKmh >= wheelieMinSpeedKmh
                && speedKmh <= wheelieMaxSpeedKmh && rearWheel.isGrounded;
            bool canSustain = brakeIn < 0.1f;
            bool eligible = wheelieForcingPose ? canSustain : canStart;

            // MINI-119 follow-up fix, user: "there is no wheelie taking
            // place when i press E and i see you swaped the Q and E
            // controls. Q leans back and E leans forward. i want E as the
            // wheelie." Rather than re-guess which raw sign
            // SuperMotoWheelieKeyRemap sends for E (this exact guess has
            // flip-flopped multiple times already this task, based on
            // ambiguous evidence each time), EITHER key now triggers the
            // SAME lift (always toward the positive/nose-up
            // rampCeilingDeg) - the sign is no longer meaningful, only
            // whether a wheelie key is held at all. E is guaranteed to
            // wheelie because both keys now do the one thing being asked
            // for, not two different things.
            bool wantsWheelie = eligible && Mathf.Abs(wheelieIn) > 0.01f;
            float targetPitch = wantsWheelie ? rampCeilingDeg : 0f;

            _currentWheelieTarget = Mathf.MoveTowards(
                _currentWheelieTarget, targetPitch, riseRateDegPerSecond * Time.fixedDeltaTime);

            if (_currentWheelieTarget <= 0.01f)
            {
                _wheelieYawLatched = false;
                return;
            }

            // Latch heading when the lift starts; steering can still turn
            // it in the air, but nothing else may drift it sideways.
            // Built from the stable flattened-forward yaw (not raw
            // transform.eulerAngles.y - already proven elsewhere in this
            // project, e.g. BikeCameraAnchor, to jump 180deg once pitch
            // crosses vertical).
            if (!_wheelieYawLatched)
            {
                _lockedWheelieYaw = StableYawDegrees();
                _wheelieYawLatched = true;
            }
            float steerIn = Mathf.Clamp(_input.HzInput, -1f, 1f);
            _lockedWheelieYaw += steerIn * wheelieAirSteerTorque * Time.fixedDeltaTime;

            // Rear contact patch, in world space - rotating around this
            // (not the Rigidbody's own centre) is what makes the FRONT
            // genuinely lift, rather than the whole bike rotating in
            // place around its middle.
            Vector3 pivot = rearWheel.transform.position - Vector3.up * rearWheel.radius;

            Quaternion newRot =
                Quaternion.Euler(0f, _lockedWheelieYaw, 0f) *
                Quaternion.Euler(-_currentWheelieTarget, 0f, 0f);

            Quaternion delta = newRot * Quaternion.Inverse(_body.rotation);
            Vector3 newPos = pivot + delta * (_body.position - pivot);

            _body.MoveRotation(newRot);
            _body.MovePosition(newPos);
            // No residual spin - otherwise the moment the forced pose ends
            // the bike would fling itself sideways with whatever angular
            // momentum had accumulated underneath it. Same as the
            // original's own note on this exact line.
            _body.angularVelocity = Vector3.zero;
        }

        /// <summary>"Press F", automated - see autoRecoverRollLimitDeg's
        /// own tooltip. Runs even while isCrashed (that's the whole
        /// point) using a gimbal-proof true-roll measurement, so a real
        /// deep wheelie (genuine, large pitch, zero actual lean) is never
        /// mistaken for having fallen over.</summary>
        private void ApplyAutoRecover()
        {
            bool fallenOver = ComputeTrueRollDeg() > autoRecoverRollLimitDeg;

            _autoRecoverTimer = fallenOver ? _autoRecoverTimer + Time.fixedDeltaTime : 0f;
            if (_autoRecoverTimer < autoRecoverSustainSeconds) return;

            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(
                transform.position + Vector3.up * 1f,
                Quaternion.Euler(0f, StableYawDegrees(), 0f));
            Physics.SyncTransforms();

            if (_ragdollForRecover != null) _ragdollForRecover.resetRider = true;
            if (_rb != null) _rb.isCrashed = false;
            if (_crash != null) { _crash.rbSpeed = 0f; _crash.lateRbSpeed = 0f; }

            _autoRecoverTimer = 0f;
            _currentWheelieTarget = 0f;
            _wheelieYawLatched = false;
        }

        private float MeasurePitch() =>
            transform == null ? 0f : Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        private Quaternion ZeroRollTarget(float pitchDeg) =>
            Quaternion.Euler(0f, StableYawDegrees(), 0f) * Quaternion.Euler(-pitchDeg, 0f, 0f);

        /// <summary>How far the bike's ACTUAL rotation is from having zero
        /// roll at its own current pitch - i.e. genuine lean, with pitch
        /// (however large, even a real wheelie) subtracted out first.
        /// Used only by auto-recover now that the wheelie itself is
        /// kinematic and mathematically cannot lean while active.</summary>
        private float ComputeTrueRollDeg() =>
            Quaternion.Angle(transform.rotation, ZeroRollTarget(MeasurePitch()));

        /// <summary>Yaw derived from the flattened forward vector rather
        /// than raw transform.eulerAngles.y, which jumps 180deg once
        /// pitch crosses vertical (same fix already proven elsewhere in
        /// this project, e.g. BikeCameraAnchor.FlatYaw).</summary>
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
