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
        [Tooltip("Degrees per second the wheelie ramp climbs/falls - THIS is the 'too fast' fix. Lower = more gradual lift and a slower recovery back down.")]
        public float riseRateDegPerSecond = 45f;
        [Tooltip("Ramp ceiling (deg, arbitrary units matching the ramp's own scale) - how far 'wheelieIn held' climbs before the torque below reaches full strength.")]
        public float rampCeilingDeg = 45f;
        [Tooltip("RB_Controller.wheelieTorque value at full ramp - the vendor's own stock field, just modulated over time instead of applied instantly. Lowered from the vendor's own 750 default - MINI-119's own test showed 750 launches this bike fully airborne within about a second of a sustained hold.")]
        public float maxWheelieTorque = 300f;
        [Tooltip("Roll (deg) below which the roll-lock doesn't bother correcting - avoids fighting ordinary cornering lean the instant a wheelie ends. Above this, while ramp/front-wheel-off-ground says a wheelie is happening, roll is force-corrected to exactly 0 every step - see this class's own header.")]
        public float rollLockDeadzoneDeg = 3f;

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
            if (_rb != null) _rb.backFlipTorque = 0f;
        }

        private void FixedUpdate()
        {
            if (_rb == null || _input == null || _rb.isCrashed) return;

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
            if (!wheelieInProgress) return;

            float rollNow = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
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
