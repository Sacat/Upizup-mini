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
    /// </summary>
    [RequireComponent(typeof(RB_Controller))]
    public class SuperMotoWheelieAssist : MonoBehaviour
    {
        [Tooltip("Degrees per second the wheelie ramp climbs/falls - THIS is the 'too fast' fix. Lower = more gradual lift and a slower recovery back down.")]
        public float riseRateDegPerSecond = 45f;
        [Tooltip("Ramp ceiling (deg, arbitrary units matching the ramp's own scale) - how far 'wheelieIn held' climbs before the torque below reaches full strength.")]
        public float rampCeilingDeg = 45f;
        [Tooltip("RB_Controller.wheelieTorque value at full ramp - the vendor's own stock field, just modulated over time instead of applied instantly. Lowered from the vendor's own 750 default - MINI-119's own test showed 750 launches this bike fully airborne within about a second of a sustained hold.")]
        public float maxWheelieTorque = 300f;

        private RB_Controller _rb;
        private Input_Manager _input;
        private AutoLeveling _autoLevel;
        private float _currentRampDeg;

        public float CurrentRampDeg => _currentRampDeg;

        private void Awake()
        {
            _rb = GetComponent<RB_Controller>();
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
        }
    }
}
