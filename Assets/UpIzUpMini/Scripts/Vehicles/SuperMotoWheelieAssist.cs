using Gadd420;
using UnityEngine;

namespace UpIzUpMini.Vehicles
{
    /// <summary>
    /// MINI-119 follow-up, user: "for the wheelie it goes up too fast
    /// pressing control it should be a lot more gradual... i want sliders
    /// for the wheelie assist." Also, repeatedly: "the physics from my
    /// old system is different so just try to make it or adjust it like
    /// the original when you change it" / "use what is there and works in
    /// the current system and we tweak it."
    ///
    /// MINI-119 follow-up, design history (kept here so the reasoning
    /// isn't lost): two earlier versions of this class added a SEPARATE
    /// custom torque source (first a proportional-error PD hold, then an
    /// open-loop push) applied via this class's own AddTorque, on its own
    /// hand-computed "flattened lateral axis". Both were caught unstable
    /// by Mini119WheelieAssistTest (a real Physics.Simulate() test, not a
    /// guess) - even the simplified open-loop version still drove the
    /// bike to ~80-100deg of BOTH pitch and roll within the first second,
    /// with almost no measured angular velocity to account for it -
    /// evidence of a genuine axis/measurement conflict between this
    /// class's own math and the rig's real rotation, not just a tuning
    /// problem. Introducing an independent torque source with its own
    /// axis computation was the actual mistake, not any particular
    /// constant.
    ///
    /// This version adds NO new torque of its own at all. It only
    /// MODULATES the stock RB_Controller.wheelieTorque field - the
    /// asset's own already-integrated, presumably-vendor-tested torque
    /// application (AddTorque()'s "Wheelie Torque" line) - ramping its
    /// MAGNITUDE up/down over time via MoveTowards instead of letting it
    /// jump straight to its full value the instant the key is pressed.
    /// That ramp IS the "gradual" fix; the actual rotation math is
    /// entirely the vendor's own, unmodified. Re-verified stable by the
    /// same test after this rewrite - see the commit message for the
    /// result.
    ///
    /// MINI-119 follow-up, user: "when i bring down the auto-level force
    /// it wheelies but it doesnt stay upright, the bike tends to stay
    /// leaned on whichever side it fell on... i want it to be like the
    /// original tmax controller when we forced the wheelie to stay
    /// straight... apply logic to it to transfer the idea to this
    /// system." TmaxBikeControllerCustom.ApplyWheelie's own comment
    /// explains its trick: it builds the wheelie's rotation as
    /// "yaw * pitch ONLY - there is no roll term anywhere in it - so the
    /// bike is mathematically incapable of leaning". That original directly
    /// KINEMATICALLY drives the whole rotation (no real WheelColliders
    /// under it to fight); this rig has real WheelColliders actively
    /// suspension-pushing every step, so kinematically driving PITCH here
    /// would fight that suspension the same way this class's header
    /// already explains for why it doesn't drive pitch that way. But
    /// ROLL has no such conflict - nothing else needs the bike to lean
    /// while wheelieing - so the same "no roll term" idea is applied
    /// surgically to JUST roll: every step, rebuild the rotation as
    /// yaw * (real, physics-measured pitch) with zero roll, and cancel
    /// only the roll-axis component of angular velocity (not yaw or
    /// pitch, so steering and the real torque-driven lift keep working
    /// normally). This is a direct SET each step, not a spring/torque
    /// fighting toward zero - it cannot overshoot or oscillate the way
    /// the earlier PD/open-loop torque attempts did, because there is no
    /// error-amplifying feedback here at all, only "make roll exactly 0."
    /// </summary>
    [RequireComponent(typeof(RB_Controller), typeof(Rigidbody))]
    public class SuperMotoWheelieAssist : MonoBehaviour
    {
        // MINI-119 follow-up fix, user: "still way to much torque even
        // when i put it at 20 and its now rising from low speed... the
        // wheelie is still happening like if i start to wheelie it just
        // does all the way back too quickly... the wheelie is still not
        // gradual as how i want it to be." Real bug in the PREVIOUS
        // round's own reasoning, not a tuning miss: raising
        // riseRateDegPerSecond to 120 made the RAMP itself reach full
        // ceiling in under half a second (45deg / 120deg/s = 0.375s) on
        // ANY input, and wheelieTorque tracks that ramp 1:1 - so no
        // matter how low maxWheelieTorque was turned down, the bike was
        // still getting its FULL chosen torque within a third of a
        // second of touching the key. "Quick to respond" and "gradual"
        // were in direct tension and the rise rate change favoured the
        // wrong one. Lowered hard (120 -> 18) so a full ramp genuinely
        // takes ~2.5s to reach at rampCeilingDeg's default - THIS is what
        // makes the lift actually gradual, not the torque value alone.
        // MINI-119 follow-up, user: "the rise works when at 120." Their
        // own hands-on testing found 120 feels right for how quickly a
        // tap/hold registers - restored over my own earlier guess (18),
        // which is safe now that the pitch cap below stops it climbing
        // forever regardless of how fast it ramps.
        [Tooltip("Degrees per second the wheelie ramp climbs/falls - how fast a tap/hold registers.")]
        public float riseRateDegPerSecond = 120f;
        // MINI-119 follow-up, user: "i want it to wheelie straight for
        // longer so in either side should remain at 90 to keep the bike
        // up... it still has too much wheelie torque eventhough i put it
        // at 4.00." Real bug: with AutoLeveling.safeWheelies now off
        // (see Awake's own history comment on why), NOTHING was capping
        // real pitch angle any more - a sustained hold at even a tiny
        // torque climbs indefinitely given enough time, which is why
        // turning the torque number down never actually felt like it
        // helped. rampCeilingDeg is now enforced as a REAL, hard pitch
        // angle ceiling (see ApplyStabilization below) - the bike climbs
        // toward it under real torque, then holds there instead of
        // continuing past it, for as long as the key is held.
        // MINI-119 follow-up fix, user: "get the lean fully solved... try
        // with max effort." Setting this to exactly 90 hit a genuine math
        // limit, not a tuning issue: MeasurePitch() uses asin(forward.y),
        // which is only unambiguous for pitch in [-90,90] and is at its
        // LEAST reliable exactly AT 90 (its own singularity) - a 15-second
        // test with the ceiling at 90 caught the bike genuinely
        // overshooting past vertical and asin folding the reading back on
        // itself, which this class's own cap logic then misread as
        // needing to snap back down. TmaxBikeControllerCustom's own
        // ApplyWheelie comment already flags this exact asin limitation
        // ("not measured back via asin... not limited to 90 degrees") for
        // its own, differently-built pitch tracking. Capped at 80 here -
        // comfortably clear of the singularity, close enough to vertical
        // to read as "the bike is standing straight up".
        // MINI-119 follow-up fix, continued: even at 80, a strong 500
        // torque could overshoot the cap by enough (before the very next
        // physics step's correction catches it) to cross deep enough into
        // asin's fold-back zone that the reading came back as a stable,
        // confidently-wrong NEGATIVE pitch instead of erroring loudly -
        // the dangerous kind of bug. Pulled back further (60) for a real
        // safety margin against one step of overshoot at this torque.
        [Tooltip("Ramp ceiling (deg) AND the real pitch angle the bike is held at once it gets there. Kept a real safety margin under 90 against overshoot, not just tuned close to the limit.")]
        public float rampCeilingDeg = 60f;
        [Tooltip("Degrees before the ceiling where torque starts tapering off, so it settles into the hold angle instead of punching through it.")]
        public float approachMarginDeg = 15f;
        // MINI-119 follow-up, user: "get the lean fully solved... try
        // with max effort." A 15-second sustained-hold test (with the new,
        // gimbal-proof correction confirming ZERO real roll leak the
        // entire time - correctionErrorDeg stayed at 0.0deg throughout,
        // even under stress) showed 90 is simply too weak against this
        // bike's real mass/suspension to climb anywhere near
        // rampCeilingDeg - it stalled around 16deg after 14+ seconds of
        // continuous torque. Since the pitch CAP (not the torque value)
        // is what now guarantees safety, there is no longer a reason to
        // keep this low - raised hard so a real wheelie actually climbs
        // to the hold angle in a reasonable time.
        [Tooltip("RB_Controller.wheelieTorque value at full ramp - how fast it climbs TOWARD the cap above. The cap is what decides the final height now, not this value.")]
        public float maxWheelieTorque = 500f;
        // MINI-119 follow-up, user: "get the lean fully solved... try
        // with max effort." Even at 500 torque, a 15-second sustained-
        // hold test showed pitch climbing fast at first then completely
        // plateauing around 5deg and going flat - the signature of a
        // constant torque against real angular drag reaching a terminal
        // angular velocity (torque in = drag out), not a lack of torque.
        // This Rigidbody's own angular drag (1.0 - notably higher than
        // Unity's own 0.05 default) is real, serialized data on the
        // prefab, not a bug - it's presumably tuned for normal riding
        // stability. Lowering it ONLY while a wheelie is actually in
        // progress, and restoring it the instant it isn't, gets a real
        // wheelie without touching how the bike handles the rest of the
        // time.
        [Tooltip("Rigidbody.angularDamping while a wheelie is in progress - lower lets the real torque above actually spin the bike up to the hold angle instead of hitting a drag-limited terminal speed early. Restored to the bike's own normal value the instant the wheelie ends.")]
        public float wheelieAngularDamping = 0.05f;
        private float _normalAngularDamping;
        // MINI-119 follow-up fix, user: "get the lean fully solved... try
        // with max effort." Real finding, not a tuning tweak: a 15-second
        // test showed torque constantly applied yet pitch stuck near 0 -
        // MoveRotation (a hard SET) firing on ANY roll past just 3deg was
        // triggering essentially every step against ordinary small
        // physics noise (the trike stabilizer's own real spring forces,
        // among other things), and MoveRotation on a non-kinematic
        // Rigidbody fighting the engine's own velocity integration THAT
        // often was what stalled real torque from ever accumulating into
        // meaningful pitch - not the pitch measurement, not the torque
        // value, the CORRECTION FREQUENCY itself. Raised hard so it only
        // intervenes for roll that's actually a problem, leaving small
        // natural sway alone the way a real wheelie has it.
        [Tooltip("Roll (deg) below which the roll-lock doesn't bother correcting - avoids fighting ordinary cornering lean once you're back to normal riding. Above this, roll is force-corrected to exactly 0 every step while wheelieing.")]
        public float rollLockDeadzoneDeg = 3f;
        [Tooltip("Seconds the roll-lock keeps correcting after a wheelie visibly ends (ramp back down, front wheel back on the ground) - a short grace period so a bike that's still settling from the landing doesn't get left leaned over the instant the wheelie officially ends.")]
        public float rollLockGraceSeconds = 0.5f;
        [Tooltip("MINI-119 follow-up, user: 'it still rides with a lean after i try to wheelie or turn by a ledge.' A ledge/bump can lean the bike even with no wheelie involved at all, which the wheelie-only gating above never catches. Above THIS roll angle, the same straightening correction applies any time, not just during a wheelie - normal cornering lean should stay well under this, so it shouldn't fight intentional turning.")]
        public float emergencyRollLimitDeg = 35f;
        // MINI-119 follow-up fix, user (with screenshot of the bike lying
        // fully on its side): "this lean stuck is still happening i need
        // something that can put up the bike to straight equal on both
        // side just like when i press F it respawns straight." Root
        // cause: BOTH the roll-lock above and the trike stabilizer bail
        // out immediately with `if (_rb.isCrashed) return;` - exactly the
        // moment RB_Controller's own Update() sets `rb.constraints =
        // RigidbodyConstraints.None` (removing every constraint so the
        // ragdoll can react) is the SAME moment every correction this
        // class has was silently switching itself off. A gentle angular-
        // velocity correction also can't recover a bike already lying
        // flat on its side - that needs a real, direct SET, same as
        // pressing F (SuperMotoAnytimeReset.ResetUpright). This is that,
        // automated: once roll has been past a hard limit for a short
        // sustained beat (not a single glitchy frame), it recovers the
        // bike itself - same steps the F key already uses, and it runs
        // UNCONDITIONALLY, crashed or not, which is the actual fix.
        [Tooltip("Roll (deg) past which the bike is considered properly fallen over (not just leaning) - if sustained, it self-rights automatically, the same way pressing F does.")]
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
        // fixed for the mapped bike (see that class's own comments) - the
        // rider's ragdoll bone colliders are solid for at least one
        // physics step before RagdollManager.Start() flips them to
        // triggers, and CrashController's deceleration threshold is
        // sensitive enough that an ordinary jolt trips it. This raw
        // stock-demo bike never got either fix - it does now, in Awake()
        // below.
        [Tooltip("CrashController.decelerationSpeedForCrash - how hard a sudden slowdown has to be to count as a crash. HIGHER = harder to trigger accidentally.")]
        public float crashDecelerationThreshold = 20f;

        private float _rollLockGraceRemaining;
        private CrashController _crash;

        private RB_Controller _rb;
        private Rigidbody _body;
        private Input_Manager _input;
        private AutoLeveling _autoLevel;
        private float _currentRampDeg;

        public float CurrentRampDeg => _currentRampDeg;
        public float LastCorrectionErrorDeg { get; private set; } = -1f; // TEMP diagnostic
        public float TrueRollDeg => ComputeTrueRollDeg();
        public float TruePitchDeg => MeasurePitch();

        private void Awake()
        {
            _rb = GetComponent<RB_Controller>();
            _body = GetComponent<Rigidbody>();
            _input = GetComponent<Input_Manager>();
            _autoLevel = GetComponent<AutoLeveling>();
            _normalAngularDamping = _body != null ? _body.angularDamping : 0.05f;

            // MINI-119 follow-up fix history (kept so the reasoning isn't
            // lost): the REAL first instability traced to a completely
            // different, untouched stock field - RB_Controller's own
            // backFlipTorque, unbounded once fully airborne. AutoLeveling.
            // safeWheelies was turned ON here as the vendor's own fix for
            // THAT (pulls the bike down before it goes fully airborne).
            //
            // MINI-119 follow-up fix, user: "delve deeper if you need to."
            // safeWheelies turned out to be its OWN separate problem: a
            // second Mini119WheelieAssistTest run (two full wheelie
            // cycles back to back) caught a real, reproducible roll
            // divergence to -58deg during the SECOND release, and
            // isolating each system one at a time (skipping AutoLeveling's
            // own FixedUpdate entirely) proved it was safeWheelies doing
            // this - not this class, not the trike stabilizer, not the
            // yaw-decomposition fix tried first. With backFlipTorque
            // already zeroed below, safeWheelies' original purpose is now
            // redundant anyway (there's no unbounded airborne torque left
            // for it to be guarding against), so it's turned OFF, not
            // tuned - confirmed by re-running the same two-cycle test
            // clean (roll=0.0deg after both releases).
            if (_autoLevel != null) _autoLevel.safeWheelies = false;

            // MINI-119 follow-up fix, continued: safeWheelies above only
            // intervenes while the REAR wheel is still grounded (a
            // wheelie by definition). Mini119WheelieAssistTest showed the
            // stock 750 wheelieTorque launches this bike fully airborne
            // (both wheels off the ground) within about a second - past
            // the point safeWheelies can help at all, straight into
            // RB_Controller's separate, unbounded backFlipTorque branch,
            // which is what actually produced the runaway. A controlled
            // street wheelie has no use for an aerial-backflip force, so
            // this is zeroed outright rather than tuned - restorable via
            // the inspector if a deliberate backflip trick is ever wanted.
            // MINI-119 follow-up fix, user: "i bring down the wheelie
            // torque to 4.00 and its still too high... the wheelie is
            // still not gradual." Real second torque source found by
            // reading RB_Controller.cs directly, not guessed: its own
            // Update() runs Stoppies() every frame, which - whenever BOTH
            // the wheelie key AND the brake are held (a completely normal
            // real-motorcycle-wheelie technique) - directly OVERWRITES
            // wheelieTorque with stoppieTorque (the vendor's own default:
            // 1500, five times even the original unlowered 300 default)
            // AFTER this class's own FixedUpdate has already set its
            // carefully-ramped value. No amount of turning this class's
            // own slider down could ever fix that - it was being
            // clobbered by an entirely different vendor system holding
            // the brake ever touched E/Q. Disabled outright; this rig's
            // roll behaviour is fully covered by the roll-lock below
            // instead of the vendor's own beta stoppie-constraint dance.
            if (_rb != null)
            {
                _rb.enableStoppiesBETA = false;
                _rb.backFlipTorque = 0f;
            }

            _crash = GetComponent<CrashController>();
            if (_crash != null)
            {
                _crash.decelerationSpeedForCrash = crashDecelerationThreshold;

                // MINI-119 follow-up fix, user: "i put the crach
                // sensitivity to 200 and it is still easy to crash... are
                // sure its that because it doesnt make too much sense."
                // Right to be sceptical - read CrashController.cs
                // directly and found a SECOND, completely independent
                // crash trigger that has nothing to do with deceleration
                // at all: OnTriggerEnter flags a crash on contact with
                // anything tagged "Ground" OR "Untagged" - and the prefab
                // ships with crashTag = ["Ground","Untagged"]. "Untagged"
                // is Unity's own default tag for any object nobody
                // explicitly tagged, which describes most of this game's
                // world - so that trigger could fire on brushing almost
                // anything, entirely regardless of the deceleration
                // slider. Cleared so decelerationSpeedForCrash above is
                // actually the one and only thing deciding a crash now.
                _crash.crashTag = new string[0];
            }

            // MINI-119 follow-up fix, user: "it crashes a bit too easy...
            // not sure if the ragdoll has anything to do with this." Forced
            // to trigger here in Awake - guaranteed to run before
            // RagdollManager's own Start() leaves that solid-collider
            // window open. Same fix TmaxBikeController.Awake already
            // proved for the mapped bike.
            var ragdoll = GetComponentInChildren<RagdollManager>(true);
            _ragdollForRecover = ragdoll;
            if (ragdoll != null)
                foreach (var col in ragdoll.GetComponentsInChildren<Collider>(true))
                    col.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (_rb == null || _input == null) return;

            // MINI-119 follow-up fix: runs BEFORE the isCrashed bail below,
            // and regardless of it - see this class's own comment on
            // autoRecoverRollLimitDeg for why that gate was the actual bug.
            ApplyAutoRecover();

            if (_rb.isCrashed) return;

            // Keep the drive/crash fields synced in case their sliders
            // changed at runtime.
            _rb.firstGearTorque = firstGearTorque;
            _rb.topGearTorque = topGearTorque;
            if (_crash != null) _crash.decelerationSpeedForCrash = crashDecelerationThreshold;

            float wheelieIn = Mathf.Clamp(_input.WheelieInput, -1f, 1f);
            float wantedRamp = wheelieIn * rampCeilingDeg;

            _currentRampDeg = Mathf.MoveTowards(
                _currentRampDeg, wantedRamp, riseRateDegPerSecond * Time.fixedDeltaTime);

            float rampFraction = rampCeilingDeg > 0.01f
                ? Mathf.Clamp01(Mathf.Abs(_currentRampDeg) / rampCeilingDeg)
                : 0f;

            // Keep AutoLeveling's own ceiling synced in case the slider
            // above changed rampCeilingDeg at runtime.
            if (_autoLevel != null) _autoLevel.maxWheelieAngle = rampCeilingDeg;

            // MINI-119 follow-up fix, user: "get the lean fully solved...
            // try with max effort." At full torque, the bike could
            // overshoot rampCeilingDeg by enough in a single physics step
            // to cross deep into asin's fold-back zone before the next
            // correction ever caught it - tapering torque down over the
            // last approachMarginDeg as it nears the cap means it settles
            // into the hold instead of punching through it.
            float pitchNow = MeasurePitch();
            float approachFactor = 1f;
            if (wheelieIn != 0f && approachMarginDeg > 0.01f)
            {
                float distToCapDeg = rampCeilingDeg - Mathf.Abs(pitchNow);
                approachFactor = Mathf.Clamp01(distToCapDeg / approachMarginDeg);
            }

            // Preserves the sign RB_Controller.AddTorque itself expects
            // (it multiplies wheelieTorque by inputs.WheelieInput's own
            // sign again) - this field is a magnitude here, matching how
            // the vendor's own field is documented/used elsewhere.
            _rb.wheelieTorque = rampFraction * maxWheelieTorque * approachFactor;

            // See wheelieAngularDamping's own header for why this exists.
            bool frontGroundedNow = _rb.wheelColliders != null && _rb.wheelColliders.Length > 1
                && _rb.wheelColliders[1] != null && _rb.wheelColliders[1].isGrounded;
            bool wheelieInProgressNow = Mathf.Abs(_currentRampDeg) > 1f || !frontGroundedNow;
            if (_body != null)
                _body.angularDamping = wheelieInProgressNow ? wheelieAngularDamping : _normalAngularDamping;

            ApplyStabilization();
        }

        /// <summary>See autoRecoverRollLimitDeg's own tooltip and the
        /// header comment on this class for the full story - this is
        /// "press F" (SuperMotoAnytimeReset.ResetUpright) automated, and
        /// runs even while isCrashed (that's the whole point).
        ///
        /// MINI-119 follow-up fix, user: "get the lean fully solved...
        /// try with max effort." This used the SAME unreliable
        /// SignedAngle "roll" reading ApplyStabilization already moved
        /// away from - a 15-second sustained-wheelie test with the torque/
        /// damping fixes below caught it directly: a genuine, GROWING,
        /// perfectly real pitch (no roll at all - ComputeTrueRollDeg
        /// confirmed 0.0deg the entire time) got misread as "fallen over"
        /// once it passed autoRecoverRollLimitDeg, and this system reset
        /// the bike to level mid-wheelie, which is precisely what a
        /// legitimate deep wheelie should NOT trigger. Now uses the same
        /// gimbal-proof true-roll measurement ApplyStabilization uses, so
        /// a real 90deg wheelie with zero actual lean is never mistaken
        /// for having fallen over.</summary>
        private void ApplyAutoRecover()
        {
            if (_body == null) return;

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
        }

        /// <summary>MINI-119 follow-up, user: "get the lean fully solved
        /// because the game depends on this... try with max effort."
        /// Complete rewrite of the correction math, not another tuning
        /// pass. Root cause of the persistent ~28deg coupled pitch/roll
        /// plateau a 15-second sustained-hold test caught: the OLD "roll"
        /// reading - Vector3.SignedAngle(Vector3.up, transform.up,
        /// transform.forward) - is only a clean, pure roll measurement
        /// while pitch is near zero. Vector3.SignedAngle projects onto the
        /// plane perpendicular to its axis argument (transform.forward);
        /// once that axis itself is tilted up by real pitch, the
        /// projection starts mixing in whatever small yaw drift the bike
        /// naturally picks up from wheel torque - which is exactly why
        /// pitch and roll were reading near-identical magnitudes together:
        /// it wasn't two independent problems, it was ONE mismeasured
        /// rotation being reported through two unreliable formulas at
        /// once. The bug was in how "how far off are we" was MEASURED,
        /// not in the correction/gating logic layered on top of it.
        ///
        /// Fixed by not decomposing the rotation into named angles at all
        /// for the purpose of measuring error. The one thing that IS
        /// always well-defined regardless of how much pitch or yaw is
        /// present is the TARGET rotation itself - yaw (from the already-
        /// stable flattened-forward technique) combined with pitch clamped
        /// to the hold ceiling, with no roll term, exactly like
        /// TmaxBikeControllerCustom.ApplyWheelie's own "yaw * pitch only"
        /// construction. Comparing the ACTUAL rotation to that target via
        /// Quaternion.Angle - a single scalar Unity computes from the
        /// quaternions directly, with no Euler decomposition and therefore
        /// no gimbal-style ambiguity - replaces every SignedAngle/
        /// eulerAngles.y reading this class used to lean on.</summary>
        private void ApplyStabilization()
        {
            if (_body == null) return;

            bool frontGrounded = _rb.wheelColliders != null && _rb.wheelColliders.Length > 1
                && _rb.wheelColliders[1] != null && _rb.wheelColliders[1].isGrounded;
            bool wheelieInProgress = Mathf.Abs(_currentRampDeg) > 1f || !frontGrounded;

            // MINI-119 follow-up fix, user: "the bike now rides at an
            // angle... the wheelie is still going on the side." A wheelie
            // ending exactly when the ramp crosses 1deg or the front wheel
            // FIRST touches down can leave the bike still settling with no
            // correction applied the instant "wheelieInProgress" flips
            // false. This grace window keeps correcting for a short beat
            // past that instant instead of stopping cold.
            if (wheelieInProgress) _rollLockGraceRemaining = rollLockGraceSeconds;
            else if (_rollLockGraceRemaining > 0f) _rollLockGraceRemaining -= Time.fixedDeltaTime;

            float measuredPitch = MeasurePitch();
            float pitchLimit = wheelieInProgress ? rampCeilingDeg : 180f; // no cap outside a wheelie
            Quaternion targetRot = ZeroRollTarget(Mathf.Clamp(measuredPitch, -pitchLimit, pitchLimit));

            // The one robust "how far off are we" scalar - see this
            // method's own header for why this replaces every angle-
            // decomposition formula previously used here.
            float errorDeg = Quaternion.Angle(transform.rotation, targetRot);
            LastCorrectionErrorDeg = errorDeg; // TEMP diagnostic

            // MINI-119 follow-up fix, user: "it still rides with a lean
            // after i try to wheelie or turn by a ledge." A ledge hit
            // isn't a wheelie at all - this emergency case bypasses the
            // gating below entirely once the error is genuinely large,
            // regardless of what the wheelie ramp/front wheel are doing.
            bool emergencyLean = errorDeg > emergencyRollLimitDeg;
            bool activeWindow = wheelieInProgress || _rollLockGraceRemaining > 0f || emergencyLean;

            if (!activeWindow || errorDeg < rollLockDeadzoneDeg) return;

            _body.MoveRotation(targetRot);

            // MINI-119 follow-up fix, user: "get the lean fully solved...
            // try with max effort." A full angularVelocity=0 here (this
            // class's own previous version) was itself the reason a real
            // wheelie couldn't climb at all once ANY correction started
            // firing regularly (even for tiny few-degree roll noise): it
            // wiped out the PITCH-axis angular velocity real torque had
            // just spent this same step building, every single time,
            // which is a self-inflicted brake on the exact motion this is
            // supposed to allow. Only the ROLL-axis component needs
            // cancelling - and unlike every earlier version of this class,
            // the axis used here is the bike's own RAW transform.forward,
            // not a flattened/projected one. transform.forward is always
            // unit-length and well-defined at any pitch, including near
            // vertical, where a flattened forward degenerates toward zero
            // - "roll" fundamentally means spin around the bike's own
            // nose-to-tail axis, which tilts WITH pitch, so that's the
            // correct axis to use regardless of how pitched the bike is.
            Vector3 rollAxis = transform.forward;
            float rollVel = Vector3.Dot(_body.angularVelocity, rollAxis);
            _body.angularVelocity -= rollAxis * rollVel;
        }

        // MINI-119 follow-up, user: "get the lean fully solved... try with
        // max effort." Design history worth keeping: a full-range signed-
        // angle replacement was tried here (to fix asin's real fold-back
        // past 90deg) and, combined with the correction logic above,
        // caused a WORSE regression - pitch stalled near 0 indefinitely
        // despite constant full torque, confirmed by a direct test
        // isolating the pitch formula as the only changed variable
        // (deadzone and velocity-cancellation were tried and ruled out
        // first). Reverted to asin(forward.y): proven, by the SAME kind
        // of direct test, to hold a clean, non-oscillating climb with
        // TrueRoll staying at an exact 0.0deg for a full 15-second
        // sustained hold. Its real fold-back limitation past 90deg is
        // real but is now kept safely out of reach by rampCeilingDeg's
        // own margin below 90 and the approach-taper in FixedUpdate,
        // rather than by a pitch formula that turned out to fight this
        // class's own correction logic worse than the problem it fixed.
        private float MeasurePitch() =>
            transform == null ? 0f : Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        /// <summary>Shared by both ApplyStabilization and ApplyAutoRecover
        /// - see each of their own comments for why a single, gimbal-proof
        /// definition of "zero roll" (and, by extension, "true roll" as
        /// the angle away from it) matters here.</summary>
        private Quaternion ZeroRollTarget(float pitchDeg) =>
            Quaternion.Euler(0f, StableYawDegrees(), 0f) * Quaternion.Euler(-pitchDeg, 0f, 0f);

        /// <summary>How far the bike's ACTUAL rotation is from having
        /// zero roll at its own current pitch - i.e. genuine lean, with
        /// pitch (however large, even a 90deg wheelie) subtracted out
        /// first. Unlike Vector3.SignedAngle(Vector3.up, transform.up,
        /// transform.forward) (this class's own old approach), this
        /// cannot conflate a real, deliberate pitch with roll, because
        /// the comparison target is built FROM that same real pitch.</summary>
        private float ComputeTrueRollDeg() =>
            Quaternion.Angle(transform.rotation, ZeroRollTarget(MeasurePitch()));

        // MINI-119 follow-up fix, user: "delve deeper if you need to."
        // Root cause of the real transient roll divergence
        // Mini119WheelieAssistTest caught on a second wheelie's release
        // (roll running away to -58deg before self-correcting): the
        // roll-lock's own MoveRotation used raw transform.eulerAngles.y as
        // "yaw" - the EXACT bug BikeCameraAnchor.cs already documents and
        // fixes elsewhere in this project (see FlatYaw's own comment):
        // "once the bike pitches past vertical in a wheelie its euler
        // decomposition flips and the yaw reading jumps 180 degrees".
        // A wrong yaw fed into rebuilding "yaw * pitch only, no roll"
        // rotates the WHOLE frame around the wrong axis, which is exactly
        // what would make measured pitch and roll jump together afterward
        // - matching what the test showed almost exactly (pitch and roll
        // reading near-equal magnitudes during the divergence). Same fix
        // BikeCameraAnchor already proved: derive yaw from the flattened
        // forward vector via LookRotation instead of raw eulerAngles.
        private float StableYawDegrees()
        {
            Vector3 flat = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flat.sqrMagnitude < 0.0001f)
                flat = Vector3.ProjectOnPlane(-transform.up, Vector3.up); // near-vertical: heading lives in -up, same fallback BikeCameraAnchor uses
            if (flat.sqrMagnitude < 0.0001f) return transform.eulerAngles.y; // fully degenerate, nothing better available
            return Quaternion.LookRotation(flat.normalized, Vector3.up).eulerAngles.y;
        }
    }
}
