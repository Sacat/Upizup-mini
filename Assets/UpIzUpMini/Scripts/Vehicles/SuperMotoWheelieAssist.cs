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
    // MINI-119 follow-up fix, user: "it never worked even at high speeds."
    // Guarantees this component's FixedUpdate runs AFTER RB_Controller's
    // and AutoLeveling's (Unity's default order between scripts is
    // otherwise arbitrary) - this class deliberately has the last word on
    // rotation each step, and an arbitrary order meant that was only
    // sometimes true in the real game while always being true in the
    // batch tests, which explicitly invoked it last. A real
    // reproduce-in-game-only difference, not a tuning issue.
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(RB_Controller), typeof(Rigidbody))]
    public class SuperMotoWheelieAssist : MonoBehaviour
    {
        [Tooltip("Degrees per second the wheelie angle climbs while held, and falls back to 0 when released.")]
        public float riseRateDegPerSecond = 72f;
        [Tooltip("Maximum wheelie angle (deg) a full, sustained hold reaches.")]
        public float rampCeilingDeg = BikeCrashEjectionController.MaximumWheelieDegrees;
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
        private SacatRagdollManager _sacatRagdoll;

        /// <summary>MINI-119 follow-up: wired once from VehicleSpawnController
        /// right after Sacat's own ragdoll is built, so ApplyAutoRecover's
        /// existing reset call reaches it too, not just the vendor's own
        /// (now-hidden) rider ragdoll.</summary>
        public void SetSacatRagdoll(SacatRagdollManager ragdoll) => _sacatRagdoll = ragdoll;

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

        /// <summary>MINI-119 follow-up, user: "it never worked even at
        /// high speeds" / "your testing is the worst it never works."
        /// A LIVE, on-screen readout of every value that decides whether a
        /// wheelie happens, rendered in the real running game (see
        /// Mini119StockDemoBikeTuner) - so what's actually going on is
        /// visible in one play session instead of being inferred from
        /// batch tests that have repeatedly disagreed with the real
        /// game.</summary>
        public string LiveWheelieStatus()
        {
            if (_rb == null || _input == null || _body == null) return "wheelie assist: not initialised";
            if (_rb.wheelColliders == null || _rb.wheelColliders.Length < 2) return "wheelie assist: wheel colliders missing";

            var rearWheel = _rb.wheelColliders[0];
            var frontWheel = _rb.wheelColliders[1];
            float speedKmh = _body.linearVelocity.magnitude * 3.6f;
            float wheelieIn = _input.WheelieInput;
            bool held = Mathf.Abs(wheelieIn) > 0.01f;
            bool speedOk = speedKmh >= wheelieMinSpeedKmh && speedKmh <= wheelieMaxSpeedKmh;
            bool brakeOk = _input.FrontBreakInput < 0.1f;
            bool rearOk = rearWheel != null && rearWheel.isGrounded;

            float liftM = 0f;
            if (frontWheel != null)
            {
                Vector3 frontBottom = frontWheel.transform.position - Vector3.up * frontWheel.radius;
                if (Physics.Raycast(frontBottom + Vector3.up * 0.05f, Vector3.down, out RaycastHit hit, 20f))
                    liftM = Mathf.Max(0f, frontBottom.y - hit.point.y);
            }

            string blocker;
            if (_rb.isCrashed) blocker = "BLOCKED: crashed (press F)";
            else if (!held) blocker = "waiting for E";
            else if (!brakeOk) blocker = "BLOCKED: brake held";
            else if (_currentWheelieTarget <= 0.01f && !speedOk) blocker = $"BLOCKED: speed {speedKmh:F0} outside {wheelieMinSpeedKmh:F0}-{wheelieMaxSpeedKmh:F0}";
            else if (_currentWheelieTarget <= 0.01f && !rearOk) blocker = "BLOCKED: rear wheel not grounded";
            else blocker = "WHEELIE ACTIVE";

            return
                $"E held: {(held ? "YES" : "no")} (raw {wheelieIn:F1})   speed: {speedKmh:F0} km/h\n" +
                $"wheelie angle: {_currentWheelieTarget:F0} deg   FRONT WHEEL LIFT: {liftM:F2} m\n" +
                $"front grounded: {(frontWheel != null && frontWheel.isGrounded ? "yes" : "NO (lifted)")}   constraints: {_body.constraints}\n" +
                $">> {blocker}";
        }

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

                // MINI-119 follow-up fix, user: "cant ride up a hill."
                // Real bug, found by reading RB_Controller.CalculateTorque()
                // directly: firstGearTorque is NOT a config field there,
                // it's a mutable working variable. Start() captures the
                // real setting once (`ogTorque = firstGearTorque`), and
                // then CalculateTorque() OVERWRITES firstGearTorque every
                // single FixedUpdate from that captured ogTorque. So the
                // hill-climb slider writing to firstGearTorque per-frame
                // (as this class used to do) was being discarded
                // immediately, every frame - it could never have done
                // anything. Setting it here in Awake, BEFORE
                // RB_Controller.Start() runs, is what actually makes it
                // take effect, because that's the value Start() captures.
                _rb.firstGearTorque = firstGearTorque;
                _rb.topGearTorque = topGearTorque;
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

            // NOTE: firstGearTorque/topGearTorque are deliberately NOT
            // written here any more - see Awake's own comment on why a
            // per-frame write to firstGearTorque is silently discarded by
            // RB_Controller.CalculateTorque(). Changing the hill-climb
            // sliders now requires a respawn to take effect, which is
            // honest, rather than appearing to work live while doing
            // nothing at all.
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
            float targetPitch = wantsWheelie
                ? BikeCrashEjectionController.ClampWheelieDegrees(rampCeilingDeg)
                : 0f;

            _currentWheelieTarget = Mathf.MoveTowards(
                _currentWheelieTarget, targetPitch, riseRateDegPerSecond * Time.fixedDeltaTime);

            if (_currentWheelieTarget <= 0.01f)
            {
                _wheelieYawLatched = false;
                // Not wheelieing - hand the bike back to RB_Controller's
                // own constraint handling completely untouched, so normal
                // riding behaves exactly as it always has (user: "riding
                // is perfect").
                return;
            }

            // MINI-119 follow-up fix, user: "it never worked even at high
            // speeds." THE bug, and the reason every batch test passed
            // while the real game never wheelied: RB_Controller.Update()
            // force-sets `rb.constraints = RigidbodyConstraints.FreezeRotationZ`
            // EVERY FRAME. That freezes rotation about the WORLD Z axis,
            // not the bike's own roll axis. It only means "lock roll" if
            // the bike happens to be facing along world Z. The Lalay road
            // runs along world X - so while riding it, the bike's own
            // PITCH axis is approximately world Z, and the constraint was
            // freezing the wheelie itself. Direction-dependent by nature,
            // which is exactly why it looked so inconsistent.
            //
            // The batch tests never caught it because Physics.Simulate()
            // applies MoveRotation more directly than the real runtime
            // solver, which enforces constraints on the resulting angular
            // velocity - a genuine test-harness blind spot, not a tuning
            // miss.
            //
            // Cleared only while a wheelie is actually in progress. This
            // costs nothing in safety: the kinematic construction below
            // builds the rotation as yaw * pitch with NO roll term, so
            // roll is mathematically impossible during a wheelie anyway -
            // the vendor's world-axis roll lock has nothing left to do
            // here, and RB_Controller restores it by itself the moment
            // the wheelie ends.
            _body.constraints = RigidbodyConstraints.None;

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
            if (_sacatRagdoll != null) _sacatRagdoll.resetRider = true;
            if (_rb != null) _rb.isCrashed = false;
            if (_crash != null) { _crash.rbSpeed = 0f; _crash.lateRbSpeed = 0f; }

            _autoRecoverTimer = 0f;
            _currentWheelieTarget = 0f;
            _wheelieYawLatched = false;
        }

        // MINI-170: user reported the bike still crashes/tips over while
        // wheelieing and gets stuck there. Root cause: this used
        // Mathf.Asin(transform.forward.y), the EXACT technique already
        // diagnosed and replaced in the sibling SuperMotoUprightAssist.
        // TruePitchDeg() ("folds back past 90deg... a bad reading got
        // physically baked in and re-asserted every frame") - but that fix
        // was never ported here. Since ApplyAutoRecover()'s fallenOver
        // check (autoRecoverRollLimitDeg=60deg) depends entirely on
        // ComputeTrueRollDeg(), which depends on this, a bad pitch reading
        // here can silently make the auto-recovery safety net never
        // trigger once the bike is rolled over 60+ degrees - measured
        // directly: Mini119RealSceneWheelieTest showed roll climb to
        // 78.7deg and stay stuck there for 6+ real seconds with
        // isCrashed=False, well past the 0.35s auto-recover window. Same
        // signed-angle fix, ported verbatim.
        private float MeasurePitch()
        {
            if (transform == null) return 0f;
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.ProjectOnPlane(-transform.up, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) return 0f;
            flatForward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flatForward);
            return Vector3.SignedAngle(flatForward, transform.forward, right);
        }

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
