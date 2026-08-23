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
        [Tooltip("Degrees per second the wheelie ramp climbs/falls - THE key gradual-vs-instant dial. LOWER = takes longer to reach full lift even while holding the key. (A too-high value here was the real cause of 'still too much torque even turned down' - the ramp was reaching full in well under a second regardless of the torque value.)")]
        public float riseRateDegPerSecond = 18f;
        [Tooltip("Ramp ceiling (deg) - how far a full, sustained hold climbs toward before the torque below is at its max.")]
        public float rampCeilingDeg = 45f;
        [Tooltip("RB_Controller.wheelieTorque value at full ramp. LOWER = gentler overall - but the RISE RATE above is what actually controls how gradual it feels.")]
        public float maxWheelieTorque = 90f;
        [Tooltip("Roll (deg) below which the roll-lock doesn't bother correcting - avoids fighting ordinary cornering lean once you're back to normal riding. Above this, roll is force-corrected to exactly 0 every step while wheelieing.")]
        public float rollLockDeadzoneDeg = 3f;
        [Tooltip("Seconds the roll-lock keeps correcting after a wheelie visibly ends (ramp back down, front wheel back on the ground) - a short grace period so a bike that's still settling from the landing doesn't get left leaned over the instant the wheelie officially ends.")]
        public float rollLockGraceSeconds = 0.5f;
        [Tooltip("MINI-119 follow-up, user: 'it still rides with a lean after i try to wheelie or turn by a ledge.' A ledge/bump can lean the bike even with no wheelie involved at all, which the wheelie-only gating above never catches. Above THIS roll angle, the same straightening correction applies any time, not just during a wheelie - normal cornering lean should stay well under this, so it shouldn't fight intentional turning.")]
        public float emergencyRollLimitDeg = 35f;

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

        private void Awake()
        {
            _rb = GetComponent<RB_Controller>();
            _body = GetComponent<Rigidbody>();
            _input = GetComponent<Input_Manager>();
            _autoLevel = GetComponent<AutoLeveling>();

            // MINI-119 follow-up fix: Mini119WheelieAssistTest traced the
            // REAL instability to a completely different, untouched stock
            // field - RB_Controller's own backFlipTorque. Once a wheelie
            // lifts far enough that BOTH wheels leave the ground,
            // RB_Controller's own !isGrounded branch switches to applying
            // backFlipTorque continuously for as long as the key is held
            // - unbounded, ungated, nothing to do with this class at all
            // - which is what actually dragged roll out to 179deg over a
            // sustained hold. The asset already ships its own fix for
            // exactly this (AutoLeveling.safeWheelies - pulls the bike
            // back down once past a max angle, BEFORE it can go fully
            // airborne into that branch) - it just defaults off. Turning
            // it on here, synced to this ramp's own ceiling, uses the
            // vendor's own existing safety system instead of adding a
            // second one of this class's own.
            if (_autoLevel != null)
            {
                _autoLevel.safeWheelies = true;
                _autoLevel.maxWheelieAngle = rampCeilingDeg;
            }

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
            if (ragdoll != null)
                foreach (var col in ragdoll.GetComponentsInChildren<Collider>(true))
                    col.isTrigger = true;
        }

        private void FixedUpdate()
        {
            if (_rb == null || _input == null || _rb.isCrashed) return;

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

            // Preserves the sign RB_Controller.AddTorque itself expects
            // (it multiplies wheelieTorque by inputs.WheelieInput's own
            // sign again) - this field is a magnitude here, matching how
            // the vendor's own field is documented/used elsewhere.
            _rb.wheelieTorque = rampFraction * maxWheelieTorque;

            ApplyRollLock();
        }

        /// <summary>See this class's own header for the full reasoning.
        /// "wheelie in progress" is judged the same two ways the trike
        /// stabilizer used - the ramp itself, or the front wheel actually
        /// being off the ground right now (covers a real lift the ramp
        /// hasn't caught up to yet) - so this engages before roll has any
        /// chance to start, not after.</summary>
        private void ApplyRollLock()
        {
            if (_body == null) return;

            bool frontGrounded = _rb.wheelColliders != null && _rb.wheelColliders.Length > 1
                && _rb.wheelColliders[1] != null && _rb.wheelColliders[1].isGrounded;
            bool wheelieInProgress = Mathf.Abs(_currentRampDeg) > 1f || !frontGrounded;

            // MINI-119 follow-up fix, user: "the bike now rides at an
            // angle... the wheelie is still going on the side." A wheelie
            // ending exactly when the ramp crosses 1deg or the front wheel
            // FIRST touches down can leave the bike still settling (residual
            // roll velocity, or the front wheel flickering grounded/
            // ungrounded for a step or two right at touchdown) with no
            // correction applied the instant "wheelieInProgress" flips
            // false. This grace window keeps correcting for a short beat
            // past that instant instead of stopping cold.
            if (wheelieInProgress) _rollLockGraceRemaining = rollLockGraceSeconds;
            else if (_rollLockGraceRemaining > 0f) _rollLockGraceRemaining -= Time.fixedDeltaTime;

            float rollNow = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);

            // MINI-119 follow-up fix, user: "it still rides with a lean
            // after i try to wheelie or turn by a ledge." A ledge hit
            // isn't a wheelie at all, so it was never covered by the
            // gating below - this emergency case bypasses that gating
            // entirely once roll is genuinely large, regardless of what
            // the wheelie ramp/front wheel are doing.
            bool emergencyLean = Mathf.Abs(rollNow) > emergencyRollLimitDeg;

            if (!wheelieInProgress && _rollLockGraceRemaining <= 0f && !emergencyLean) return;
            if (Mathf.Abs(rollNow) < rollLockDeadzoneDeg) return;

            float yaw = transform.eulerAngles.y;
            float measuredPitch = MeasurePitch();
            Quaternion noRollRot = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-measuredPitch, 0f, 0f);
            _body.MoveRotation(noRollRot);

            // Cancel ONLY the roll-axis component of angular velocity -
            // pitch (real, torque-driven lift) and yaw (steering) keep
            // whatever velocity they already had, so this doesn't fight
            // either of those, only the lean.
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = transform.forward;
            flatForward.Normalize();
            float rollVel = Vector3.Dot(_body.angularVelocity, flatForward);
            _body.angularVelocity -= flatForward * rollVel;
        }

        private float MeasurePitch() =>
            transform == null ? 0f : Mathf.Asin(Mathf.Clamp(transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
    }
}
